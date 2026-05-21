using Game.Fight.AI.Action;
using Game.Fight.AI.Action.Type;
using Game.Fight.Effect;
using Game.Spell;
using System.Linq;

namespace Game.Fight.AI.Brain
{
    public sealed class DefaultAIBrain : AIBrain
    {
        private const double HEAL_HP_THRESHOLD = 0.40;
        private const double CRITICAL_HP_THRESHOLD = 0.20;

        public DefaultAIBrain(AIFighter fighter)
            : base(fighter)
        {
        }

        public override void OnTurnStart()
        {
            var startDelay = new DelayAction(Fighter, WorldConfig.FIGHT_AI_START_DELAY);
            CurrentAction = startDelay;

            double hpRatio = Fighter.MaxLife > 0 ? (double)Fighter.Life / Fighter.MaxLife : 1.0;
            bool isCriticalHp = hpRatio < CRITICAL_HP_THRESHOLD;
            bool isLowHp = hpRatio < HEAL_HP_THRESHOLD;

            bool hasHeal = Fighter.SpellBook.GetSpells()
                .Any(s => s.Effects != null &&
                          s.Effects.Any(e => e.TypeEnum == EffectEnum.Heal) &&
                          s.APCost <= Fighter.AP);

            bool hasInvocation = HasAvailableInvocationSlot() && Fighter.SpellBook.GetSpells()
                .Any(s => s.APCost <= Fighter.AP &&
                          s.Effects != null &&
                          s.Effects.Any(e => e.TypeEnum == EffectEnum.Invocation ||
                                             e.TypeEnum == EffectEnum.InvocDouble ||
                                             e.TypeEnum == EffectEnum.InvocationStatic));

            bool hasBuff = Fighter.SpellBook.GetSpells()
                .Any(s => s.APCost <= Fighter.AP &&
                          s.Effects != null &&
                          s.Effects.Any(e => IsSelfBuff(e.TypeEnum)) &&
                          !s.Effects.Any(e => CastInfos.IsDamageEffect(e.TypeEnum)));

            AIAction tail = startDelay;

            if (isCriticalHp && hasHeal)
            {
                tail = tail.LinkWith(new HealAction(Fighter));
                tail = tail.LinkWith(new DelayAction(Fighter, WorldConfig.FIGHT_AI_THINK_DELAY));
            }

            if (hasInvocation)
            {
                tail = tail.LinkWith(new InvocationAction(Fighter));
                tail = tail.LinkWith(new DelayAction(Fighter, WorldConfig.FIGHT_AI_THINK_DELAY));
            }

            if (hasBuff)
            {
                tail = tail.LinkWith(new BuffAction(Fighter));
                tail = tail.LinkWith(new DelayAction(Fighter, WorldConfig.FIGHT_AI_THINK_DELAY));
            }

            tail = tail.LinkWith(new AttackAction(Fighter));
            tail = tail.LinkWith(new DelayAction(Fighter, WorldConfig.FIGHT_AI_THINK_DELAY));
            tail = tail.LinkWith(new MoveAction(Fighter));
            tail = tail.LinkWith(new DelayAction(Fighter, WorldConfig.FIGHT_AI_THINK_DELAY));
            tail = tail.LinkWith(new AttackAction(Fighter));

            if (isLowHp && hasHeal && !isCriticalHp)
            {
                tail = tail.LinkWith(new DelayAction(Fighter, WorldConfig.FIGHT_AI_THINK_DELAY));
                tail = tail.LinkWith(new HealAction(Fighter));
            }

            tail.LinkWith(new EndTurnAction(Fighter));
        }

        private bool HasAvailableInvocationSlot()
        {
            var maxInvoc = Fighter.Statistics.GetTotal(EffectEnum.AddInvocationMax);
            var currentInvoc = Fighter.Team.AliveFighters.Count(f => f.Invocator == Fighter && !f.StaticInvocation);
            return currentInvoc < maxInvoc;
        }

        private static bool IsSelfBuff(EffectEnum effect)
        {
            switch (effect)
            {
                case EffectEnum.AddAP:
                case EffectEnum.AddAPBis:
                case EffectEnum.AddMP:
                case EffectEnum.MPBonus:
                case EffectEnum.AddStrength:
                case EffectEnum.AddIntelligence:
                case EffectEnum.AddAgility:
                case EffectEnum.AddChance:
                case EffectEnum.AddDamage:
                case EffectEnum.AddDamagePercent:
                case EffectEnum.AddDamageCritic:
                case EffectEnum.Mastery:
                case EffectEnum.MultiplyDamage:
                case EffectEnum.IncreaseSpellDamage:
                case EffectEnum.AddArmor:
                case EffectEnum.AddReflectDamage:
                case EffectEnum.AddReduceDamageAir:
                case EffectEnum.AddReduceDamageEarth:
                case EffectEnum.AddReduceDamageFire:
                case EffectEnum.AddReduceDamageWater:
                case EffectEnum.AddReduceDamageNeutral:
                    return true;
            }
            return false;
        }
    }
}


