using Game.Entity;
using Game.Network;
using Game.Spell;
using System;

namespace Game.Fight.Effect.Type
{
    public sealed class KamasStealEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            if (castInfos.Caster is not CharacterEntity thief || castInfos.Target is not MonsterEntity monster || monster.IsFighterDead)
                return FightActionResultEnum.RESULT_NOTHING;

            long requested = Math.Min(castInfos.RandomJet, Math.Max(castInfos.Value1, castInfos.Value2));

            long stolen = monster.StealKamas(requested);
            if (stolen <= 0)
                return FightActionResultEnum.RESULT_NOTHING;

            thief.Inventory.AddKamas(stolen);
            castInfos.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.KAMAS_ROBO, thief.Id, stolen.ToString()));

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}
