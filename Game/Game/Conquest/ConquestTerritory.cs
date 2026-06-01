using Game.Database.Repository;
using Game.Database.Structure;
using Game.Entity;
using Game.Fight;
using System;

namespace Game.Conquest
{
    public enum ConquestTerritoryStateEnum
    {
        STATE_NORMAL = 0,
        STATE_UNDER_ATTACK = 1,
        STATE_VULNERABLE = 2,
        STATE_NEW = 3,
        STATE_DOOR_OPEN = 4,
        STATE_PRISM_ROOM_OPEN = 5,
    }

    public sealed class ConquestTerritory
    {
        private ConquestTerritoryDAO m_record;
        private AbstractFight m_currentFight;
        private bool m_isPersisted;

        public int SubAreaId => m_record.SubAreaId;
        public int AlignmentId => m_record.AlignmentId;
        public int BonusType => m_record.BonusType;
        public int Life => m_record.Life;
        public int MaxLife => m_record.MaxLife;
        public ConquestTerritoryStateEnum State => (ConquestTerritoryStateEnum)m_record.State;
        public int PrismMapId => m_record.PrismMapId;
        public int PrismCellId => m_record.PrismCellId;
        public int PrismLevel => m_record.PrismLevel <= 0 ? ConquestPrismEntity.DefaultLevel : m_record.PrismLevel;
        public int PrismHonor => m_record.PrismHonor;
        public ConquestPrismType PrismType => (ConquestPrismType)m_record.PrismType;

        public AbstractFight CurrentFight => m_currentFight;

        public bool IsUnderAttack => m_currentFight != null;
        public bool IsNeutral => m_record.AlignmentId <= 0;
        public bool IsPersisted => m_isPersisted;

        // persisted=false: temporary neutral territory created for a fight; persisted to DB on capture.
        public ConquestTerritory(ConquestTerritoryDAO record, bool persisted = true)
        {
            m_record = record;
            m_isPersisted = persisted;
            EnsurePrismDefaults();
        }

        public bool CanAttack(CharacterEntity character)
        {
            if (character == null)
                return false;
            if (m_currentFight != null)
                return false;
            if (character.AlignmentId <= 0)
                return false;
            if (!character.AlignmentEnabled)
                return false;
            if (character.AlignmentId == m_record.AlignmentId)
                return false;
            return true;
        }

        public void SetFight(AbstractFight fight)
        {
            m_currentFight = fight;
            m_record.State = fight != null
                ? (int)ConquestTerritoryStateEnum.STATE_UNDER_ATTACK
                : (int)ConquestTerritoryStateEnum.STATE_NORMAL;
        }

        public void SetVulnerable()
        {
            if (m_currentFight != null)
                return;

            m_record.State = (int)ConquestTerritoryStateEnum.STATE_VULNERABLE;
        }

        public void TakeDamage(int damage)
        {
            m_record.Life = Math.Max(0, m_record.Life - damage);
        }

        public void Restore()
        {
            m_record.Life = m_record.MaxLife;
        }

        public void SetPrismPosition(int mapId, int cellId)
        {
            m_record.PrismMapId = mapId;
            m_record.PrismCellId = cellId;
        }

        public void SetPrismType(ConquestPrismType type)
        {
            m_record.PrismType = (int)type;
        }

        public void SetPrismHonor(int honor)
        {
            m_record.PrismHonor = Math.Max(0, honor);
        }

        public void SetPrismLevel(int level)
        {
            m_record.PrismLevel = Math.Max(ConquestPrismEntity.DefaultLevel, level);
            m_record.MaxLife = ConquestPrismEntity.GetMaxLifeForLevel(m_record.PrismLevel);
            m_record.Life = Math.Min(Math.Max(1, m_record.Life), m_record.MaxLife);
        }

        public void Capture(CharacterEntity character)
        {
            m_record.AlignmentId = character.AlignmentId;
            m_record.Life = m_record.MaxLife;
            m_record.State = (int)ConquestTerritoryStateEnum.STATE_NORMAL;
            m_currentFight = null;

            if (!m_isPersisted)
            {
                ConquestTerritoryRepository.Instance.Add(m_record);
                m_isPersisted = true;
            }
        }

        public void Destroy()
        {
            m_currentFight = null;
            m_record.State = (int)ConquestTerritoryStateEnum.STATE_NORMAL;
            if (m_isPersisted)
                ConquestTerritoryRepository.Instance.Remove(m_record);
        }

        private void EnsurePrismDefaults()
        {
            if (m_record.PrismLevel <= 0)
                m_record.PrismLevel = ConquestPrismEntity.DefaultLevel;

            var maxLife = ConquestPrismEntity.GetMaxLifeForLevel(m_record.PrismLevel);
            if (m_record.MaxLife <= 0 || m_record.MaxLife == 3000)
                m_record.MaxLife = maxLife;

            if (m_record.Life <= 0 || m_record.Life > m_record.MaxLife)
                m_record.Life = m_record.MaxLife;
        }
    }
}
