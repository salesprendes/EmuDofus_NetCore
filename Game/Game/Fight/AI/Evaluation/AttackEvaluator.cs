using Game.Fight.AI.Core;
using System.Collections.Generic;

namespace Game.Fight.AI.Evaluation
{
    public sealed class AttackEvaluator : IAIEvaluator
    {
        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            if (context?.Fighter == null || context.Enemies == null || context.SpellBook?.DamageSpells == null)
            {
                yield break;
            }



            var hasMP = context.CurrentMP > 0;
            var reachableCells = hasMP ? context.TurnCache.Cells.GetReachableCells() : null;

            foreach (var spell in context.SpellBook.DamageSpells)
            {
                if (spell == null || spell.APCost > context.CurrentAP)
                {
                    continue;
                }

                var estimatedDamage = SpellEvaluator.EstimateDamage(spell);

                foreach (var enemy in context.Enemies)
                {
                    if (enemy?.Cell == null || enemy.IsFighterDead)
                    {
                        continue;
                    }

                    int targetCellId = enemy.Cell.Id;




                    if (SpellEvaluator.CanCastFromCurrentCell(context, spell, targetCellId))
                    {
                        int killScore = TargetEvaluator.ScoreKillChance(context.Fighter, enemy, estimatedDamage);
                        int score = 100 + estimatedDamage + TargetEvaluator.ScoreLowHp(enemy) + TargetEvaluator.ScorePriorityTarget(enemy) / 4 + killScore;

                        score += SpellEvaluator.ScoreAreaImpact(context, spell, targetCellId, false);

                        yield return new AIDecision
                        {
                            Type = AIDecisionType.CastSpell,
                            Priority = killScore > 0 ? AIDecisionPriority.Critical : AIDecisionPriority.Normal,
                            Score = score,
                            SpellId = spell.SpellId,
                            TargetId = enemy.Id,
                            CellId = (short)targetCellId,
                            Reason = killScore > 0 ? "Golpe mortal" : "Hechizo de danio"
                        };

                        continue;
                    }




                    if (!hasMP || reachableCells == null)
                    {
                        continue;
                    }




                    var canAttackAfterMove = false;
                    foreach (var reachCell in reachableCells)
                    {
                        if (reachCell == context.CurrentCellId)
                        {
                            continue;
                        }

                        if (SpellEvaluator.CanCastFromCell(context, spell, reachCell, targetCellId))
                        {
                            canAttackAfterMove = true;
                            break;
                        }
                    }

                    if (!canAttackAfterMove)
                    {
                        continue;
                    }






                    {
                        var killScore = TargetEvaluator.ScoreKillChance(context.Fighter, enemy, estimatedDamage);
                        var score = 75 + estimatedDamage / 2 + TargetEvaluator.ScoreLowHp(enemy) / 2 + killScore;

                        yield return new AIDecision
                        {
                            Type = AIDecisionType.CastSpell,
                            Priority = AIDecisionPriority.Low,
                            Score = score,
                            SpellId = spell.SpellId,
                            TargetId = enemy.Id,
                            CellId = (short)targetCellId,
                            Reason = killScore > 0 ? "Golpe mortal tras movimiento" : "Ataque tras movimiento"
                        };
                    }
                }
            }
        }
    }
}
