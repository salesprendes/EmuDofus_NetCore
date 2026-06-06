using Game.Action;
using Game.Fight.Effect;
using Game.Network;
using Game.Spell;
using System;
using System.Collections.Generic;

namespace Game.Fight
{
    /// <summary>
    ///
    /// </summary>
    public enum FighterStateEnum
    {
        STATE_DRUNK = 1,
        STATE_CARRIER = 3,
        STATE_ROOTED = 6,
        STATE_GRAVITY = 7,
        STATE_CARRIED = 8,
        STATE_WEAKENED = 42,
        STATE_ALTRUISM = 50,
        STATE_STEALTH = 600,
        STATE_KRALAMAR_PRIMARY_INK = 31,
        STATE_KRALAMAR_SECONDARY_INK = 32,
        STATE_KRALAMAR_TERTIARY_INK = 33,
        STATE_KRALAMAR_QUATERNARY_INK = 34,
        STATE_KRALAMAR_DESIRE_KILL = 35,
        STATE_KRALAMAR_DESIRE_PARALYZE = 36,
        STATE_KRALAMAR_DESIRE_CURSE = 37,
        STATE_KRALAMAR_DESIRE_POISON = 38,
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class FighterStateManager : IDisposable
    {
        private static readonly HashSet<int> BossMechanicStateIds = new HashSet<int>();

        public static void RegisterCodeManagedState(int stateId) => BossMechanicStateIds.Add(stateId);

        private AbstractFighter m_fighter;
        private Dictionary<FighterStateEnum, AbstractSpellBuff> m_states = new Dictionary<FighterStateEnum, AbstractSpellBuff>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fighter"></param>
        public FighterStateManager(AbstractFighter fighter)
        {
            m_fighter = fighter;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool CanState(FighterStateEnum state)
        {
            switch (state)
            {
                case FighterStateEnum.STATE_CARRIED:
                case FighterStateEnum.STATE_CARRIER:
                    return !HasState(FighterStateEnum.STATE_GRAVITY);
            }

            return !HasState(state);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool HasState(FighterStateEnum state)
        {
            return m_states.ContainsKey(state);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buff"></param>
        public void AddState(AbstractSpellBuff buff)
        {
            if (BossMechanicStateIds.Contains(buff.CastInfos.Value3))
                return;

            buff.CastInfos.SubEffect = EffectEnum.AddState;

            if (buff.Caster.Fight.State == FightStateEnum.STATE_FIGHTING)
            {
                switch (buff.CastInfos.EffectType)
                {
                    case EffectEnum.Stealth:

                        if (HasState(FighterStateEnum.STATE_STEALTH))
                            return;

                        m_fighter.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.Stealth, m_fighter.Id, m_fighter.Id + "," + buff.Duration));

                        m_states.Add(FighterStateEnum.STATE_STEALTH, buff);

                        return;

                    default:

                        m_fighter.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.AddState, m_fighter.Id, m_fighter.Id + "," + buff.CastInfos.Value3 + ",1"));

                        break;
                }

                if (HasState((FighterStateEnum)buff.CastInfos.Value3))
                    return;

                m_states.Add((FighterStateEnum)buff.CastInfos.Value3, buff);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buff"></param>
        public void RemoveState(AbstractSpellBuff buff)
        {
            if (BossMechanicStateIds.Contains(buff.CastInfos.Value3))
                return;

            if (buff.Caster.Fight.State == FightStateEnum.STATE_FIGHTING)
            {
                switch (buff.CastInfos.EffectType)
                {
                    case EffectEnum.Stealth:
                        m_fighter.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.Stealth, m_fighter.Id, m_fighter.Id.ToString()));
                        m_fighter.Fight.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.MAP_TELEPORT, m_fighter.Id, m_fighter.Id + "," + m_fighter.Cell.Id));

                        m_states.Remove(FighterStateEnum.STATE_STEALTH);
                    return;

                    default:
                        m_fighter.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.AddState, m_fighter.Id, m_fighter.Id + "," + buff.CastInfos.Value3 + ",0"));
                    break;
                }
            }

            m_states.Remove((FighterStateEnum)buff.CastInfos.Value3);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public AbstractSpellBuff FindState(FighterStateEnum state)
        {
            if (HasState(state))
                return m_states[state];
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <summary>
        /// Applies a state directly without requiring a spell buff — used for boss mechanics
        /// that need to set game states (like Kralamar's "Ganas") via code.
        /// Also dispatches the state-change network message to clients.
        /// </summary>
        public void ForceAddState(FighterStateEnum state)
        {
            if (HasState(state))
                return;

            m_states[state] = null;

            if (m_fighter?.Fight?.State == FightStateEnum.STATE_FIGHTING)
                m_fighter.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.AddState, m_fighter.Id, m_fighter.Id + "," + (int)state + ",1"));
        }

        /// <summary>
        /// Removes a state that was applied via <see cref="ForceAddState"/>.
        /// </summary>
        public void ForceRemoveState(FighterStateEnum state)
        {
            if (!HasState(state))
                return;

            m_states.Remove(state);

            if (m_fighter?.Fight?.State == FightStateEnum.STATE_FIGHTING)
                m_fighter.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.AddState, m_fighter.Id, m_fighter.Id + "," + (int)state + ",0"));
        }

        public void Clear()
        {
            foreach (var state in m_states.Values)
                state?.RemoveEffect();

            m_states.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            m_states.Clear();
            m_states = null;
            m_fighter = null;
        }
    }
}


