using Game.Fight.AI.Core;
using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Evaluation
{
    // Coloca glifos y trampas en las casillas que cubren a mas enemigos (y evita pillar aliados).
    // Un glifo dania a quien este/entre en su zona; una trampa se dispara cuando un enemigo la pisa,
    // por lo que tambien valen las casillas adyacentes a enemigos.
    public sealed class GlyphTrapEvaluator : IAIEvaluator
    {
        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            if (context?.Fighter?.Cell == null || context.Enemies == null || context.SpellBook == null)
                yield break;

            foreach (var decision in EvaluateZoneSpells(context, context.SpellBook.GlyphSpells, "Glifo", 70))
                yield return decision;

            foreach (var decision in EvaluateZoneSpells(context, context.SpellBook.TrapSpells, "Trampa", 60))
                yield return decision;
        }

        private static IEnumerable<AIDecision> EvaluateZoneSpells(AIContext context, IReadOnlyList<SpellLevel> spells, string label, int baseScore)
        {
            if (spells == null)
                yield break;

            foreach (var spell in spells)
            {
                if (spell == null || spell.APCost > context.CurrentAP)
                    continue;

                // Casillas candidatas: la de cada enemigo y sus adyacentes (donde caera/pisara).
                var candidateCells = new HashSet<int>();
                foreach (var enemy in context.Enemies)
                {
                    if (enemy?.Cell == null || enemy.IsFighterDead)
                        continue;

                    candidateCells.Add(enemy.Cell.Id);
                    foreach (var adj in context.TurnCache.Cells.GetNeighbors(enemy.Cell.Id))
                        candidateCells.Add(adj);
                }

                foreach (var cellId in candidateCells)
                {
                    if (!SpellEvaluator.CanCastFromCurrentCell(context, spell, cellId))
                        continue;

                    var affected = SpellEvaluator.GetAffectedFighters(context, spell, cellId).ToList();
                    var enemiesInZone = affected.Count(f => f.Team != context.Fighter.Team);
                    var alliesInZone = affected.Count(f => f.Team == context.Fighter.Team);

                    // Para trampas, los enemigos adyacentes la pisaran aunque no esten ya en la zona.
                    var adjacentEnemies = context.Enemies.Count(e => e?.Cell != null && !e.IsFighterDead
                        && context.TurnCache.Cells.GetDistance(cellId, e.Cell.Id) <= 1);

                    var coverage = Math.Max(enemiesInZone, adjacentEnemies);
                    if (coverage <= 0)
                        continue;

                    var score = baseScore + coverage * 45 - alliesInZone * 60;
                    if (score <= 0)
                        continue;

                    yield return AIDecision.CastSpell(spell.SpellId, cellId, 0, score,
                        AIDecisionPriority.Normal, label + " sobre " + coverage + " enemigos");
                }
            }
        }
    }
}
