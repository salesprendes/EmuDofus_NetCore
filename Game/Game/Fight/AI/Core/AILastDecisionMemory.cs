using System.Collections.Generic;

namespace Game.Fight.AI.Core
{
    public sealed class AILastDecisionMemory
    {

        public int? LastUsefulSpellId { get; private set; }
        public long? LastTargetId { get; private set; }
        public short? LastCellId { get; private set; }
        public AIDecisionType? LastType { get; private set; }


        public int ConsecutiveSameSpell { get; private set; }
        public int ConsecutiveSameMove { get; private set; }


        private int m_retriesRemaining;


        private readonly Dictionary<AIDecisionType, int> m_typeUsageThisTurn = new Dictionary<AIDecisionType, int>();


        private const int MaxRetries = 2;
        private const int MaxSameSpell = 4;
        private const int MaxSameMove = 3;



        public void Record(AIDecision decision)
        {
            if (decision == null || !decision.IsValid || decision.Type == AIDecisionType.EndTurn)
                return;

            bool sameSpell = decision.SpellId.HasValue && decision.SpellId == LastUsefulSpellId;
            bool sameCell = decision.CellId.HasValue && decision.CellId == LastCellId;

            if (sameSpell)
                ConsecutiveSameSpell++;
            else
                ConsecutiveSameSpell = 0;

            if (decision.Type == AIDecisionType.Move && sameCell)
                ConsecutiveSameMove++;
            else if (decision.Type == AIDecisionType.Move)
                ConsecutiveSameMove = 0;

            LastUsefulSpellId = decision.SpellId;
            LastTargetId = decision.TargetId;
            LastCellId = decision.CellId;
            LastType = decision.Type;
            m_retriesRemaining = MaxRetries;
        }

        public void BeginTurn()
        {
            m_typeUsageThisTurn.Clear();
        }

        public void TrackUsage(AIDecisionType type)
        {
            if (!m_typeUsageThisTurn.ContainsKey(type))
                m_typeUsageThisTurn[type] = 0;
            m_typeUsageThisTurn[type]++;
        }

        public int GetContinuityBonus(AIDecision decision)
        {
            if (decision == null || m_retriesRemaining <= 0)
                return 0;

            int bonus = 0;


            if (decision.SpellId.HasValue && decision.SpellId == LastUsefulSpellId)
                bonus += 10;
            // Foco persistente: seguir castigando al mismo objetivo (ya herido) en vez de
            // repartir el daño entre varios enemigos sin matar a ninguno.
            if (decision.TargetId.HasValue && decision.TargetId == LastTargetId)
                bonus += 25;
            if (decision.CellId.HasValue && decision.CellId == LastCellId)
                bonus += 4;


            if (ConsecutiveSameSpell > MaxSameSpell && decision.SpellId.HasValue && decision.SpellId == LastUsefulSpellId)
            {
                bonus -= 30 * (ConsecutiveSameSpell - MaxSameSpell);
            }


            if (decision.Type == AIDecisionType.Move && ConsecutiveSameMove > MaxSameMove && decision.CellId == LastCellId)
            {
                bonus -= 40 * (ConsecutiveSameMove - MaxSameMove);
            }

            return bonus;
        }

        public int GetUsageCountThisTurn(AIDecisionType type)
        {
            return m_typeUsageThisTurn.TryGetValue(type, out var count) ? count : 0;
        }
    }
}
