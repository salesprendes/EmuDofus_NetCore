using Game.Entity;
using Game.Fight.AI.Bosses.Mechanics;
using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Bosses
{
    /// <summary>
    /// Cerebro principal del Kralamar Gigante (template 423).
    ///
    /// Delega la lógica de boss en <see cref="KralamarMechanic"/> y complementa con
    /// los evaluadores genéricos de debuff, ataque y movimiento.
    ///
    /// Fases detectadas:
    ///   FASE TENTÁCULOS  (tentáculos vivos > 0):
    ///     El Kralamar prioriza invocar y proteger tentáculos.  Los evaluadores genéricos
    ///     actúan con prioridad reducida.
    ///   FASE ENRAGE      (sin tentáculos o HP ≤ 40%):
    ///     Todos los ataques se elevan a prioridad High/Critical.
    /// </summary>
    public sealed class KralamarBrain : AIBrain
    {
        private readonly IBossMechanic m_mechanic;
        private static readonly HashSet<int> TentacleTemplateIds = new HashSet<int> { 424, 425, 1090, 1091, 1092 };

        private const double HpThresholdEnrage = 0.40;

        public KralamarBrain(AIFighter fighter)
            : base(fighter)
        {
            m_mechanic = new KralamarMechanic();
        }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            // ── Detección de fase ─────────────────────────────────────────────
            int livingTentacles = context.Allies.Count(a => a != null && !a.IsFighterDead && TentacleTemplateIds.Contains((a as MonsterEntity)?.Grade?.MonsterId ?? 0));

            double hpRatio = context.Fighter.MaxLife > 0 ? (double)context.Fighter.Life / context.Fighter.MaxLife : 1.0;

            bool enragePhase = livingTentacles == 0 || hpRatio <= HpThresholdEnrage;

            if (WorldConfig.LOG_DEBUG)
            {
                Logger.Debug("[AI][Kralamar] Fighter=" + (Fighter?.Id ?? 0) + " HP=" + context.Fighter.Life + "/" + context.Fighter.MaxLife + " Tentacles=" + livingTentacles + " Phase=" + (enragePhase ? "ENRAGE" : "PROTECT"));
            }

            // ── Mecánica del boss (invocaciones, Kraken, Vulnerabilidad, Skupehagua) ──
            foreach (var decision in m_mechanic.Evaluate(context))
            {
                if (decision != null)
                {
                    yield return decision;
                }
            }

            // ── Debuff genérico ───────────────────────────────────────────────
            foreach (var decision in new DebuffEvaluator().Evaluate(context))
            {
                if (decision == null)
                {
                    continue;
                }

                // En enrage, elevar los debuffs de AP/MP a Critical
                if (enragePhase && context.SpellBook.RemoveAPSpells.Any(s => s?.SpellId == decision.SpellId) || context.SpellBook.RemoveMPSpells.Any(s => s?.SpellId == decision.SpellId))
                {
                    decision.Priority = AIDecisionPriority.Critical;
                    decision.Score += 150;
                }

                yield return decision;
            }

            // ── Ataque genérico ───────────────────────────────────────────────
            foreach (AIDecision decision in new AttackEvaluator().Evaluate(context))
            {
                if (decision == null)
                {
                    continue;
                }

                if (enragePhase && decision.Priority < AIDecisionPriority.High)
                {
                    decision.Priority = AIDecisionPriority.High;
                    decision.Score += 100;
                }

                yield return decision;
            }

            // ── Movimiento ────────────────────────────────────────────────────
            // En fase de tentáculos: el Kralamar se mantiene centrado.
            // En enrage: se acerca al objetivo más peligroso.
            if (!enragePhase)
            {
                // Mantener distancia media — evitar estar adyacente a múltiples enemigos
                foreach (var decision in new MovementEvaluator().Evaluate(context))
                {
                    yield return decision;
                }
            }
            else
            {
                // Acercarse al objetivo más peligroso
                var target = TargetEvaluator.GetMostDangerousEnemy(context);
                if (target != null)
                {
                    var cell = new MovementEvaluator().GetBestCellNearEnemy(context);
                    if (cell.HasValue)
                    {
                        yield return AIDecision.Move(
                            cell.Value,
                            score: 80,
                            priority: AIDecisionPriority.Normal,
                            reason: "Kralamar enrage — acercarse al objetivo");
                    }
                }
            }
        }

        protected override void LogDecision(AIContext context, AIDecision decision)
        {
            if (!WorldConfig.LOG_DEBUG || decision == null)
            {
                return;
            }

            Logger.Debug("[AI][Kralamar] Fighter=" + (Fighter?.Id ?? 0) + " Decision=" + decision.Type + " Priority=" + decision.Priority + " Score=" + decision.Score + " Reason=" + decision.Reason);
        }
    }
}
