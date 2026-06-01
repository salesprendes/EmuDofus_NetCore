using Game.Fight.AI.Bosses;
using Game.Fight.AI.Dopeuls;
using Game.Fight.AI.Profiles;
using Game.Fight.AI.TaxCollectors;

namespace Game.Fight.AI.Core
{
    public static class AIBrainFactory
    {
        public static AIBrain Create(AIFighter fighter, AIProfile profile)
        {
            switch (profile)
            {
                case AIProfile.Passive:
                    return new PassiveBrain(fighter);
                case AIProfile.Aggressive:
                    return new AggressiveBrain(fighter);
                case AIProfile.Distance:
                    return new DistanceBrain(fighter);
                case AIProfile.Healer:
                    return new HealerBrain(fighter);
                case AIProfile.Summoner:
                    return new SummonerBrain(fighter);
                case AIProfile.Coward:
                    return new CowardBrain(fighter);

                case AIProfile.RoyalGobball:
                    return new RoyalGobballBrain(fighter);
                case AIProfile.RoyalBlop:
                    return new RoyalBlopBrain(fighter);
                case AIProfile.DragonPig:
                    return new DragonPigBrain(fighter);
                case AIProfile.SoftOak:
                    return new SoftOakBrain(fighter);
                case AIProfile.Kralamar:
                    return new KralamarBrain(fighter);
                case AIProfile.KralamarTentacle:
                    return new KralamarTentacleBrain(fighter);

                case AIProfile.TaxCollector:
                    return new TaxCollectorBrain(fighter);

                case AIProfile.DopeulPandawa:
                    return new DopeulPandawaBrain(fighter);
                case AIProfile.DopeulFeca:
                    return new DopeulFecaBrain(fighter);
                case AIProfile.DopeulSacrieur:
                    return new DopeulSacrieurBrain(fighter);
                case AIProfile.DopeulSadida:
                    return new DopeulSadidaBrain(fighter);
                case AIProfile.DopeulOsamodas:
                    return new DopeulOsamodasBrain(fighter);
                case AIProfile.DopeulEnutrof:
                    return new DopeulEnutrofBrain(fighter);
                case AIProfile.DopeulSram:
                    return new DopeulSramBrain(fighter);
                case AIProfile.DopeulXelor:
                    return new DopeulXelorBrain(fighter);
                case AIProfile.DopeulEcaflip:
                    return new DopeulEcaflipBrain(fighter);
                case AIProfile.DopeulEniripsa:
                    return new DopeulEniripsaBrain(fighter);
                case AIProfile.DopeulIop:
                    return new DopeulIopBrain(fighter);
                case AIProfile.DopeulCra:
                    return new DopeulCraBrain(fighter);

                case AIProfile.Default:
                case AIProfile.Boss:
                case AIProfile.Kimbo:
                case AIProfile.Rasboul:
                default:
                    return new DefaultBrain(fighter);
            }
        }
    }
}
