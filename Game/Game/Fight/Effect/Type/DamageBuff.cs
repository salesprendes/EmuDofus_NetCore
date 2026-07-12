using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    /// <summary>
    /// Veneno: todo efecto de daño (E96-E100) con duración. Tickea al INICIO del turno del
    /// envenenado, simétrico a <see cref="HealBuff"/>, y decrementa al final de ese mismo turno,
    /// de modo que una duración N produce exactamente N ticks (el +1 de AbstractSpellBuff cubre
    /// el caso de envenenarse a sí mismo dentro de la propia zona de efecto).
    /// </summary>
    public sealed class DamageBuff : AbstractSpellBuff
    {
        public DamageBuff(CastInfos castInfos, AbstractFighter target)
            : base(castInfos, target, ActiveType.ACTIVE_BEGINTURN, DecrementType.TYPE_ENDTURN)
        {
        }

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            var damageJet = CastInfos.RandomJet;

            return DamageEffect.ApplyDamages(CastInfos, Target, ref damageJet);
        }
    }
}


