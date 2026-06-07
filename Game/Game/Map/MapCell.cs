using Game.Database.Structure;
using Game.Entity;
using Game.Interactive;
using Game.Manager;

namespace Game.Map
{
    public sealed class MapCell
    {
        public int Id;
        public bool Walkable { get; }
        public bool LineOfSight;
        public short LayerObject1Num { get; }
        public short LayerObject2Num { get; }

        public InteractiveObject InteractiveObject
        {
            get;
        }

        public MapTriggerDAO Trigger
        {
            get;
        }

        public MapCell(MapInstance map, int id, byte[] data, MapTriggerDAO trigger = null)
        {
            Id = id;
            Trigger = trigger;

            bool walkable = ((data[2] & 56) >> 3) > 0;
            LineOfSight = (data[0] & 1) == 1;

            LayerObject1Num = (short)(((data[0] & 4) << 11) + ((data[4] & 1) << 12) + (data[5] << 6) + data[6]);
            LayerObject2Num = (short)(((data[0] & 2) << 12) + ((data[7] & 1) << 12) + (data[8] << 6) + data[9]);

            if ((data[7] & 2) >> 1 == 1)
            {
                int interactiveObjectId = LayerObject2Num;
                if (InteractiveObjectManager.Instance.Exists(interactiveObjectId))
                {
                    InteractiveObject = InteractiveObjectManager.Instance.Generate(interactiveObjectId, map, Id);
                }

                Walkable = walkable && InteractiveObject != null && InteractiveObject.CanWalkThrough;
            }
            else
            {
                Walkable = walkable;
            }
        }

        public bool SatisfyConditions(CharacterEntity character)
        {
            return Trigger.SatisfyConditions(character);
        }

        public void ApplyActions(CharacterEntity character)
        {
            foreach (var action in Trigger.ActionsList)
            {
                ActionEffectManager.Instance.ApplyEffect(character, action.Effect, action.Parameters);
            }
        }
    }
}
