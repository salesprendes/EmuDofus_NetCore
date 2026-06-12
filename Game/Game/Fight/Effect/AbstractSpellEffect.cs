using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect
{
    public abstract class AbstractSpellEffect
    {
        public abstract FightActionResultEnum ApplyEffect(CastInfos castInfos);
    }
}


