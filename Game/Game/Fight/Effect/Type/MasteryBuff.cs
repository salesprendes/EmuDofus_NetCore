using Game.Database.Structure;

namespace Game.Fight.Effect.Type
{
    /// <summary>
    /// Applies weapon mastery only to matching weapon attacks.
    /// </summary>
    public sealed class MasteryBuff : AbstractSpellBuff
    {
        public MasteryBuff(CastInfos castInfos, AbstractFighter target)
            : base(castInfos, target, ActiveType.ACTIVE_ATTACK_BEFORE_JET, DecrementType.TYPE_ENDTURN)
        {
        }

        public override FightActionResultEnum ApplyEffect(ref int damageValue, CastInfos damageInfos = null)
        {
            if (damageInfos == null || damageInfos.SpellId != -1)
                return base.ApplyEffect(ref damageValue, damageInfos);

            var weapon = Target.Inventory.Items.Find(item => item.Slot == ItemSlotEnum.SLOT_WEAPON);
            if (weapon?.Template == null || weapon.Template.Type != CastInfos.Value1)
                return base.ApplyEffect(ref damageValue, damageInfos);

            if (CastInfos.Value2 > 0)
                damageValue += damageValue * CastInfos.Value2 / 100;

            return base.ApplyEffect(ref damageValue, damageInfos);
        }
    }
}


