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
    /// <summary>
    /// 
    /// </summary>
    public sealed class GameActionFrame : AbstractNetworkFrame<GameActionFrame, CharacterEntity, string>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message.Length < 2)
                return null;

            switch (message[0])
            {
                case 'G':
                    return message[1] switch
                    {
                        'A' => GameActionStart,
                        'K' => message[2] switch
                        {
                            'K' => GameActionFinish,
                            'E' => GameActionAbort,
                            _ => null,
                        },
                        _ => null,
                    };

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
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
                    if(target == null || target.Type != EntityTypeEnum.TYPE_NPC)
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    var npc = (NonPlayerCharacterEntity)target;
                    if(npc.InitialQuestion == null)
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    if(!character.CanGameAction(GameActionTypeEnum.NPC_DIALOG))
                    {
                        character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_YOU_ARE_AWAY));
                        return;
                    }

                    character.NpcDialogStart(npc);
                });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
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

            if(!Enum.IsDefined(typeof(GameActionTypeEnum), actionId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
            {
                var actionType = (GameActionTypeEnum)actionId;
                if (!character.CanGameAction(actionType))
                {
                    Logger.Debug("GameActionFrame::Start la entidad no puede iniciar una accion de juego: " + character.Name);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private static void GameSkillUse(CharacterEntity character, string message)
        {
            if (message.Length <= 5) { character.Dispatch(WorldMessage.BASIC_NO_OPERATION()); return; }
            var skillData = message.Substring(5).Split(';');
            if (skillData.Length < 2 || !int.TryParse(skillData[0], out var cellId) || !int.TryParse(skillData[1], out var skillId))
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
                        // Some interactives provide their own skill without a job entry.
                        if (interactiveCell.InteractiveObject == null || !interactiveCell.InteractiveObject.CanUseWithoutJobSkill(skillId))
                        {
                            Logger.Debug("GameActionFrame::SkillUse el personaje no tiene la habilidad: " + (SkillIdEnum)skillId);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private static void GameAlignmentAggression(CharacterEntity character, string message)
        {
            if (character.Map.FightTeam0Cells.Count == 0 || character.Map.FightTeam1Cells.Count == 0)
                return;

            if (message.Length <= 5) { character.Dispatch(WorldMessage.BASIC_NO_OPERATION()); return; }

            long victimId = -1;
            if (!long.TryParse(message.Substring(5), out victimId))
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
                Logger.Debug("GameActionFrame::AlignmentAggression id de victima desconocido: " + character.Name);
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (distantEntity.Type != EntityTypeEnum.TYPE_CHARACTER)
            {
                Logger.Debug("GameActionFrame::AlignmentAggression se ha intentado agredir a una entidad no valida: " + character.Name);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void GameTaxcollectorAggression(CharacterEntity character, string message)
        {
            if (character.Map.FightTeam0Cells.Count == 0 || character.Map.FightTeam1Cells.Count == 0)
            {
                character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_SERVER_MESSAGE, "En este mapa no hay celdas de combate configuradas."));
                return;
            }

            if (message.Length <= 5) { character.Dispatch(WorldMessage.BASIC_NO_OPERATION()); return; }

            long taxcollectorId = -1;
            if (!long.TryParse(message.Substring(5), out taxcollectorId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var distantEntity = character.Map.GetEntity(taxcollectorId);
            if (distantEntity == null)
            {
                Logger.Debug("GameActionFrame::TaxcollectorAggression id de recaudador desconocido: " + character.Name);
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }
            
            if (distantEntity.Type != EntityTypeEnum.TYPE_TAX_COLLECTOR)
            {
                Logger.Debug("GameActionFrame::TaxCollectorAggression se ha intentado agredir a una entidad que no es recaudador: " + character.Name);
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var taxCollector = distantEntity as TaxCollectorEntity;
            if(character.GuildMember != null && character.GuildMember.GuildId == taxCollector.Guild.Id)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if(!taxCollector.CanGameAction(GameActionTypeEnum.FIGHT))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.Map.FightManager.StartTaxCollectorAggression(character, taxCollector);
        }

        private void GamePrismAggression(CharacterEntity character, string message)
        {
            if (character.Map.FightTeam0Cells.Count == 0 || character.Map.FightTeam1Cells.Count == 0)
            {
                character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_SERVER_MESSAGE, "En este mapa no hay celdas de combate configuradas."));
                return;
            }

            if (message.Length <= 5) { character.Dispatch(WorldMessage.BASIC_NO_OPERATION()); return; }

            long prismId = -1;
            if (!long.TryParse(message.Substring(5), out prismId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var distantEntity = character.Map.GetEntity(prismId);
            var prism = distantEntity as ConquestPrismEntity;
            if (prism == null)
            {
                Logger.Debug("GameActionFrame::PrismAggression id de prisma desconocido: " + character.Name);
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

        private void GamePrismUse(CharacterEntity character, string message)
        {
            if (message.Length <= 5) { character.Dispatch(WorldMessage.BASIC_NO_OPERATION()); return; }
            long prismId = -1;
            if (!long.TryParse(message.Substring(5), out prismId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var prism = character.Map.GetEntity(prismId) as ConquestPrismEntity;
            if (prism == null || prism.Territory == null)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var territory = prism.Territory;

            // Solo prismas de tipo SubArea pueden usarse como subway
            if (territory.PrismType != Game.Conquest.ConquestPrismType.SubArea)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            // El personaje debe estar alineado y tener el mismo alineamiento que el prisma
            if (!character.AlignmentEnabled || character.AlignmentId <= 0
                || territory.AlignmentId != character.AlignmentId)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.PrismSubwayStart(territory);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void GameWeaponUse(CharacterEntity character, string message)
        {
            if (message.Length <= 5) { character.Dispatch(WorldMessage.BASIC_NO_OPERATION()); return; }
            var cellId = -1;
            if(!int.TryParse(message.Substring(5), out cellId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.Fight.TryUseWeapon(character, cellId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void GameFightJoin(CharacterEntity character, string message)
        {
            if (message.Length <= 5) { character.Dispatch(WorldMessage.BASIC_NO_OPERATION()); return; }
            var fightData = message.Substring(5).Split(';');
            int fightId = -1;
            if (!int.TryParse(fightData[0], out fightId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var fight = character.Map.FightManager.GetFight(fightId);

            if(fight == null)
            {
                Logger.Debug("GameActionFrame::ChallengeJoin combate desconocido: " + character.Name);
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if(fightData.Length == 1)
            {
                fight.TrySpectate(character);
                return;
            }
            
            long leaderId = -1;
            if(!long.TryParse(fightData[1], out leaderId))
            {                
                Logger.Debug("GameActionFrame::ChallengeJoin id de lider desconocido: " + character.Name);
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            fight.TryJoin(character, leaderId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void GameFightSpellLaunch(CharacterEntity character, string message)
        {
            if (message.Length <= 5 || !message.Contains(';'))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var spellData = message.Substring(5).Split(';');
            if(spellData.Length < 2)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var spellId = -1;
            if(!int.TryParse(spellData[0], out spellId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var cellId = -1;
            if(!int.TryParse(spellData[1], out cellId))
            {
                Logger.Debug("GameActionFrame::SpellLaunch contenido del paquete invalido: " + character.Name);
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.Fight.TryLaunchSpell(character, spellId, cellId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void GameChallengeDeny(CharacterEntity character, string message)
        {
            character.AbortAction(GameActionTypeEnum.CHALLENGE_REQUEST);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void GameChallengeAccept(CharacterEntity character, string message)
        {
            character.StopAction(GameActionTypeEnum.CHALLENGE_REQUEST);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void GameChallengeRequest(CharacterEntity character, string message)
        {
            if (character.Map.FightTeam0Cells.Count == 0 || character.Map.FightTeam1Cells.Count == 0)
            {
                character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_SERVER_MESSAGE, "En este mapa no hay celdas de combate configuradas."));
                return;
            }

            if (message.Length <= 5) { character.Dispatch(WorldMessage.BASIC_NO_OPERATION()); return; }

            long distantEntityId = -1;
            if(!long.TryParse(message.Substring(5), out distantEntityId))
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
            if(distantEntity == null)
            {
                Logger.Debug("GameActionFrame::ChallengeRequest id de entidad objetivo desconocido: " + character.Name);
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if(distantEntity.Type != EntityTypeEnum.TYPE_CHARACTER)
            {
                Logger.Debug("GameActionFrame::ChallengeRequest se ha intentado retar a una entidad que no es jugador: " + character.Name);
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }
            
            if(!distantEntity.CanGameAction(GameActionTypeEnum.CHALLENGE_REQUEST))
            {
                character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_PLAYER_AWAY_NOT_INVITABLE));
                return;
            }

            character.ChallengePlayer((CharacterEntity)distantEntity);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void GameMapMovement(CharacterEntity character, string message)
        {
            if(character.MovementHandler == null)
            {
                Logger.Debug("GameActionFrame::MapMovement la entidad no esta en ningun mapa: " + character.Name);
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (message.Length <= 5)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var path = message.Substring(5);

            // Limit path length to prevent processing of artificially long paths.
            // A Dofus 1.29 map has at most 560 cells (14x40); each path step is 3 chars.
            if (path.Length > 560 * 3)
            {
                Logger.Debug("GameActionFrame::MapMovement ruta demasiado larga recibida de: " + character.Name);
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            switch(character.MovementHandler.FieldType)
            {
                case FieldTypeEnum.TYPE_MAP:
                    character.MovementHandler.Move(character, character.CellId, path);
                    break;
                case FieldTypeEnum.TYPE_FIGHT:
                    var fighter = (AbstractFighter)character;
                    fighter.Fight.Move(character, fighter.Cell.Id, path);
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void GameActionAbort(CharacterEntity character, string message)
        {
            if(!message.Contains('|'))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var abortData = message.Split('|');
            if (abortData.Length < 2)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var actionId = -1;
            if (abortData[0].Length < 3 || !int.TryParse(abortData[0].Substring(3), out actionId))
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var actionArgs = abortData[1];

            character.AddMessage(() =>
                {
                    var action = character.CurrentAction;
                    if (action == null)
                    {
                        Logger.Debug("GameActionFrame::GameActionFinish la entidad no tiene accion activa: " + character.Name);
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    if ((int)action.Type != actionId)
                    {
                        Logger.Debug("GameActionFrame::GameActionAbort id de accion incorrecto: " + character.Name);
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    character.AbortAction(action.Type, actionArgs);
                });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="message"></param>
        private void GameActionFinish(CharacterEntity character, string message)
        {
            var actionId = -1;
            if (!int.TryParse(message.Substring(3), out actionId))
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
                    Logger.Debug("GameActionFrame::GameActionFinish id de accion incorrecto: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (action is GameMapMovementAction moveAction && moveAction.StartedAt > 0)
                {
                    const long MinExpectedMs = 800;
                    long rtt      = Math.Min(character.RttMs, 1500);
                    long elapsed  = Environment.TickCount64 - moveAction.StartedAt;
                    long expected = (long)(moveAction.Path.MovementTime * 0.6);

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



