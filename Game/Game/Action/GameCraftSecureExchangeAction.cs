using Game.Entity;
using Game.Exchange;
using Game.Job;
using Game.Network;

namespace Game.Action
{
    /// <summary>
    /// Acción del craft seguro entre jugadores. Los roles (artesano/cliente) son fijos, pero
    /// la petición puede iniciarla cualquiera de los dos: el iniciador (localEntity) invita al
    /// invitado (distantEntity), que es quien acepta. La habilidad puede ser de craft
    /// (CraftSkill) o de forjamagia (MagicSkill).
    /// </summary>
    public sealed class GameCraftSecureExchangeAction : AbstractGameExchangeAction
    {
        public GameCraftSecureExchangeAction(CharacterEntity initiator, CharacterEntity invited, CharacterEntity artisan, CharacterEntity client, JobSkill skill, int requestType) : base(new ExchangeCraftSecure(artisan, client, skill), initiator, invited)
        {
            Exchange.Dispatch(WorldMessage.EXCHANGE_CRAFT_SECURE_REQUEST(initiator.Id, invited.Id, requestType));
        }

        public override void Stop(params object[] args)
        {
            base.Leave(true);
            base.Stop(args);
        }

        public override void Abort(params object[] args)
        {
            base.Leave();
            base.Abort(args);
        }
    }
}
