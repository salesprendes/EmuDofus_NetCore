using Game.Fight.Effect.Type;
using Game.Spell;
using Protocolo.Framework.Generic;
using System.Collections.Generic;

namespace Game.Fight.Effect
{
    public sealed class EffectManager : Singleton<EffectManager>
    {
        private Dictionary<EffectEnum, AbstractSpellEffect> m_effects;

        public EffectManager()
        {
            m_effects = new Dictionary<EffectEnum, AbstractSpellEffect>
            {
                { EffectEnum.SelfDamage, new SelfDamageEffect() },
                { EffectEnum.DamageEarth, new DamageEffect() },
                { EffectEnum.DamageNeutral, new DamageEffect() },
                { EffectEnum.DamageFire, new DamageEffect() },
                { EffectEnum.DamageWater, new DamageEffect() },
                { EffectEnum.DamageAir, new DamageEffect() },
                { EffectEnum.DamageLifeNeutral, new DamageLifePercentEffect(EffectEnum.DamageBrut) },
                { EffectEnum.DamageLifeAir, new DamageLifePercentEffect(EffectEnum.DamageAir) },
                { EffectEnum.DamageLifeEarth, new DamageLifePercentEffect(EffectEnum.DamageEarth) },
                { EffectEnum.DamageLifeFire, new DamageLifePercentEffect(EffectEnum.DamageFire) },
                { EffectEnum.DamageLifeWater, new DamageLifePercentEffect(EffectEnum.DamageWater) },
                { EffectEnum.DamageDropLife, new DropLifeEffect() },
                { EffectEnum.Punition, new PunishmentDamageEffect() },
                { EffectEnum.ReflectSpell, new ReflectSpellEffect() },
                { EffectEnum.LifeSteal, new PureLifeStealEffect() },
                { EffectEnum.DamagePerAP, new DamagePerAPEffect() },


                { EffectEnum.StealNeutral, new LifeStealEffect() },
                { EffectEnum.StealEarth, new LifeStealEffect() },
                { EffectEnum.StealFire, new LifeStealEffect() },
                { EffectEnum.StealWater, new LifeStealEffect() },
                { EffectEnum.StealAir, new LifeStealEffect() },


                { EffectEnum.Heal, new HealEffect() },


                { EffectEnum.Teleport, new TeleportEffect() },


                { EffectEnum.AddArmor, new ArmorEffect() },
                { EffectEnum.AddArmorBis, new ArmorEffect() },


                { EffectEnum.AddAP, new StatsEffect() },
                { EffectEnum.AddAPBis, new StatsEffect() },
                { EffectEnum.AddMP, new StatsEffect() },
                { EffectEnum.MPBonus, new StatsEffect() },
                { EffectEnum.SubAP, new StatsEffect() },
                { EffectEnum.SubMP, new StatsEffect() },
                { EffectEnum.SubAPDodgeable, new APDodgeSubstractEffect() },
                { EffectEnum.SubMPDodgeable, new MPDodgeSubstractEffect() },
                { EffectEnum.AddAPDodge, new StatsEffect() },
                { EffectEnum.AddMPDodge, new StatsEffect() },
                { EffectEnum.SubAPDodge, new StatsEffect() },
                { EffectEnum.SubMPDodge, new StatsEffect() },


                { EffectEnum.AddReduceDamagePhysic, new StatsEffect() },
                { EffectEnum.AddReduceDamageMagic, new StatsEffect() },
                { EffectEnum.AddPO, new StatsEffect() },
                { EffectEnum.SubPO, new StatsEffect() },
                { EffectEnum.AddStrength, new StatsEffect() },
                { EffectEnum.AddIntelligence, new StatsEffect() },
                { EffectEnum.AddAgility, new StatsEffect() },
                { EffectEnum.AddChance, new StatsEffect() },
                { EffectEnum.AddWisdom, new StatsEffect() },
                { EffectEnum.AddLife, new StatsEffect() },
                { EffectEnum.AddVitality, new StatsEffect() },
                { EffectEnum.SubStrength, new StatsEffect() },
                { EffectEnum.SubIntelligence, new StatsEffect() },
                { EffectEnum.SubAgility, new StatsEffect() },
                { EffectEnum.SubChance, new StatsEffect() },
                { EffectEnum.SubWisdom, new StatsEffect() },
                { EffectEnum.SubVitality, new StatsEffect() },
                { EffectEnum.AddInvocationMax, new StatsEffect() },
                { EffectEnum.AddProspection, new StatsEffect() },


                { EffectEnum.AddHealCare, new StatsEffect() },
                { EffectEnum.SubHealCare, new StatsEffect() },


                { EffectEnum.AddReduceDamageAir, new StatsEffect() },
                { EffectEnum.AddReduceDamageWater, new StatsEffect() },
                { EffectEnum.AddReduceDamageFire, new StatsEffect() },
                { EffectEnum.AddReduceDamageNeutral, new StatsEffect() },
                { EffectEnum.AddReduceDamageEarth, new StatsEffect() },
                { EffectEnum.SubReduceDamageAir, new StatsEffect() },
                { EffectEnum.SubReduceDamageWater, new StatsEffect() },
                { EffectEnum.SubReduceDamageFire, new StatsEffect() },
                { EffectEnum.SubReduceDamageNeutral, new StatsEffect() },
                { EffectEnum.SubReduceDamageEarth, new StatsEffect() },
                { EffectEnum.AddReduceDamagePercentAir, new StatsEffect() },
                { EffectEnum.AddReduceDamagePercentWater, new StatsEffect() },
                { EffectEnum.AddReduceDamagePercentFire, new StatsEffect() },
                { EffectEnum.AddReduceDamagePercentNeutral, new StatsEffect() },
                { EffectEnum.AddReduceDamagePercentEarth, new StatsEffect() },
                { EffectEnum.SubReduceDamagePercentAir, new StatsEffect() },
                { EffectEnum.SubReduceDamagePercentWater, new StatsEffect() },
                { EffectEnum.SubReduceDamagePercentFire, new StatsEffect() },
                { EffectEnum.SubReduceDamagePercentNeutral, new StatsEffect() },
                { EffectEnum.SubReduceDamagePercentEarth, new StatsEffect() },


                { EffectEnum.AddDamage, new StatsEffect() },
                { EffectEnum.AddDamagePhysic, new StatsEffect() },
                { EffectEnum.AddDamageMagic, new StatsEffect() },
                { EffectEnum.AddEchecCritic, new StatsEffect() },
                { EffectEnum.AddDamageCritic, new StatsEffect() },
                { EffectEnum.AddDamagePercent, new StatsEffect() },
                { EffectEnum.SubDamagePercent, new StatsEffect() },
                { EffectEnum.SubDamage, new StatsEffect() },
                { EffectEnum.SubDamageCritic, new StatsEffect() },
                { EffectEnum.SubDamageMagic, new StatsEffect() },
                { EffectEnum.SubDamagePhysic, new StatsEffect() },
                { EffectEnum.AddReflectDamage, new StatsEffect() },
                { EffectEnum.AddReflectDamageItem, new StatsEffect() },


                { EffectEnum.AddChatiment, new PunishmentEffect() },


                { EffectEnum.PushBack, new PushEffect() },
                { EffectEnum.PushFront, new PushEffect() },
                { EffectEnum.PushFear, new PushFearEffect() },


                { EffectEnum.ChangeSkin, new SkinChangeEffect() },
                { EffectEnum.AddState, new StateAddEffect() },
                { EffectEnum.RemoveState, new StateRemoveEffect() },
                { EffectEnum.Stealth, new StateAddEffect() },


                { EffectEnum.StrengthSteal, new StatsStealEffect() },
                { EffectEnum.WisdomSteal, new StatsStealEffect() },
                { EffectEnum.IntelligenceSteal, new StatsStealEffect() },
                { EffectEnum.AgilitySteal, new StatsStealEffect() },
                { EffectEnum.ChanceSteal, new StatsStealEffect() },
                { EffectEnum.VitalitySteal, new StatsStealEffect() },
                { EffectEnum.APSteal, new StatsStealEffect() },
                { EffectEnum.MPSteal, new StatsStealEffect() },
                { EffectEnum.POSteal, new StatsStealEffect() },


                { EffectEnum.EcaflipChance, new EcaflipChanceEffect() },
                { EffectEnum.Perception, new PerceptionEffect() },
                { EffectEnum.TurnPass, new TurnPassEffect() },
                { EffectEnum.MultiplyDamage, new MultiplyDamageEffect() },
                { EffectEnum.Mastery, new MasteryEffect() },


                { EffectEnum.Sacrifice, new SacrificeEffect() },
                { EffectEnum.Transpose, new TransposeEffect() },


                { EffectEnum.Evasion, new DamageDodgeEffect() },


                { EffectEnum.IncreaseSpellDamage, new IncreaseSpellJetEffect() },


                { EffectEnum.Invocation, new SummoningEffect() },
                { EffectEnum.InvocDouble, new SummoningEffect() },
                { EffectEnum.InvocationStatic, new SummoningEffect(true) },


                { EffectEnum.DeleteAllBonus, new BuffRemoveEffect() },


                { EffectEnum.PandaCarrier, new PandaCarrierEffect() },
                { EffectEnum.PandaLaunch, new PandaLaunchEffect() },


                { EffectEnum.UseGlyph, new ActivableObjectEffect() },
                { EffectEnum.UseTrap, new ActivableObjectEffect() }
            };
        }

        public FightActionResultEnum TryApplyEffect(CastInfos castInfos)
        {
            if (!m_effects.ContainsKey(castInfos.EffectType))
            {
                Logger.Debug("EffectManager::TryApplyEffect efecto desconocido: " + castInfos.EffectType);
                return FightActionResultEnum.RESULT_NOTHING;
            }
            return m_effects[castInfos.EffectType].ApplyEffect(castInfos);
        }
    }
}


