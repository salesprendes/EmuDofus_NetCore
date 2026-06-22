namespace Game.Fight.AI.Core
{
    public sealed class AITurnBudget
    {
        public int MaxActions { get; set; }
        public int MaxSpellCasts { get; set; }
        public int MaxMovements { get; set; }
        public int MaxFailedActions { get; set; }
        public int ActionsUsed { get; private set; }
        public int SpellCastsUsed { get; private set; }
        public int MovementsUsed { get; private set; }
        public int FailedActions { get; private set; }

        public bool CanContinue
        {
            get
            {
                // Los topes por tipo (hechizos/movimientos) los aplica CanUse; aqui basta con el
                // limite global de acciones y de fallos para saber si queda algo por intentar.
                return ActionsUsed < MaxActions && FailedActions < MaxFailedActions;
            }
        }

        public AITurnBudget()
        {
            MaxActions = AITuning.MaxActionsPerTurn;
            MaxSpellCasts = AITuning.MaxSpellCastsPerTurn;
            MaxMovements = AITuning.MaxMovementsPerTurn;
            MaxFailedActions = AITuning.MaxFailedActionsPerTurn;
        }

        public bool CanUse(AIDecisionType type)
        {
            if (!CanContinue)
                return false;

            if (IsSpellDecision(type) && SpellCastsUsed >= MaxSpellCasts)
                return false;

            if (type == AIDecisionType.Move && MovementsUsed >= MaxMovements)
                return false;

            return true;
        }

        public void UseAction(AIDecisionType type)
        {
            ActionsUsed++;

            if (IsSpellDecision(type))
                SpellCastsUsed++;

            if (type == AIDecisionType.Move)
                MovementsUsed++;
        }

        public void FailAction()
        {
            FailedActions++;
        }

        public static bool IsSpellDecision(AIDecisionType type)
        {
            return type == AIDecisionType.CastSpell || type == AIDecisionType.Heal || type == AIDecisionType.Buff || type == AIDecisionType.Debuff || type == AIDecisionType.Summon;
        }
    }
}
