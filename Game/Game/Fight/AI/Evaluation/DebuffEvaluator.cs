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

            var hasMP = context.CurrentMP > 0;
            var reachableCells = hasMP ? context.TurnCache.Cells.GetReachableCells() : null;

            foreach (var spell in context.SpellBook.DebuffSpells)
            {
                if (spell == null || spell.APCost > context.CurrentAP)
                    continue;

                var debuffValue = SpellEvaluator.EstimateDebuffValue(spell);
                var isHighPriority = context.SpellBook.RemoveAPSpells.Contains(spell) || context.SpellBook.RemoveMPSpells.Contains(spell);

                foreach (var enemy in context.Enemies)
                {
                    if (enemy?.Cell == null || enemy.IsFighterDead)
                        continue;

                    // El enemigo ya sufre este debuff: gastar los PA en otra cosa.
                    if (SpellEvaluator.HasActiveBuffFromSpell(enemy, spell.SpellId))
                        continue;

                    var targetCellId = enemy.Cell.Id;


                    if (SpellEvaluator.CanCastFromCurrentCell(context, spell, targetCellId))
                    {
                        var priority = isHighPriority ? AIDecisionPriority.High : AIDecisionPriority.Normal;

                        yield return new AIDecision
                        {
                            Type = AIDecisionType.Debuff,
                            Priority = priority,
                            Score = 80 + debuffValue + TargetEvaluator.ScorePriorityTarget(enemy) / 3,
                            SpellId = spell.SpellId,
                            TargetId = enemy.Id,
                            CellId = (short)targetCellId,
                            Reason = "Useful debuff/control"
                        };

                        continue;
                    }

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


                    yield return new AIDecision
                    {
                        Type = AIDecisionType.Debuff,
                        Priority = AIDecisionPriority.Low,
                        Score = 70 + debuffValue / 2 + TargetEvaluator.ScorePriorityTarget(enemy) / 6,
                        SpellId = spell.SpellId,
                        TargetId = enemy.Id,
                        CellId = (short)targetCellId,
                        Reason = "Debilitamiento tras movimiento"
                    };
                }
            }
        }
    }
}
