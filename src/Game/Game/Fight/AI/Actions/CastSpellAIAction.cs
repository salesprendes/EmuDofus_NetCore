using Game.Fight.AI.Core;
using Game.Spell;
using System.Linq;

namespace Game.Fight.AI.Actions
{
    public abstract class SpellAIActionBase : IAIAction
    {
        protected readonly AIDecision Decision;

        public abstract AIDecisionType Type { get; }
        protected virtual int LaunchDelayMs => WorldConfig.FIGHT_AI_SPELL_LAUNCH_TIME;
        public virtual int EstimatedDelayMs => LaunchDelayMs + WorldConfig.FIGHT_AI_THINK_DELAY;

        protected SpellAIActionBase(AIDecision decision)
        {
            Decision = decision;
        }

        public virtual bool CanExecute(AIContext context)
        {
            if (context?.Fighter == null || context.Fight == null || Decision == null || !Decision.SpellId.HasValue)
                return false;

            if (context.Fighter.IsFighterDead || context.Fighter.Cell == null || context.Fight.CurrentFighter != context.Fighter)
                return false;

            var spell = GetSpell(context);
            var targetCell = GetTargetCell(context);
            if (spell == null || targetCell < 0)
                return false;

            return context.Fight.CanLaunchSpell(
                context.Fighter,
                spell,
                Decision.SpellId.Value,
                context.Fighter.Cell.Id,
                targetCell) == FightSpellLaunchResultEnum.RESULT_OK;
        }

        public virtual AIActionResult Execute(AIContext context)
        {
            if (!CanExecute(context))
                return AIActionResult.Fail("Spell no longer castable");

            var targetCell = GetTargetCell(context);
            context.Fight.TryLaunchSpell(context.Fighter, Decision.SpellId.Value, targetCell, LaunchDelayMs);
            return AIActionResult.Ok(EstimatedDelayMs, "Spell cast queued");
        }

        protected SpellLevel GetSpell(AIContext context)
        {
            return Decision?.SpellId == null ? null : context.Fighter.SpellBook.GetSpellLevel(Decision.SpellId.Value);
        }

        protected int GetTargetCell(AIContext context)
        {
            if (Decision?.CellId != null)
                return Decision.CellId.Value;

            if (Decision?.TargetId == null)
                return -1;

            var target = context.Fight.AliveFighters.FirstOrDefault(f => f.Id == Decision.TargetId.Value);
            return target?.Cell?.Id ?? -1;
        }
    }

    public class CastSpellAIAction : SpellAIActionBase
    {
        public override AIDecisionType Type => AIDecisionType.CastSpell;

        public CastSpellAIAction(AIDecision decision)
            : base(decision)
        {
        }
    }
}
