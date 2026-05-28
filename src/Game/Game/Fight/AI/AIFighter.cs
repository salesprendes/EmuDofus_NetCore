using Game.Entity;
using Game.Fight.AI.Core;

namespace Game.Fight.AI
{
    public abstract class AIFighter : AbstractFighter
    {
        public override bool TurnReady
        {
            get;
            set;
        }

        public override bool TurnPass
        {
            get;
            set;
        }

        public AIBrain CurrentBrain
        {
            get;
            protected set;
        }

        protected AIFighter(EntityTypeEnum type, long id, bool staticInvocation = false) : base(type, id, staticInvocation)
        {
            CurrentBrain = AIBrainFactory.Create(this, AIProfile.Default);
        }

        public override bool CanBeMoved()
        {
            return true;
        }

        public void RefreshBrain()
        {
            var profile = AIProfileResolver.Resolve(this);
            CurrentBrain = AIBrainFactory.Create(this, profile);
        }

        protected void SetBrain(AIProfile profile)
        {
            CurrentBrain = AIBrainFactory.Create(this, profile);
        }

        public override void JoinFight(AbstractFight fight, FightTeam team)
        {
            Life = MaxLife;

            base.JoinFight(fight, team);
        }
    }
}


