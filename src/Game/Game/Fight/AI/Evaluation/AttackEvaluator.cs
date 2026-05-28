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

            // Las celdas alcanzables se calculan una sola vez y se reutilizan en el pase 2.
            // Solo se materializan si hay PM disponibles para no desperdiciar la cache.
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

                    // ── Pase 1: ataque desde la celda actual ─────────────────────────────
                    // Si el hechizo ya alcanza al enemigo desde aquí, generamos la decisión
                    // con prioridad Normal/Critical y no evaluamos el pase 2 para este par.
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
                            Reason = killScore > 0 ? "Killing blow" : "Damage spell"
                        };

                        continue; // ya cubierto por el pase 1
                    }

                    // ── Pase 2: ataque post-movimiento ───────────────────────────────────
                    //
                    // Si no hay PM o el combatiente no puede moverse, no hay nada que hacer.
                    if (!hasMP || reachableCells == null)
                    {
                        continue;
                    }

                    // Buscamos si ALGUNA celda alcanzable (con los PM actuales) permite
                    // lanzar el hechizo sobre este enemigo.  Aprovecha SpellEvaluator.CanCastFromCell
                    // que ya maneja rango, LoS, línea, estados y límites de lanzamiento.
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

                    // Generamos la decisión con prioridad Low y score < 100 (score del Move).
                    // Así en la cadena queda: Move (Low, 100) → Attack_postmove (Low, ~75).
                    // Al ejecutarse, DecisionAIAction.OnInitialize llama CanExecute usando
                    // context.Fighter.Cell.Id (la posición real post-movimiento), por lo que
                    // CanLaunchSpell pasará si el monstruo se movió a una celda válida.
                    {
                        var killScore = TargetEvaluator.ScoreKillChance(context.Fighter, enemy, estimatedDamage);
                        var score = 75
                            + estimatedDamage / 2
                            + TargetEvaluator.ScoreLowHp(enemy) / 2
                            + killScore;

                        yield return new AIDecision
                        {
                            Type = AIDecisionType.CastSpell,
                            Priority = AIDecisionPriority.Low,
                            Score = score,
                            SpellId = spell.SpellId,
                            TargetId = enemy.Id,
                            CellId = (short)targetCellId,
                            Reason = killScore > 0 ? "Killing blow after move" : "Attack after movement"
                        };
                    }
                }
            }
        }
    }
}
