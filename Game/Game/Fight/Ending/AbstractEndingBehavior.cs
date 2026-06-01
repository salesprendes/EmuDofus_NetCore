using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Ending
{
    public abstract class AbstractEndingBehavior
    {
        public abstract void Execute(AbstractFight fight);
    }
}


