using Game.Fight.AI.Action;

namespace Game.Fight.AI.Brain
{
    public abstract class AIBrain
    {
        public AIFighter Fighter
        {
            get;
            private set;
        }

        public AIAction CurrentAction
        {
            get;
            protected set;
        }

        protected AIBrain(AIFighter fighter)
        {
            Fighter = fighter;
        }

        public virtual void OnTurnStart()
        {            
        }

        public virtual void OnUpdate()
        {
            if(CurrentAction != null)
            {
                CurrentAction.Update();
                if (CurrentAction.State == AIActionState.FINISH)
                    CurrentAction = CurrentAction.NextAction;
            }
        }
    }
}


