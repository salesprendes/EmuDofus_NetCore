using Game.Fight.AI.Core;
using Game.Fight;
using Game.Spell;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Evaluation
{
    public sealed class BuffEvaluator : IAIEvaluator
    {
        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            if (context?.Allies == null || context.SpellBook?.BuffSpells == null)
                yield break;

            foreach (var spell in context.SpellBook.BuffSpells)
            {
                if (spell == null || spell.APCost > context.CurrentAP)
                    continue;

                var buffValue = SpellEvaluator.EstimateBuffValue(spell);

                foreach (var ally in context.Allies)
                {
                    if (ally?.Cell == null || ally.IsFighterDead)
                        continue;

                    if (!SpellEvaluator.CanCastFromCurrentCell(context, spell, ally.Cell.Id))
                        continue;

                    // ── TODO resuelto: evitar relanzar estados ya activos ────────────────
                    // Si el hechizo aplica un estado (EffectEnum.AddState = 950) y el aliado
                    // ya tiene ese estado activo, no tiene sentido relanzarlo (el motor lo
                    // rechazaría o desperdiciaría PA).
                    if (WouldRecastActiveState(spell, ally))
                        continue;

                    var score = 70 + buffValue;
                    if (ally == context.Fighter)
                        score += 35;
                    if (ally.MaxLife > 0)
                        score += (int)(60 * (1.0 - (double)ally.Life / ally.MaxLife));

                    // Penalizar el buff si ya tiene estados relacionados activos
                    // (aunque no exactamente el mismo — reducir ligeramente el score)
                    int activeStatePenalty = CountRelatedActiveStates(spell, ally) * 25;
                    score = System.Math.Max(1, score - activeStatePenalty);

                    yield return new AIDecision
                    {
                        Type     = AIDecisionType.Buff,
                        Priority = AIDecisionPriority.Normal,
                        Score    = score,
                        SpellId  = spell.SpellId,
                        TargetId = ally.Id,
                        CellId   = (short)ally.Cell.Id,
                        Reason   = "Buff útil sobre aliado"
                    };
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Devuelve true si el hechizo aplica un estado (EffectEnum.AddState)
        /// que el objetivo ya tiene activo en su StateManager.
        /// Esto evita desperdiciar PA relanzando buffs de estado duplicados.
        /// </summary>
        private static bool WouldRecastActiveState(SpellLevel spell, AbstractFighter target)
        {
            if (spell?.Effects == null || target?.StateManager == null)
                return false;

            foreach (var effect in spell.Effects)
            {
                if (effect == null)
                    continue;

                // EffectEnum.AddState = 950; el stateId va en Value3
                if (effect.TypeEnum == EffectEnum.AddState && effect.Value3 > 0)
                {
                    if (target.StateManager.HasState((FighterStateEnum)effect.Value3))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Cuenta cuántos estados aplicados por el hechizo ya están activos en el objetivo.
        /// Usado para penalizar el score cuando el buff es parcialmente redundante.
        /// </summary>
        private static int CountRelatedActiveStates(SpellLevel spell, AbstractFighter target)
        {
            if (spell?.Effects == null || target?.StateManager == null)
                return 0;

            return spell.Effects.Count(effect =>
                effect != null
                && effect.TypeEnum == EffectEnum.AddState
                && effect.Value3 > 0
                && target.StateManager.HasState((FighterStateEnum)effect.Value3));
        }
    }
}
