using Protocolo.Framework.Generic;
using Game.Interactive.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Manager
{
    public sealed class WaypointManager : Singleton<WaypointManager>
    {
        private readonly Dictionary<int, Waypoint> m_waypointByMap;

        public WaypointManager()
        {
            m_waypointByMap = new Dictionary<int, Waypoint>();
        }

        public void AddWaypoint(int mapId, Waypoint zaap)
        {
            if (!m_waypointByMap.ContainsKey(mapId))
                m_waypointByMap.Add(mapId, zaap);
        }

        public IEnumerable<Waypoint> All()
        {
            return m_waypointByMap.Values;
        }

        public Waypoint GetByMapId(int mapId)
        {
            if (m_waypointByMap.ContainsKey(mapId))
                return m_waypointByMap[mapId];
            return null;
        }
    }
}


