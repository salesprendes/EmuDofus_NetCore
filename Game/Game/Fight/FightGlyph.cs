using Game.Fight.Effect;
using Game.Network;

namespace Game.Fight
{
    public sealed class FightGlyph : AbstractActivableObject
    {
        public FightGlyph(AbstractFight fight, AbstractFighter caster, CastInfos effect, int cell, int duration) : base(FightObstacleTypeEnum.TYPE_GLYPH, ActiveType.ACTIVE_BEGINTURN, fight, caster, effect, cell, duration, 307, true, true) { }

        public override void AppearForAll()
        {
            m_fight.CachedBuffer = true;
            m_fight.Dispatch(WorldMessage.GAME_DATA_ZONE(OperatorEnum.OPERATOR_ADD, Cell.Id, Length, Color));
            m_fight.Dispatch(WorldMessage.GAME_DATA_ZONE_CREATE(Cell.Id));
            m_fight.CachedBuffer = false;
        }

        public override void Appear(MessageDispatcher dispatcher)
        {
        }

        public override void DisappearForAll()
        {
            m_fight.CachedBuffer = true;
            m_fight.Dispatch(WorldMessage.GAME_DATA_ZONE(OperatorEnum.OPERATOR_REMOVE, Cell.Id, Length, Color));
            m_fight.Dispatch(WorldMessage.GAME_DATA_ZONE_CREATE(Cell.Id));
            m_fight.CachedBuffer = false;
        }
    }
}


