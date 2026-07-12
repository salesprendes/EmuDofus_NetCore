using Game.Entity;
using Game.Fight.Challenge;
using Game.Fight.Ending;
using Game.Map;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Fight
{
    public sealed class MonsterFight : AbstractFight, IDisposable
    {
        public CharacterEntity Character
        {
            get;
            private set;
        }

        public MonsterGroupEntity MonsterGroup
        {
            get;
            private set;
        }

        internal IEnumerable<MonsterEntity> KilledMonsters => Team1.Fighters.OfType<MonsterEntity>().Where(fighter => fighter.Invocator == null && fighter.IsFighterDead);

        private string m_serializedFlag;

        public MonsterFight(MapInstance map, long id, CharacterEntity character, MonsterGroupEntity monsterGroup) : base(FightTypeEnum.TYPE_PVM, map, id, character.Id, -1, character.CellId, monsterGroup.Id, -1, monsterGroup.CellId, 60000, 30000, false, false, new LootMonsterBehavior())
        {
            Character = character;
            MonsterGroup = monsterGroup;

            JoinFight(Character, Team0);
            foreach (var monster in monsterGroup.Monsters)
                JoinFight(monster, Team1);

            foreach (var challenge in ChallengeManager.Instance.Generate(1))
                Team0.AddChallenge(challenge);
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
                    if (kick)
                    {
                        character.Fight.Dispatch(WorldMessage.FIGHT_FLAG_UPDATE(OperatorEnum.OPERATOR_REMOVE, character.Team.LeaderId, character));
                        character.Fight.Dispatch(WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_REMOVE, character));
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

                case FightStateEnum.STATE_FIGHTING:
                    if (character.IsSpectating)
                    {
                        character.EndFight(kick);
                        character.Dispatch(WorldMessage.FIGHT_LEAVE());

                        return FightActionResultEnum.RESULT_NOTHING;
                    }

                    if (TryKillFighter(character, character, true, true) != FightActionResultEnum.RESULT_END)
                    {
                        Result.AddResult(character);
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
            MonsterGroupEntity group = MonsterGroup;

            // El panel de fin de combate muestra las estrellas (bonus) del grupo de monstruos.
            if (group != null && group.AgeBonus > 0)
            {
                Result.StarsBonus = group.AgeBonus;
            }

            if (WinnerTeam == Team0)
            {
                Map.DestroyEntity(group);
                Map.ScheduleRepop(group.CellId);
            }
            else
            {
                // El jugador ha perdido el combate PvM: los retos en curso se dan por fallidos.
                Team0.FailChallenges();

                Map.AddMessage(() => { Map.DestroyEntity(group); Map.SpawnEntity(group); });
            }
            base.FightEnd();
        }

        public override void SerializeAs_FightList(StringBuilder message)
        {
            message.Append(Id.ToString()).Append(';');
            message.Append(UpdateTime).Append(';');
            message.Append("0,");
            message.Append(Team0.AlignmentId).Append(',');
            message.Append(Team0.AliveFighters.Count()).Append(';');
            message.Append("1,");
            message.Append(Team0.AlignmentId).Append(',');
            message.Append(Team1.AliveFighters.Count()).Append(';');
            message.Append('|');
        }

        public override void SerializeAs_FightFlag(StringBuilder message)
        {
            if (m_serializedFlag == null)
            {
                var m_serialized = new StringBuilder();
                m_serialized.Append(Id).Append(';');
                m_serialized.Append((int)Type).Append('|');
                m_serialized.Append(Team0.LeaderId).Append(';');
                m_serialized.Append(Team0.FlagCellId).Append(';');
                m_serialized.Append('0').Append(';');
                m_serialized.Append(Team0.AlignmentId).Append('|');
                m_serialized.Append(Team1.LeaderId).Append(';');
                m_serialized.Append(Team1.FlagCellId).Append(';');
                m_serialized.Append("1").Append(';');
                m_serialized.Append(Team1.AlignmentId);
                m_serializedFlag = m_serialized.ToString();
            }

            message.Append(m_serializedFlag);
        }

        public override void Dispose()
        {
            Character = null;
            MonsterGroup = null;
            m_serializedFlag = null;
            base.Dispose();
        }
    }
}


