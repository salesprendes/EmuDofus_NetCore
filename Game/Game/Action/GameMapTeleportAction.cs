using Game.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public sealed class GameMapTeleportAction : AbstractGameAction
    {
        public override bool CanAbort => false;

        public int MapId
        {
            get;
            private set;
        }

        public int CellId
        {
            get;
            private set;
        }

        public GameMapTeleportAction(AbstractEntity entity, int mapId, int cellId)
    : base(GameActionTypeEnum.MAP_TELEPORT, entity)
        {
            MapId = mapId;
            CellId = cellId;
        }

        public override void Stop(params object[] args)
        {
            Entity.MapId = MapId;
            Entity.CellId = CellId;

            base.Stop(args);
        }
    }
}


