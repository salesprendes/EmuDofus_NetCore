using Game.Fight.AI.Core;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Evaluation
{
    public sealed class DebuffEvaluator : IAIEvaluator
    {
        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            if (context?.Enemies == null || context.SpellBook?.DebuffSpells == null)
                yield break;

            var hasMP          = context.CurrentMP > 0;
            var reachableCells = hasMP ? context.TurnCache.Cells.GetReachableCells() : null;

            foreach (var spell in context.SpellBook.DebuffSpells)
            {
                if (spell == null || spell.APCost > context.CurrentAP)
                    continue;

                var debuffValue = SpellEvaluator.EstimateDebuffValue(spell);
                var isHighPriority = context.SpellBook.RemoveAPSpells.Contains(spell)
                                  || context.SpellBook.RemoveMPSpells.Contains(spell);

                foreach (var enemy in context.Enemies)
                {
                    if (enemy?.Cell == null || enemy.IsFighterDead)
                        continue;

                    var targetCellId = enemy.Cell.Id;

                    // ── Pase 1: debuff desde la celda actual ─────────────────────────────
                    if (SpellEvaluator.CanCastFromCurrentCell(context, spell, targetCellId))
                    {
                        var priority = isHighPriority ? AIDecisionPriority.High : AIDecisionPriority.Normal;

                        yield return new AIDecision
                        {
                            Type     = AIDecisionType.Debuff,
                            Priority = priority,
                            Score    = 80 + debuffValue + TargetEvaluator.ScorePriorityTarget(enemy) / 3,
                            SpellId  = spell.SpellId,
                            TargetId = enemy.Id,
                            CellId   = (short)targetCellId,
                            Reason   = "Useful debuff/control"
                        };

                        continue;
                    }

                    // ── Pase 2: debuff post-movimiento ───────────────────────────────────
                    // Los hechizos de debuff suelen tener buen rango en Dofus 1.29, pero
                    // algunos (trabas CAC, p. ej.) requieren estar adyacente.
                    if (!hasMP || reachableCells == null)
                        continue;

                    var canDebuffAfterMove = false;
                    foreach (var reachCell in reachableCells)
                    {
                        if (reachCell == context.CurrentCellId)
                            continue;

                        if (SpellEvaluator.CanCastFromCell(context, spell, reachCell, targetCellId))
                        {
                            canDebuffAfterMove = true;
                            break;
                        }
                    }

                    if (!canDebuffAfterMove)
                        continue;

                    // Score algo menor que el Move (Low, 100) para que quede después en la cadena.
                    yield return new AIDecision
                    {
                        Type     = AIDecisionType.Debuff,
                        Priority = AIDecisionPriority.Low,
                        Score    = 70 + debuffValue / 2 + TargetEvaluator.ScorePriorityTarget(enemy) / 6,
                        SpellId  = spell.SpellId,
                        TargetId = enemy.Id,
                        CellId   = (short)targetCellId,
                        Reason   = "Debuff after movement"
                    };
                }
            }
        }
    }
}
