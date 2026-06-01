using Game.Entity;
using Game.Exchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class GamePersonalShopExchangeAction : AbstractGameExchangeAction
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        public GamePersonalShopExchangeAction(CharacterEntity character)
            : base(new PersonalShopExchange(character), character, null)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Start()
        {
            Exchange.Create();
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public override void Stop(params object[] args)
        {
            base.Leave(true);
            base.Stop(args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public override void Abort(params object[] args)
        {
            base.Leave();
            base.Abort(args);
        }
    }
}


