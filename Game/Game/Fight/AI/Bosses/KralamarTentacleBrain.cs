using Game.Entity;
using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using Game.Map;
using Game.Spell;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Bosses
{
    public sealed class KralamarTentacleBrain : AIBrain
    {
        private const int KralamarTemplateId = 423;

        private const int TentaculoPrimario = 424;
        private const int TentaculoCuaternario = 1090;
        private const int TentaculoTerciario = 1091;
        private const int TentaculoSecundario = 1092;

        private const int SpellKrakenPrimario = 1096;
        private const int SpellKrakenSecundario = 1097;
        private const int SpellKrakenTerciario = 1098;
        private const int SpellKrakenCuaternario = 1099;

        public KralamarTentacleBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            int? characteristicSpellId = GetCharacteristicSpellId(Fighter);

            if (characteristicSpellId.HasValue)
            {
                var spell = context.SpellBook.AllSpells.FirstOrDefault(s => s?.SpellId == characteristicSpellId.Value);
                if (spell != null)
                {
                    // El hechizo caracteristico es la razon de ser del tentaculo (p.ej. el Tentaculo
                    // Primario "mata" cuerpo a cuerpo y su golpe es imprescindible para que el
                    // Kralamar Gigante se vuelva vulnerable). Objetivo: lanzarlo SIEMPRE que pueda.
                    if (context.CurrentAP >= spell.APCost)
                    {
                        // Ya tiene PA: lanzarlo si o si, reposicionandose a cuerpo a cuerpo si hace
                        // falta (antes solo se lanzaba desde la celda actual, asi que un hechizo
                        // melee no se lanzaba nunca si no habia un enemigo justo al lado).
                        foreach (var decision in EvaluateCharacteristicCast(context, spell))
                            yield return decision;
                    }
                    else if (Fighter.MaxAP < spell.APCost)
                    {
                        // Le falta CAPACIDAD de PA para su hechizo: se motiva (Motivacion Natural,
                        // +PA persistente) hasta que su maximo alcance el coste. El Primario nace con
                        // 4 PA y su Kraken cuesta 5, de ahi que sea "reticente a jugar" el primer turno.
                        var motivation = FindNaturalMotivationSpell(context, characteristicSpellId.Value);
                        if (motivation != null
                            && context.CurrentAP >= motivation.APCost
                            && SpellEvaluator.CanCastFromCurrentCell(context, motivation, context.CurrentCellId))
                        {
                            yield return new AIDecision
                            {
                                Type = AIDecisionType.Buff,
                                Priority = AIDecisionPriority.Critical,
                                Score = 1200,
                                SpellId = motivation.SpellId,
                                TargetId = Fighter.Id,
                                CellId = (short)context.CurrentCellId,
                                Reason = "Tentaculo acumula PA (Motivacion Natural) para su hechizo caracteristico"
                            };
                        }
                    }
                }
            }

            var kralamarHpRatio = GetKralamarHpRatio(context);
            var enragePhase = kralamarHpRatio <= 0.40;

            foreach (var decision in new DebuffEvaluator().Evaluate(context))
            {
                if (decision == null)
                    continue;

                if (enragePhase && decision.Priority < AIDecisionPriority.High)
                    decision.Priority = AIDecisionPriority.High;

                yield return decision;
            }

            foreach (AIDecision decision in new AttackEvaluator().Evaluate(context))
            {
                if (decision == null)
                    continue;

                if (enragePhase && decision.Priority < AIDecisionPriority.High)
                    decision.Priority = AIDecisionPriority.High;

                yield return decision;
            }

            if (!Fighter.StaticInvocation && context.CurrentMP > 0)
            {
                foreach (var decision in new MovementEvaluator().Evaluate(context))
                {
                    yield return decision;
                }
            }
        }

        // Lanza el hechizo caracteristico contra el mejor objetivo. Si no llega desde la celda
        // actual (es cuerpo a cuerpo), se reposiciona: emite un mover-y-lanzar atomico hacia la
        // celda de MENOR riesgo desde la que SI alcanza. Todo a prioridad Critica: es su cometido.
        private IEnumerable<AIDecision> EvaluateCharacteristicCast(AIContext context, SpellLevel spell)
        {
            if (context?.Fight?.Map == null || context.Fighter?.Cell == null || spell == null)
                yield break;

            var targets = context.Enemies
                .Where(enemy => enemy?.Cell != null && !enemy.IsFighterDead)
                .OrderByDescending(TargetEvaluator.ScorePriorityTarget)
                .ThenBy(enemy => Pathfinding.GoalDistance(context.Fight.Map, context.Fighter.Cell.Id, enemy.Cell.Id))
                .ToList();

            // 1) Lanzamiento directo si ya esta a rango desde su celda.
            foreach (var enemy in targets)
            {
                if (SpellEvaluator.CanCastFromCurrentCell(context, spell, enemy.Cell.Id))
                {
                    yield return new AIDecision
                    {
                        Type = AIDecisionType.CastSpell,
                        Priority = AIDecisionPriority.Critical,
                        Score = 1000 + TargetEvaluator.ScorePriorityTarget(enemy) / 2,
                        SpellId = spell.SpellId,
                        TargetId = enemy.Id,
                        CellId = (short)enemy.Cell.Id,
                        Reason = "Tentaculo hechizo caracteristico"
                    };
                    yield break;
                }
            }

            // 2) No llega desde su celda: mover-y-lanzar hacia una celda a rango (si puede moverse).
            if (Fighter.StaticInvocation || context.CurrentMP <= 0)
                yield break;

            var reachable = context.TurnCache?.Cells?.GetReachableCells();
            if (reachable == null)
                yield break;

            foreach (var enemy in targets)
            {
                int? bestCell = null;
                var bestRisk = int.MaxValue;

                foreach (var reachCell in reachable)
                {
                    if (reachCell == context.CurrentCellId)
                        continue;

                    if (!SpellEvaluator.CanCastFromCell(context, spell, reachCell, enemy.Cell.Id))
                        continue;

                    var risk = RiskEvaluator.ScoreCellRisk(context, reachCell, false);
                    if (risk < bestRisk)
                    {
                        bestRisk = risk;
                        bestCell = reachCell;
                    }
                }

                if (bestCell.HasValue)
                {
                    yield return AIDecision.MoveAndCast(
                        bestCell.Value,
                        spell.SpellId,
                        enemy.Cell.Id,
                        enemy.Id,
                        1000 + TargetEvaluator.ScorePriorityTarget(enemy) / 2,
                        AIDecisionPriority.Critical,
                        "Tentaculo se reposiciona para lanzar su hechizo caracteristico");
                    yield break;
                }
            }
        }

        private static SpellLevel FindNaturalMotivationSpell(AIContext context, int characteristicSpellId)
        {
            if (context?.SpellBook?.AllSpells == null)
                return null;

            return context.SpellBook.AllSpells.Where(spell => spell != null && spell.SpellId != characteristicSpellId).Where(spell => GetAPBonus(spell) > 0).Where(spell => !AISpellBook.HasDamageEffect(spell)).Where(spell => SpellEvaluator.CanCastFromCurrentCell(context, spell, context.CurrentCellId)).OrderByDescending(GetAPBonus).ThenBy(spell => spell.APCost).FirstOrDefault();
        }

        private static int GetAPBonus(SpellLevel spell)
        {
            if (spell?.Effects == null)
                return 0;

            var bonus = 0;
            foreach (var effect in spell.Effects)
            {
                if (effect == null)
                    continue;

                if (effect.TypeEnum != EffectEnum.STAT_MAS_PA && effect.TypeEnum != EffectEnum.STAT_MAS_PA_BIS)
                    continue;

                if (effect.Value1 > 0)
                    bonus += effect.Value1;
                else if (effect.Value2 > 0)
                    bonus += effect.Value2;
                else if (effect.Value3 > 0)
                    bonus += effect.Value3;
            }

            return bonus;
        }

        private static int? GetCharacteristicSpellId(AbstractFighter fighter)
        {
            switch ((fighter as MonsterEntity)?.Grade?.MonsterId ?? 0)
            {
                case TentaculoPrimario:
                    return SpellKrakenPrimario;

                case TentaculoSecundario:
                    return SpellKrakenSecundario;

                case TentaculoTerciario:
                    return SpellKrakenTerciario;

                case TentaculoCuaternario:
                    return SpellKrakenCuaternario;

                default:
                    return null;
            }
        }

        private static double GetKralamarHpRatio(AIContext context)
        {
            foreach (var ally in context.Allies)
            {
                if (ally == null || ally.IsFighterDead)
                    continue;

                if ((ally as MonsterEntity)?.Grade?.MonsterId == KralamarTemplateId && ally.MaxLife > 0)
                    return (double)ally.Life / ally.MaxLife;
            }

            return 1.0;
        }

        protected override void LogDecision(AIContext context, AIDecision decision)
        {
            if (!WorldConfig.LOG_DEBUG || decision == null)
                return;

            Logger.Debug($"[IA][TentaculoKralamar] Luchador={(Fighter?.Id ?? 0)} Plantilla={((Fighter as MonsterEntity)?.Grade?.MonsterId ?? 0)} Decision={decision.Type} Prioridad={decision.Priority} Puntuacion={decision.Score} Motivo={decision.Reason}");
        }
    }
}
