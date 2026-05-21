using Game.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.AI.Action.Type
{
    public enum MoveStateEnum
    {
        STATE_CALCULATE_CELL,
        STATE_MOVE,
        STATE_MOVING,
    }

    public class MoveAction : AIAction
    {
        private MoveStateEnum MoveState
        {
            get;
            set;
        }

        private string StringPath
        {
            get;
            set;
        }

        private MovementPath Path
        {
            get;
            set;
        }

        private int CellId
        {
            get; 
            set;
        }

        private int RealCellId
        {
            get;
            set;
        }

        public MoveAction(AIFighter fighter) 
            : base(fighter)
        {
        }

        public override AIActionResult Initialize()
        {
            MoveState = MoveStateEnum.STATE_CALCULATE_CELL;

            return Fighter.MP > 0 ? AIActionResult.RUNNING : AIActionResult.FAILURE;
        }

        public override AIActionResult Execute()
        {
            switch (MoveState)
            {
                case MoveStateEnum.STATE_CALCULATE_CELL:
                    // Determine optimal movement based on spell ranges
                    var availableSpells = Fighter.SpellBook.GetSpells()
                        .Where(s => s.APCost <= Fighter.AP + Fighter.MaxAP)
                        .ToList();

                    // Collect all trap cells to avoid stepping on them
                    var trapCells = Fight.Cells.Values
                        .Where(c => c.HasObject(FightObstacleTypeEnum.TYPE_TRAP))
                        .Select(c => c.Id)
                        .ToHashSet();

                    var sortedEnemies = Fighter.Team.OpponentTeam.AliveFighters
                        .OrderBy(f => f.MaxLife > 0 ? (double)f.Life / f.MaxLife : 1.0);

                    foreach (var ennemy in sortedEnemies)
                    {
                        int currentDist = Pathfinding.GoalDistance(Map, Fighter.Cell.Id, ennemy.Cell.Id);

                        // Skip movement only if at least one spell actually reaches this enemy.
                        bool canAlreadyAttack = availableSpells
                            .Any(s => currentDist >= s.MinPO && currentDist <= s.MaxPO);

                        if (canAlreadyAttack)
                        {
                            StringPath = string.Empty;
                            break;
                        }

                        // Build obstacles including traps (AI avoids known traps)
                        var obstacles = Fighter.Fight.Obstacles
                            .Union(trapCells.Where(tc => tc != Fighter.Cell.Id))
                            .ToList();

                        StringPath = Fighter.Fight.Map.Pathmaker.FindPathAsString(Fighter.Cell.Id, ennemy.Cell.Id, false, Fighter.MP, obstacles);
                        if (StringPath == string.Empty)
                        {
                            // If can't avoid traps, try without trap avoidance
                            StringPath = Fighter.Fight.Map.Pathmaker.FindPathAsString(Fighter.Cell.Id, ennemy.Cell.Id, false, Fighter.MP, Fighter.Fight.Obstacles);
                        }

                        if (StringPath == string.Empty)
                            continue;

                        Path = Pathfinding.IsValidPath(Fighter.Fight, Fighter, Fighter.Cell.Id, StringPath);
                        if (Path != null)
                            break;
                    }

                    MoveState = MoveStateEnum.STATE_MOVE;

                    return AIActionResult.RUNNING;

                case MoveStateEnum.STATE_MOVE:

                    if (StringPath == null || StringPath == string.Empty || Path == null || Path.MovementLength == 0)
                        return AIActionResult.FAILURE;

                    CellId = Path.EndCell;
                    Timeout = GetMovementActionTime(Path.MovementTime);
                    Fighter.Fight.Move(Fighter, Fighter.Cell.Id, StringPath);

                    MoveState = MoveStateEnum.STATE_MOVING;

                    return AIActionResult.RUNNING;

                case MoveStateEnum.STATE_MOVING:

                    if (!Timedout)
                        return AIActionResult.RUNNING;  
  
                    if (Fighter.CurrentAction != null)
                        Fighter.CurrentAction.Stop();

                    if (!Fighter.IsFighterDead && Fighter.Fight.CurrentFighter == Fighter && Fighter.Cell.Id != CellId && Fighter.Fight.GetCell(CellId).CanWalk)
                        return Initialize();
                    
                    return AIActionResult.SUCCESS;

                default:
                    throw new Exception("AI movement action : invalid state.");
            }                       
        }
    }
}


