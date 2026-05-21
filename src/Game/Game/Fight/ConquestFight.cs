using Game.Action;
using Game.Conquest;
using Game.Entity;
using Game.Fight.Ending;
using Game.Map;
using Game.Manager;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Fight
{
    public sealed class ConquestFight : AbstractFight, IDisposable
    {
        public CharacterEntity Attacker { get; private set; }
        public ConquestTerritory Territory { get; private set; }
        public ConquestPrismEntity Prism { get; private set; }
        public bool CanDefend { get; private set; }

        private List<CharacterEntity> m_defenders;
        private string m_serializedFlag;

        public ConquestFight(MapInstance map, long id, CharacterEntity attacker, ConquestTerritory territory, ConquestPrismEntity prism)
            : base(FightTypeEnum.TYPE_PVMA,
                  map,
                  id,
                  attacker.Id,
                  attacker.AlignmentId,
                  attacker.CellId,
                  prism.Id,
                  territory.AlignmentId,
                  prism.MapCellId,
                  WorldConfig.PVT_START_TIMEOUT,
                  WorldConfig.PVT_TURN_TIME,
                  false, true, new HonorGainBehavior())
        {
            CanDefend = true;
            m_defenders = new List<CharacterEntity>();

            Attacker = attacker;
            Territory = territory;
            Prism = prism;
            Territory.SetFight(this);

            JoinFight(Attacker, Team0);
            JoinFight(Prism, Team1);

            AddTimer(WorldConfig.PVT_TELEPORT_DEFENDERS_TIMEOUT, TeleportDefenders, true);

            ConquestManager.Instance.TerritoryAttacked(territory, map);
        }

        // -1 to exclude the prism's placement cell
        private int MaxDefenders => Math.Max(0, Team1.PlacesCount - 1);

        public IEnumerable<CharacterEntity> AllDefenders =>
            Team1.Fighters.OfType<CharacterEntity>().Concat(m_defenders);

        public void DefenderJoin(CharacterEntity character)
        {
            if (m_defenders.Contains(character))
                return;

            var inFightCount = Team1.Fighters.OfType<CharacterEntity>().Count();
            var availableSlots = MaxDefenders - inFightCount;

            if (m_defenders.Count < availableSlots)
            {
                m_defenders.Add(character);
            }
            else
            {
                // Queue full — bump lowest-level defender if new one has higher level
                var weakest = m_defenders.OrderBy(d => d.Level).FirstOrDefault(d => d.Level < character.Level);
                if (weakest == null)
                {
                    character.AddMessage(() => character.StopAction(GameActionTypeEnum.PRISM_AGGRESSION));
                    return;
                }
                m_defenders.Remove(weakest);
                weakest.AddMessage(() => weakest.StopAction(GameActionTypeEnum.PRISM_AGGRESSION));
                m_defenders.Add(character);
            }

            Map.Dispatch(WorldMessage.CONQUEST_PRISM_FIGHT_DEFENDERS(this, AllDefenders.ToArray()));
        }

        public void DefenderLeave(CharacterEntity character)
        {
            if (m_defenders.Remove(character))
                Map.Dispatch(WorldMessage.CONQUEST_PRISM_FIGHT_DEFENDER_LEAVE(this, character));
        }

        private void TeleportDefenders()
        {
            CanDefend = false;

            foreach (var defender in m_defenders.OrderByDescending(d => d.Level))
            {
                var character = defender;
                character.AddMessage(() =>
                {
                    character.StopAction(GameActionTypeEnum.PRISM_AGGRESSION);
                    JoinFight(character, Team1);
                });
            }

            m_defenders.Clear();
        }

        public override void OnCharacterJoin(CharacterEntity character, FightTeam team)
        {
            character.EnableAlignment();
        }

        public override bool CanJoin(CharacterEntity character)
        {
            // Defenders (same alignment as prism) can join
            if (character.AlignmentId == Territory.AlignmentId)
                return State == FightStateEnum.STATE_PLACEMENT;
            // Attackers from same alignment as attacker can join
            if (character.AlignmentId == Attacker.AlignmentId)
                return State == FightStateEnum.STATE_PLACEMENT;
            return false;
        }

        public override FightActionResultEnum FightQuit(CharacterEntity character, bool kick = false)
        {
            if (LoopState == FightLoopStateEnum.STATE_WAIT_END || LoopState == FightLoopStateEnum.STATE_ENDED)
                return FightActionResultEnum.RESULT_NOTHING;

            switch (State)
            {
                case FightStateEnum.STATE_PLACEMENT:
                    if (TryKillFighter(character, character, true, true) == FightActionResultEnum.RESULT_END)
                        return FightActionResultEnum.RESULT_END;

                    if (kick)
                    {
                        character.Fight.Dispatch(WorldMessage.FIGHT_FLAG_UPDATE(OperatorEnum.OPERATOR_REMOVE, character.Team.LeaderId, character));
                        character.Fight.Dispatch(WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_REMOVE, character));
                        character.EndFight(true);
                        character.Dispatch(WorldMessage.FIGHT_LEAVE());
                    }
                    return FightActionResultEnum.RESULT_NOTHING;

                case FightStateEnum.STATE_FIGHTING:
                    if (character.IsSpectating)
                    {
                        character.EndFight(true);
                        character.Dispatch(WorldMessage.FIGHT_LEAVE());
                        return FightActionResultEnum.RESULT_NOTHING;
                    }

                    if (TryKillFighter(character, character, true, true) != FightActionResultEnum.RESULT_END)
                    {
                        character.EndFight();
                        character.Dispatch(WorldMessage.FIGHT_LEAVE());
                        return FightActionResultEnum.RESULT_DEATH;
                    }
                    return FightActionResultEnum.RESULT_END;
            }

            return FightActionResultEnum.RESULT_NOTHING;
        }

        protected override void FightEnd()
        {
            if (WinnerTeam == Team0)
            {
                if (Territory.PrismType != ConquestPrismType.SubArea)
                {
                    // Village prism defeated: city is captured by the attacking alignment
                    var capturer = WinnerTeam.Fighters.OfType<CharacterEntity>().FirstOrDefault() ?? Attacker;
                    Territory.SetPrismPosition(Map.Id, Prism.MapCellId);
                    Territory.Capture(capturer);
                    ConquestManager.Instance.TerritoryCaptured(Territory, Map);
                }
                else
                {
                    ConquestManager.Instance.TerritoryDefeated(Territory, Map);
                }
            }
            else
            {
                Territory.Restore();
                Territory.SetFight(null);
                ConquestManager.Instance.TerritoryDefended(Territory, Map);
            }

            base.FightEnd();
        }

        public override void SerializeAs_FightList(StringBuilder message)
        {
            message.Append(Id.ToString()).Append(';');
            message.Append(UpdateTime).Append(';');
            message.Append("0,").Append(Team0.AlignmentId).Append(',');
            message.Append(Team0.AliveFighters.Count()).Append(';');
            message.Append("1,").Append(Team1.AlignmentId).Append(',');
            message.Append(Team1.AliveFighters.Count()).Append(';');
            message.Append('|');
        }

        public override void SerializeAs_FightFlag(StringBuilder message)
        {
            if (m_serializedFlag == null)
            {
                var sb = new StringBuilder();
                sb.Append(Id).Append(';');
                sb.Append((int)Type).Append('|');
                sb.Append(Team0.LeaderId).Append(';');
                sb.Append(Team0.FlagCellId).Append(';');
                sb.Append('0').Append(';');
                sb.Append(Team0.AlignmentId).Append('|');
                sb.Append(Team1.LeaderId).Append(';');
                sb.Append(Team1.FlagCellId).Append(';');
                sb.Append('1').Append(';');
                sb.Append(Team1.AlignmentId);
                m_serializedFlag = sb.ToString();
            }
            message.Append(m_serializedFlag);
        }

        public override void Dispose()
        {
            CanDefend = false;
            m_defenders?.Clear();
            m_defenders = null;

            if (Territory != null && LoopState != FightLoopStateEnum.STATE_ENDED)
                Territory.SetFight(null);
            Attacker = null;
            Prism = null;
            Territory = null;
            m_serializedFlag = null;
            base.Dispose();
        }
    }
}
