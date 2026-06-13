using Game.Entity;
using Game.Exchange;
using Game.Interactive.Type;
using Game.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public sealed class GameCraftPlanExchangeAction : AbstractGameExchangeAction
    {
        public GameCraftPlanExchangeAction(CharacterEntity character, CraftPlan plan, JobSkill skill) : base(new CraftPlanExchange(character, plan, skill), character)
        {
        }

        public override void Start()
        {
            Exchange.Create();
        }

        public override void Stop(params object[] args)
        {
            base.Leave(true);
            base.Stop(args);
        }

        public override void Abort(params object[] args)
        {
            base.Leave();
            base.Abort(args);
        }
    }
}


