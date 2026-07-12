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

                    if (WouldRecastActiveState(spell, ally))
                        continue;

                    // El aliado ya lleva este buff activo: no desperdiciar PA relanzándolo.
                    if (SpellEvaluator.HasActiveBuffFromSpell(ally, spell.SpellId))
                        continue;

                    var score = 70 + buffValue;
                    if (ally == context.Fighter)
                        score += 35;
                    if (ally.MaxLife > 0)
                        score += (int)(60 * (1.0 - (double)ally.Life / ally.MaxLife));



                    int activeStatePenalty = CountRelatedActiveStates(spell, ally) * 25;
                    score = System.Math.Max(1, score - activeStatePenalty);

                    yield return new AIDecision
                    {
                        Type = AIDecisionType.Buff,
                        Priority = AIDecisionPriority.Normal,
                        Score = score,
                        SpellId = spell.SpellId,
                        TargetId = ally.Id,
                        CellId = (short)ally.Cell.Id,
                        Reason = "Buff útil sobre aliado"
                    };
                }
            }
        }



        private static bool WouldRecastActiveState(SpellLevel spell, AbstractFighter target)
        {
            if (spell?.Effects == null || target?.StateManager == null)
                return false;

            foreach (var effect in spell.Effects)
            {
                if (effect == null)
                    continue;


                if (effect.TypeEnum == EffectEnum.ESTADO_MAS && effect.Value3 > 0)
                {
                    if (target.StateManager.HasState((FighterStateEnum)effect.Value3))
                        return true;
                }
            }

            return false;
        }

        private static int CountRelatedActiveStates(SpellLevel spell, AbstractFighter target)
        {
            if (spell?.Effects == null || target?.StateManager == null)
                return 0;

            return spell.Effects.Count(effect =>
                effect != null
                && effect.TypeEnum == EffectEnum.ESTADO_MAS
                && effect.Value3 > 0
                && target.StateManager.HasState((FighterStateEnum)effect.Value3));
        }
    }
}
