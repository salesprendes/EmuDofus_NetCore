using Game.Action;
using Game.Conquest;
using Game.Fight.AI;
using Game.Fight.AI.Core;
using Game.Manager;
using Game.Network;
using Game.Spell;
using Game.Stats;
using System;
using System.Text;

namespace Game.Entity
{
    public sealed class ConquestPrismEntity : AIFighter
    {
        public const int DefaultLevel = 1;

        private const int ALIGNMENT_BONTA = 1;
        private const int ALIGNMENT_BRAKMAR = 2;
        private const int BONTA_MONSTER_ID = 1111;
        private const int BRAKMAR_MONSTER_ID = 1112;
        private const int BONTA_GFX_ID = 8101;
        private const int BRAKMAR_GFX_ID = 8100;
        private const int VILLAGE_PRISM_GFX_ID = 1200;

        private static readonly int[] s_gradeHonorFloors =
        {
            0, 500, 1500, 3000, 5000, 7500, 10000, 12500, 15000, 18000
        };

        private int m_mapId;
        private int m_cellId;
        private int m_realLife;
        private int m_restriction;

        public ConquestTerritory Territory { get; private set; }

        public override int MapId
        {
            get { return Fight?.Map?.Id ?? m_mapId; }
            set { m_mapId = value; }
        }

        public override int CellId
        {
            get { return Cell?.Id ?? m_cellId; }
            set { m_cellId = value; }
        }

        public int MapCellId => m_cellId;

        public override string Name => MonsterId.ToString();

        public override int Level
        {
            get { return Math.Max(DefaultLevel, Territory?.PrismLevel ?? DefaultLevel); }
            set { }
        }

        public override int BaseLife => GetMaxLifeForLevel(Level);

        public override int RealLife
        {
            get { return m_realLife; }
            set { m_realLife = value; }
        }

        public override int Restriction
        {
            get { return m_restriction; }
            set { m_restriction = value; }
        }

        public override int SkinBase => GfxId;

        public override int SkinSizeBase => 100;

        public override bool CanDrop => false;

        public override int AlignmentId => Territory?.AlignmentId ?? 0;

        public override bool CanBeMoved()
        {
            return false;
        }

        public int Alignment => AlignmentId;

        public int MonsterId => AlignmentId == ALIGNMENT_BRAKMAR ? BRAKMAR_MONSTER_ID : BONTA_MONSTER_ID;

        public int GfxId => AlignmentId == ALIGNMENT_BRAKMAR ? BRAKMAR_GFX_ID : BONTA_GFX_ID;

        public int Grade => GetGradeFromHonor(Territory?.PrismHonor ?? 0);

        public ConquestPrismEntity(long id, ConquestTerritory territory, int mapId, int cellId)
            : base(EntityTypeEnum.TYPE_PRISM, id)
        {
            Territory = territory;
            m_mapId = mapId;
            m_cellId = cellId;
            Orientation = 1;

            Statistics = CreateStats(Level);
            SpellBook = new SpellBook((int)EntityTypeEnum.TYPE_MONSTER_FIGHTER, ((long)MonsterId << 32) | (uint)Grade);
            SetBrain(AIProfile.Default);

            Skin = SkinBase;
            SkinSize = SkinSizeBase;
            Life = Math.Min(Math.Max(1, territory?.Life ?? MaxLife), MaxLife);
        }

        public bool Represents(ConquestTerritory territory, int mapId, int cellId)
        {
            return territory != null
                && Territory != null
                && Territory.SubAreaId == territory.SubAreaId
                && mapId == m_mapId
                && cellId == m_cellId
                && AlignmentId == territory.AlignmentId
                && Level == territory.PrismLevel
                && Grade == GetGradeFromHonor(territory.PrismHonor);
        }

        public static int GetMaxLifeForLevel(int level)
        {
            return Math.Max(DefaultLevel, level) * 10000;
        }

        public static GenericStats CreateStats(int level)
        {
            level = Math.Max(DefaultLevel, level);
            var mainStat = 1000 + (500 * level);
            var resistance = 9 * level;
            var stats = new GenericStats();

            stats.AddEffect(EffectEnum.AddStrength, mainStat);
            stats.AddEffect(EffectEnum.AddIntelligence, mainStat);
            stats.AddEffect(EffectEnum.AddAgility, mainStat);
            stats.AddEffect(EffectEnum.AddWisdom, mainStat);
            stats.AddEffect(EffectEnum.AddChance, mainStat);
            stats.AddEffect(EffectEnum.AddReduceDamagePercentNeutral, resistance);
            stats.AddEffect(EffectEnum.AddReduceDamagePercentFire, resistance);
            stats.AddEffect(EffectEnum.AddReduceDamagePercentWater, resistance);
            stats.AddEffect(EffectEnum.AddReduceDamagePercentAir, resistance);
            stats.AddEffect(EffectEnum.AddReduceDamagePercentEarth, resistance);
            stats.AddEffect(EffectEnum.AddAPDodge, resistance);
            stats.AddEffect(EffectEnum.AddMPDodge, resistance);
            stats.AddEffect(EffectEnum.AddAP, 6);
            stats.AddEffect(EffectEnum.AddMP, 0);

            return stats;
        }

        public override void SerializeAs_GameMapInformations(OperatorEnum operation, StringBuilder message)
        {
            switch (operation)
            {
                case OperatorEnum.OPERATOR_REMOVE:
                    message.Append(Id);
                    break;

                case OperatorEnum.OPERATOR_ADD:
                case OperatorEnum.OPERATOR_REFRESH:
                    if (HasGameAction(GameActionTypeEnum.FIGHT))
                        SerializeFightActor(message);
                    else
                        SerializeMapActor(message);
                    break;
            }
        }

        public override void EndFight(bool win = false)
        {
            base.EndFight(win);

            if (win)
                StartAction(GameActionTypeEnum.MAP);
        }

        public override void Dispose()
        {
            Territory = null;
            base.Dispose();
        }

        // Village: GM|+268;5;38;-1;541;-3;1200^100;1;-1,-1,-1;0,0,0,0
        // SubArea: GM|+cellId;orientation;0;id;monsterId;-10;gfx^scale;level;grade;alignment
        private void SerializeMapActor(StringBuilder message)
        {
            message.Append(m_cellId).Append(';');

            if (Territory?.PrismType == ConquestPrismType.Village)
            {
                var alignment = Territory?.AlignmentId ?? 0;
                var templateId = ConquestManager.Instance.GetVillagePrismTemplateId(Territory?.SubAreaId ?? 0, alignment);
                message.Append(5).Append(';');
                message.Append(Id).Append(';');
                message.Append(-1).Append(';');
                message.Append(templateId).Append(';');
                message.Append(-3).Append(';');
                message.Append(VILLAGE_PRISM_GFX_ID).Append('^').Append(SkinSizeBase).Append(';');
                message.Append(alignment).Append(';');
                message.Append("-1,-1,-1").Append(';');
                message.Append("0,0,0,0");
            }
            else
            {
                message.Append(Orientation).Append(';');
                message.Append('0').Append(';');
                message.Append(Id).Append(';');
                message.Append(MonsterId).Append(';');
                message.Append((int)EntityTypeEnum.TYPE_PRISM).Append(';');
                message.Append(GfxId).Append('^').Append(SkinSizeBase).Append(';');
                message.Append(Level).Append(';');
                message.Append(Grade).Append(';');
                message.Append(AlignmentId);
            }
        }

        private void SerializeFightActor(StringBuilder message)
        {
            message.Append(Cell?.Id ?? CellId).Append(';');
            message.Append(Orientation).Append(';');
            message.Append('0').Append(';');
            message.Append(Id).Append(';');
            message.Append(Name).Append(';');
            message.Append((int)EntityTypeEnum.TYPE_PRISM).Append(';');
            message.Append(Skin).Append('^').Append(SkinSize).Append(';');
            message.Append(Level).Append(';');
            message.Append("-1;-1;-1;");
            message.Append("0,0,0,0;");
            message.Append(Life).Append(';');
            message.Append(AP).Append(';');
            message.Append(MP).Append(';');
            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentNeutral)).Append(';');
            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentEarth)).Append(';');
            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentFire)).Append(';');
            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentWater)).Append(';');
            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentAir)).Append(';');
            message.Append(Statistics.GetTotal(EffectEnum.AddAPDodge)).Append(';');
            message.Append(Statistics.GetTotal(EffectEnum.AddMPDodge)).Append(';');
            message.Append(Team?.Id ?? 1);
        }

        private static int GetGradeFromHonor(int honor)
        {
            for (int i = s_gradeHonorFloors.Length - 1; i >= 0; i--)
                if (honor >= s_gradeHonorFloors[i])
                    return i + 1;

            return 1;
        }
    }
}
