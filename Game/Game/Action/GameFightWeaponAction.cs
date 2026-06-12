using Game.Fight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public sealed class GameFightWeaponAction : AbstractGameFightAction
    {
        public System.Action Callback
        {
            get;
            private set;
        }

        public int CellId
        {
            get;
            private set;
        }

        public GameFightWeaponAction(AbstractFighter fighter, int cellId, long duration, System.Action callback)
    : base(GameActionTypeEnum.FIGHT_WEAPON_USE, fighter, duration)
        {
            Callback = callback;
            CellId = cellId;
        }

        public override void Stop(params object[] args)
        {
            Callback();

            base.Stop(args);
        }

        public override string SerializeAs_GameAction()
        {
            return CellId.ToString();
        }
    }
}


