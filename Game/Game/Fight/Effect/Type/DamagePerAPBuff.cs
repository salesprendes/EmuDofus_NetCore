using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class DamagePerAPBuff : AbstractSpellBuff
    {
        public DamagePerAPBuff(CastInfos infos, AbstractFighter target)
    : base(infos, target, ActiveType.ACTIVE_ENDTURN, DecrementType.TYPE_ENDTURN)
        {
        }

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            var usedAp = Target.UsedAP;
            if (usedAp < 1 || CastInfos.Value1 <= 0)
                return FightActionResultEnum.RESULT_NOTHING;

            // Daño de fin de turno: se marca como veneno para que NO revele invisibilidad ni
            // dispare armaduras/renvoi como un ataque directo.
            damageInfos = new CastInfos(EffectEnum.DANO_NEUTRAL, -1, -1, -1, -1, -1, -1, -1, CastInfos.Caster, CastInfos.Target);
            damageInfos.IsPoison = true;
            var damageJet = (usedAp / CastInfos.Value1) * CastInfos.Value2;

            return DamageEffect.ApplyDamages(damageInfos, CastInfos.Target, ref damageJet);
        }
    }
}


