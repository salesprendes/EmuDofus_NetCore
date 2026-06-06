using Game.Entity;
using Game.Fight.AI.Bosses.Mechanics;
using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using Game.Fight.Effect;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Bosses
{
    public sealed class KralamarBrain : AIBrain, IDamageReceivedBrain
    {
        private static readonly HashSet<int> TentacleTemplateIds = new HashSet<int> { 424, 425, 1090, 1091, 1092 };
        private const double HpThresholdEnrage = 0.40;

        private readonly KralamarMechanic m_mechanic;

        public KralamarBrain(AIFighter fighter)
            : base(fighter)
        {
            m_mechanic = new KralamarMechanic();
        }

        public override void OnTurnStart()
        {
            m_mechanic.OnKralamarTurnStart(Fighter);
            base.OnTurnStart();
        }

        public void OnDamageReceived(CastInfos castInfos, int damageBeforeResistance)
        {
            m_mechanic.OnDamageReceived(Fighter, castInfos, damageBeforeResistance);
        }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            var livingTentacles = context.Allies.Count(a =>
                a != null
                && !a.IsFighterDead
                && TentacleTemplateIds.Contains((a as MonsterEntity)?.Grade?.MonsterId ?? 0));

            var hpRatio = context.Fighter.MaxLife > 0
                ? (double)context.Fighter.Life / context.Fighter.MaxLife
                : 1.0;

            var enragePhase = livingTentacles == 0 || hpRatio <= HpThresholdEnrage;

            if (WorldConfig.LOG_DEBUG)
            {
                Logger.Debug("[AI][Kralamar] Fighter=" + (Fighter?.Id ?? 0)
                    + " HP=" + context.Fighter.Life + "/" + context.Fighter.MaxLife
                    + " Tentacles=" + livingTentacles
                    + " Phase=" + (enragePhase ? "ENRAGE" : "PROTECT"));
            }

            foreach (var decision in m_mechanic.Evaluate(context))
            {
                if (decision != null)
                    yield return decision;
            }

            foreach (var decision in new DebuffEvaluator().Evaluate(context))
            {
                if (decision == null)
                    continue;

                if (enragePhase
                    && (context.SpellBook.RemoveAPSpells.Any(s => s?.SpellId == decision.SpellId)
                        || context.SpellBook.RemoveMPSpells.Any(s => s?.SpellId == decision.SpellId)))
                {
                    decision.Priority = AIDecisionPriority.Critical;
                    decision.Score += 150;
                }

                yield return decision;
            }

            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                if (decision == null)
                    continue;

                if (enragePhase && decision.Priority < AIDecisionPriority.High)
                {
                    decision.Priority = AIDecisionPriority.High;
                    decision.Score += 100;
                }

                yield return decision;
            }
        }

        protected override void LogDecision(AIContext context, AIDecision decision)
        {
            if (!WorldConfig.LOG_DEBUG || decision == null)
                return;

            Logger.Debug("[AI][Kralamar] Fighter=" + (Fighter?.Id ?? 0)
                + " Decision=" + decision.Type
                + " Priority=" + decision.Priority
                + " Score=" + decision.Score
                + " Reason=" + decision.Reason);
        }
    }
}
