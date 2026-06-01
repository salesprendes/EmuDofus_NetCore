using Game.Entity;
using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;

namespace Game.Fight.AI.Bosses
{
    /// <summary>
    /// Cerebro de los tentáculos del Kralamoure Géant.
    ///
    /// Los tentáculos tienen un papel defensivo/de apoyo:
    ///   • Tentáculo Primario  (424) — hechizos 261 + 1096
    ///   • Tentáculo Secundario(1092), Terciario (1091), Cuaternario (1090) — hechizos similares
    ///
    /// Comportamiento por fases (detectadas a través del Kralamar aliado):
    ///   FASE PROTECCIÓN  (Kralamar HP > 40%): prioridad en debuff + ataque moderado.
    ///   FASE ENRAGE      (Kralamar HP ≤ 40%): prioridad máxima en todo; los tentáculos
    ///                    se vuelven agresivos para proteger al jefe.
    ///
    /// Los tentáculos son invocaciones estáticas (no se mueven): la lógica de movimiento
    /// se omite salvo que haya posibilidad de mejorar el ángulo de ataque.
    /// </summary>
    public sealed class KralamarTentacleBrain : AIBrain
    {
        // Template IDs del Kralamar — tentáculos usan esto para detectar al jefe
        private const int KralamarTemplateId = 423;

        public KralamarTentacleBrain(AIFighter fighter)
            : base(fighter)
        {
        }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            // Detectar fase del Kralamar (jefe aliado con template 423)
            var kralamardHpRatio = GetKralamarHpRatio(context);
            bool enragePhase = kralamardHpRatio <= 0.40;

            // ── Debuff: prioridad siempre presente (drena PA/PM del jugador más peligroso)
            foreach (var decision in new DebuffEvaluator().Evaluate(context))
            {
                if (decision != null)
                {
                    // Elevar prioridad en fase de enrage
                    if (enragePhase && decision.Priority < AIDecisionPriority.High)
                        decision.Priority = AIDecisionPriority.High;
                    yield return decision;
                }
            }

            // ── Ataque: siempre activo
            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                if (decision != null)
                {
                    if (enragePhase && decision.Priority < AIDecisionPriority.High)
                        decision.Priority = AIDecisionPriority.High;
                    yield return decision;
                }
            }

            // ── Movimiento: solo si los tentáculos no son invocaciones estáticas
            //    En Dofus 1.29 los tentáculos del Kralamar son StaticInvocation=false
            //    (efecto 181, no InvocationStatic), por lo que sí pueden moverse si tienen PM.
            if (!Fighter.StaticInvocation && context.CurrentMP > 0)
            {
                foreach (var decision in new MovementEvaluator().Evaluate(context))
                    yield return decision;
            }
        }

        /// <summary>
        /// Busca al Kralamar Gigante entre los aliados y devuelve su ratio de HP.
        /// Devuelve 1.0 si no se encuentra (sin información → asumir fase normal).
        /// </summary>
        private static double GetKralamarHpRatio(AIContext context)
        {
            foreach (var ally in context.Allies)
            {
                if (ally == null || ally.IsFighterDead)
                    continue;

                var monsterId = (ally as MonsterEntity)?.Grade?.MonsterId ?? 0;
                if (monsterId == KralamarTemplateId && ally.MaxLife > 0)
                    return (double)ally.Life / ally.MaxLife;
            }

            return 1.0;
        }

        protected override void LogDecision(AIContext context, AIDecision decision)
        {
            if (!WorldConfig.LOG_DEBUG || decision == null)
                return;

            Logger.Debug("[AI][KralamarTentacle] Fighter=" + (Fighter?.Id ?? 0)
                + " Template=" + ((Fighter as MonsterEntity)?.Grade?.MonsterId ?? 0)
                + " Decision=" + decision.Type
                + " Priority=" + decision.Priority
                + " Score=" + decision.Score
                + " Reason=" + decision.Reason);
        }
    }
}
