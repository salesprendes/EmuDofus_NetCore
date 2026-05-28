using Game.Fight.Effect;
using Game.Spell;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Core
{
    public sealed class AISpellBook
    {
        public IReadOnlyList<SpellLevel> AllSpells { get; private set; }
        public IReadOnlyList<SpellLevel> DamageSpells { get; private set; }
        public IReadOnlyList<SpellLevel> HealSpells { get; private set; }
        public IReadOnlyList<SpellLevel> BuffSpells { get; private set; }
        public IReadOnlyList<SpellLevel> DebuffSpells { get; private set; }
        public IReadOnlyList<SpellLevel> SummonSpells { get; private set; }
        public IReadOnlyList<SpellLevel> MovementSpells { get; private set; }
        public IReadOnlyList<SpellLevel> TrapSpells { get; private set; }
        public IReadOnlyList<SpellLevel> GlyphSpells { get; private set; }
        public IReadOnlyList<SpellLevel> RemoveAPSpells { get; private set; }
        public IReadOnlyList<SpellLevel> RemoveMPSpells { get; private set; }
        public IReadOnlyList<SpellLevel> RemoveRangeSpells { get; private set; }
        public IReadOnlyList<SpellLevel> PushPullSpells { get; private set; }
        public IReadOnlyList<SpellLevel> DefensiveSpells { get; private set; }
        public IReadOnlyList<SpellLevel> UnbewitchSpells { get; private set; }
        public IReadOnlyList<SpellLevel> VulnerabilitySpells { get; private set; }

        public AISpellBook(AIFighter fighter)
        {
            AllSpells = fighter?.SpellBook?.GetSpells()?.Where(s => s != null).ToList() ?? new List<SpellLevel>();

            DamageSpells = Filter(AllSpells, HasDamageEffect);
            HealSpells = Filter(AllSpells, HasHealEffect);
            BuffSpells = Filter(AllSpells, HasBuffEffect);
            DebuffSpells = Filter(AllSpells, HasDebuffEffect);
            SummonSpells = Filter(AllSpells, HasSummonEffect);
            MovementSpells = Filter(AllSpells, HasMovementEffect);
            TrapSpells = Filter(AllSpells, HasTrapEffect);
            GlyphSpells = Filter(AllSpells, HasGlyphEffect);
            RemoveAPSpells = Filter(AllSpells, HasRemoveAPEffect);
            RemoveMPSpells = Filter(AllSpells, HasRemoveMPEffect);
            RemoveRangeSpells = Filter(AllSpells, HasRemoveRangeEffect);
            PushPullSpells = Filter(AllSpells, HasPushPullEffect);
            DefensiveSpells = Filter(AllSpells, HasDefensiveEffect);
            UnbewitchSpells = Filter(AllSpells, HasUnbewitchEffect);
            VulnerabilitySpells = Filter(AllSpells, HasVulnerabilityEffect);
        }

        private static IReadOnlyList<SpellLevel> Filter(IEnumerable<SpellLevel> spells, System.Func<SpellLevel, bool> predicate)
        {
            return spells.Where(predicate).ToList();
        }

        public static bool HasDamageEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => CastInfos.IsDamageEffect(e.TypeEnum));
        }

        public static bool HasHealEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.Heal);
        }

        public static bool HasBuffEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => CastInfos.IsBonusEffect(e.TypeEnum) && e.TypeEnum != EffectEnum.Heal)
                && !HasDamageEffect(spell);
        }

        public static bool HasDebuffEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => IsDebuff(e.TypeEnum));
        }

        public static bool HasSummonEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.Invocation
                || e.TypeEnum == EffectEnum.InvocDouble
                || e.TypeEnum == EffectEnum.InvocationStatic);
        }

        public static bool HasMovementEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.Teleport
                || e.TypeEnum == EffectEnum.Transpose
                || e.TypeEnum == EffectEnum.PandaCarrier
                || e.TypeEnum == EffectEnum.PandaLaunch);
        }

        public static bool HasTrapEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.UseTrap);
        }

        public static bool HasGlyphEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.UseGlyph);
        }

        public static bool HasRemoveAPEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.SubAP
                || e.TypeEnum == EffectEnum.SubAPDodgeable
                || e.TypeEnum == EffectEnum.APSteal);
        }

        public static bool HasRemoveMPEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.SubMP
                || e.TypeEnum == EffectEnum.SubMPDodgeable
                || e.TypeEnum == EffectEnum.MPSteal);
        }

        public static bool HasPushPullEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.PushBack
                || e.TypeEnum == EffectEnum.PushFront
                || e.TypeEnum == EffectEnum.PushFear
                || e.TypeEnum == EffectEnum.PandaLaunch);
        }

        public static bool HasRemoveRangeEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.SubPO
                || e.TypeEnum == EffectEnum.POSteal);
        }

        public static bool HasDefensiveEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => IsDefensive(e.TypeEnum));
        }

        public static bool HasUnbewitchEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.DeleteAllBonus
                || e.TypeEnum == EffectEnum.RemoveState);
        }

        public static bool HasVulnerabilityEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => IsVulnerability(e.TypeEnum));
        }

        private static bool HasEffect(SpellLevel spell, System.Func<SpellEffect, bool> predicate)
        {
            if (spell?.Effects == null)
                return false;

            return spell.Effects.Any(predicate);
        }

        private static bool IsDebuff(EffectEnum effect)
        {
            switch (effect)
            {
                case EffectEnum.SubAP:
                case EffectEnum.SubAPDodgeable:
                case EffectEnum.SubMP:
                case EffectEnum.SubMPDodgeable:
                case EffectEnum.POSteal:
                case EffectEnum.SubPO:
                case EffectEnum.SubStrength:
                case EffectEnum.SubIntelligence:
                case EffectEnum.SubAgility:
                case EffectEnum.SubChance:
                case EffectEnum.SubWisdom:
                case EffectEnum.SubDamage:
                case EffectEnum.SubDamagePercent:
                case EffectEnum.DeleteAllBonus:
                case EffectEnum.RemoveState:
                    return true;
            }

            return IsVulnerability(effect);
        }

        private static bool IsDefensive(EffectEnum effect)
        {
            switch (effect)
            {
                case EffectEnum.AddArmor:
                case EffectEnum.AddArmorAir:
                case EffectEnum.AddArmorBis:
                case EffectEnum.AddArmorEarth:
                case EffectEnum.AddArmorFire:
                case EffectEnum.AddArmorNeutral:
                case EffectEnum.AddArmorWater:
                case EffectEnum.AddLife:
                case EffectEnum.AddVitality:
                case EffectEnum.AddAPDodge:
                case EffectEnum.AddMPDodge:
                case EffectEnum.AddReduceDamageAir:
                case EffectEnum.AddReduceDamageEarth:
                case EffectEnum.AddReduceDamageFire:
                case EffectEnum.AddReduceDamageMagic:
                case EffectEnum.AddReduceDamageNeutral:
                case EffectEnum.AddReduceDamagePercentAir:
                case EffectEnum.AddReduceDamagePercentEarth:
                case EffectEnum.AddReduceDamagePercentFire:
                case EffectEnum.AddReduceDamagePercentNeutral:
                case EffectEnum.AddReduceDamagePercentWater:
                case EffectEnum.AddReduceDamagePhysic:
                case EffectEnum.AddReduceDamageWater:
                case EffectEnum.AddReflectDamage:
                case EffectEnum.ReflectSpell:
                case EffectEnum.Evasion:
                case EffectEnum.Sacrifice:
                    return true;
            }

            return false;
        }

        private static bool IsVulnerability(EffectEnum effect)
        {
            switch (effect)
            {
                case EffectEnum.SubReduceDamageAir:
                case EffectEnum.SubReduceDamageEarth:
                case EffectEnum.SubReduceDamageFire:
                case EffectEnum.SubReduceDamageWater:
                case EffectEnum.SubReduceDamageNeutral:
                case EffectEnum.SubReduceDamagePercentAir:
                case EffectEnum.SubReduceDamagePercentEarth:
                case EffectEnum.SubReduceDamagePercentFire:
                case EffectEnum.SubReduceDamagePercentWater:
                case EffectEnum.SubReduceDamagePercentNeutral:
                    return true;
            }
            return false;
        }
    }
}
