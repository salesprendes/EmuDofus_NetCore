using Game.Entity;
using Game.Map;
using Game.Spell;
using System.Linq;

namespace Game.Fight.Effect.Type
{
    /// <summary>
    ///
    /// </summary>
    public sealed class SummoningEffect : AbstractSpellEffect
    {
        /// <summary>
        ///
        /// </summary>
        private bool m_static;

        /// <summary>
        ///
        /// </summary>
        /// <param name="staticInvoc"></param>
        public SummoningEffect(bool staticInvoc = false)
        {
            m_static = staticInvoc;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="castInfos"></param>
        /// <returns></returns>
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            var cell = FindFreeSummonCell(castInfos.Fight, castInfos.CellId);
            if (cell == null)
                return FightActionResultEnum.RESULT_NOTHING;

            if (castInfos.EffectType == EffectEnum.InvocDouble)
                return SummonDouble(castInfos, cell);

            return SummonMonster(castInfos, cell);
        }

        private FightActionResultEnum SummonMonster(CastInfos castInfos, FightCell cell)
        {
            var monsterTemplate = Database.Repository.MonsterRepository.Instance.GetById(castInfos.Value1);
            if (monsterTemplate == null)
                return FightActionResultEnum.RESULT_NOTHING;

            var monsterGrade = monsterTemplate.Grades.FirstOrDefault(grade => grade.Grade == castInfos.Value2);
            if (monsterGrade == null)
                return FightActionResultEnum.RESULT_NOTHING;

            return castInfos.Fight.SummonFighter(new MonsterEntity(castInfos.Fight.NextFighterId, monsterGrade, castInfos.Caster, m_static),castInfos.Caster.Team, cell.Id);
        }

        private static FightActionResultEnum SummonDouble(CastInfos castInfos, FightCell cell)
        {
            var character = castInfos.Caster as CharacterEntity;
            if (character == null)
                return FightActionResultEnum.RESULT_NOTHING;

            return castInfos.Fight.SummonFighter(new DoubleFighter(castInfos.Fight.NextFighterId, character),character.Team, cell.Id);
        }

        private static FightCell FindFreeSummonCell(AbstractFight fight, int centerCellId)
        {
            var center = fight.GetCell(centerCellId);
            if (center != null && center.CanWalk)
                return center;

            // Target cell is occupied — search the closest free cell within radius 3
            return CellZone.GetCircleCells(fight.Map, centerCellId, 3)
                .Where(cid => cid != centerCellId)
                .Select(cid => fight.GetCell(cid))
                .Where(c => c != null && c.CanWalk)
                .OrderBy(c => Pathfinding.GoalDistance(fight.Map, centerCellId, c.Id))
                .FirstOrDefault();
        }
    }
}


