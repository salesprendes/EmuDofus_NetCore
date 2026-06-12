using System;

namespace Game.Fight
{
    public enum FightObstacleTypeEnum
    {
        TYPE_FIGHTER,
        TYPE_TRAP,
        TYPE_GLYPH,
    }

    public interface IFightObstacle : IComparable<IFightObstacle>
    {
        FightObstacleTypeEnum ObstacleType
        {
            get;
        }

        int Priority
        {
            get;
        }

        bool CanGoThrough
        {
            get;
        }

        bool CanStack
        {
            get;
        }

        FightCell Cell
        {
            get;
        }
    }
}


