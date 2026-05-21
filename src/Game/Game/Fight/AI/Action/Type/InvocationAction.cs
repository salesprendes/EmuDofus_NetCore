using Game.Fight.Effect;
using Game.Map;
using Game.Spell;
using System.Linq;
using Game.Fight;

namespace Game.Fight.AI.Action.Type
{
    public enum InvocationStateEnum
    {
        STATE_FIND_SPELL,
        STATE_CAST,
        STATE_WAITING,
    }

    public sealed class InvocationAction : AIAction
    {
        private InvocationStateEnum InvocationState { get; set; }
        private int SpellId { get; set; }
        private int TargetCell { get; set; }
        private SpellLevel SelectedSpellLevel { get; set; }

        public InvocationAction(AIFighter fighter) : base(fighter)
        {
        }

        public override AIActionResult Initialize()
        {
            InvocationState = InvocationStateEnum.STATE_FIND_SPELL;
            SpellId = 0;
            TargetCell = 0;
            SelectedSpellLevel = null;

            var maxInvoc = Fighter.Statistics.GetTotal(EffectEnum.AddInvocationMax);
            var currentInvoc = Fighter.Team.AliveFighters.Count(f => f.Invocator == Fighter && !f.StaticInvocation);

            if (currentInvoc >= maxInvoc)
                return AIActionResult.FAILURE;

            bool hasInvocationSpell = Fighter.SpellBook.GetSpells()
                .Any(s => s.APCost <= Fighter.AP &&
                          s.Effects != null &&
                          s.Effects.Any(e => e.TypeEnum == EffectEnum.Invocation ||
                                             e.TypeEnum == EffectEnum.InvocDouble ||
                                             e.TypeEnum == EffectEnum.InvocationStatic));

            return hasInvocationSpell ? AIActionResult.RUNNING : AIActionResult.FAILURE;
        }

        public override AIActionResult Execute()
        {
            switch (InvocationState)
            {
                case InvocationStateEnum.STATE_FIND_SPELL:
                    SpellLevel bestSpell = null;
                    int bestCell = 0;

                    foreach (var spell in Fighter.SpellBook.GetSpells())
                    {
                        if (spell.APCost > Fighter.AP)
                            continue;
                        if (spell.Effects == null)
                            continue;
                        if (!spell.Effects.Any(e => e.TypeEnum == EffectEnum.Invocation ||
                                                     e.TypeEnum == EffectEnum.InvocDouble ||
                                                     e.TypeEnum == EffectEnum.InvocationStatic))
                            continue;

                        // Look for a free cell to summon on (prefer closest free cell, skip occupied)
                        foreach (var cellId in CellZone.GetCircleCells(Map, Fighter.Cell.Id, System.Math.Max(1, spell.MaxPO)))
                        {
                            var fightCell = Fight.GetCell(cellId);
                            if (fightCell == null || fightCell.HasObject(FightObstacleTypeEnum.TYPE_FIGHTER))
                                continue;

                            if (Fight.CanLaunchSpell(Fighter, spell, spell.SpellId, Fighter.Cell.Id, cellId) == FightSpellLaunchResultEnum.RESULT_OK)
                            {
                                bestSpell = spell;
                                bestCell = cellId;
                                break;
                            }
                        }

                        if (bestSpell != null)
                            break;
                    }

                    if (bestSpell == null || bestCell == 0)
                        return AIActionResult.FAILURE;

                    SpellId = bestSpell.SpellId;
                    TargetCell = bestCell;
                    SelectedSpellLevel = bestSpell;
                    InvocationState = InvocationStateEnum.STATE_CAST;
                    return AIActionResult.RUNNING;

                case InvocationStateEnum.STATE_CAST:
                    var actionTime = GetSpellActionTime(SelectedSpellLevel);
                    Fight.TryLaunchSpell(Fighter, SpellId, TargetCell, actionTime);
                    Timeout = actionTime + GetActionThinkTime();
                    InvocationState = InvocationStateEnum.STATE_WAITING;
                    return AIActionResult.RUNNING;

                case InvocationStateEnum.STATE_WAITING:
                    if (!Timedout)
                        return AIActionResult.RUNNING;
                    return AIActionResult.SUCCESS;
            }

            return AIActionResult.FAILURE;
        }
    }
}


