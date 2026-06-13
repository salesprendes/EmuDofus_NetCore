using System;
using System.Linq;
using Protocolo.Framework.Network;
using Game;
using Game.Action;
using Game.Entity;
using Game.Fight;
using Game.Map;
using Game.Network;
using Game.Interactive.Type;
using Game.Job;
using Game.Manager;

namespace Game.Frame
{
    public sealed class GameActionFrame : AbstractNetworkFrame<GameActionFrame, CharacterEntity, string>
    {
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message.Length < 2)
                return null;

            switch (message[0])
            {
                case 'G':
                    return message[1] switch { 'A' => GameActionStart, 'K' => message[2] switch { 'K' => GameActionFinish, 'E' => GameActionAbort, _ => null, }, _ => null, };

                case 'D':
                    switch (message[1])
                    {
                        case 'C':
                            return DialogCreate;

                        default:
                            break;
                    }
                    break;
            }

            return null;
        }

        private void DialogCreate(CharacterEntity character, string message)
        {
            long npcId = -1;
            if (message.Length < 3 || !long.TryParse(message.AsSpan(2), out npcId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
                {
                    var target = character.Map.GetEntity(npcId);
                    if (target == null || target.Type != EntityTypeEnum.TYPE_NPC)
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    var npc = (NonPlayerCharacterEntity)target;
                    if (npc.InitialQuestion == null)
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    if (!character.CanGameAction(GameActionTypeEnum.NPC_DIALOG))
                    {
                        character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_YOU_ARE_AWAY));
                        return;
                    }

                    character.NpcDialogStart(npc);
                });
        }

        private void GameActionStart(CharacterEntity character, string message)
        {
            if (message.Length < 5)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var actionId = -1;
            if (!int.TryParse(message.AsSpan(2, 3), out actionId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (!Enum.IsDefined(typeof(GameActionTypeEnum), actionId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
            {
                var actionType = (GameActionTypeEnum)actionId;
                if (!character.CanGameAction(actionType))
                {
                    Logger.Debug($"GameActionFrame::Start la entidad no puede iniciar una accion de juego: {character.Name}");
                    character.CachedBuffer = true;
                    character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_YOU_ARE_AWAY));
                    character.Dispatch(WorldMessage.GAME_ACTION_FAILED());
                    character.CachedBuffer = false;
                    return;
                }

                switch (actionType)
                {
                    case GameActionTypeEnum.MAP_MOVEMENT:
                        GameMapMovement(character, message);
                        break;

                    case GameActionTypeEnum.CHALLENGE_REQUEST:
                        GameChallengeRequest(character, message);
                        break;

                    case GameActionTypeEnum.CHALLENGE_ACCEPT:
                        GameChallengeAccept(character, message);
                        break;

                    case GameActionTypeEnum.CHALLENGE_DECLINE:
                        GameChallengeDeny(character, message);
                        break;

                    case GameActionTypeEnum.FIGHT_JOIN:
                        GameFightJoin(character, message);
                        break;

                    case GameActionTypeEnum.FIGHT_SPELL_LAUNCH:
                        GameFightSpellLaunch(character, message);
                        break;

                    case GameActionTypeEnum.FIGHT_WEAPON_USE:
                        GameWeaponUse(character, message);
                        break;

                    case GameActionTypeEnum.TAXCOLLECTOR_AGGRESSION:
                        GameTaxcollectorAggression(character, message);
                        break;

                    case GameActionTypeEnum.PRISM_AGGRESSION:
                        GamePrismAggression(character, message);
                        break;

                    case GameActionTypeEnum.PRISM_USE:
                        GamePrismUse(character, message);
                        break;

                    case GameActionTypeEnum.SKILL_USE:
                        GameSkillUse(character, message);
                        break;

                    case GameActionTypeEnum.FIGHT_AGGRESSION:
                        GameAlignmentAggression(character, message);
                        break;
                }
            });
        }

        private static void GameSkillUse(CharacterEntity character, string message)
        {
            if (message.Length <= 5) { character.Dispatch(WorldMessage.BASIC_NO_OPERATION()); return; }
            var skillData = message.AsSpan(5);
            Span<Range> skillParts = stackalloc Range[3];
            var skillPartCount = skillData.Split(skillParts, ';');
            if (skillPartCount < 2 || !int.TryParse(skillData[skillParts[0]], out var cellId) || !int.TryParse(skillData[skillParts[1]], out var skillId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.Map.AddMessage(() =>
            {
                if (!character.CanGameAction(GameActionTypeEnum.SKILL_USE))
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var interactiveCell = character.Map.GetCell(cellId);
                if (interactiveCell == null)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (!character.CharacterJobs.HasSkill(skillId))
                {

                    if (interactiveCell.InteractiveObject == null || !interactiveCell.InteractiveObject.CanUseWithoutJobSkill(skillId))
                    {
                        Logger.Debug($"GameActionFrame::SkillUse el personaje no tiene la habilidad: {(SkillIdEnum)skillId}");
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }
                }

                if (character.CurrentAction is GameMapMovementAction action && !action.IsFinished)
                {
                    action.SkillCellId = cellId;
                    action.SkillId = skillId;
                    action.SkillMapId = character.MapId;
                    return;
                }

                if (character.Map.IsInInteractiveSkillRange(character, character.CellId, cellId, skillId))
                {
                    character.ClearPendingInteractiveSkill();
                    character.Map.InteractiveExecute(character, cellId, skillId);
                    return;
                }

                character.QueuePendingInteractiveSkill(character.MapId, cellId, skillId);
            });
        }

        private static void GameAlignmentAggression(CharacterEntity character, string message)
        {
            if (character.Map.FightTeam0Cells.Count == 0 || character.Map.FightTeam1Cells.Count == 0)
                return;

            if (message.Length <= 5) { character.Dispatch(WorldMessage.BASIC_NO_OPERATION()); return; }

            if (!long.TryParse(message.AsSpan(5), out long victimId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (victimId == character.Id)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var distantEntity = character.Map.GetEntity(victimId);
            if (distantEntity == null)
            {
                Logger.Debug($"GameActionFrame::AlignmentAggression id de victima desconocido: {character.Name}");
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (distantEntity.Type != EntityTypeEnum.TYPE_CHARACTER)
            {
                Logger.Debug($"GameActionFrame::AlignmentAggression se ha intentado agredir a una entidad no valida: {character.Name}");
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var victim = (CharacterEntity)distantEntity;
            if (!victim.CanGameAction(GameActionTypeEnum.FIGHT_AGGRESSION))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (!victim.CanGameAction(GameActionTypeEnum.FIGHT))
                victim.AbortAction(victim.CurrentAction.Type);


            character.EnableAlignment();
            character.Map.FightManager.StartAggression(character, victim);
        }

        private static void GameTaxcollectorAggression(CharacterEntity character, string message)
        {
            if (character.Map.FightTeam0Cells.Count == 0 || character.Map.FightTeam1Cells.Count == 0)
            {
                character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_SERVER_MESSAGE, "En este mapa no hay celdas de combate configuradas."));
                return;
            }

            if (message.Length <= 5) { character.Dispatch(WorldMessage.BASIC_NO_OPERATION()); return; }

            if (!long.TryParse(message.AsSpan(5), out long taxcollectorId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var distantEntity = character.Map.GetEntity(taxcollectorId);
            if (distantEntity == null)
            {
                Logger.Debug($"GameActionFrame::TaxcollectorAggression id de recaudador desconocido: {character.Name}");
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (distantEntity.Type != EntityTypeEnum.TYPE_TAX_COLLECTOR)
            {
                Logger.Debug($"GameActionFrame::TaxCollectorAggression se ha intentado agredir a una entidad que no es recaudador: {character.Name}");
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var taxCollector = distantEntity as TaxCollectorEntity;
            if (character.GuildMember != null && character.GuildMember.GuildId == taxCollector.Guild.Id)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (!taxCollector.CanGameAction(GameActionTypeEnum.FIGHT))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.Map.FightManager.StartTaxCollectorAggression(character, taxCollector);
        }

        private static void GamePrismAggression(CharacterEntity character, string message)
        {
            if (character.Map.FightTeam0Cells.Count == 0 || character.Map.FightTeam1Cells.Count == 0)
            {
                character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_SERVER_MESSAGE, "En este mapa no hay celdas de combate configuradas."));
                return;
            }

            if (message.Length <= 5) { character.Dispatch(WorldMessage.BASIC_NO_OPERATION()); return; }

            if (!long.TryParse(message.AsSpan(5), out long prismId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var distantEntity = character.Map.GetEntity(prismId);
            if (distantEntity is not ConquestPrismEntity prism)
            {
                Logger.Debug($"GameActionFrame::PrismAggression id de prisma desconocido: {character.Name}");
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (!prism.CanGameAction(GameActionTypeEnum.FIGHT))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var territory = prism.Territory ?? ConquestManager.Instance.GetByCharacterMap(character);
            if (!ConquestManager.Instance.CanAttack(territory, character))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.Map.FightManager.StartConquestFight(character, territory, prism);
        }

        private static void GamePrismUse(CharacterEntity character, string message)
        {
            if (message.Length <= 5)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (!long.TryParse(message.AsSpan(5), out long prismId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (character.Map.GetEntity(prismId) is not ConquestPrismEntity prism || prism.Territory == null)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var territory = prism.Territory;


            if (territory.PrismType != Game.Conquest.ConquestPrismType.SubArea)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }


            if (!character.AlignmentEnabled || character.AlignmentId <= 0
                || territory.AlignmentId != character.AlignmentId)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.PrismSubwayStart(territory);
        }

        private static void GameWeaponUse(CharacterEntity character, string message)
        {
            if (message.Length <= 5)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (!int.TryParse(message.AsSpan(5), out int cellId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.Fight.TryUseWeapon(character, cellId);
        }

        private static void GameFightJoin(CharacterEntity character, string message)
        {
            if (message.Length <= 5)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var fightData = message.AsSpan(5);
            var separatorIndex = fightData.IndexOf(';');
            ReadOnlySpan<char> fightIdData = separatorIndex < 0 ? fightData : fightData.Slice(0, separatorIndex);

            if (!int.TryParse(fightIdData, out int fightId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var fight = character.Map.FightManager.GetFight(fightId);

            if (fight == null)
            {
                Logger.Debug($"GameActionFrame::ChallengeJoin combate desconocido: {character.Name}");
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (separatorIndex < 0)
            {
                fight.TrySpectate(character);
                return;
            }

            if (!long.TryParse(fightData.Slice(separatorIndex + 1), out long leaderId))
            {
                Logger.Debug($"GameActionFrame::ChallengeJoin id de lider desconocido: {character.Name}");
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            fight.TryJoin(character, leaderId);
        }

        private void GameFightSpellLaunch(CharacterEntity character, string message)
        {
            if (message.Length <= 5)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var spellData = message.AsSpan(5);
            Span<Range> spellParts = stackalloc Range[3];
            var spellPartCount = spellData.Split(spellParts, ';');
            if (spellPartCount < 2 || spellData[spellParts[0]].IsEmpty || spellData[spellParts[1]].IsEmpty)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var spellId = -1;
            if (!int.TryParse(spellData[spellParts[0]], out spellId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var cellId = -1;
            if (!int.TryParse(spellData[spellParts[1]], out cellId))
            {
                Logger.Debug($"GameActionFrame::SpellLaunch contenido del paquete invalido: {character.Name}");
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.Fight.TryLaunchSpell(character, spellId, cellId);
        }

        private static void GameChallengeDeny(CharacterEntity character, string message)
        {
            character.AbortAction(GameActionTypeEnum.CHALLENGE_REQUEST);
        }

        private static void GameChallengeAccept(CharacterEntity character, string message)
        {
            character.StopAction(GameActionTypeEnum.CHALLENGE_REQUEST);
        }

        private static void GameChallengeRequest(CharacterEntity character, string message)
        {
            if (character.Map.FightTeam0Cells.Count == 0 || character.Map.FightTeam1Cells.Count == 0)
            {
                character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_SERVER_MESSAGE, "En este mapa no hay celdas de combate configuradas."));
                return;
            }

            if (message.Length <= 5) { character.Dispatch(WorldMessage.BASIC_NO_OPERATION()); return; }

            if (!long.TryParse(message.AsSpan(5), out long distantEntityId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (distantEntityId == character.Id)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var distantEntity = character.Map.GetEntity(distantEntityId);
            if (distantEntity == null)
            {
                Logger.Debug($"GameActionFrame::ChallengeRequest id de entidad objetivo desconocido: {character.Name}");
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (distantEntity.Type != EntityTypeEnum.TYPE_CHARACTER)
            {
                Logger.Debug($"GameActionFrame::ChallengeRequest se ha intentado retar a una entidad que no es jugador: {character.Name}");
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (!distantEntity.CanGameAction(GameActionTypeEnum.CHALLENGE_REQUEST))
            {
                character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_PLAYER_AWAY_NOT_INVITABLE));
                return;
            }

            character.ChallengePlayer((CharacterEntity)distantEntity);
        }

        private static void GameMapMovement(CharacterEntity character, string message)
        {
            if (character.MovementHandler == null)
            {
                Logger.Debug($"GameActionFrame::MapMovement la entidad no esta en ningun mapa: {character.Name}");
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (message.Length <= 5)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var path = message.AsSpan(5);



            if (path.Length > 560 * 3)
            {
                Logger.Debug($"GameActionFrame::MapMovement ruta demasiado larga recibida de: {character.Name}");
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            switch (character.MovementHandler.FieldType)
            {
                case FieldTypeEnum.TYPE_MAP:
                    character.MovementHandler.Move(character, character.CellId, path.ToString());
                    break;
                case FieldTypeEnum.TYPE_FIGHT:
                    var fighter = (AbstractFighter)character;
                    fighter.Fight.Move(character, fighter.Cell.Id, path.ToString());
                    break;
            }
        }

        private void GameActionAbort(CharacterEntity character, string message)
        {
            var abortData = message.AsSpan();
            var separatorIndex = abortData.IndexOf('|');
            if (separatorIndex < 0)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var actionId = -1;
            var actionIdData = abortData.Slice(0, separatorIndex);
            if (actionIdData.Length < 3 || !int.TryParse(actionIdData.Slice(3), out actionId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var actionArgsData = abortData.Slice(separatorIndex + 1);
            var nextSeparatorIndex = actionArgsData.IndexOf('|');
            var actionArgs = (nextSeparatorIndex < 0 ? actionArgsData : actionArgsData.Slice(0, nextSeparatorIndex)).ToString();

            character.AddMessage(() =>
                {
                    var action = character.CurrentAction;
                    if (action == null)
                    {
                        Logger.Debug($"GameActionFrame::GameActionFinish la entidad no tiene accion activa: {character.Name}");
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    if ((int)action.Type != actionId)
                    {
                        Logger.Debug($"GameActionFrame::GameActionAbort id de accion incorrecto: {character.Name}");
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    character.AbortAction(action.Type, actionArgs);
                });
        }

        private void GameActionFinish(CharacterEntity character, string message)
        {
            var actionId = -1;
            if (!int.TryParse(message.AsSpan(3), out actionId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
            {
                var action = character.CurrentAction;
                if (action == null)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if ((int)action.Type != actionId)
                {
                    Logger.Debug($"GameActionFrame::GameActionFinish id de accion incorrecto: {character.Name}");
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (action is GameMapMovementAction moveAction && moveAction.StartedAt > 0)
                {
                    const long MinExpectedMs = 800;
                    long rtt = Math.Min(character.RttMs, 1500);
                    long elapsed = Environment.TickCount64 - moveAction.StartedAt;
                    double[] speeds = character.RidingMount ? Pathfinding.MOUNT_SPEEDS : moveAction.Path.MovementLength > 6 ? Pathfinding.RUN_SPEEDS : Pathfinding.WALK_SPEEDS;
                    long expected = (long)(moveAction.Path.SegmentLengths.Select((len, i) => { int d = i * 2 < moveAction.Path.Directions.Count ? moveAction.Path.Directions[i * 2] : 0; return (Pathfinding.CELL_PIXEL_DIST[d] / speeds[d]) * len; }).Sum() * 0.3);

                    if (expected > MinExpectedMs && elapsed < expected + rtt)
                    {
                        Logger.Warn($"GameActionFrame::GameActionFinish speedhack detectado: {character.Name} ({elapsed}ms < {expected + rtt}ms esperados, rtt={rtt}ms)");
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }
                }

                character.StopAction(action.Type);
            });
        }
    }
}



