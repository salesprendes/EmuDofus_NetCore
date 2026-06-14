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
            var all = fighter?.SpellBook?.GetSpells()?.Where(s => s != null).ToList() ?? new List<SpellLevel>();
            AllSpells = all;



            var damage = new List<SpellLevel>();
            var heal = new List<SpellLevel>();
            var buff = new List<SpellLevel>();
            var debuff = new List<SpellLevel>();
            var summon = new List<SpellLevel>();
            var movement = new List<SpellLevel>();
            var trap = new List<SpellLevel>();
            var glyph = new List<SpellLevel>();
            var removeAP = new List<SpellLevel>();
            var removeMP = new List<SpellLevel>();
            var removeRange = new List<SpellLevel>();
            var pushPull = new List<SpellLevel>();
            var defensive = new List<SpellLevel>();
            var unbewitch = new List<SpellLevel>();
            var vulnerability = new List<SpellLevel>();

            foreach (var spell in all)
            {
                if (HasDamageEffect(spell)) damage.Add(spell);
                if (HasHealEffect(spell)) heal.Add(spell);
                if (HasBuffEffect(spell)) buff.Add(spell);
                if (HasDebuffEffect(spell)) debuff.Add(spell);
                if (HasSummonEffect(spell)) summon.Add(spell);
                if (HasMovementEffect(spell)) movement.Add(spell);
                if (HasTrapEffect(spell)) trap.Add(spell);
                if (HasGlyphEffect(spell)) glyph.Add(spell);
                if (HasRemoveAPEffect(spell)) removeAP.Add(spell);
                if (HasRemoveMPEffect(spell)) removeMP.Add(spell);
                if (HasRemoveRangeEffect(spell)) removeRange.Add(spell);
                if (HasPushPullEffect(spell)) pushPull.Add(spell);
                if (HasDefensiveEffect(spell)) defensive.Add(spell);
                if (HasUnbewitchEffect(spell)) unbewitch.Add(spell);
                if (HasVulnerabilityEffect(spell)) vulnerability.Add(spell);
            }

            DamageSpells = damage;
            HealSpells = heal;
            BuffSpells = buff;
            DebuffSpells = debuff;
            SummonSpells = summon;
            MovementSpells = movement;
            TrapSpells = trap;
            GlyphSpells = glyph;
            RemoveAPSpells = removeAP;
            RemoveMPSpells = removeMP;
            RemoveRangeSpells = removeRange;
            PushPullSpells = pushPull;
            DefensiveSpells = defensive;
            UnbewitchSpells = unbewitch;
            VulnerabilitySpells = vulnerability;
        }

        public static bool HasDamageEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => CastInfos.IsDamageEffect(e.TypeEnum));
        }

        public static bool HasHealEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.CURACION);
        }

        public static bool HasBuffEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => CastInfos.IsBonusEffect(e.TypeEnum) && e.TypeEnum != EffectEnum.CURACION) && !HasDamageEffect(spell);
        }

        public static bool HasDebuffEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => IsDebuff(e.TypeEnum));
        }

        public static bool HasSummonEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.INVOCACION_CRIATURA || e.TypeEnum == EffectEnum.INVOCACION_DOBLE || e.TypeEnum == EffectEnum.INVOCACION_ESTATICA);
        }

        public static bool HasMovementEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.MOVIMIENTO_TELETRANSPORTAR
                || e.TypeEnum == EffectEnum.MOVIMIENTO_INTERCAMBIAR_POSICION
                || e.TypeEnum == EffectEnum.PANDA_CARGAR
                || e.TypeEnum == EffectEnum.PANDA_LANZAR);
        }

        public static bool HasTrapEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.COMBATE_COLOCAR_TRAMPA);
        }

        public static bool HasGlyphEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.COMBATE_COLOCAR_GLIFO);
        }

        public static bool HasRemoveAPEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.STAT_MENOS_PA || e.TypeEnum == EffectEnum.STAT_MENOS_PA_ESQUIVABLE || e.TypeEnum == EffectEnum.STAT_ROBO_PA);
        }

        public static bool HasRemoveMPEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.STAT_MENOS_PM || e.TypeEnum == EffectEnum.STAT_MENOS_PM_ESQUIVABLE || e.TypeEnum == EffectEnum.STAT_ROBO_PM);
        }

        public static bool HasPushPullEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.MOVIMIENTO_EMPUJAR || e.TypeEnum == EffectEnum.MOVIMIENTO_ATRAER || e.TypeEnum == EffectEnum.MOVIMIENTO_EMPUJAR_MIEDO || e.TypeEnum == EffectEnum.PANDA_LANZAR);
        }

        public static bool HasRemoveRangeEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.STAT_MENOS_ALCANCE || e.TypeEnum == EffectEnum.STAT_ROBO_ALCANCE);
        }

        public static bool HasDefensiveEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => IsDefensive(e.TypeEnum));
        }

        public static bool HasUnbewitchEffect(SpellLevel spell)
        {
            return HasEffect(spell, e => e.TypeEnum == EffectEnum.BUFF_QUITAR_TODOS || e.TypeEnum == EffectEnum.ESTADO_QUITAR);
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
                case EffectEnum.STAT_MENOS_PA:
                case EffectEnum.STAT_MENOS_PA_ESQUIVABLE:
                case EffectEnum.STAT_MENOS_PM:
                case EffectEnum.STAT_MENOS_PM_ESQUIVABLE:
                case EffectEnum.STAT_ROBO_ALCANCE:
                case EffectEnum.STAT_MENOS_ALCANCE:
                case EffectEnum.STAT_MENOS_FUERZA:
                case EffectEnum.STAT_MENOS_INTELIGENCIA:
                case EffectEnum.STAT_MENOS_AGILIDAD:
                case EffectEnum.STAT_MENOS_SUERTE:
                case EffectEnum.STAT_MENOS_SABIDURIA:
                case EffectEnum.STAT_MENOS_DANO:
                case EffectEnum.STAT_MENOS_DANO_PORCENTAJE:
                case EffectEnum.BUFF_QUITAR_TODOS:
                case EffectEnum.ESTADO_QUITAR:
                    return true;
            }

            return IsVulnerability(effect);
        }

        private static bool IsDefensive(EffectEnum effect)
        {
            switch (effect)
            {
                case EffectEnum.STAT_MAS_ARMADURA:
                case EffectEnum.STAT_MAS_ARMADURA_AIRE:
                case EffectEnum.STAT_MAS_ARMADURA_BIS:
                case EffectEnum.STAT_MAS_ARMADURA_TIERRA:
                case EffectEnum.STAT_MAS_ARMADURA_FUEGO:
                case EffectEnum.STAT_MAS_ARMADURA_NEUTRAL:
                case EffectEnum.STAT_MAS_ARMADURA_AGUA:
                case EffectEnum.STAT_MAS_VIDA:
                case EffectEnum.STAT_MAS_VITALIDAD:
                case EffectEnum.STAT_MAS_ESQUIVA_PA:
                case EffectEnum.STAT_MAS_ESQUIVA_PM:
                case EffectEnum.STAT_MAS_RESISTENCIA_AIRE:
                case EffectEnum.STAT_MAS_RESISTENCIA_TIERRA:
                case EffectEnum.STAT_MAS_RESISTENCIA_FUEGO:
                case EffectEnum.STAT_MAS_REDUCCION_DANO_MAGICO:
                case EffectEnum.STAT_MAS_RESISTENCIA_NEUTRAL:
                case EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE:
                case EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA:
                case EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO:
                case EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL:
                case EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA:
                case EffectEnum.STAT_MAS_REDUCCION_DANO_FISICO:
                case EffectEnum.STAT_MAS_RESISTENCIA_AGUA:
                case EffectEnum.STAT_MAS_DANO_DEVUELTO:
                case EffectEnum.DEFENSA_DEVOLVER_HECHIZO:
                case EffectEnum.DEFENSA_EVASION:
                case EffectEnum.COMBATE_SACRIFICIO:
                    return true;
            }

            return false;
        }

        private static bool IsVulnerability(EffectEnum effect)
        {
            switch (effect)
            {
                case EffectEnum.STAT_MENOS_RESISTENCIA_AIRE:
                case EffectEnum.STAT_MENOS_RESISTENCIA_TIERRA:
                case EffectEnum.STAT_MENOS_RESISTENCIA_FUEGO:
                case EffectEnum.STAT_MENOS_RESISTENCIA_AGUA:
                case EffectEnum.STAT_MENOS_RESISTENCIA_NEUTRAL:
                case EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_AIRE:
                case EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_TIERRA:
                case EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_FUEGO:
                case EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_AGUA:
                case EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_NEUTRAL:
                    return true;
            }
            return false;
        }
    }
}
