using Game.Entity;
using Game.Fight;
using Game.Map;

namespace Game.Action
{
    public sealed class GameFightMovementAction : AbstractGameFightAction
    {
        public MovementPath Path
        {
            get;
            private set;
        }

        public GameFightMovementAction(AbstractFighter entity, MovementPath path) : base(GameActionTypeEnum.MAP_MOVEMENT, entity, entity.Type == EntityTypeEnum.TYPE_CHARACTER ? 5000 : (long)path.MovementTime) => Path = path;


        public override void Stop(params object[] args)
        {
            Entity.MovementHandler.MovementFinish(Entity, Path, Path.EndCell);
            base.Stop(args);
        }

        public override string SerializeAs_GameAction() => Path.ToString();
    }
}


