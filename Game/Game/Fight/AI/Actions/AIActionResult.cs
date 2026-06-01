namespace Game.Fight.AI.Actions
{
    public sealed class AIActionResult
    {
        public bool Success { get; set; }
        public int DelayMs { get; set; }
        public string Reason { get; set; }
        public bool ShouldEndTurn { get; set; }

        public static AIActionResult Ok(int delayMs, string reason = null)
        {
            return new AIActionResult
            {
                Success = true,
                DelayMs = delayMs,
                Reason = reason ?? string.Empty
            };
        }

        public static AIActionResult Fail(string reason)
        {
            return new AIActionResult
            {
                Success = false,
                DelayMs = 0,
                Reason = reason ?? string.Empty
            };
        }

        public static AIActionResult EndTurn(string reason)
        {
            return new AIActionResult
            {
                Success = true,
                DelayMs = 0,
                Reason = reason ?? string.Empty,
                ShouldEndTurn = true
            };
        }
    }
}
