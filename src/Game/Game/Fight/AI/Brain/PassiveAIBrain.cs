using Game.Fight.AI.Action.Type;

namespace Game.Fight.AI.Brain
{
    /// <summary>
    /// Passive summon brain used by decoys such as the Sram double.
    /// It only tries to move into a blocking position, then ends the turn.
    /// </summary>
    public sealed class PassiveAIBrain : AIBrain
    {
        public PassiveAIBrain(AIFighter fighter) : base(fighter)
        {
        }

        public override void OnTurnStart()
        {
            var startDelay = new DelayAction(Fighter, WorldConfig.FIGHT_AI_START_DELAY);
            CurrentAction = startDelay;

            var tail = startDelay.LinkWith(new MoveAction(Fighter));
            tail = tail.LinkWith(new DelayAction(Fighter, WorldConfig.FIGHT_AI_THINK_DELAY));
            tail.LinkWith(new EndTurnAction(Fighter));
        }
    }
}


