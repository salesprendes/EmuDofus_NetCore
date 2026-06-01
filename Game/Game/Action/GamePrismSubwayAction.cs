using Game.Conquest;
using Game.Entity;
using Game.Network;

namespace Game.Action
{
    public sealed class GamePrismSubwayAction : AbstractGameAction
    {
        public override bool CanAbort => false;

        public CharacterEntity Character { get; private set; }
        public ConquestTerritory Territory { get; private set; }

        public GamePrismSubwayAction(CharacterEntity character, ConquestTerritory territory)
            : base(GameActionTypeEnum.PRISM_USE, character)
        {
            Character = character;
            Territory = territory;
        }

        public override void Start()
        {
            Character.Dispatch(WorldMessage.PRISM_CREATE(Character));
        }

        public override void Abort(params object[] args)
        {
            Character.Dispatch(WorldMessage.PRISM_LEAVE());
            base.Abort(args);
        }

        public override void Stop(params object[] args)
        {
            Character.Dispatch(WorldMessage.PRISM_LEAVE());
            base.Stop(args);
        }
    }
}
