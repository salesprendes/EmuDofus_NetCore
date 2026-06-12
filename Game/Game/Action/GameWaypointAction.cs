using Game.Entity;
using Game.Interactive.Type;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public sealed class GameWaypointAction : AbstractGameAction
    {
        public override bool CanAbort => false;

        public CharacterEntity Character
        {
            get;
            private set;
        }

        public Waypoint Waypoint
        {
            get;
            private set;
        }

        public GameWaypointAction(CharacterEntity character, Waypoint waypoint)
    : base(GameActionTypeEnum.WAYPOINT, character)
        {
            Character = character;
            Waypoint = waypoint;
        }

        public override void Start()
        {
            Character.Dispatch(WorldMessage.WAYPOINT_CREATE(Character, Waypoint.Map.SubArea.Area.SuperAreaId));
        }

        public override void Abort(params object[] args)
        {
            Character.Dispatch(WorldMessage.WAYPOINT_LEAVE());
            base.Abort(args);
        }

        public override void Stop(params object[] args)
        {
            Character.Dispatch(WorldMessage.WAYPOINT_LEAVE());
            base.Stop(args);
        }
    }
}


