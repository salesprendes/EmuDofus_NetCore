using Protocolo.Framework.Generic;
using Game.Database.Structure;
using Game.Manager;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Entity.Inventory;

namespace Game.Entity
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MonsterGroupEntity : AbstractEntity
    {
        /// <summary>
        /// 
        /// </summary>
        public override string Name => "MonsterGroup_" + Id;

        /// <summary>
        /// 
        /// </summary>
        public override int MapId
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public override int CellId
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public override int Level
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override int RealLife
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public override int BaseLife
        {
            get 
            { 
                throw new NotImplementedException(); 
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override int Restriction
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int AggressionRange
        {
            get;
            private set;
        }

        public int AlignmentId
        {
            get
            {
                var map = MapManager.Instance.GetById(MapId);
                if (map?.SubArea == null) return -1;
                return ConquestManager.Instance.GetMonsterGroupAlignment(map.SubArea.Id);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<MonsterEntity> Monsters => m_monsters;

        /// <summary>
        /// 
        /// </summary>
        public bool HasMonsters => m_monsters.Count > 0;

        /// <summary>
        /// 
        /// </summary>
        public int AgeBonus
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool Resurect
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        private StringBuilder m_serializedMapInformations;

        /// <summary>
        /// 
        /// </summary>
        private List<MonsterEntity> m_monsters;

        /// <summary>
        /// 
        /// </summary>
        private UpdatableTimer m_ageTimer;

        /// <summary>
        /// 
        /// </summary>
        private int m_nextMonsterId;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="mapId"></param>
        /// <param name="cellId"></param>
        /// <param name="grade"></param>
        public MonsterGroupEntity(long id, int mapId, int cellId)
            : base(EntityTypeEnum.TYPE_MONSTER_GROUP, id)
        {
            m_monsters = new List<MonsterEntity>();
            m_nextMonsterId = -1;

            Resurect = true;
            MapId = mapId;
            CellId = cellId;
            Orientation = GetRandomOrientation();

            Inventory = new EntityInventory(this, (int)EntityTypeEnum.TYPE_MONSTER_GROUP, Id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="mapId"></param>
        /// <param name="cellId"></param>
        /// <param name="monsters"></param>
        public MonsterGroupEntity(long id, int mapId, int cellId, IEnumerable<MonsterGradeDAO> monsters)
            : this(id, mapId, cellId)
        {
            Resurect = false;
            foreach(var grade in monsters)
                m_monsters.Add(new MonsterEntity(m_nextMonsterId--, grade));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public MonsterGroupEntity(long id, int mapId, int cellId, IEnumerable<MonsterSpawnDAO> monsters, int maxSize = 6)
            : this(id, mapId, cellId)
        {
            var size = 1;
            if (monsters.All(monster => monster.Probability == 1))
            {
                size = monsters.Count();
            }
            else
            {
                var rand = Util.Next(0, 100);
                if (rand < 10)
                    size = 1;
                else if (rand < 25)
                    size = 2;
                else if (rand < 50)
                    size = 3;
                else if (rand < 75)
                    size = 4;
                else if (rand < 90)
                    size = 5;
                else
                    size = 6;
            }

            if (size > maxSize)
                size = maxSize;

            if (monsters.Count() > 0)
            {
                while (m_monsters.Count < size)
                {
                    foreach (var spawn in monsters)
                    {
                        var chance = Util.Next(0, 100);
                        if (chance < spawn.Probability * 100)
                        {
                            m_monsters.Add(new MonsterEntity(m_nextMonsterId--, spawn.Grade));

                            if (m_monsters.Count == size)
                                break;
                        }
                    }
                }

                AggressionRange = m_monsters.Max(monster => monster.Grade.Template.AggressionRange);
            }

            base.AddTimer(m_ageTimer = new UpdatableTimer(1000 * WorldConfig.PVM_STAR_BONUS_PERCENT_SECONDS, UpdateAge));
        }

        /// <summary>
        ///
        /// </summary>
        public void UpdateAge()
        {
            if (AgeBonus > WorldConfig.PVM_MAX_STAR_BONUS - 2)
                base.RemoveTimer(m_ageTimer);
            AgeBonus++;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool CanBeMoved()
        {
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static int GetRandomOrientation()
        {
            switch (Util.Next(0, 4))
            {
                case 0:
                    return 1;

                case 1:
                    return 3;

                case 2:
                    return 5;

                default:
                    return 7;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        private static string SerializeMonsterGroupLook(MonsterEntity monster)
        {
            return SerializeMonsterGroupColors(monster) + ";0,0,0,0";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        private static string SerializeMonsterGroupGfx(MonsterEntity monster)
        {
            return monster.SkinBase + "^" + monster.SkinSizeBase;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        private static string SerializeMonsterGroupColors(MonsterEntity monster)
        {
            var rawColors = monster.Grade.Template.Colors ?? string.Empty;
            var colors = rawColors.Replace(';', ',').Split(',');

            return string.Join(",", new[]
            {
                GetMonsterGroupColor(colors, 0),
                GetMonsterGroupColor(colors, 1),
                GetMonsterGroupColor(colors, 2)
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="colors"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static string GetMonsterGroupColor(string[] colors, int index)
        {
            if (colors.Length <= index)
                return "-1";

            var color = colors[index].Trim();
            return color.Length == 0 ? "-1" : color;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="message"></param>
        public override void SerializeAs_GameMapInformations(OperatorEnum operation, StringBuilder message)
        {
            switch (operation)
            {
                case OperatorEnum.OPERATOR_REMOVE:
                    message.Append(Id);
                    break;

                case OperatorEnum.OPERATOR_ADD:
                case OperatorEnum.OPERATOR_REFRESH: 
                    // cell/orientation/bonus may change
                    message.Append(CellId).Append(";");
                    message.Append(Orientation).Append(';');
                    message.Append(AgeBonus).Append(';');
                    if (m_serializedMapInformations == null)
                    {
                        string mobIds = string.Join(",", m_monsters.Select(monster => monster.Grade.MonsterId.ToString()));
                        string mobGfxs = string.Join(",", m_monsters.Select(SerializeMonsterGroupGfx));
                        string mobLevels = string.Join(",", m_monsters.Select(monster => monster.Grade.Level.ToString()));
                        string mobColors = string.Join(";", m_monsters.Select(SerializeMonsterGroupLook));

                        m_serializedMapInformations = new StringBuilder();
                        m_serializedMapInformations.Append(Id).Append(";");
                        m_serializedMapInformations.Append(mobIds).Append(";");
                        m_serializedMapInformations.Append((int)EntityTypeEnum.TYPE_MONSTER_GROUP).Append(';');
                        m_serializedMapInformations.Append(mobGfxs).Append(";");
                        m_serializedMapInformations.Append(mobLevels).Append(";");
                        m_serializedMapInformations.Append(mobColors);
                    }
                    message.Append(m_serializedMapInformations.ToString());
                    break;
            }
        }
    }
}


