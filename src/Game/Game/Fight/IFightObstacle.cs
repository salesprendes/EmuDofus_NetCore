using System;

namespace Game.Fight
{
    public enum FightObstacleTypeEnum
    {
        TYPE_FIGHTER,
        TYPE_TRAP,
        TYPE_GLYPH,
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IFightObstacle : IComparable<IFightObstacle>
    {
        /// <summary>
        /// 
        /// </summary>
        FightObstacleTypeEnum ObstacleType
        {
            get;
        }
        /// <summary>
        /// 
        /// </summary>
        int Priority
        {
            get;
        }
        /// <summary>
        /// 
        /// </summary>
        bool CanGoThrough
        {
            get;
        }
        /// <summary>
        /// 
        /// </summary>
        bool CanStack
        {
            get;
        }
        /// <summary>
        /// 
        /// </summary>
        FightCell Cell
        {
            get;
        }
    }
}


