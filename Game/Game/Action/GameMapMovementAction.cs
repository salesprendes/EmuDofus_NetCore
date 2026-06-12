using Game.Entity;
using Game.Map;

namespace Game.Action
{
    public sealed class GameMapMovementAction : AbstractGameAction
    {
        public override bool CanAbort => true;

        public MovementPath Path
        {
            get;
            private set;
        }

        public int SkillMapId
        {
            get;
            set;
        }

        public int SkillCellId
        {
            get;
            set;
        }

        public int SkillId
        {
            get;
            set;
        }

        public GameMapMovementAction(AbstractEntity entity, MovementPath path) : base(GameActionTypeEnum.MAP_MOVEMENT, entity, (long)path.MovementTime)
        {
            Path = path;
            SkillId = -1;
        }

        public override void Abort(params object[] args)
        {
            int stopCell = 0;
            if (args.Length > 0)
                stopCell = int.Parse(args[0].ToString());
            else
                stopCell = Entity.CellId;


            if (stopCell == Entity.Id)
                stopCell = Entity.CellId;

            base.Abort(args);

            Entity.MovementHandler.MovementFinish(Entity, Path, stopCell);
        }

        public override void Stop(params object[] args)
        {
            base.Stop(args);

            Entity.MovementHandler.MovementFinish(Entity, Path, Path.EndCell);











            if (SkillId != -1
                && Entity.MapId == SkillMapId
                && Entity.CellId == Path.EndCell
                && Entity is CharacterEntity character
                && character.CanGameAction(GameActionTypeEnum.SKILL_USE)
                && character.Map.IsInInteractiveSkillRange(character, Entity.CellId, SkillCellId, SkillId))
            {
                character.Map.InteractiveExecute(character, SkillCellId, SkillId);
            }
        }

        public override string SerializeAs_GameAction()
        {
            return Path.ToString();
        }
    }
}
