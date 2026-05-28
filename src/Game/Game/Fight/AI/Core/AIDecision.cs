namespace Game.Fight.AI.Core
{
    public sealed class AIDecision
    {
        public AIDecisionType Type { get; set; }
        public AIDecisionPriority Priority { get; set; }
        public int Score { get; set; }
        public int? SpellId { get; set; }
        public long? TargetId { get; set; }
        public short? CellId { get; set; }
        public string Reason { get; set; }
        public bool IsValid { get; set; }

        public AIDecision()
        {
            IsValid = true;
            Priority = AIDecisionPriority.Normal;
            Reason = string.Empty;
        }

        public static AIDecision EndTurn(string reason)
        {
            return new AIDecision
            {
                Type = AIDecisionType.EndTurn,
                Priority = AIDecisionPriority.Low,
                Score = 1,
                Reason = reason ?? string.Empty
            };
        }

        public static AIDecision CastSpell(int spellId, int cellId, long targetId, int score, AIDecisionPriority priority, string reason)
        {
            return new AIDecision
            {
                Type = AIDecisionType.CastSpell,
                SpellId = spellId,
                CellId = (short)cellId,
                TargetId = targetId,
                Score = score,
                Priority = priority,
                Reason = reason ?? string.Empty
            };
        }

        public static AIDecision Move(int cellId, int score, AIDecisionPriority priority, string reason)
        {
            return new AIDecision
            {
                Type = AIDecisionType.Move,
                CellId = (short)cellId,
                Score = score,
                Priority = priority,
                Reason = reason ?? string.Empty
            };
        }
    }
}
