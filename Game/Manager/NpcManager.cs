using System.Collections;
using System.Collections.Generic;
using Protocolo.Framework.Generic;
using Game.Action;
using Game.Database.Repository;
using Game.Database.Structure;

namespace Game.Manager
{
    public sealed class NpcManager : Singleton<NpcManager>
    {
        private readonly Dictionary<int, List<NpcInstanceDAO>> m_npcByMap;

        public NpcManager()
        {
            m_npcByMap = new Dictionary<int, List<NpcInstanceDAO>>();
        }

        public void Initialize()
        {
            long npcCount = 0;
            foreach (var npcInstance in NpcInstanceRepository.Instance.All)
            {
                if (!m_npcByMap.ContainsKey(npcInstance.MapId))
                    m_npcByMap.Add(npcInstance.MapId, new List<NpcInstanceDAO>());
                m_npcByMap[npcInstance.MapId].Add(npcInstance);
                npcCount++;
            }
            Logger.Info("NpcManager: " + npcCount + " NPC cargados.");
        }

        public List<NpcInstanceDAO> GetByMapId(int mapId)
        {
            if (m_npcByMap.ContainsKey(mapId))
                return m_npcByMap[mapId];
            return new List<NpcInstanceDAO>();
        }
    }
}


