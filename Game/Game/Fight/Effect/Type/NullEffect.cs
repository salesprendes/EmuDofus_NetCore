namespace Game.Fight.Effect.Type
{
    /// <summary>
    /// E666 "ningún efecto": entrada decorativa de los datos del cliente. Debe estar registrada
    /// para actuar como peso de fallo dentro de los grupos de probabilidad (p. ej. Ralentización:
    /// 90% de que no ocurra nada) y para no ensuciar el log como efecto desconocido.
    /// </summary>
    public sealed class NullEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}
