using Game.Database.Structure;
using Game.Entity;
using Game.Interactive;
using Game.Manager;

namespace Game.Map
{
    public sealed class MapCell
    {
        public int Id;
        public bool LineOfSight { get; }
        public int GroundLevel { get; }
        public bool Walkable { get; }
        public bool IsDestinationOnly { get; }
        public int Movement { get; }
        public InteractiveObject InteractiveObject { get; }
        public MapTriggerDAO Trigger { get; }

        public MapCell(MapInstance map, int id, byte[] data, MapTriggerDAO trigger = null)
        {
            Id = id;
            Trigger = trigger;

            bool active = (data[0] & 32) != 0;
            int movement = (data[2] & 56) >> 3;

            LineOfSight = (data[0] & 1) != 0;
            GroundLevel = data[1] & 15;
            Movement = active ? movement : 0;
            IsDestinationOnly = active && movement == 1;

            bool baseWalkable = active && movement > 0;
            bool layerObject2Interactive = (data[7] & 2) != 0;

            if (!layerObject2Interactive)
            {
                Walkable = baseWalkable;
                return;
            }

            int obj2Id = ((data[0] & 2) << 12) | ((data[7] & 1) << 12) | (data[8] << 6) | data[9];

            if (InteractiveObjectManager.Instance.Exists(obj2Id))
                InteractiveObject = InteractiveObjectManager.Instance.Generate(obj2Id, map, Id);

            Walkable = baseWalkable && InteractiveObject != null && InteractiveObject.CanWalkThrough;
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
