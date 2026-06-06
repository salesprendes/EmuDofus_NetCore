using Game.Fight.Effect;

namespace Game.Fight.AI.Core
{
    public interface IDamageReceivedBrain
    {
        void OnDamageReceived(CastInfos castInfos, int damageBeforeResistance);
    }
}
