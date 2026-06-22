using Game.Fight.AI.Core;
using Game.Fight;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Evaluation
{
    // Disipa (desenvruja) los buffs de los enemigos mas potenciados. Cuanto mas potenciado este un
    // enemigo, mayor es la puntuacion para quitarle sus ventajas.
    public sealed class DispelEvaluator : IAIEvaluator
    {
        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            if (context?.Fighter == null || context.Enemies == null || context.SpellBook?.UnbewitchSpells == null)
                yield break;

            foreach (var spell in context.SpellBook.UnbewitchSpells)
            {
                if (spell == null || spell.APCost > context.CurrentAP)
                    continue;

                foreach (var enemy in context.Enemies)
                {
                    if (enemy?.Cell == null || enemy.IsFighterDead)
                        continue;

                    var buffCount = CountBuffs(enemy);
                    if (buffCount <= 0)
                        continue;

                    if (!SpellEvaluator.CanCastFromCurrentCell(context, spell, enemy.Cell.Id))
                        continue;

                    yield return AIDecision.CastSpell(spell.SpellId, enemy.Cell.Id, enemy.Id, 50 + buffCount * 60,
                        AIDecisionPriority.Normal, "Disipar " + buffCount + " buffs enemigos");
                }
            }
        }

        private static int CountBuffs(AbstractFighter fighter)
        {
            return fighter?.BuffManager?.GetAllBuffs()?.Count() ?? 0;
        }
    }
}
