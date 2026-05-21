using Game.Fight.Effect;
using Game.Spell;
using System.Linq;

namespace Game.Fight.AI.Action.Type
{
    public enum BuffStateEnum
    {
        STATE_FIND_SPELL,
        STATE_CAST,
        STATE_WAITING,
    }

    public sealed class BuffAction : AIAction
    {
        private BuffStateEnum BuffState { get; set; }
        private int SpellId { get; set; }
        private int TargetCell { get; set; }
        private SpellLevel SelectedSpellLevel { get; set; }

        public BuffAction(AIFighter fighter) : base(fighter)
        {
        }

        public override AIActionResult Initialize()
        {
            BuffState = BuffStateEnum.STATE_FIND_SPELL;
            SpellId = 0;
            TargetCell = 0;
            SelectedSpellLevel = null;

            bool hasBuffSpell = Fighter.SpellBook.GetSpells()
                .Any(s => s.APCost <= Fighter.AP &&
                          s.Effects != null &&
                          s.Effects.Any(e => IsBuff(e.TypeEnum)) &&
                          !s.Effects.Any(e => CastInfos.IsDamageEffect(e.TypeEnum)));

            return hasBuffSpell ? AIActionResult.RUNNING : AIActionResult.FAILURE;
        }

        public override AIActionResult Execute()
        {
            switch (BuffState)
            {
                case BuffStateEnum.STATE_FIND_SPELL:
                    SpellLevel bestSpell = null;
                    int bestCell = 0;
                    int bestScore = int.MinValue;

                    foreach (var spell in Fighter.SpellBook.GetSpells())
                    {
                        if (spell.APCost > Fighter.AP)
                            continue;
                        if (spell.Effects == null)
                            continue;
                        if (spell.Effects.Any(e => CastInfos.IsDamageEffect(e.TypeEnum)))
                            continue;
                        if (!spell.Effects.Any(e => IsBuff(e.TypeEnum)))
                            continue;

                        // Pure buff spells: prefer casting on self or allied fighter with lowest HP
                        foreach (var ally in Fighter.Team.AliveFighters)
                        {
                            if (Fight.CanLaunchSpell(Fighter, spell, spell.SpellId, Fighter.Cell.Id, ally.Cell.Id) != FightSpellLaunchResultEnum.RESULT_OK)
                                continue;

                            var buffValue = spell.Effects
                                .Where(e => IsBuff(e.TypeEnum))
                                .Sum(e => e.Value1 + e.Value2);

                            // Prefer buffing self or low-HP allies
                            int score = buffValue;
                            if (ally == Fighter)
                                score += 20;
                            else if (ally.MaxLife > 0)
                                score += (int)(30 * (1.0 - (double)ally.Life / ally.MaxLife));

                            if (score > bestScore)
                            {
                                bestScore = score;
                                bestSpell = spell;
                                bestCell = ally.Cell.Id;
                            }
                        }
                    }

                    if (bestSpell == null || bestCell == 0)
                        return AIActionResult.FAILURE;

                    SpellId = bestSpell.SpellId;
                    TargetCell = bestCell;
                    SelectedSpellLevel = bestSpell;
                    BuffState = BuffStateEnum.STATE_CAST;
                    return AIActionResult.RUNNING;

                case BuffStateEnum.STATE_CAST:
                    var actionTime = GetSpellActionTime(SelectedSpellLevel);
                    Fight.TryLaunchSpell(Fighter, SpellId, TargetCell, actionTime);
                    Timeout = actionTime + GetActionThinkTime();
                    BuffState = BuffStateEnum.STATE_WAITING;
                    return AIActionResult.RUNNING;

                case BuffStateEnum.STATE_WAITING:
                    if (!Timedout)
                        return AIActionResult.RUNNING;
                    return AIActionResult.SUCCESS;
            }

            return AIActionResult.FAILURE;
        }

        private static bool IsBuff(EffectEnum effect)
        {
            switch (effect)
            {
                case EffectEnum.AddAP:
                case EffectEnum.AddAPBis:
                case EffectEnum.AddMP:
                case EffectEnum.MPBonus:
                case EffectEnum.AddStrength:
                case EffectEnum.AddIntelligence:
                case EffectEnum.AddAgility:
                case EffectEnum.AddChance:
                case EffectEnum.AddWisdom:
                case EffectEnum.AddVitality:
                case EffectEnum.AddDamage:
                case EffectEnum.AddDamagePercent:
                case EffectEnum.AddDamageCritic:
                case EffectEnum.Mastery:
                case EffectEnum.MultiplyDamage:
                case EffectEnum.IncreaseSpellDamage:
                case EffectEnum.AddReduceDamageAir:
                case EffectEnum.AddReduceDamageEarth:
                case EffectEnum.AddReduceDamageFire:
                case EffectEnum.AddReduceDamageWater:
                case EffectEnum.AddReduceDamageNeutral:
                case EffectEnum.AddReduceDamagePercentAir:
                case EffectEnum.AddReduceDamagePercentEarth:
                case EffectEnum.AddReduceDamagePercentFire:
                case EffectEnum.AddReduceDamagePercentNeutral:
                case EffectEnum.AddReduceDamagePercentWater:
                case EffectEnum.AddArmor:
                case EffectEnum.AddArmorAir:
                case EffectEnum.AddArmorEarth:
                case EffectEnum.AddArmorFire:
                case EffectEnum.AddArmorWater:
                case EffectEnum.AddArmorNeutral:
                case EffectEnum.AddReflectDamage:
                case EffectEnum.AddAPDodge:
                case EffectEnum.AddMPDodge:
                case EffectEnum.AddPO:
                case EffectEnum.AddHealCare:
                case EffectEnum.AddLife:
                    return true;
            }
            return false;
        }
    }
}


