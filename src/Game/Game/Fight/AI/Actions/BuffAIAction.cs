using Game.Fight.AI.Core;
using Game.Fight;
using Game.Spell;
using System.Linq;

namespace Game.Fight.AI.Actions
{
    public sealed class BuffAIAction : SpellAIActionBase
    {
        public override AIDecisionType Type => AIDecisionType.Buff;

        public BuffAIAction(AIDecision decision)
            : base(decision)
        {
        }

        public override bool CanExecute(AIContext context)
        {
            if (!base.CanExecute(context) || Decision.TargetId == null)
                return false;

            var target = context.Allies?.FirstOrDefault(a => a.Id == Decision.TargetId.Value);
            if (target == null || target.IsFighterDead)
                return false;

            var spell = GetSpell(context);

            // Guarda 1 — estado ya activo (AddState):
            // El motor lo rechaza silenciosamente en FighterStateManager.AddState si HasState()
            // devuelve true, así que nos ahorramos el PA sin lanzar el hechizo.
            if (spell != null && WouldRecastActiveState(spell, target))
                return false;

            // Guarda 2 — buff del mismo hechizo ya activo en el objetivo:
            // BuffManager.GetAllBuffs() expone todos los buffs activos; comparamos por SpellId.
            // Evita desperdiciar PA re-buffando cuando la aplicación anterior aún no ha expirado
            // (p. ej. re-lanzar AddAP cuando el buff de AP actual todavía tiene turnos restantes).
            if (Decision.SpellId.HasValue && HasActiveBuffFromSpell(target, Decision.SpellId.Value))
                return false;

            return true;
        }

        // ─── Helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Devuelve true si algún efecto del hechizo aplica un estado (EffectEnum.AddState = 950)
        /// que el objetivo ya tiene activo en su StateManager.
        /// Implementa la misma lógica que <c>BuffEvaluator.WouldRecastActiveState</c> para que
        /// la acción y el evaluador sean consistentes.
        /// </summary>
        private static bool WouldRecastActiveState(SpellLevel spell, AbstractFighter target)
        {
            if (spell?.Effects == null || target?.StateManager == null)
                return false;

            foreach (var effect in spell.Effects)
            {
                if (effect == null)
                    continue;

                if (effect.TypeEnum == EffectEnum.AddState && effect.Value3 > 0
                    && target.StateManager.HasState((FighterStateEnum)effect.Value3))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Devuelve true si el objetivo ya tiene algún buff activo originado por el mismo hechizo.
        /// Se apoya en <see cref="BuffEffectManager.GetAllBuffs"/> y compara
        /// <c>buff.CastInfos.SpellId</c> para detectar re-lanzamientos redundantes.
        /// </summary>
        private static bool HasActiveBuffFromSpell(AbstractFighter target, int spellId)
        {
            if (target?.BuffManager == null)
                return false;

            foreach (var buff in target.BuffManager.GetAllBuffs())
            {
                if (buff?.CastInfos != null && buff.CastInfos.SpellId == spellId)
                    return true;
            }

            return false;
        }
    }
}
