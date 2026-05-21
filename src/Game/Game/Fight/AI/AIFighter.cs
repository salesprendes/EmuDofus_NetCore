using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entity;
using Game.Action;
using Game.Fight.AI.Brain;

namespace Game.Fight.AI
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class AIFighter : AbstractFighter
    {
        /// <summary>
        /// 
        /// </summary>
        public override bool TurnReady
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public override bool TurnPass
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public AIBrain CurrentBrain
        {
            get;
            protected set;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="staticInvocation"></param>
        protected AIFighter(EntityTypeEnum type, long id, bool staticInvocation = false) 
            : base(type, id, staticInvocation)
        {
            CurrentBrain = new DefaultAIBrain(this);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fight"></param>
        /// <param name="team"></param>
        public override void JoinFight(AbstractFight fight, FightTeam team)
        {
            Life = MaxLife;

            base.JoinFight(fight, team);
        }     
    }
}


