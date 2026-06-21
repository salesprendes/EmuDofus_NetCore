using Game.Database.Repository;
using Game.Database.Structure;
using Game.Manager;
using Game.Mount;
using Game.Network;
using Game.Stats;
using System;
using System.Text;
using System.Threading;

namespace Game.Entity
{
    [Flags]
    public enum MountCapacityEnum
    {
        TIRELESS = 1,
        CARRIER = 2,
        REPRODUCTIVE = 4,
        WISE = 8,
        TOUGH = 16,
        INLOVE = 32,
        PRECOCIOUS = 64,
        GENETIC_PRONE = 128,
        CHAMELEON = 256,
    }

    public sealed class MountEntity : AbstractEntity
    {
        public const int MAX_REPRODUCTION = 10;
        public const int MAX_TIRED = 250;
        public const int MIN_SERENITY = -10000;
        public const int MAX_SERENITY = 10000;
        public const int MAX_STAMINA = 10000;
        public const int MAX_LOVE = 10000;
        public const int MAX_ENERGY = 2000;

        private static long NextId = -10000;

        public override int BaseLife
        {
            get;
        }

        public override int CellId
        {
            get;
            set;
        }

        public override int Level
        {
            get
            {
                return ExperienceManager.Instance.GetLevel(ExperienceTypeEnum.MOUNT, Experience);
            }
            set
            {
            }
        }

        public override int MapId
        {
            get
            {
                return m_record.PaddockId;
            }

            set
            {
                m_record.PaddockId = value;
            }
        }

        public override string Name => m_record.Name;

        public override int RealLife
        {
            get; set;
        }

        public override int Restriction
        {
            get; set;
        }

        public string Capacities => ",";

        public int MapEnergyCost
        {
            get
            {
                if (Tired >= 220 && Tired < 230)
                {
                    return 1;
                }

                if (Tired >= 230 && Tired < 240)
                {
                    return 2;
                }

                return 0;
            }
        }

        public int RideEnergyCost
        {
            get
            {
                if (Tired <= 170)
                {
                    return 4;
                }

                if (Tired <= 180)
                {
                    return 5;
                }

                if (Tired <= 200)
                {
                    return 6;
                }

                if (Tired <= 210)
                {
                    return 7;
                }

                if (Tired <= 220)
                {
                    return 8;
                }

                if (Tired <= 230)
                {
                    return 10;
                }

                if (Tired <= 240)
                {
                    return 12;
                }

                return 0;
            }
        }

        public int MaturityPercent => (int)((Maturity / (double)Template.MaxMaturity) * 100);
        public int Size => (int)Math.Round((double)(45 * (1 / (MaturityPercent + 1)) + MaturityPercent));


        public bool PregnancyTerminated => false;


        public string SerializedPregnancyTime => "-1";

        public bool Fecondable => !Pregnant && Love >= 7500 && Stamina >= 7500 && Reproduction < MAX_REPRODUCTION && Level >= 5;

        public bool Pregnant => m_fecondation != null;

        public bool Ridable => Maturity == Template.MaxMaturity && !Wild;

        public int XPSharePercent
        {
            get => m_record.XPSharePercent;
            set => m_record.XPSharePercent = value;
        }
        public void SetName(string name) => m_record.Name = name;
        public void SetCastrated() => m_record.Castrated = true;
        public void SetWild(bool value) => m_record.Wild = value;
        public void SetOwner(long ownerId) => m_record.OwnerId = ownerId;
        public long UniqueId => m_record.Id;
        public long OwnerId => m_record.OwnerId;
        public int Reproduction => m_record.Reproduction;
        public bool Castrated => m_record.Castrated;
        public int MaxEnergy => Template.DefaultEnergy + Template.EnergyPerLevel * Level;
        public int MaxPods => Template.DefaultPods + Template.PodsPerLevel * Level;
        public bool Wild => m_record.Wild;
        public int Tired => m_record.Tired;
        public long Energy => m_record.Energy;
        public long Stamina => m_record.Stamina;
        public long Maturity => m_record.Maturity;
        public long Love => m_record.Love;
        public long Serenity => m_record.Serenity;
        public MountCapacityEnum Capacity => (MountCapacityEnum)m_record.Capacity;

        public long ExperienceFloorNext => ExperienceManager.Instance.GetFloorNext(ExperienceTypeEnum.MOUNT, Experience);
        public long ExperienceFloorCurrent => ExperienceManager.Instance.GetFlootCurrent(ExperienceTypeEnum.MOUNT, Experience);
        public long Experience
        {
            get
            {
                return m_record.Experience;
            }
            set
            {
                m_record.Experience = value;
            }
        }

        public bool Sex => m_record.Sex;
        public int TemplateId => m_record.TemplateId;
        public MountTemplateDAO Template => m_record.Template;

        private MountDAO m_record;


        private Fecondation m_fecondation = null;

        public MountEntity(MountDAO record)
    : base(EntityTypeEnum.TYPE_MOUNT, Interlocked.Decrement(ref NextId))
        {
            m_record = record;
        }

        // La montura esta al nivel maximo cuando su experiencia alcanza el ultimo piso definido.
        public bool IsMaxLevel
        {
            get
            {
                var maxExperience = ExperienceTemplateRepository.Instance.GetMaxMountExperience();
                return maxExperience > 0 && Experience >= maxExperience;
            }
        }

        // Anade experiencia a la montura. El nivel se deriva de la experiencia, asi que se
        // limita al ultimo piso definido para no disparar un bucle infinito en GetLevel.
        public void AddExperience(long amount)
        {
            if (amount <= 0)
                return;

            var newExperience = Experience + amount;
            var maxExperience = ExperienceTemplateRepository.Instance.GetMaxMountExperience();
            if (maxExperience > 0 && newExperience > maxExperience)
                newExperience = maxExperience;

            Experience = newExperience;
        }

        public GenericStats GetStatistics()
        {
            var statistics = new GenericStats();
            foreach (var effect in Template.RandomEffects)
            {
                statistics.AddEffect(effect.Type, effect.Random * Level);
            }

            return statistics;
        }

        public bool HasCapacity(MountCapacityEnum capacity)
    => (Capacity & capacity) == capacity;

        public string SerializeAs_MountLightInfos()
        {
            if (HasCapacity(MountCapacityEnum.CHAMELEON))
            {
                return TemplateId.ToString() + ",-1,-1,-1";
            }
            else
            {
                return TemplateId.ToString();
            }
        }

        public string SerializeAs_MountInfos()
        {
            var message = new StringBuilder();
            message.Append(Id).Append(':');
            message.Append(TemplateId).Append(':');


            message.Append(string.Empty).Append(':');


            message.Append(Capacities).Append(':');

            message.Append(Name).Append(':');
            message.Append(Sex ? "1" : "0").Append(':');

            message.Append(Experience).Append(',').Append(ExperienceFloorCurrent).Append(',').Append(ExperienceFloorNext).Append(':');

            message.Append(Level).Append(':');
            message.Append(Ridable ? "1" : "0").Append(':');
            message.Append(MaxPods).Append(':');
            message.Append(Wild ? "1" : "0").Append(':');

            message.Append(Stamina).Append(',').Append(MAX_STAMINA).Append(':');

            message.Append(Maturity).Append(',').Append(Template.MaxMaturity).Append(':');

            message.Append(Energy).Append(',').Append(MAX_ENERGY).Append(':');

            message.Append(Serenity).Append(',').Append(MIN_SERENITY).Append(',').Append(MAX_SERENITY).Append(':');

            message.Append(Love).Append(',').Append(MAX_LOVE).Append(':');

            message.Append(SerializedPregnancyTime).Append(':');
            message.Append(Fecondable ? "1" : "0").Append(':');
            message.Append(GetStatistics().ToItemStats()).Append(':');

            message.Append(Tired).Append(',').Append(MAX_TIRED).Append(':');

            message.Append(Castrated ? "-1" : Reproduction.ToString()).Append(',').Append(MAX_REPRODUCTION).Append(':');

            return message.ToString();
        }

        public override void SerializeAs_GameMapInformations(OperatorEnum operation, StringBuilder message)
        {
            message.Append(CellId).Append(';');
            message.Append(Orientation).Append(';');
            message.Append(0).Append(';');
            message.Append(Id).Append(';');
            message.Append(Name).Append(';');
            message.Append("-9").Append(";");
            message.Append("7002").Append('^').Append(Size);
        }
    }
}


