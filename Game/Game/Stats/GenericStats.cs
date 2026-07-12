using Game.Database.Structure;
using Game.Spell;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Stats
{
    [Flags]
    public enum StatsType
    {
        TYPE_BASE,
        TYPE_BOOST,
        TYPE_ITEM,
        TYPE_DON,
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    public sealed class GenericStats : IDisposable
    {
        private static readonly Dictionary<EffectEnum, List<EffectEnum>> OppositeStats = new Dictionary<EffectEnum, List<EffectEnum>>()
        {
            {EffectEnum.STAT_MAS_INICIATIVA, new List<EffectEnum>() { EffectEnum.STAT_MENOS_INICIATIVA }},
            {EffectEnum.STAT_MAS_PA, new List<EffectEnum>() { EffectEnum.STAT_MENOS_PA,  EffectEnum.STAT_MENOS_PA_ESQUIVABLE }},
            {EffectEnum.STAT_MAS_PM, new List<EffectEnum>() { EffectEnum.STAT_MENOS_PM, EffectEnum.STAT_MENOS_PM_ESQUIVABLE }},
            {EffectEnum.STAT_MAS_ALCANCE, new List<EffectEnum>() { EffectEnum.STAT_MENOS_ALCANCE }},
            {EffectEnum.STAT_MAS_CURAS, new List<EffectEnum>() { EffectEnum.STAT_MENOS_CURAS }},
            {EffectEnum.STAT_MAS_PROSPECCION, new List<EffectEnum>() { EffectEnum.STAT_MENOS_PROSPECCION }},
            {EffectEnum.STAT_MAS_PODS, new List<EffectEnum>() { EffectEnum.STAT_MENOS_PODS }},
            {EffectEnum.STAT_MAS_VITALIDAD, new List<EffectEnum>() { EffectEnum.STAT_MENOS_VITALIDAD }},
            {EffectEnum.STAT_MAS_SABIDURIA, new List<EffectEnum>() { EffectEnum.STAT_MENOS_SABIDURIA }},
            {EffectEnum.STAT_MAS_FUERZA, new List<EffectEnum>() { EffectEnum.STAT_MENOS_FUERZA }},
            {EffectEnum.STAT_MAS_INTELIGENCIA, new List<EffectEnum>() { EffectEnum.STAT_MENOS_INTELIGENCIA }},
            {EffectEnum.STAT_MAS_AGILIDAD, new List<EffectEnum>() { EffectEnum.STAT_MENOS_AGILIDAD }},
            {EffectEnum.STAT_MAS_SUERTE, new List<EffectEnum>() { EffectEnum.STAT_MENOS_SUERTE }},

            {EffectEnum.STAT_MAS_DANO, new List<EffectEnum>() { EffectEnum.STAT_MENOS_DANO }},
            {EffectEnum.STAT_MAS_DANO_PORCENTAJE, new List<EffectEnum>() { EffectEnum.STAT_MENOS_DANO_PORCENTAJE }},
            {EffectEnum.STAT_MAS_DANO_CRITICO, new List<EffectEnum>() { EffectEnum.STAT_MENOS_DANO_CRITICO }},
            {EffectEnum.STAT_MAS_DANO_MAGICO, new List<EffectEnum>() { EffectEnum.STAT_MENOS_DANO_MAGICO }},
            {EffectEnum.STAT_MAS_DANO_FISICO, new List<EffectEnum>() { EffectEnum.STAT_MENOS_DANO_FISICO }},
            {EffectEnum.STAT_MAS_ESQUIVA_PA, new List<EffectEnum>() { EffectEnum.STAT_MENOS_PA_ESQUIVABLE }},
            {EffectEnum.STAT_MAS_ESQUIVA_PM, new List<EffectEnum>() { EffectEnum.STAT_MENOS_PM_ESQUIVABLE }},

            {EffectEnum.STAT_MAS_RESISTENCIA_AIRE, new List<EffectEnum>() { EffectEnum.STAT_MENOS_RESISTENCIA_AIRE }},
            {EffectEnum.STAT_MAS_RESISTENCIA_AGUA, new List<EffectEnum>() { EffectEnum.STAT_MENOS_RESISTENCIA_AGUA }},
            {EffectEnum.STAT_MAS_RESISTENCIA_FUEGO, new List<EffectEnum>() { EffectEnum.STAT_MENOS_RESISTENCIA_FUEGO }},
            {EffectEnum.STAT_MAS_RESISTENCIA_NEUTRAL, new List<EffectEnum>() { EffectEnum.STAT_MENOS_RESISTENCIA_NEUTRAL }},
            {EffectEnum.STAT_MAS_RESISTENCIA_TIERRA, new List<EffectEnum>() { EffectEnum.STAT_MENOS_RESISTENCIA_TIERRA }},

            {EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE, new List<EffectEnum>() { EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_AIRE }},
            {EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA, new List<EffectEnum>() { EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_AGUA }},
            {EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO, new List<EffectEnum>() { EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_FUEGO }},
            {EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL, new List<EffectEnum>() { EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_NEUTRAL }},
            {EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA, new List<EffectEnum>() { EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_TIERRA }},

            {EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_AIRE, new List<EffectEnum>() { EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_PVP_AIRE }},
            {EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_AGUA, new List<EffectEnum>() { EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_PVP_AGUA }},
            {EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_FUEGO, new List<EffectEnum>() { EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_PVP_FUEGO }},
            {EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_NEUTRAL, new List<EffectEnum>() { EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_PVP_NEUTRAL }},
            {EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_TIERRA, new List<EffectEnum>() { EffectEnum.STAT_MENOS_RESISTENCIA_PORCENTAJE_PVP_TIERRA }},
        };


        public static int GetRequiredStatsPoint(CharacterBreedEnum breed, int statId, int value)
        {
            switch (statId)
            {
                case 11:
                    return 1;
                case 12:
                    return 3;
                case 10:
                    switch (breed)
                    {
                        case CharacterBreedEnum.BREED_SACRIEUR:
                            return 3;

                        case CharacterBreedEnum.BREED_FECA:
                            if (value < 50)
                                return 2;
                            if (value < 150)
                                return 3;
                            if (value < 250)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_XELOR:
                            if (value < 50)
                                return 2;
                            if (value < 150)
                                return 3;
                            if (value < 250)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_SRAM:
                            if (value < 100)
                                return 1;
                            if (value < 200)
                                return 2;
                            if (value < 300)
                                return 3;
                            if (value < 400)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_OSAMODAS:
                            if (value < 50)
                                return 2;
                            if (value < 150)
                                return 3;
                            if (value < 250)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_ENIRIPSA:
                            if (value < 50)
                                return 2;
                            if (value < 150)
                                return 3;
                            if (value < 250)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_PANDAWA:
                            if (value < 50)
                                return 1;
                            if (value < 200)
                                return 2;
                            return 3;

                        case CharacterBreedEnum.BREED_SADIDAS:
                            if (value < 50)
                                return 1;
                            if (value < 250)
                                return 2;
                            if (value < 300)
                                return 3;
                            if (value < 400)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_CRA:
                            if (value < 50)
                                return 1;
                            if (value < 150)
                                return 2;
                            if (value < 250)
                                return 3;
                            if (value < 350)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_ENUTROF:
                            if (value < 50)
                                return 1;
                            if (value < 150)
                                return 2;
                            if (value < 250)
                                return 3;
                            if (value < 350)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_ECAFLIP:
                            if (value < 100)
                                return 1;
                            if (value < 200)
                                return 2;
                            if (value < 300)
                                return 3;
                            if (value < 400)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_IOP:
                            if (value < 100)
                                return 1;
                            if (value < 200)
                                return 2;
                            if (value < 300)
                                return 3;
                            if (value < 400)
                                return 4;
                            return 5;

                    }
                    break;
                case 13:
                    switch (breed)
                    {
                        case CharacterBreedEnum.BREED_FECA:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_XELOR:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_SACRIEUR:
                            return 3;

                        case CharacterBreedEnum.BREED_SRAM:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_SADIDAS:
                            if (value < 100)
                                return 1;
                            if (value < 200)
                                return 2;
                            if (value < 300)
                                return 3;
                            if (value < 400)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_PANDAWA:
                            if (value < 50)
                                return 1;
                            if (value < 200)
                                return 2;
                            return 3;

                        case CharacterBreedEnum.BREED_IOP:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_ENUTROF:
                            if (value < 100)
                                return 1;
                            if (value < 150)
                                return 2;
                            if (value < 230)
                                return 3;
                            if (value < 330)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_OSAMODAS:
                            if (value < 100)
                                return 1;
                            if (value < 200)
                                return 2;
                            if (value < 300)
                                return 3;
                            if (value < 400)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_ECAFLIP:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_ENIRIPSA:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_CRA:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;
                    }
                    break;
                case 14:
                    switch (breed)
                    {
                        case CharacterBreedEnum.BREED_FECA:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_XELOR:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_SACRIEUR:
                            return 3;

                        case CharacterBreedEnum.BREED_SRAM:
                            if (value < 100)
                                return 1;
                            if (value < 200)
                                return 2;
                            if (value < 300)
                                return 3;
                            if (value < 400)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_SADIDAS:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_PANDAWA:
                            if (value < 50)
                                return 1;
                            if (value < 200)
                                return 2;
                            return 3;

                        case CharacterBreedEnum.BREED_ENIRIPSA:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_IOP:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_ENUTROF:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_ECAFLIP:
                            if (value < 50)
                                return 1;
                            if (value < 100)
                                return 2;
                            if (value < 150)
                                return 3;
                            if (value < 200)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_CRA:
                            if (value < 50)
                                return 1;
                            if (value < 100)
                                return 2;
                            if (value < 150)
                                return 3;
                            if (value < 200)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_OSAMODAS:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;
                    }
                    break;
                case 15:
                    switch (breed)
                    {
                        case CharacterBreedEnum.BREED_XELOR:
                            if (value < 100)
                                return 1;
                            if (value < 200)
                                return 2;
                            if (value < 300)
                                return 3;
                            if (value < 400)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_FECA:
                            if (value < 100)
                                return 1;
                            if (value < 200)
                                return 2;
                            if (value < 300)
                                return 3;
                            if (value < 400)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_SACRIEUR:
                            return 3;

                        case CharacterBreedEnum.BREED_SRAM:
                            if (value < 50)
                                return 2;
                            if (value < 150)
                                return 3;
                            if (value < 250)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_SADIDAS:
                            if (value < 100)
                                return 1;
                            if (value < 200)
                                return 2;
                            if (value < 300)
                                return 3;
                            if (value < 400)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_ENUTROF:
                            if (value < 20)
                                return 1;
                            if (value < 60)
                                return 2;
                            if (value < 100)
                                return 3;
                            if (value < 140)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_PANDAWA:
                            if (value < 50)
                                return 1;
                            if (value < 200)
                                return 2;
                            return 3;

                        case CharacterBreedEnum.BREED_IOP:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_ENIRIPSA:
                            if (value < 100)
                                return 1;
                            if (value < 200)
                                return 2;
                            if (value < 300)
                                return 3;
                            if (value < 400)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_CRA:
                            if (value < 50)
                                return 1;
                            if (value < 150)
                                return 2;
                            if (value < 250)
                                return 3;
                            if (value < 350)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_OSAMODAS:
                            if (value < 100)
                                return 1;
                            if (value < 200)
                                return 2;
                            if (value < 300)
                                return 3;
                            if (value < 400)
                                return 4;
                            return 5;

                        case CharacterBreedEnum.BREED_ECAFLIP:
                            if (value < 20)
                                return 1;
                            if (value < 40)
                                return 2;
                            if (value < 60)
                                return 3;
                            if (value < 80)
                                return 4;
                            return 5;
                    }
                    break;
            }
            return 5;
        }



        public static GenericStats ParseFromString(string stringEffects)
        {
            var stats = new GenericStats();
            if (string.IsNullOrEmpty(stringEffects))
                return stats;
            foreach (var part in stringEffects.Split(','))
            {
                var fields = part.Trim().Split('#');
                if (fields.Length < 2) continue;
                int id;
                if (!int.TryParse(fields[0], System.Globalization.NumberStyles.HexNumber, null, out id)) continue;
                int v1 = 0, v2 = 0, v3 = 0;
                if (fields.Length > 1) int.TryParse(fields[1], System.Globalization.NumberStyles.HexNumber, null, out v1);
                if (fields.Length > 2) int.TryParse(fields[2], System.Globalization.NumberStyles.HexNumber, null, out v2);
                if (fields.Length > 3) int.TryParse(fields[3], System.Globalization.NumberStyles.HexNumber, null, out v3);
                string args = fields.Length > 4 ? fields[4] : "0";
                stats.AddEffect((EffectEnum)id, v1, v2, v3, args);
            }
            return stats;
        }

        [ProtoIgnore] public Dictionary<EffectEnum, GenericEffect> Effects => m_effects;

        [ProtoIgnore]
        public IEnumerable<KeyValuePair<EffectEnum, GenericEffect>> WeaponEffects
        {
            get
            {
                return m_effects.Where(x => ItemTemplateDAO.IsWeaponEffect(x.Key));
            }
        }

        private Dictionary<EffectEnum, GenericEffect> m_effects = new Dictionary<EffectEnum, GenericEffect>();

        public GenericStats()
        {
        }

        public GenericStats(MonsterGradeDAO monster)
        {
            m_effects.Add(EffectEnum.STAT_MAS_PA, new GenericEffect(EffectEnum.STAT_MAS_PA, monster.AP));
            m_effects.Add(EffectEnum.STAT_MAS_PM, new GenericEffect(EffectEnum.STAT_MAS_PM, monster.MP));
            m_effects.Add(EffectEnum.STAT_MAS_INVOCACIONES_MAX, new GenericEffect(EffectEnum.STAT_MAS_INVOCACIONES_MAX, monster.MaxInvocation));
            m_effects.Add(EffectEnum.STAT_MAS_INICIATIVA, new GenericEffect(EffectEnum.STAT_MAS_INICIATIVA, monster.Initiative));
            m_effects.Add(EffectEnum.STAT_MAS_SABIDURIA, new GenericEffect(EffectEnum.STAT_MAS_SABIDURIA, monster.Wisdom));
            m_effects.Add(EffectEnum.STAT_MAS_FUERZA, new GenericEffect(EffectEnum.STAT_MAS_FUERZA, monster.Strenght));
            m_effects.Add(EffectEnum.STAT_MAS_INTELIGENCIA, new GenericEffect(EffectEnum.STAT_MAS_INTELIGENCIA, monster.Intelligence));
            m_effects.Add(EffectEnum.STAT_MAS_AGILIDAD, new GenericEffect(EffectEnum.STAT_MAS_AGILIDAD, monster.Agility));
            m_effects.Add(EffectEnum.STAT_MAS_SUERTE, new GenericEffect(EffectEnum.STAT_MAS_SUERTE, monster.Chance));

            m_effects.Add(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL, new GenericEffect(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL, monster.NeutralResistance));
            m_effects.Add(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA, new GenericEffect(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA, monster.EarthResistance));
            m_effects.Add(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO, new GenericEffect(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO, monster.FireResistance));
            m_effects.Add(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA, new GenericEffect(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA, monster.WaterResistance));
            m_effects.Add(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE, new GenericEffect(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE, monster.AirResistance));

            m_effects.Add(EffectEnum.STAT_MAS_ESQUIVA_PA, new GenericEffect(EffectEnum.STAT_MAS_ESQUIVA_PA, monster.APDodgePercent));
            m_effects.Add(EffectEnum.STAT_MAS_ESQUIVA_PM, new GenericEffect(EffectEnum.STAT_MAS_ESQUIVA_PM, monster.MPDodgePercent));
        }

        public GenericStats(GuildDAO guild)
        {
            m_effects.Add(EffectEnum.STAT_MAS_PA, new GenericEffect(EffectEnum.STAT_MAS_PA, 6));
            m_effects.Add(EffectEnum.STAT_MAS_PM, new GenericEffect(EffectEnum.STAT_MAS_PM, 5));
            m_effects.Add(EffectEnum.STAT_MAS_PROSPECCION, new GenericEffect(EffectEnum.STAT_MAS_PROSPECCION, 100));
            m_effects.Add(EffectEnum.STAT_MAS_PODS, new GenericEffect(EffectEnum.STAT_MAS_PODS, 1000));
            m_effects.Add(EffectEnum.STAT_MAS_INICIATIVA, new GenericEffect(EffectEnum.STAT_MAS_INICIATIVA, 100));
            m_effects.Add(EffectEnum.STAT_MAS_VITALIDAD, new GenericEffect(EffectEnum.STAT_MAS_VITALIDAD, 100 * guild.Level));
            m_effects.Add(EffectEnum.STAT_MAS_SABIDURIA, new GenericEffect(EffectEnum.STAT_MAS_SABIDURIA, guild.Level * 4));
            m_effects.Add(EffectEnum.STAT_MAS_FUERZA, new GenericEffect(EffectEnum.STAT_MAS_FUERZA, guild.Level));
            m_effects.Add(EffectEnum.STAT_MAS_INTELIGENCIA, new GenericEffect(EffectEnum.STAT_MAS_INTELIGENCIA, guild.Level));
            m_effects.Add(EffectEnum.STAT_MAS_AGILIDAD, new GenericEffect(EffectEnum.STAT_MAS_AGILIDAD, guild.Level));
            m_effects.Add(EffectEnum.STAT_MAS_SUERTE, new GenericEffect(EffectEnum.STAT_MAS_SUERTE, guild.Level));
            m_effects.Add(EffectEnum.STAT_MAS_DANO, new GenericEffect(EffectEnum.STAT_MAS_DANO, guild.Level));
            m_effects.Add(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE, new GenericEffect(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE, guild.Level / 2));
            m_effects.Add(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA, new GenericEffect(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA, guild.Level / 2));
            m_effects.Add(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO, new GenericEffect(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO, guild.Level / 2));
            m_effects.Add(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA, new GenericEffect(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA, guild.Level / 2));
            m_effects.Add(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL, new GenericEffect(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL, guild.Level / 2));
        }

        public GenericStats(CharacterDAO character)
        {
            m_effects.Add(EffectEnum.STAT_MAS_PA, new GenericEffect(EffectEnum.STAT_MAS_PA, character.Level >= 100 ? 7 : 6));
            m_effects.Add(EffectEnum.STAT_MAS_PM, new GenericEffect(EffectEnum.STAT_MAS_PM, 3));
            m_effects.Add(EffectEnum.STAT_MAS_PROSPECCION, new GenericEffect(EffectEnum.STAT_MAS_PROSPECCION, ((CharacterBreedEnum)character.Breed == CharacterBreedEnum.BREED_ENUTROF ? 120 : 100)));
            m_effects.Add(EffectEnum.STAT_MAS_PODS, new GenericEffect(EffectEnum.STAT_MAS_PODS, 1000));
            m_effects.Add(EffectEnum.STAT_MAS_INVOCACIONES_MAX, new GenericEffect(EffectEnum.STAT_MAS_INVOCACIONES_MAX, 1));
            m_effects.Add(EffectEnum.STAT_MAS_INICIATIVA, new GenericEffect(EffectEnum.STAT_MAS_INICIATIVA, 100));
            m_effects.Add(EffectEnum.STAT_MAS_VITALIDAD, new GenericEffect(EffectEnum.STAT_MAS_VITALIDAD, character.Vitality));
            m_effects.Add(EffectEnum.STAT_MAS_SABIDURIA, new GenericEffect(EffectEnum.STAT_MAS_SABIDURIA, character.Wisdom));
            m_effects.Add(EffectEnum.STAT_MAS_FUERZA, new GenericEffect(EffectEnum.STAT_MAS_FUERZA, character.Strength));
            m_effects.Add(EffectEnum.STAT_MAS_INTELIGENCIA, new GenericEffect(EffectEnum.STAT_MAS_INTELIGENCIA, character.Intelligence));
            m_effects.Add(EffectEnum.STAT_MAS_AGILIDAD, new GenericEffect(EffectEnum.STAT_MAS_AGILIDAD, character.Agility));
            m_effects.Add(EffectEnum.STAT_MAS_SUERTE, new GenericEffect(EffectEnum.STAT_MAS_SUERTE, character.Chance));
        }

        public GenericEffect GetTotalEffect(EffectEnum effectType)
        {
            var effect = GetEffect(effectType);

            int totalBase = effect.Base;
            int totalItems = effect.Items;
            int totalDons = effect.Dons;
            int totalBoosts = effect.Boosts;

            switch (effectType)
            {
                case EffectEnum.STAT_MAS_ESQUIVA_PA:
                case EffectEnum.STAT_MAS_ESQUIVA_PM:
                    totalBase += GetTotal(EffectEnum.STAT_MAS_SABIDURIA) / 4;
                    break;
                case EffectEnum.STAT_MAS_PA:
                    totalItems += GetTotal(EffectEnum.STAT_MAS_PA_BIS);
                    break;
                case EffectEnum.STAT_MAS_PM:
                    totalItems += GetTotal(EffectEnum.STAT_MAS_PM_BONUS);
                    break;
                case EffectEnum.STAT_MAS_DANO_DEVUELTO:
                    totalItems += GetTotal(EffectEnum.STAT_MAS_DANO_DEVUELTO_OBJETO);
                    break;
            }

            if (OppositeStats.TryGetValue(effectType, out List<EffectEnum> value))
            {
                foreach (EffectEnum OppositeEffect in value)
                {
                    if (m_effects.TryGetValue(OppositeEffect, out GenericEffect value1))
                    {
                        totalBase -= value1.Base;
                        totalBoosts -= value1.Boosts;
                        totalDons -= value1.Dons;
                        totalItems -= value1.Items;
                    }
                }
            }

            return new GenericEffect(effectType, totalBase, totalItems, totalDons, totalBoosts);
        }

        public int GetTotal(EffectEnum effectType)
        {
            int total = 0;

            if (m_effects.TryGetValue(effectType, out GenericEffect value))
                total += value.Total;

            switch (effectType)
            {
                case EffectEnum.STAT_MAS_ESQUIVA_PA:
                case EffectEnum.STAT_MAS_ESQUIVA_PM:
                    total += GetTotal(EffectEnum.STAT_MAS_SABIDURIA) / 4;
                    break;

                case EffectEnum.STAT_MAS_PA:
                    total += GetTotal(EffectEnum.STAT_MAS_PA_BIS);
                    break;

                case EffectEnum.STAT_MAS_PM:
                    total += GetTotal(EffectEnum.STAT_MAS_PM_BONUS);
                    break;
            }

            if (OppositeStats.TryGetValue(effectType, out List<EffectEnum> value1))
                foreach (EffectEnum OppositeEffect in value1)
                    if (m_effects.TryGetValue(OppositeEffect, out GenericEffect value2))
                        total -= value2.Total;

            return total;
        }

        public void AddEffect(EffectEnum id, int value1, int value2 = 0, int value3 = 0, string args = "0")
        {
            if (m_effects.ContainsKey(id))
            {
                if (!ItemTemplateDAO.IsWeaponEffect(id))
                {
                    var effect = m_effects[id];
                    var value = value1 != 0 || value3 == 0 ? value1 : value3;
                    if (effect.Value1 != 0 || effect.Value3 == 0)
                        effect.Value1 += value;
                    else
                        effect.Value3 += value;
                    effect.Value2 += value2;
                    effect.Args = args;
                    StatisticsChanged();
                }
                return;
            }

            if (!m_effects.ContainsKey(id))
            {
                m_effects.Add((EffectEnum)id, new GenericEffect(id, value1, value2, value3, args));
                StatisticsChanged();
            }
        }

        public GenericEffect GetEffect(EffectEnum id)
        {
            if (!m_effects.ContainsKey(id))
                m_effects.Add(id, new GenericEffect(id));
            return m_effects[id];
        }

        public bool HasEffect(EffectEnum id)
        {
            return m_effects.ContainsKey(id);
        }

        public bool RemoveEffect(EffectEnum id)
        {
            if (!m_effects.Remove(id))
                return false;

            StatisticsChanged();
            return true;
        }

        public void AddBase(EffectEnum id, int value)
        {
            if (!m_effects.ContainsKey(id))
                m_effects.Add(id, new GenericEffect(id));
            m_effects[id].Base += value;
        }

        public void AddDon(EffectEnum effectType, int value)
        {
            if (!m_effects.ContainsKey(effectType))
                m_effects.Add(effectType, new GenericEffect(effectType));
            m_effects[effectType].Dons += value;
        }

        public void AddBoosts(EffectEnum effectType, int value)
        {
            if (!m_effects.ContainsKey(effectType))
                m_effects.Add(effectType, new GenericEffect(effectType));
            m_effects[effectType].Boosts += value;
        }

        public void Merge(StatsType type, GenericStats Stats)
        {
            foreach (var effect in Stats.Effects.Except(Stats.WeaponEffects))
            {
                if (!m_effects.ContainsKey(effect.Key))
                    m_effects.Add(effect.Key, new GenericEffect(effect.Key));
                m_effects[effect.Key].Merge(type, effect.Value);
            }
        }

        public void UnMerge(StatsType type, GenericStats Stats)
        {
            foreach (var effect in Stats.Effects.Except(Stats.WeaponEffects))
            {
                if (!m_effects.ContainsKey(effect.Key))
                    m_effects.Add(effect.Key, new GenericEffect(effect.Key));
                m_effects[effect.Key].UnMerge(type, effect.Value);
            }
        }

        public void Merge(GenericStats Stats)
        {
            foreach (var effect in Stats.Effects)
            {
                if (!m_effects.ContainsKey(effect.Key))
                    m_effects.Add(effect.Key, new GenericEffect(effect.Key));
                m_effects[effect.Key].Merge(effect.Value);
            }
        }

        public void UnMerge(GenericStats Stats)
        {
            foreach (var effect in Stats.Effects)
            {
                if (!m_effects.ContainsKey(effect.Key))
                    m_effects.Add(effect.Key, new GenericEffect(effect.Key));
                m_effects[effect.Key].UnMerge(effect.Value);
            }
        }

        public void ClearDons()
        {
            foreach (var effect in m_effects)
                effect.Value.Dons = 0;
        }

        public void ClearBoosts()
        {
            foreach (var effect in m_effects)
                effect.Value.Boosts = 0;
        }

        /// <summary>
        /// Captura los buckets que el combate puede mutar (Boosts y Dons) para restaurarlos al
        /// terminar. Necesario porque los buffs de combate y los boosts de objetos/caramelos
        /// comparten el bucket Boosts, y las armaduras usan Dons: un ClearBoosts()/ClearDons()
        /// a ciegas corrompería los boosts legítimos o dejaría las armaduras permanentes.
        /// </summary>
        public Dictionary<EffectEnum, (int Boosts, int Dons)> CaptureCombatBuckets()
        {
            var snapshot = new Dictionary<EffectEnum, (int, int)>(m_effects.Count);
            foreach (var effect in m_effects)
                snapshot[effect.Key] = (effect.Value.Boosts, effect.Value.Dons);
            return snapshot;
        }

        public void RestoreCombatBuckets(Dictionary<EffectEnum, (int Boosts, int Dons)> snapshot)
        {
            if (snapshot == null)
            {
                ClearBoosts();
                ClearDons();
                return;
            }

            foreach (var effect in m_effects)
            {
                if (snapshot.TryGetValue(effect.Key, out var saved))
                {
                    effect.Value.Boosts = saved.Boosts;
                    effect.Value.Dons = saved.Dons;
                }
                else
                {
                    // Efecto añadido durante el combate (un buff sobre una stat que no existía):
                    // no tenía Boosts/Dons antes del combate.
                    effect.Value.Boosts = 0;
                    effect.Value.Dons = 0;
                }
            }
        }

        public void StatisticsChanged()
        {
            m_serialized = null;
        }

        [ProtoIgnore]
        private string m_serialized;

        public string ToItemStats()
        {
            if (m_serialized == null)
            {
                var serialized = new StringBuilder();
                if (Effects.Count > 0)
                {
                    foreach (var effect in m_effects)
                        serialized.Append(effect.Value.ToItemString()).Append(',');
                    serialized.Remove(serialized.Length - 1, 1);
                }
                m_serialized = serialized.ToString();
            }
            return m_serialized;
        }

        public void Dispose()
        {
            m_effects.Clear();
            m_effects = null;
        }
    }
}
