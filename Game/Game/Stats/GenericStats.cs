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
            {EffectEnum.AddInitiative, new List<EffectEnum>() { EffectEnum.SubInitiative }},
            {EffectEnum.AddAP, new List<EffectEnum>() { EffectEnum.SubAP,  EffectEnum.SubAPDodgeable }},
            {EffectEnum.AddMP, new List<EffectEnum>() { EffectEnum.SubMP, EffectEnum.SubMPDodgeable }},
            {EffectEnum.AddPO, new List<EffectEnum>() { EffectEnum.SubPO }},
            {EffectEnum.AddHealCare, new List<EffectEnum>() { EffectEnum.SubHealCare }},
            {EffectEnum.AddProspection, new List<EffectEnum>() { EffectEnum.SubProspection }},
            {EffectEnum.AddPods, new List<EffectEnum>() { EffectEnum.SubPods }},
            {EffectEnum.AddVitality, new List<EffectEnum>() { EffectEnum.SubVitality }},
            {EffectEnum.AddWisdom, new List<EffectEnum>() { EffectEnum.SubWisdom }},
            {EffectEnum.AddStrength, new List<EffectEnum>() { EffectEnum.SubStrength }},
            {EffectEnum.AddIntelligence, new List<EffectEnum>() { EffectEnum.SubIntelligence }},
            {EffectEnum.AddAgility, new List<EffectEnum>() { EffectEnum.SubAgility }},
            {EffectEnum.AddChance, new List<EffectEnum>() { EffectEnum.SubChance }},

            {EffectEnum.AddDamage, new List<EffectEnum>() { EffectEnum.SubDamage }},
            {EffectEnum.AddDamagePercent, new List<EffectEnum>() { EffectEnum.SubDamagePercent }},
            {EffectEnum.AddDamageCritic, new List<EffectEnum>() { EffectEnum.SubDamageCritic }},
            {EffectEnum.AddDamageMagic, new List<EffectEnum>() { EffectEnum.SubDamageMagic }},
            {EffectEnum.AddDamagePhysic, new List<EffectEnum>() { EffectEnum.SubDamagePhysic }},
            {EffectEnum.AddAPDodge, new List<EffectEnum>() { EffectEnum.SubAPDodgeable }},
            {EffectEnum.AddMPDodge, new List<EffectEnum>() { EffectEnum.SubMPDodgeable }},

            {EffectEnum.AddReduceDamageAir, new List<EffectEnum>() { EffectEnum.SubReduceDamageAir }},
            {EffectEnum.AddReduceDamageWater, new List<EffectEnum>() { EffectEnum.SubReduceDamageWater }},
            {EffectEnum.AddReduceDamageFire, new List<EffectEnum>() { EffectEnum.SubReduceDamageFire }},
            {EffectEnum.AddReduceDamageNeutral, new List<EffectEnum>() { EffectEnum.SubReduceDamageNeutral }},
            {EffectEnum.AddReduceDamageEarth, new List<EffectEnum>() { EffectEnum.SubReduceDamageEarth }},

            {EffectEnum.AddReduceDamagePercentAir, new List<EffectEnum>() { EffectEnum.SubReduceDamagePercentAir }},
            {EffectEnum.AddReduceDamagePercentWater, new List<EffectEnum>() { EffectEnum.SubReduceDamagePercentWater }},
            {EffectEnum.AddReduceDamagePercentFire, new List<EffectEnum>() { EffectEnum.SubReduceDamagePercentFire }},
            {EffectEnum.AddReduceDamagePercentNeutral, new List<EffectEnum>() { EffectEnum.SubReduceDamagePercentNeutral }},
            {EffectEnum.AddReduceDamagePercentEarth, new List<EffectEnum>() { EffectEnum.SubReduceDamagePercentEarth }},

            {EffectEnum.AddReduceDamagePercentPvPAir, new List<EffectEnum>() { EffectEnum.SubReduceDamagePercentPvPAir }},
            {EffectEnum.AddReduceDamagePercentPvPWater, new List<EffectEnum>() { EffectEnum.SubReduceDamagePercentPvPWater }},
            {EffectEnum.AddReduceDamagePercentPvPFire, new List<EffectEnum>() { EffectEnum.SubReduceDamagePercentPvPFire }},
            {EffectEnum.AddReduceDamagePercentPvPNeutral, new List<EffectEnum>() { EffectEnum.SubReduceDamagePercentPvpNeutral }},
            {EffectEnum.AddReduceDamagePercentPvPEarth, new List<EffectEnum>() { EffectEnum.SubReduceDamagePercentPvPEarth }},
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
            m_effects.Add(EffectEnum.AddAP, new GenericEffect(EffectEnum.AddAP, monster.AP));
            m_effects.Add(EffectEnum.AddMP, new GenericEffect(EffectEnum.AddMP, monster.MP));
            m_effects.Add(EffectEnum.AddInvocationMax, new GenericEffect(EffectEnum.AddInvocationMax, monster.MaxInvocation));
            m_effects.Add(EffectEnum.AddInitiative, new GenericEffect(EffectEnum.AddInitiative, monster.Initiative));
            m_effects.Add(EffectEnum.AddWisdom, new GenericEffect(EffectEnum.AddWisdom, monster.Wisdom));
            m_effects.Add(EffectEnum.AddStrength, new GenericEffect(EffectEnum.AddStrength, monster.Strenght));
            m_effects.Add(EffectEnum.AddIntelligence, new GenericEffect(EffectEnum.AddIntelligence, monster.Intelligence));
            m_effects.Add(EffectEnum.AddAgility, new GenericEffect(EffectEnum.AddAgility, monster.Agility));
            m_effects.Add(EffectEnum.AddChance, new GenericEffect(EffectEnum.AddChance, monster.Chance));

            m_effects.Add(EffectEnum.AddReduceDamagePercentNeutral, new GenericEffect(EffectEnum.AddReduceDamagePercentNeutral, monster.NeutralResistance));
            m_effects.Add(EffectEnum.AddReduceDamagePercentEarth, new GenericEffect(EffectEnum.AddReduceDamagePercentEarth, monster.EarthResistance));
            m_effects.Add(EffectEnum.AddReduceDamagePercentFire, new GenericEffect(EffectEnum.AddReduceDamagePercentFire, monster.FireResistance));
            m_effects.Add(EffectEnum.AddReduceDamagePercentWater, new GenericEffect(EffectEnum.AddReduceDamagePercentWater, monster.WaterResistance));
            m_effects.Add(EffectEnum.AddReduceDamagePercentAir, new GenericEffect(EffectEnum.AddReduceDamagePercentAir, monster.AirResistance));

            m_effects.Add(EffectEnum.AddAPDodge, new GenericEffect(EffectEnum.AddAPDodge, monster.APDodgePercent));
            m_effects.Add(EffectEnum.AddMPDodge, new GenericEffect(EffectEnum.AddMPDodge, monster.MPDodgePercent));
        }

        public GenericStats(GuildDAO guild)
        {
            m_effects.Add(EffectEnum.AddAP, new GenericEffect(EffectEnum.AddAP, 6));
            m_effects.Add(EffectEnum.AddMP, new GenericEffect(EffectEnum.AddMP, 5));
            m_effects.Add(EffectEnum.AddProspection, new GenericEffect(EffectEnum.AddProspection, 100));
            m_effects.Add(EffectEnum.AddPods, new GenericEffect(EffectEnum.AddPods, 1000));
            m_effects.Add(EffectEnum.AddInitiative, new GenericEffect(EffectEnum.AddInitiative, 100));
            m_effects.Add(EffectEnum.AddVitality, new GenericEffect(EffectEnum.AddVitality, 100 * guild.Level));
            m_effects.Add(EffectEnum.AddWisdom, new GenericEffect(EffectEnum.AddWisdom, guild.Level * 4));
            m_effects.Add(EffectEnum.AddStrength, new GenericEffect(EffectEnum.AddStrength, guild.Level));
            m_effects.Add(EffectEnum.AddIntelligence, new GenericEffect(EffectEnum.AddIntelligence, guild.Level));
            m_effects.Add(EffectEnum.AddAgility, new GenericEffect(EffectEnum.AddAgility, guild.Level));
            m_effects.Add(EffectEnum.AddChance, new GenericEffect(EffectEnum.AddChance, guild.Level));
            m_effects.Add(EffectEnum.AddDamage, new GenericEffect(EffectEnum.AddDamage, guild.Level));
            m_effects.Add(EffectEnum.AddReduceDamagePercentAir, new GenericEffect(EffectEnum.AddReduceDamagePercentAir, guild.Level / 2));
            m_effects.Add(EffectEnum.AddReduceDamagePercentWater, new GenericEffect(EffectEnum.AddReduceDamagePercentWater, guild.Level / 2));
            m_effects.Add(EffectEnum.AddReduceDamagePercentFire, new GenericEffect(EffectEnum.AddReduceDamagePercentFire, guild.Level / 2));
            m_effects.Add(EffectEnum.AddReduceDamagePercentEarth, new GenericEffect(EffectEnum.AddReduceDamagePercentEarth, guild.Level / 2));
            m_effects.Add(EffectEnum.AddReduceDamagePercentNeutral, new GenericEffect(EffectEnum.AddReduceDamagePercentNeutral, guild.Level / 2));
        }

        public GenericStats(CharacterDAO character)
        {
            m_effects.Add(EffectEnum.AddAP, new GenericEffect(EffectEnum.AddAP, character.Level >= 100 ? 7 : 6));
            m_effects.Add(EffectEnum.AddMP, new GenericEffect(EffectEnum.AddMP, 3));
            m_effects.Add(EffectEnum.AddProspection, new GenericEffect(EffectEnum.AddProspection, ((CharacterBreedEnum)character.Breed == CharacterBreedEnum.BREED_ENUTROF ? 120 : 100)));
            m_effects.Add(EffectEnum.AddPods, new GenericEffect(EffectEnum.AddPods, 1000));
            m_effects.Add(EffectEnum.AddInvocationMax, new GenericEffect(EffectEnum.AddInvocationMax, 1));
            m_effects.Add(EffectEnum.AddInitiative, new GenericEffect(EffectEnum.AddInitiative, 100));
            m_effects.Add(EffectEnum.AddVitality, new GenericEffect(EffectEnum.AddVitality, character.Vitality));
            m_effects.Add(EffectEnum.AddWisdom, new GenericEffect(EffectEnum.AddWisdom, character.Wisdom));
            m_effects.Add(EffectEnum.AddStrength, new GenericEffect(EffectEnum.AddStrength, character.Strength));
            m_effects.Add(EffectEnum.AddIntelligence, new GenericEffect(EffectEnum.AddIntelligence, character.Intelligence));
            m_effects.Add(EffectEnum.AddAgility, new GenericEffect(EffectEnum.AddAgility, character.Agility));
            m_effects.Add(EffectEnum.AddChance, new GenericEffect(EffectEnum.AddChance, character.Chance));
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
                case EffectEnum.AddAPDodge:
                case EffectEnum.AddMPDodge:
                    totalBase += GetTotal(EffectEnum.AddWisdom) / 4;
                    break;
                case EffectEnum.AddAP:
                    totalItems += GetTotal(EffectEnum.AddAPBis);
                    break;
                case EffectEnum.AddMP:
                    totalItems += GetTotal(EffectEnum.MPBonus);
                    break;
                case EffectEnum.AddReflectDamage:
                    totalItems += GetTotal(EffectEnum.AddReflectDamageItem);
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
                case EffectEnum.AddAPDodge:
                case EffectEnum.AddMPDodge:
                    total += GetTotal(EffectEnum.AddWisdom) / 4;
                    break;

                case EffectEnum.AddAP:
                    total += GetTotal(EffectEnum.AddAPBis);
                    break;

                case EffectEnum.AddMP:
                    total += GetTotal(EffectEnum.MPBonus);
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
