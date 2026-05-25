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

        public GameMapMovementAction(AbstractEntity entity, MovementPath path)
            : base(GameActionTypeEnum.MAP_MOVEMENT, entity, (long)path.MovementTime)
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

            // Cas d'une deconnexion
            if (stopCell == Entity.Id)
                stopCell = Entity.CellId;

            base.Abort(args);

            Entity.MovementHandler.MovementFinish(Entity, Path, stopCell);
        }

        public override void Stop(params object[] args)
        {
            base.Stop(args);

            Entity.MovementHandler.MovementFinish(Entity, Path, Path.EndCell);

            // Execute the queued interactive skill only when ALL of these hold:
            //  1. A skill was actually queued (SkillId != -1).
            //  2. The entity is still on the map where the skill was requested.
            //  3. MovementFinish placed the entity at the expected destination
            //     (it can return early on trigger-cell condition failure, leaving
            //     CellId at the origin — the previous code fired the skill anyway).
            //  4. The entity is a CharacterEntity (safe cast; avoids InvalidCastException
            //     if the action is ever reused for a non-character entity in the future).
            //  5. The character is in a state that allows interactive skill use
            //     (not in a fight after aggro, not tombstoned, no IO restriction).
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
