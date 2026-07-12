using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class PunishmentEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos CastInfos)
        {
            if (CastInfos.Target == null)
                return FightActionResultEnum.RESULT_NOTHING;

            // Los castigos distintos conviven; relanzar el MISMO castigo lo refresca
            // (retira la instancia anterior y su erosión) en vez de apilarse consigo mismo.
            CastInfos.Target.BuffManager.RemovePunishment(CastInfos.SpellId);

            CastInfos.Target.BuffManager.AddBuff(new PunishmentBuff(CastInfos, CastInfos.Target));

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


