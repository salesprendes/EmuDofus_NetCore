using Game.Manager;
using Game.Map;
using Game.Spell;
using System;
using System.Linq;

namespace Game.Fight.Effect.Type
{
    /// <summary>
    /// E405 "lanza un hechizo": aplica los efectos de otro hechizo (Value1 = plantilla,
    /// Value2 = nivel) sobre la celda del objetivo. Lo usan Siega, Desfile de Juguetes Viejos y
    /// varios monstruos, normalmente dentro de un grupo de probabilidad (sub-hechizo aleatorio).
    /// </summary>
    public sealed class SubSpellEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos castInfos)
        {
            var fight = castInfos.Caster?.Fight;
            if (fight?.Map == null)
                return FightActionResultEnum.RESULT_NOTHING;

            var template = SpellManager.Instance.GetTemplate(castInfos.Value1);
            var subLevel = template?.GetLevel(Math.Max(1, castInfos.Value2));
            if (subLevel?.Effects == null || subLevel.Effects.Count == 0)
                return FightActionResultEnum.RESULT_NOTHING;

            var castCell = castInfos.Target?.Cell?.Id ?? castInfos.CellId;
            var casterCell = castInfos.Caster.Cell?.Id ?? castCell;

            var effectIndex = 0;
            foreach (var effect in subLevel.Effects)
            {
                // Sin recursión: un sub-hechizo no puede encadenar otro sub-hechizo.
                if (effect.TypeEnum == EffectEnum.LANZAR_HECHIZO)
                {
                    effectIndex++;
                    continue;
                }

                var zone = subLevel.GetEffectZone(effectIndex, false);
                foreach (var cellId in CellZone.GetCells(fight.Map, castCell, casterCell, zone).Distinct())
                {
                    var cell = fight.GetCell(cellId);
                    if (cell == null)
                        continue;

                    foreach (var target in cell.FightObjects.OfType<AbstractFighter>())
                    {
                        if (target.IsFighterDead)
                            continue;

                        fight.AddProcessingTarget(new CastInfos(
                            effect.TypeEnum,
                            castInfos.Value1,
                            castCell,
                            effect.Value1,
                            effect.Value2,
                            effect.Value3,
                            effect.Chance,
                            effect.Duration,
                            castInfos.Caster,
                            target,
                            "",
                            target.Cell.Id,
                            subLevel.Level));
                    }
                }

                effectIndex++;
            }

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}
