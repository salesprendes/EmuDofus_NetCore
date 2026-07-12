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

                foreach (var enemy in context.Enemies)
                {
                    if (enemy?.Cell == null || enemy.IsFighterDead)
                    {
                        continue;
                    }

                    // Danio estimado contra ESTE enemigo (segun sus resistencias y nuestro elemento).
                    var estimatedDamage = SpellEvaluator.EstimateDamageOnTarget(context.Fighter, spell, enemy);

                    int targetCellId = enemy.Cell.Id;




                    if (SpellEvaluator.CanCastFromCurrentCell(context, spell, targetCellId))
                    {
                        int killScore = TargetEvaluator.ScoreKillChance(context.Fighter, enemy, estimatedDamage);
                        int score = 100 + estimatedDamage + TargetEvaluator.ScoreLowHp(enemy) + TargetEvaluator.ScorePriorityTarget(enemy) / 4 + TargetEvaluator.ScoreThreat(context, enemy) + killScore;

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




                    // Busca la celda alcanzable de MENOR riesgo desde la que el hechizo alcanza al
                    // enemigo: el movimiento y el lanzamiento se emiten como una unica decision
                    // atomica, garantizando que el desplazamiento habilita realmente el ataque.
                    int? bestMoveCell = null;
                    var bestMoveRisk = int.MaxValue;
                    foreach (var reachCell in reachableCells)
                    {
                        if (reachCell == context.CurrentCellId)
                        {
                            continue;
                        }

                        if (!SpellEvaluator.CanCastFromCell(context, spell, reachCell, targetCellId))
                        {
                            continue;
                        }

                        var risk = RiskEvaluator.ScoreCellRisk(context, reachCell, false);
                        if (risk < bestMoveRisk)
                        {
                            bestMoveRisk = risk;
                            bestMoveCell = reachCell;
                        }
                    }

                    if (bestMoveCell == null)
                    {
                        continue;
                    }

                    {
                        var killScore = TargetEvaluator.ScoreKillChance(context.Fighter, enemy, estimatedDamage);
                        var score = 75 + estimatedDamage / 2 + TargetEvaluator.ScoreLowHp(enemy) / 2 + TargetEvaluator.ScoreThreat(context, enemy) / 2 + killScore - bestMoveRisk / 4;

                        yield return AIDecision.MoveAndCast(
                            bestMoveCell.Value,
                            spell.SpellId,
                            targetCellId,
                            enemy.Id,
                            score,
                            killScore > 0 ? AIDecisionPriority.Normal : AIDecisionPriority.Low,
                            killScore > 0 ? "Golpe mortal tras movimiento" : "Ataque tras movimiento");
                    }
                }
            }
        }
    }
}
