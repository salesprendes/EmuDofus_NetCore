using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Map
{
    public enum FieldTypeEnum
    {
        TYPE_MAP,
        TYPE_FIGHT,
    }

    public interface IMovementHandler
    {
        bool CanAbortMovement
        {
            get;
        }

        FieldTypeEnum FieldType
        {
            get;
        }

        void Move(AbstractEntity entity, int cellId, string movementPath);
        void MovementFinish(AbstractEntity entity, MovementPath path, int cellId);
        void Dispatch(string message);
    }
}


