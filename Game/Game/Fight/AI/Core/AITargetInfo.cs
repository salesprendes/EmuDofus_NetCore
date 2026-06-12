using Game.Fight;

namespace Game.Fight.AI.Core
{
    public sealed class AITargetInfo
    {
        public AbstractFighter Target { get; }

        public bool IsVisible { get; }

        public int CellId { get; }

        public int Distance { get; }

        public AITargetInfo(AbstractFighter target, int distance)
        {
            Target = target;
            Distance = distance;
            IsVisible = true;
            CellId = target?.Cell?.Id ?? -1;
        }
    }
}
