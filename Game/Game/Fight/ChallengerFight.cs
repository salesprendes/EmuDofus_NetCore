using Game.Entity;
using Game.Fight.Challenge;
using Game.Map;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Fight.Ending;

namespace Game.Fight
{
    public sealed class ChallengerFight : AbstractFight, IDisposable
    {
        public CharacterEntity Attacker
        {
            get;
            private set;
        }

        public CharacterEntity Defender
        {
            get;
            private set;
        }

        private StringBuilder m_serializedFlag;

        public ChallengerFight(MapInstance map, long id, CharacterEntity attacker, CharacterEntity defender)
    : base(FightTypeEnum.TYPE_CHALLENGE,
          map,
          id,
          attacker.Id,
          0,
          attacker.CellId,
          defender.Id,
          0,
          defender.CellId,
          60000,
          30000,
          true,
          false,
          new RegenerateLosersBehavior(),
          new RegenerateWinnersBehavior())
        {
            Attacker = attacker;
            Defender = defender;

            JoinFight(Attacker, Team0);
            JoinFight(Defender, Team1);
        }

        public override bool CanJoin(CharacterEntity character)
        {
            return true;
        }

        public override FightActionResultEnum FightQuit(CharacterEntity character, bool kick = false)
        {
            if (LoopState == FightLoopStateEnum.STATE_WAIT_END || LoopState == FightLoopStateEnum.STATE_ENDED)
                return FightActionResultEnum.RESULT_NOTHING;

            switch (State)
            {
                case FightStateEnum.STATE_PLACEMENT:
                    if (character.IsLeader)
                    {
                        foreach (var teamFighter in character.Team.Fighters)
                        {
                            if (base.TryKillFighter(teamFighter, teamFighter, true, true) == FightActionResultEnum.RESULT_END)
                            {
                                return FightActionResultEnum.RESULT_END;
                            }
                        }

                        return FightActionResultEnum.RESULT_END;
                    }

                    character.Fight.Dispatch(WorldMessage.FIGHT_FLAG_UPDATE(OperatorEnum.OPERATOR_REMOVE, character.Team.LeaderId, character));
                    character.Fight.Dispatch(WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_REMOVE, character));
                    character.EndFight(true);
                    character.Dispatch(WorldMessage.FIGHT_LEAVE());

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

        public override void SerializeAs_FightList(StringBuilder message)
        {
            message.Append(Id.ToString()).Append(';');
            message.Append(UpdateTime).Append(';');
            message.Append("0,-1,");
            message.Append(Team0.AliveFighters.Count()).Append(';');
            message.Append("0,-1,");
            message.Append(Team1.AliveFighters.Count()).Append(';');
            message.Append('|');
        }

        public override void SerializeAs_FightFlag(StringBuilder message)
        {
            if (m_serializedFlag == null)
            {
                m_serializedFlag = new StringBuilder();
                m_serializedFlag.Append(Id).Append(';');
                m_serializedFlag.Append((int)Type).Append('|');
                m_serializedFlag.Append(Team0.LeaderId).Append(';');
                m_serializedFlag.Append(Team0.FlagCellId).Append(';');
                m_serializedFlag.Append('2').Append(';');
                m_serializedFlag.Append("-1").Append('|');
                m_serializedFlag.Append(Team1.LeaderId).Append(';');
                m_serializedFlag.Append(Team1.FlagCellId).Append(';');
                m_serializedFlag.Append('2').Append(';');
                m_serializedFlag.Append("-1");
            }

            message.Append(m_serializedFlag.ToString());
        }

        public override void Dispose()
        {
            Attacker = null;
            Defender = null;

            m_serializedFlag.Clear();
            m_serializedFlag = null;

            base.Dispose();
        }
    }
}


