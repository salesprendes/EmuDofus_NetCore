using Game.Entity;
using Game.Interactive.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public sealed class GameHarvestAction : AbstractGameAction
    {
        public override bool CanAbort => true;

        public HarvestableResource HarvestableResource
        {
            get;
        }

        public GameHarvestAction(CharacterEntity character, HarvestableResource harvestableResource, int duration)
    : base(GameActionTypeEnum.SKILL_HARVEST, character, duration)
        {
            HarvestableResource = harvestableResource;
        }

        public override void Abort(params object[] args)
        {
            HarvestableResource.AbortHarvest();

            base.Abort(args);
        }

        public override string SerializeAs_GameAction()
        {
            return HarvestableResource.CellId + "," + Duration;
        }
    }
}


