using Game.Map;
using Game.Spell;
using log4net;
using System;
using System.Collections.Generic;

namespace Game.Fight.AI.Action
{
    public enum AIActionResult
    {
        SUCCESS,
        FAILURE,
        RUNNING
    }

    public enum AIActionState
    {
        INITIALIZE,
        EXECUTE,
        FINISH,
    }

    public abstract class AIAction
    {
        protected static ILog Logger = LogManager.GetLogger(typeof(AIAction));

        public bool Timedout => m_timeout <= Fighter.Fight.UpdateTime;

        private long m_timeout;
        public long Timeout
        {
            set
            {
                m_timeout = Fighter.Fight.UpdateTime + value;
            }
        }

        public MapInstance Map
        {
            get;
            private set;
        }

        public AbstractFight Fight
        {
            get;
            private set;
        }

        public AIFighter Fighter
        {
            get;
            private set;
        }

        public AIActionState State
        {
            get;
            protected set;
        }

        public AIAction NextAction
        {
            get;
            private set;
        }

        protected AIAction(AIFighter fighter)
        {
            Fighter = fighter;
            Fight = Fighter.Fight;
            Map = Fight.Map;
            State = AIActionState.INITIALIZE;
        }

        protected int GetActionThinkTime()
        {
            return WorldConfig.FIGHT_AI_THINK_DELAY;
        }

        protected int GetMovementActionTime(double movementTime)
        {
            return Math.Max(1, (int)Math.Ceiling(movementTime) + WorldConfig.FIGHT_AI_MOVE_DELAY);
        }

        protected int GetSpellActionTime(SpellLevel spellLevel)
        {
            var actionTime = WorldConfig.FIGHT_AI_SPELL_LAUNCH_TIME;
            if (spellLevel == null)
                return actionTime;

            var effectCount = 0;
            var hasSpecialEffect = false;

            UpdateSpellTiming(spellLevel.Effects, ref effectCount, ref hasSpecialEffect);
            UpdateSpellTiming(spellLevel.CriticalEffects, ref effectCount, ref hasSpecialEffect);

            if (effectCount > 1)
                actionTime += (effectCount - 1) * WorldConfig.FIGHT_AI_SPELL_EFFECT_DELAY;

            if (hasSpecialEffect)
                actionTime += WorldConfig.FIGHT_AI_SPELL_SPECIAL_DELAY;

            return Math.Max(1, actionTime);
        }

        private static void UpdateSpellTiming(ICollection<SpellEffect> effects, ref int effectCount, ref bool hasSpecialEffect)
        {
            if (effects == null || effects.Count == 0)
                return;

            effectCount = Math.Max(effectCount, effects.Count);

            foreach (var effect in effects)
            {
                switch (effect.TypeEnum)
                {
                    case EffectEnum.Teleport:
                    case EffectEnum.Invocation:
                    case EffectEnum.InvocDouble:
                    case EffectEnum.InvocationStatic:
                    case EffectEnum.UseTrap:
                    case EffectEnum.UseGlyph:
                        hasSpecialEffect = true;
                        return;
                }
            }
        }

        public virtual AIActionResult Initialize()
        {
            return AIActionResult.FAILURE;
        }

        public virtual AIActionResult Execute()
        {
            return AIActionResult.FAILURE;
        }

        public virtual AIActionResult Finish()
        {
            return AIActionResult.FAILURE;
        }

        public virtual void Update()
        {
            switch (State)
            {
                case AIActionState.INITIALIZE:
                    State = Initialize() != AIActionResult.RUNNING ? AIActionState.FINISH : AIActionState.EXECUTE;
                    break;

                case AIActionState.EXECUTE:
                    State = Execute() != AIActionResult.RUNNING ? AIActionState.FINISH : State;
                    break;

                case AIActionState.FINISH:
                    Finish();
                    break;
            }
        }

        public AIAction LinkWith(AIAction action)
        {
            NextAction = action;
            return action;
        }
    }
}


