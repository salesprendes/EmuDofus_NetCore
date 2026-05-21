using Game.Fight.Effect;
using Game.Spell;
using System;
using System.Linq;

namespace Game.Fight.AI.Action.Type
{
    public enum HealStateEnum
    {
        STATE_FIND_SPELL,
        STATE_CAST,
        STATE_WAITING,
    }

    public sealed class HealAction : AIAction
    {
        private HealStateEnum HealState { get; set; }
        private int SpellId { get; set; }
        private int TargetCell { get; set; }
        private SpellLevel SelectedSpellLevel { get; set; }

        public HealAction(AIFighter fighter) : base(fighter)
        {
        }

        public override AIActionResult Initialize()
        {
            HealState = HealStateEnum.STATE_FIND_SPELL;
            SpellId = 0;
            TargetCell = 0;
            SelectedSpellLevel = null;

            bool canHeal = Fighter.SpellBook.GetSpells()
                .Any(s => s.Effects != null &&
                          s.Effects.Any(e => e.TypeEnum == EffectEnum.Heal) &&
                          s.APCost <= Fighter.AP);

            return canHeal ? AIActionResult.RUNNING : AIActionResult.FAILURE;
        }

        public override AIActionResult Execute()
        {
            switch (HealState)
            {
                case HealStateEnum.STATE_FIND_SPELL:
                    SpellLevel bestHeal = null;
                    int bestCellId = 0;
                    int bestScore = int.MinValue;

                    foreach (var spell in Fighter.SpellBook.GetSpells())
                    {
                        if (spell.APCost > Fighter.AP)
                            continue;
                        if (spell.Effects == null)
                            continue;
                        if (!spell.Effects.Any(e => e.TypeEnum == EffectEnum.Heal))
                            continue;

                        var estimatedHeal = spell.Effects
                            .Where(e => e.TypeEnum == EffectEnum.Heal)
                            .Sum(e => e.Value1 + e.Value2 + e.Value3);

                        foreach (var ally in Fighter.Team.AliveFighters)
                        {
                            if (Fight.CanLaunchSpell(Fighter, spell, spell.SpellId, Fighter.Cell.Id, ally.Cell.Id) != FightSpellLaunchResultEnum.RESULT_OK)
                                continue;

                            var missingLife = ally.MaxLife - ally.Life;
                            if (missingLife <= 0)
                                continue;

                            var score = missingLife + Math.Min(missingLife, estimatedHeal);
                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestHeal = spell;
                                bestCellId = ally.Cell.Id;
                            }
                        }
                    }

                    if (bestHeal == null || bestCellId == 0)
                        return AIActionResult.FAILURE;

                    SpellId = bestHeal.SpellId;
                    TargetCell = bestCellId;
                    SelectedSpellLevel = bestHeal;
                    HealState = HealStateEnum.STATE_CAST;
                    return AIActionResult.RUNNING;

                case HealStateEnum.STATE_CAST:
                    var actionTime = GetSpellActionTime(SelectedSpellLevel);
                    Fight.TryLaunchSpell(Fighter, SpellId, TargetCell, actionTime);
                    Timeout = actionTime + GetActionThinkTime();
                    HealState = HealStateEnum.STATE_WAITING;
                    return AIActionResult.RUNNING;

                case HealStateEnum.STATE_WAITING:
                    if (!Timedout)
                        return AIActionResult.RUNNING;

                    return Initialize();
            }

            return AIActionResult.FAILURE;
        }
    }
}


