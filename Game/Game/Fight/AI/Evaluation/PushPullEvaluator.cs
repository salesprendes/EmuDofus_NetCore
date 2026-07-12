using Game.Fight.AI.Core;
using Game.Fight;
using Game.Map;
using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Evaluation
{
    // Evalua hechizos de empuje y atraccion. Empujar a un enemigo contra un muro u otro luchador
    // genera danio por choque proporcional a las casillas que NO consigue recorrer (mismo modelo
    // que el motor: floor(coef * nivel/50) * casillas_bloqueadas). La atraccion sirve para acercar
    // a enemigos lejanos.
    public sealed class PushPullEvaluator : IAIEvaluator
    {
        // Promedio del coeficiente de danio por choque del motor (Util.Next(9, 17)).
        private const int AvgPushCoef = 13;

        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            if (context?.Fighter?.Cell == null || context.Enemies == null || context.SpellBook?.PushPullSpells == null)
                yield break;

            var map = context.Fight?.Map;
            if (map == null)
                yield break;

            var fromCell = context.Fighter.Cell.Id;
            var casterLevel = context.Fighter.Level;

            foreach (var spell in context.SpellBook.PushPullSpells)
            {
                if (spell == null || spell.APCost > context.CurrentAP)
                    continue;

                var pushesAway = SpellPushesAway(spell);
                var pushLength = GetPushLength(spell);

                foreach (var enemy in context.Enemies)
                {
                    if (enemy?.Cell == null || enemy.IsFighterDead)
                        continue;

                    var targetCell = enemy.Cell.Id;
                    if (!SpellEvaluator.CanCastFromCurrentCell(context, spell, targetCell))
                        continue;

                    if (pushesAway)
                    {
                        // El empuje aleja al objetivo del lanzador, en linea recta.
                        if (fromCell == targetCell || !Pathfinding.InLine(map, fromCell, targetCell))
                            continue;

                        var direction = Pathfinding.GetDirection(map, fromCell, targetCell);
                        var blocked = EstimateBlockedCells(context, map, targetCell, direction, pushLength);
                        if (blocked <= 0)
                            continue;

                        var collisionDamage = (int)Math.Floor(AvgPushCoef * Math.Max(0.1, casterLevel / 50.0)) * blocked;
                        if (collisionDamage <= 0)
                            continue;

                        var killScore = TargetEvaluator.ScoreKillChance(context.Fighter, enemy, collisionDamage);
                        var score = 60 + collisionDamage + killScore + TargetEvaluator.ScoreLowHp(enemy) / 2;

                        yield return AIDecision.CastSpell(spell.SpellId, targetCell, enemy.Id, score,
                            killScore > 0 ? AIDecisionPriority.Critical : AIDecisionPriority.Normal,
                            "Empuje contra obstaculo (" + blocked + " casillas)");
                    }
                    else
                    {
                        // Atraccion: util para acercar a un enemigo lejano hacia nuestro alcance.
                        var distance = context.TurnCache.Cells.GetDistance(context.CurrentCellId, targetCell);
                        if (distance < 3)
                            continue;

                        yield return AIDecision.CastSpell(spell.SpellId, targetCell, enemy.Id, 30 + distance * 4,
                            AIDecisionPriority.Low, "Atraccion para acercar enemigo");
                    }
                }
            }
        }

        // Numero de casillas que el objetivo NO podra recorrer al ser empujado (choque) = danio.
        private static int EstimateBlockedCells(AIContext context, MapInstance map, int targetCell, DirectionEnum direction, int length)
        {
            var current = targetCell;
            for (var i = 0; i < length; i++)
            {
                // Paso validado como en el empuje real: el borde del mapa cuenta como choque.
                var cell = Pathfinding.TryGetCellInDirection(map, current, direction, 1, out var next)
                    ? context.Fight.GetCell(next)
                    : null;

                if (cell == null || !cell.CanWalk)
                    return length - i;   // choca con muro/borde/luchador: casillas restantes

                if (cell.HasObject(FightObstacleTypeEnum.TYPE_TRAP))
                    return 0;            // cae sobre una trampa: sin danio por choque

                current = next;
            }

            return 0;   // recorre todo el empuje sin chocar
        }

        private static bool SpellPushesAway(SpellLevel spell)
        {
            return spell.Effects != null && spell.Effects.Any(e =>
                e.TypeEnum == EffectEnum.MOVIMIENTO_EMPUJAR
                || e.TypeEnum == EffectEnum.MOVIMIENTO_EMPUJAR_MIEDO
                || e.TypeEnum == EffectEnum.PANDA_LANZAR);
        }

        private static int GetPushLength(SpellLevel spell)
        {
            if (spell.Effects == null)
                return 2;

            var effect = spell.Effects.FirstOrDefault(e =>
                e.TypeEnum == EffectEnum.MOVIMIENTO_EMPUJAR
                || e.TypeEnum == EffectEnum.MOVIMIENTO_EMPUJAR_MIEDO
                || e.TypeEnum == EffectEnum.MOVIMIENTO_ATRAER);

            var length = effect?.Value1 ?? 0;
            return length > 0 ? length : 2;
        }
    }
}
