using Game.Entity;
using Game.Fight;

namespace Game.Action
{
    public sealed class GameConquestDefenderAction : AbstractGameAction
    {
        public override bool CanAbort => true;

        public CharacterEntity Character { get; private set; }

        private ConquestFight m_fight;

        public GameConquestDefenderAction(CharacterEntity character, ConquestFight fight)
            : base(GameActionTypeEnum.PRISM_AGGRESSION, character)
        {
            Character = character;
            m_fight = fight;
        }

        public override void Stop(params object[] args)
        {
            var fight = m_fight;
            m_fight = null;
            fight?.AddMessage(() => fight.DefenderLeave(Character));
            base.Stop(args);
        }
    }
}
