using System;
using System.Linq;
using System.Collections.Generic;
using Protocolo.Framework.Database;
using Game;
using Game.Network;
namespace Game.Database.Structure
{
    /// <summary>
    /// 
    /// </summary>
    [Table("maptemplate")]
    public sealed class MapTemplateDAO : DataAccessObject<MapTemplateDAO>
    {
        private int _id;
        private int _subAreaId;
        private int _x;
        private int _y;
        private int _width;
        private int _height;
        private string _data;
        private string _dataKey;
        private string _createTime;
        private string _places;
        private int _capabilities;


        /// <summary>
        /// 
        /// </summary>
        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int SubAreaId
        {
            get => _subAreaId;
            set => SetProperty(ref _subAreaId, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string Data
        {
            get => _data;
            set => SetProperty(ref _data, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string DataKey
        {
            get => _dataKey;
            set => SetProperty(ref _dataKey, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string CreateTime
        {
            get => _createTime;
            set => SetProperty(ref _createTime, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public string Places
        {
            get => _places;
            set => SetProperty(ref _places, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public int Capabilities
        {
            get => _capabilities;
            set => SetProperty(ref _capabilities, value);
        }

        private List<int> m_fightTeam0Cells, m_fightTeam1Cells;

        [Write(false)]
        public List<int> FightTeam0Cells
        {
            get
            {
                if(m_fightTeam0Cells == null)
                {
                    m_fightTeam0Cells = new List<int>();
                    if (Places != "")
                    {
                        var places = Places.Split('|')[0];
                        var length = places.Length / 2;
                        for (int i = 0; i < length; i++)
                            m_fightTeam0Cells.Add(Util.CharToCell(places.Substring(i * 2, 2)));
                    }
                }
                return m_fightTeam0Cells;
            }
        }

        [Write(false)]
        public List<int> FightTeam1Cells
        {
            get
            {
                if (m_fightTeam1Cells == null)
                {
                    m_fightTeam1Cells = new List<int>();
                    if (Places != "")
                    {
                        var places = Places.Split('|')[1];
                        var length = places.Length / 2;
                        for (int i = 0; i < length; i++)
                        {
                            m_fightTeam1Cells.Add(Util.CharToCell(places.Substring(i * 2, 2)));
                        }
                    }
                }
                return m_fightTeam1Cells;
            }
        }

        public override void OnBeforeUpdate()
        {
            Places = string.Join("", FightTeam0Cells.Select(x => Util.CellToChar(x))) + "|" + string.Join("", FightTeam1Cells.Select(x => Util.CellToChar(x)));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Id + " ( " + X + ", " + Y +  ", " + SubAreaId + " )";
        }
    }
}


