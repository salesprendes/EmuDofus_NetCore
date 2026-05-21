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

            if ((data[7] & 2) >> 1 == 1)
            {
                int interactiveObjectId = ((data[0] & 2) << 12) + ((data[7] & 1) << 12) + (data[8] << 6) + data[9];
                if (InteractiveObjectManager.Instance.Exists(interactiveObjectId))
                    InteractiveObject = InteractiveObjectManager.Instance.Generate(interactiveObjectId, map, Id);

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
                ActionEffectManager.Instance.ApplyEffect(character, action.Effect, action.Parameters);
        }
    }
}
