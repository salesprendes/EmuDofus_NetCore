using Game.Entity;
using Game.Exchange;
using Game.Interactive.Type;
using Game.Job;

namespace Game.Action
{
    public sealed class AccionIntercambioForjamagia : AbstractGameExchangeAction
    {
        public AccionIntercambioForjamagia(CharacterEntity character, CraftPlan plan, JobSkill skill)
    : base(new IntercambioForjamagia(character, plan, skill), character)
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
