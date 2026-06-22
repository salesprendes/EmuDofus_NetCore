using Game.Fight.AI.Core;
using Game.Fight.Effect;
using Game.Map;
using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Evaluation
{
    // Helpers de hechizos: alcance, lanzabilidad, estimacion de danio/cura y zonas. No es un
    // evaluador: lo usan los evaluadores reales por sus metodos estaticos.
    public static class SpellEvaluator
    {
        public static bool CanCastFromCurrentCell(AIContext context, SpellLevel spell, int castCell)
        {
            return CanCastFromCell(context, spell, context?.CurrentCellId ?? -1, castCell);
        }

        public static bool CanCastFromCell(AIContext context, SpellLevel spell, int fromCell, int castCell)
        {
            return CanCastSpellThisTurn(context, spell) && CanReachCell(context, spell, fromCell, castCell);
        }

        public static bool CanCastSpellThisTurn(AIContext context, SpellLevel spell)
        {
            if (context?.Fighter == null || context.Fight == null || spell == null)
                return false;

            var fighter = context.Fighter;
            if (fighter.Cell == null || fighter.Statistics == null || fighter.IsFighterDead)
                return false;

            if (fighter.AP < spell.APCost)
                return false;

            if (spell.RequiredLevel > 0 && fighter.Level < spell.RequiredLevel)
                return false;

            if (fighter.StateManager != null
                && (fighter.StateManager.HasState(FighterStateEnum.STATE_WEAKENED)
                    || fighter.StateManager.HasState(FighterStateEnum.STATE_CARRIED)))
                return false;

            if (spell.Effects != null
                && spell.Effects.Any(effect => effect.TypeEnum == EffectEnum.INVOCACION_CRIATURA || effect.TypeEnum == EffectEnum.INVOCACION_DOBLE))
            {
                var invocationCount = fighter.Team?.AliveFighters?.Count(f => f.Invocator == fighter && !f.StaticInvocation) ?? 0;
                if (invocationCount >= fighter.Statistics.GetTotal(EffectEnum.STAT_MAS_INVOCACIONES_MAX))
                    return false;
            }

            return true;
        }

        public static bool CanReachCell(AIContext context, SpellLevel spell, int fromCell, int castCell)
        {
            if (context?.Fighter == null || context.Fight == null || spell == null)
                return false;

            var fighter = context.Fighter;
            if (fighter.Statistics == null || castCell < 0 || fromCell < 0)
                return false;

            var fightCell = context.Fight.GetCell(castCell);
            if (fightCell == null)
                return false;

            var distance = context.TurnCache.Cells.GetDistance(fromCell, castCell);
            var maxPo = spell.AllowPOBoost && spell.MaxPO != 0 ? spell.MaxPO + fighter.Statistics.GetTotal(EffectEnum.STAT_MAS_ALCANCE) : spell.MaxPO;

            if (maxPo < spell.MinPO)
                maxPo = spell.MinPO;

            if (distance > maxPo || distance < spell.MinPO)
                return false;

            if (spell.EmptyCell && !fightCell.CanWalk)
                return false;

            try
            {
                if (spell.InLine && !Pathfinding.InLine(context.Fight.Map, fromCell, castCell))
                    return false;
            }
            catch (System.Exception ex)
            {
                AIDiagnostics.LogSwallowed("SpellEvaluator.CanReachCell", ex);
                return false;
            }

            if (spell.EmptyCell && fightCell.HasObject(FightObstacleTypeEnum.TYPE_FIGHTER))
                return false;

            if (spell.LOS && !context.TurnCache.LineOfSight.HasLineOfSight(fromCell, castCell))
                return false;

            var target = context.Fight.GetFighterOnCell(castCell);
            var targetId = target?.Id ?? 0;
            return fighter.SpellManager == null || fighter.SpellManager.CanLaunchSpell(spell, spell.SpellId, targetId);
        }

        public static int EstimateDamage(SpellLevel spell)
        {
            if (spell?.Effects == null)
                return 0;

            return spell.Effects.Where(e => CastInfos.IsDamageEffect(e.TypeEnum)).Sum(EstimateEffectValue);
        }

        // Estimacion de danio realista contra un objetivo concreto: aplica el mismo modelo que el
        // motor (caracteristicas elementales del lanzador y resistencias/armadura del objetivo) en
        // lugar del valor crudo del efecto. Permite priorizar el mejor elemento por enemigo y
        // detectar golpes mortales de forma fiable.
        public static int EstimateDamageOnTarget(AbstractFighter caster, SpellLevel spell, AbstractFighter target)
        {
            if (spell?.Effects == null)
                return 0;

            var total = 0;
            foreach (var effect in spell.Effects)
            {
                if (effect == null || !CastInfos.IsDamageEffect(effect.TypeEnum))
                    continue;

                var jet = EstimateEffectValue(effect);
                if (jet <= 0)
                    continue;

                caster?.CalculDamages(effect.TypeEnum, ref jet);

                if (target != null)
                {
                    target.CalculReduceDamages(effect.TypeEnum, ref jet);

                    var armor = target.CalculArmor(effect.TypeEnum);
                    if (armor > 0)
                        jet -= armor;
                }

                if (jet > 0)
                    total += jet;
            }

            return total;
        }

        public static int EstimateHeal(SpellLevel spell)
        {
            if (spell?.Effects == null)
                return 0;

            return spell.Effects.Where(e => e.TypeEnum == EffectEnum.CURACION).Sum(EstimateEffectValue);
        }

        public static int EstimateBuffValue(SpellLevel spell)
        {
            if (spell?.Effects == null)
                return 0;

            return spell.Effects.Where(e => CastInfos.IsBonusEffect(e.TypeEnum) && e.TypeEnum != EffectEnum.CURACION).Sum(EstimateEffectValue);
        }

        public static int EstimateDebuffValue(SpellLevel spell)
        {
            if (spell?.Effects == null)
                return 0;

            var score = 0;
            foreach (var effect in spell.Effects)
            {
                switch (effect.TypeEnum)
                {
                    case EffectEnum.STAT_MENOS_PA:
                    case EffectEnum.STAT_MENOS_PA_ESQUIVABLE:
                    case EffectEnum.STAT_ROBO_PA:
                        score += 220 + EstimateEffectValue(effect) * 60;
                        break;

                    case EffectEnum.STAT_MENOS_PM:
                    case EffectEnum.STAT_MENOS_PM_ESQUIVABLE:
                    case EffectEnum.STAT_ROBO_PM:
                        score += 180 + EstimateEffectValue(effect) * 45;
                        break;

                    case EffectEnum.STAT_MENOS_ALCANCE:
                        score += 100 + EstimateEffectValue(effect) * 30;
                        break;

                    default:
                        if (AISpellBook.HasVulnerabilityEffect(spell))
                            score += 120 + EstimateEffectValue(effect);
                        else if (CastInfos.IsMalusEffect(effect.TypeEnum))
                            score += 70 + EstimateEffectValue(effect);
                        break;
                }
            }

            return score;
        }

        public static IEnumerable<AbstractFighter> GetAffectedFighters(AIContext context, SpellLevel spell, int castCell)
        {
            if (context?.Fight == null || context.Fighter == null || spell == null)
                yield break;

            IEnumerable<int> cells;
            try
            {
                cells = string.IsNullOrEmpty(spell.RangeType) ? [castCell] : CellZone.GetCells(context.Fight.Map, castCell, context.Fighter.Cell.Id, spell.RangeType);
            }
            catch (System.Exception ex)
            {
                AIDiagnostics.LogSwallowed("SpellEvaluator.GetAffectedFighters", ex);
                cells = [castCell];
            }

            foreach (var cellId in cells)
            {
                var fightCell = context.Fight.GetCell(cellId);
                if (fightCell == null)
                    continue;

                foreach (var fighter in fightCell.FightObjects.OfType<AbstractFighter>())
                {
                    if (fighter != null && !fighter.IsFighterDead)
                        yield return fighter;
                }
            }
        }

        public static int ScoreAreaImpact(AIContext context, SpellLevel spell, int castCell, bool friendlySpell)
        {
            var score = 0;
            foreach (var target in GetAffectedFighters(context, spell, castCell))
            {
                var isAlly = target.Team == context.Fighter.Team;
                if (friendlySpell)
                    score += isAlly ? 45 : -120;
                else
                    score += isAlly ? -350 : 80;
            }

            return score;
        }

        private static int EstimateEffectValue(SpellEffect effect)
        {
            if (effect == null)
                return 0;

            var low = Math.Max(0, effect.Value1);
            var high = Math.Max(low, effect.Value2);
            return ((low + high) / 2) + Math.Max(0, effect.Value3);
        }
    }
}
