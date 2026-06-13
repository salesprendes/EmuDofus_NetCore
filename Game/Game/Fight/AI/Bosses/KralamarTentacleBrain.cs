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
                    var projectedAP = context.CurrentAP;

                    if (spell.APCost > projectedAP)
                    {
                        var motivation = FindNaturalMotivationSpell(context, characteristicSpellId.Value);
                        if (motivation != null)
                        {
                            projectedAP = projectedAP - motivation.APCost + GetAPBonus(motivation);

                            yield return new AIDecision
                            {
                                Type = AIDecisionType.Buff,
                                Priority = AIDecisionPriority.Critical,
                                Score = 1200,
                                SpellId = motivation.SpellId,
                                TargetId = Fighter.Id,
                                CellId = (short)context.CurrentCellId,
                                Reason = "Tentaculo intenta utilizar bonificacion de PA"
                            };
                        }
                    }

                    var target = spell.APCost <= projectedAP ? GetBestTargetForSpell(context, spell, projectedAP) : null;

                    if (target?.Cell != null)
                    {
                        yield return new AIDecision
                        {
                            Type = AIDecisionType.CastSpell,
                            Priority = AIDecisionPriority.Critical,
                            Score = 1000,
                            SpellId = characteristicSpellId.Value,
                            TargetId = target.Id,
                            CellId = (short)target.Cell.Id,
                            Reason = "Tentaculo hechizo caracteristico"
                        };
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

        private static AbstractFighter GetBestTargetForSpell(AIContext context, SpellLevel spell, int projectedAP)
        {
            if (context?.Fight?.Map == null || context.Fighter?.Cell == null || spell == null)
                return null;

            return context.Enemies
                .Where(enemy => enemy?.Cell != null && !enemy.IsFighterDead)
                .Where(enemy => CanCastFromCurrentCell(context, spell, enemy.Cell.Id, projectedAP))
                .OrderByDescending(TargetEvaluator.ScorePriorityTarget)
                .ThenBy(enemy => Pathfinding.GoalDistance(context.Fight.Map, context.Fighter.Cell.Id, enemy.Cell.Id))
                .FirstOrDefault();
        }

        private static bool CanCastFromCurrentCell(AIContext context, SpellLevel spell, int castCell, int projectedAP)
        {
            if (context?.Fighter == null || spell == null)
                return false;

            var fighter = context.Fighter;
            if (projectedAP < spell.APCost
                || fighter.Cell == null
                || fighter.Statistics == null
                || fighter.IsFighterDead)
                return false;

            if (spell.RequiredLevel > 0 && fighter.Level < spell.RequiredLevel)
                return false;

            if (fighter.StateManager != null
                && (fighter.StateManager.HasState(FighterStateEnum.STATE_WEAKENED)
                    || fighter.StateManager.HasState(FighterStateEnum.STATE_CARRIED)))
                return false;

            return SpellEvaluator.CanReachCell(context, spell, context.CurrentCellId, castCell);
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

                if (effect.TypeEnum != EffectEnum.AddAP && effect.TypeEnum != EffectEnum.AddAPBis)
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
