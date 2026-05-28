using Game.Fight;
using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.TaxCollectors
{
    public sealed class TaxCollectorEvaluator : IAIEvaluator
    {
        private readonly TaxCollectorDefenseMode m_mode;
        private readonly AttackEvaluator m_attackEvaluator;
        private readonly HealEvaluator m_healEvaluator;
        private readonly BuffEvaluator m_buffEvaluator;
        private readonly DebuffEvaluator m_debuffEvaluator;
        private readonly MovementEvaluator m_movementEvaluator;

        public TaxCollectorEvaluator(TaxCollectorDefenseMode mode)
        {
            m_mode = mode;
            m_attackEvaluator = new AttackEvaluator();
            m_healEvaluator = new HealEvaluator();
            m_buffEvaluator = new BuffEvaluator();
            m_debuffEvaluator = new DebuffEvaluator();
            m_movementEvaluator = new MovementEvaluator();
        }

        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            if (!HasUsableContext(context))
                yield break;

            var attacks = SafeEvaluate(m_attackEvaluator, context).ToList();
            var heals = SafeEvaluate(m_healEvaluator, context).ToList();
            var buffs = SafeEvaluate(m_buffEvaluator, context).ToList();
            var debuffs = SafeEvaluate(m_debuffEvaluator, context).ToList();

            foreach (var decision in EvaluateFinishEnemy(context, attacks))
                yield return decision;

            foreach (var decision in EvaluateCriticalDefense(context, heals, buffs, debuffs))
                yield return decision;

            foreach (var decision in EvaluateSelfProtection(context, heals, buffs, debuffs))
                yield return decision;

            foreach (var decision in EvaluateDefenderSupport(context, heals, buffs))
                yield return decision;

            foreach (var decision in EvaluateDangerousAttackers(context, debuffs))
                yield return decision;

            foreach (var decision in EvaluateBestDebuffSpell(context, debuffs))
                yield return decision;

            foreach (var decision in EvaluateBestHealSpell(context, heals))
                yield return decision;

            foreach (var decision in EvaluateBestBuffSpell(context, buffs))
                yield return decision;

            foreach (var decision in EvaluateBestDamageSpell(context, attacks))
                yield return decision;

            foreach (var decision in EvaluateReposition(context))
                yield return decision;
        }

        public IEnumerable<AIDecision> EvaluateCriticalDefense(AIContext context)
        {
            return EvaluateCriticalDefense(
                context,
                SafeEvaluate(m_healEvaluator, context).ToList(),
                SafeEvaluate(m_buffEvaluator, context).ToList(),
                SafeEvaluate(m_debuffEvaluator, context).ToList());
        }

        public IEnumerable<AIDecision> EvaluateSelfProtection(AIContext context)
        {
            return EvaluateSelfProtection(
                context,
                SafeEvaluate(m_healEvaluator, context).ToList(),
                SafeEvaluate(m_buffEvaluator, context).ToList(),
                SafeEvaluate(m_debuffEvaluator, context).ToList());
        }

        public IEnumerable<AIDecision> EvaluateDefenderSupport(AIContext context)
        {
            return EvaluateDefenderSupport(
                context,
                SafeEvaluate(m_healEvaluator, context).ToList(),
                SafeEvaluate(m_buffEvaluator, context).ToList());
        }

        public IEnumerable<AIDecision> EvaluateDangerousAttackers(AIContext context)
        {
            return EvaluateDangerousAttackers(context, SafeEvaluate(m_debuffEvaluator, context).ToList());
        }

        public IEnumerable<AIDecision> EvaluateFinishEnemy(AIContext context)
        {
            return EvaluateFinishEnemy(context, SafeEvaluate(m_attackEvaluator, context).ToList());
        }

        public IEnumerable<AIDecision> EvaluateBestDamageSpell(AIContext context)
        {
            return EvaluateBestDamageSpell(context, SafeEvaluate(m_attackEvaluator, context).ToList());
        }

        public IEnumerable<AIDecision> EvaluateBestDebuffSpell(AIContext context)
        {
            return EvaluateBestDebuffSpell(context, SafeEvaluate(m_debuffEvaluator, context).ToList());
        }

        public IEnumerable<AIDecision> EvaluateBestBuffSpell(AIContext context)
        {
            return EvaluateBestBuffSpell(context, SafeEvaluate(m_buffEvaluator, context).ToList());
        }

        public IEnumerable<AIDecision> EvaluateBestHealSpell(AIContext context)
        {
            return EvaluateBestHealSpell(context, SafeEvaluate(m_healEvaluator, context).ToList());
        }

        public IEnumerable<AIDecision> EvaluateReposition(AIContext context)
        {
            if (!CanMove(context))
                yield break;

            var currentRisk = RiskEvaluator.ScoreCellRisk(context, context.CurrentCellId, false);

            foreach (var raw in SafeEvaluate(m_movementEvaluator, context))
            {
                if (raw?.CellId == null)
                    continue;

                var targetCell = raw.CellId.Value;
                var targetRisk = RiskEvaluator.ScoreCellRisk(context, targetCell, false);
                var reducesRisk = targetRisk < currentRisk;
                var enablesCast = EnablesUsefulCastFrom(context, targetCell);

                if (!reducesRisk && !enablesCast)
                    continue;

                if (targetRisk > currentRisk + 120 && !enablesCast)
                    continue;

                var decision = Copy(raw);
                decision.Score += enablesCast ? 200 : 0;
                decision.Score += reducesRisk ? (currentRisk - targetRisk) * 2 : 0;

                if (m_mode == TaxCollectorDefenseMode.LowHealth
                    || m_mode == TaxCollectorDefenseMode.Surrounded
                    || m_mode == TaxCollectorDefenseMode.NoDefenders)
                {
                    decision.Score += reducesRisk ? 220 : 80;
                    if (reducesRisk && decision.Priority < AIDecisionPriority.High)
                        decision.Priority = AIDecisionPriority.High;
                }

                decision.Score = Math.Max(1, decision.Score);
                decision.Reason = "TaxCollector reposition";
                yield return decision;
            }
        }

        private IEnumerable<AIDecision> EvaluateFinishEnemy(AIContext context, IReadOnlyList<AIDecision> attacks)
        {
            foreach (var raw in attacks)
            {
                var target = FindTarget(context, raw);
                var spell = FindSpell(context, raw);
                if (target == null || spell == null || !CanCastNow(context, raw, spell))
                    continue;

                var damage = SpellEvaluator.EstimateDamage(spell);
                if (damage <= 0 || target.Life > damage)
                    continue;

                var decision = Copy(raw);
                decision.Priority = AIDecisionPriority.Critical;
                decision.Score += 1000 + TargetEvaluator.ScorePriorityTarget(target) / 2;
                decision.Reason = "TaxCollector finish attacker";
                yield return decision;
            }
        }

        private IEnumerable<AIDecision> EvaluateCriticalDefense(
            AIContext context,
            IReadOnlyList<AIDecision> heals,
            IReadOnlyList<AIDecision> buffs,
            IReadOnlyList<AIDecision> debuffs)
        {
            if (m_mode != TaxCollectorDefenseMode.LowHealth
                && m_mode != TaxCollectorDefenseMode.Surrounded
                && m_mode != TaxCollectorDefenseMode.NoDefenders)
            {
                yield break;
            }

            foreach (var raw in heals)
            {
                if (!IsSelfTarget(context, raw))
                    continue;

                var decision = Copy(raw);
                decision.Priority = AIDecisionPriority.Critical;
                decision.Score += 700 + MissingLife(context.Fighter);
                decision.Reason = "TaxCollector critical self heal";
                yield return decision;
            }

            foreach (var raw in buffs)
            {
                var spell = FindSpell(context, raw);
                if (!IsSelfTarget(context, raw) || !IsDefensiveSpell(context, spell))
                    continue;

                var decision = Copy(raw);
                decision.Priority = AIDecisionPriority.Critical;
                decision.Score += 650 + MissingLife(context.Fighter) / 2;
                decision.Reason = "TaxCollector critical self protection";
                yield return decision;
            }

            foreach (var raw in debuffs)
            {
                var target = FindTarget(context, raw);
                var spell = FindSpell(context, raw);
                if (target == null || !IsCloseThreat(context, target, 2) || !IsControlSpell(context, spell))
                    continue;

                var decision = BoostSpellDecision(context, raw, AIDecisionPriority.High, 500, "TaxCollector critical control");
                yield return decision;
            }
        }

        private IEnumerable<AIDecision> EvaluateSelfProtection(
            AIContext context,
            IReadOnlyList<AIDecision> heals,
            IReadOnlyList<AIDecision> buffs,
            IReadOnlyList<AIDecision> debuffs)
        {
            foreach (var raw in heals)
            {
                if (!IsSelfTarget(context, raw))
                    continue;

                var decision = BoostSpellDecision(context, raw, AIDecisionPriority.High, 300, "TaxCollector self heal");
                if (HpRatio(context.Fighter) <= 0.35)
                {
                    decision.Priority = AIDecisionPriority.Critical;
                    decision.Score += 400;
                }
                yield return decision;
            }

            foreach (var raw in buffs)
            {
                var spell = FindSpell(context, raw);
                if (!IsSelfTarget(context, raw) || !IsDefensiveSpell(context, spell))
                    continue;

                var bonus = m_mode == TaxCollectorDefenseMode.Normal ? 180 : 320;
                yield return BoostSpellDecision(context, raw, AIDecisionPriority.High, bonus, "TaxCollector self protection");
            }

            foreach (var raw in debuffs)
            {
                var target = FindTarget(context, raw);
                var spell = FindSpell(context, raw);
                if (target == null || !IsCloseThreat(context, target, 3) || !IsControlSpell(context, spell))
                    continue;

                yield return BoostSpellDecision(context, raw, AIDecisionPriority.High, 260, "TaxCollector pressure control");
            }
        }

        private IEnumerable<AIDecision> EvaluateDefenderSupport(
            AIContext context,
            IReadOnlyList<AIDecision> heals,
            IReadOnlyList<AIDecision> buffs)
        {
            foreach (var raw in heals)
            {
                var target = FindTarget(context, raw);
                if (!IsLivingDefender(context, target) || target.MaxLife <= 0)
                    continue;

                var missingLife = MissingLife(target);
                if (missingLife <= 0)
                    continue;

                var priority = HpRatio(target) <= 0.35 ? AIDecisionPriority.High : AIDecisionPriority.Normal;
                var decision = BoostSpellDecision(context, raw, priority, 260 + missingLife / 2, "TaxCollector heal defender");
                yield return decision;
            }

            foreach (var raw in buffs)
            {
                var target = FindTarget(context, raw);
                var spell = FindSpell(context, raw);
                if (!IsLivingDefender(context, target))
                    continue;

                var bonus = IsDefensiveSpell(context, spell) && HpRatio(target) <= 0.50 ? 260 : 120;
                yield return BoostSpellDecision(context, raw, AIDecisionPriority.Normal, bonus, "TaxCollector support defender");
            }
        }

        private IEnumerable<AIDecision> EvaluateDangerousAttackers(AIContext context, IReadOnlyList<AIDecision> debuffs)
        {
            foreach (var raw in debuffs)
            {
                var target = FindTarget(context, raw);
                var spell = FindSpell(context, raw);
                if (target == null || spell == null)
                    continue;

                var closeThreat = IsCloseThreat(context, target, 3);
                var dangerous = TargetEvaluator.ScorePriorityTarget(target) >= 80;
                if (!closeThreat && !dangerous)
                    continue;

                var bonus = 160 + TargetEvaluator.ScorePriorityTarget(target) / 3;
                if (IsControlSpell(context, spell))
                    bonus += closeThreat ? 350 : 220;

                yield return BoostSpellDecision(context, raw, AIDecisionPriority.High, bonus, "TaxCollector debuff dangerous attacker");
            }
        }

        private IEnumerable<AIDecision> EvaluateBestDebuffSpell(AIContext context, IReadOnlyList<AIDecision> debuffs)
        {
            foreach (var raw in debuffs)
            {
                var target = FindTarget(context, raw);
                var spell = FindSpell(context, raw);
                if (target == null || spell == null)
                    continue;

                var bonus = 100 + TargetEvaluator.ScorePriorityTarget(target) / 4;
                var priority = AIDecisionPriority.Normal;

                if (IsRemoveAPSpell(context, spell) || IsRemoveMPSpell(context, spell))
                {
                    bonus += 350;
                    priority = AIDecisionPriority.High;
                }

                if (IsRemoveRangeSpell(context, spell))
                    bonus += 220;

                if (IsPushPullSpell(context, spell) && IsCloseThreat(context, target, 2))
                    bonus += 260;

                if (IsUnbewitchSpell(context, spell))
                    bonus += 240;

                yield return BoostSpellDecision(context, raw, priority, bonus, "TaxCollector best debuff");
            }
        }

        private IEnumerable<AIDecision> EvaluateBestHealSpell(AIContext context, IReadOnlyList<AIDecision> heals)
        {
            foreach (var raw in heals)
            {
                var target = FindTarget(context, raw);
                if (target == null)
                    continue;

                var bonus = 100 + MissingLife(target) / 2;
                var priority = AIDecisionPriority.Normal;

                if (target == context.Fighter)
                {
                    bonus += 250;
                    if (HpRatio(target) <= 0.50)
                        priority = AIDecisionPriority.High;
                }
                else if (IsLivingDefender(context, target) && HpRatio(target) <= 0.35)
                {
                    bonus += 300;
                    priority = AIDecisionPriority.High;
                }

                yield return BoostSpellDecision(context, raw, priority, bonus, "TaxCollector best heal");
            }
        }

        private IEnumerable<AIDecision> EvaluateBestBuffSpell(AIContext context, IReadOnlyList<AIDecision> buffs)
        {
            foreach (var raw in buffs)
            {
                var target = FindTarget(context, raw);
                var spell = FindSpell(context, raw);
                if (target == null || spell == null)
                    continue;

                var bonus = 100;
                var priority = AIDecisionPriority.Normal;

                if (target == context.Fighter)
                {
                    bonus += IsDefensiveSpell(context, spell) ? 250 : 120;
                    if (m_mode != TaxCollectorDefenseMode.Normal)
                        priority = AIDecisionPriority.High;
                }
                else if (IsLivingDefender(context, target))
                {
                    bonus += IsDefensiveSpell(context, spell) && HpRatio(target) <= 0.50 ? 220 : 100;
                }
                else
                {
                    bonus -= 200;
                }

                yield return BoostSpellDecision(context, raw, priority, bonus, "TaxCollector best buff");
            }
        }

        private IEnumerable<AIDecision> EvaluateBestDamageSpell(AIContext context, IReadOnlyList<AIDecision> attacks)
        {
            foreach (var raw in attacks)
            {
                var target = FindTarget(context, raw);
                var spell = FindSpell(context, raw);
                if (target == null || spell == null)
                    continue;

                var bonus = 300 + TargetEvaluator.ScorePriorityTarget(target) / 4;
                var priority = raw.Priority;

                if (IsCloseThreat(context, target, 3)
                    && (m_mode == TaxCollectorDefenseMode.UnderPressure || m_mode == TaxCollectorDefenseMode.Surrounded))
                {
                    bonus += 160;
                    if (priority < AIDecisionPriority.High)
                        priority = AIDecisionPriority.High;
                }

                yield return BoostSpellDecision(context, raw, priority, bonus, "TaxCollector best damage");
            }
        }

        private AIDecision BoostSpellDecision(
            AIContext context,
            AIDecision raw,
            AIDecisionPriority priority,
            int scoreBonus,
            string reason)
        {
            var decision = Copy(raw);
            var spell = FindSpell(context, decision);

            if (!CanCastNow(context, decision, spell))
            {
                decision.Priority = AIDecisionPriority.Low;
                decision.Score = Math.Min(Math.Max(1, decision.Score), 80);
                decision.Reason = reason + " after movement";
                return decision;
            }

            if (decision.Priority < priority)
                decision.Priority = priority;

            decision.Score = Math.Max(1, decision.Score + scoreBonus);
            decision.Reason = reason;
            return decision;
        }

        private static IReadOnlyList<AIDecision> SafeEvaluate(IAIEvaluator evaluator, AIContext context)
        {
            var results = new List<AIDecision>();

            if (evaluator == null)
                return results;

            try
            {
                var decisions = evaluator.Evaluate(context);
                if (decisions == null)
                    return results;

                foreach (var decision in decisions)
                {
                    if (decision != null && decision.IsValid)
                        results.Add(decision);
                }
            }
            catch
            {
                return results;
            }

            return results;
        }

        private static bool HasUsableContext(AIContext context)
        {
            return context?.Fighter != null
                && context.Fight != null
                && context.Fighter.Team != null
                && context.Fighter.Cell != null
                && !context.Fighter.IsFighterDead;
        }

        private static bool CanMove(AIContext context)
        {
            return context?.Fighter != null
                && context.CurrentMP > 0
                && context.Fighter.CanBeMoved();
        }

        private static bool EnablesUsefulCastFrom(AIContext context, int fromCell)
        {
            if (context?.SpellBook?.AllSpells == null || context.Fighter == null)
                return false;

            foreach (var spell in context.SpellBook.DamageSpells.Concat(context.SpellBook.DebuffSpells))
            {
                if (spell == null || spell.APCost > context.CurrentAP)
                    continue;

                foreach (var enemy in context.Enemies)
                {
                    if (enemy?.Cell == null || enemy.IsFighterDead)
                        continue;

                    if (SpellEvaluator.CanCastFromCell(context, spell, fromCell, enemy.Cell.Id))
                        return true;
                }
            }

            foreach (var spell in context.SpellBook.HealSpells.Concat(context.SpellBook.BuffSpells))
            {
                if (spell == null || spell.APCost > context.CurrentAP)
                    continue;

                foreach (var ally in context.Allies)
                {
                    if (ally?.Cell == null || ally.IsFighterDead)
                        continue;

                    if (SpellEvaluator.CanCastFromCell(context, spell, fromCell, ally.Cell.Id))
                        return true;
                }
            }

            return false;
        }

        private static AIDecision Copy(AIDecision source)
        {
            if (source == null)
                return null;

            return new AIDecision
            {
                Type = source.Type,
                Priority = source.Priority,
                Score = source.Score,
                SpellId = source.SpellId,
                TargetId = source.TargetId,
                CellId = source.CellId,
                Reason = source.Reason,
                IsValid = source.IsValid
            };
        }

        private static AbstractFighter FindTarget(AIContext context, AIDecision decision)
        {
            if (context == null || decision?.TargetId == null)
                return null;

            var targetId = decision.TargetId.Value;
            return context.Allies.Concat(context.Enemies).FirstOrDefault(f => f != null && f.Id == targetId);
        }

        private static SpellLevel FindSpell(AIContext context, AIDecision decision)
        {
            if (context?.Fighter?.SpellBook == null || decision?.SpellId == null)
                return null;

            return context.Fighter.SpellBook.GetSpellLevel(decision.SpellId.Value);
        }

        private static bool CanCastNow(AIContext context, AIDecision decision, SpellLevel spell)
        {
            return decision?.CellId != null
                && spell != null
                && SpellEvaluator.CanCastFromCurrentCell(context, spell, decision.CellId.Value);
        }

        private static bool IsSelfTarget(AIContext context, AIDecision decision)
        {
            return context?.Fighter != null
                && decision?.TargetId != null
                && decision.TargetId.Value == context.Fighter.Id;
        }

        private static bool IsLivingDefender(AIContext context, AbstractFighter fighter)
        {
            return context?.Fighter != null
                && fighter != null
                && fighter != context.Fighter
                && fighter.Team == context.Fighter.Team
                && !fighter.IsFighterDead;
        }

        private static bool IsCloseThreat(AIContext context, AbstractFighter target, int distance)
        {
            if (context?.Fighter?.Cell == null || target?.Cell == null)
                return false;

            return context.TurnCache.Cells.GetDistance(context.CurrentCellId, target.Cell.Id) <= distance;
        }

        private static int MissingLife(AbstractFighter fighter)
        {
            if (fighter == null || fighter.MaxLife <= 0)
                return 0;

            return Math.Max(0, fighter.MaxLife - fighter.Life);
        }

        private static double HpRatio(AbstractFighter fighter)
        {
            if (fighter == null || fighter.MaxLife <= 0)
                return 1.0;

            return (double)fighter.Life / fighter.MaxLife;
        }

        private static bool IsDefensiveSpell(AIContext context, SpellLevel spell)
        {
            return ContainsSpell(context?.SpellBook?.DefensiveSpells, spell);
        }

        private static bool IsRemoveAPSpell(AIContext context, SpellLevel spell)
        {
            return ContainsSpell(context?.SpellBook?.RemoveAPSpells, spell);
        }

        private static bool IsRemoveMPSpell(AIContext context, SpellLevel spell)
        {
            return ContainsSpell(context?.SpellBook?.RemoveMPSpells, spell);
        }

        private static bool IsRemoveRangeSpell(AIContext context, SpellLevel spell)
        {
            return ContainsSpell(context?.SpellBook?.RemoveRangeSpells, spell);
        }

        private static bool IsPushPullSpell(AIContext context, SpellLevel spell)
        {
            return ContainsSpell(context?.SpellBook?.PushPullSpells, spell);
        }

        private static bool IsUnbewitchSpell(AIContext context, SpellLevel spell)
        {
            return ContainsSpell(context?.SpellBook?.UnbewitchSpells, spell);
        }

        private static bool IsControlSpell(SpellLevel spell)
        {
            return spell != null
                && (AISpellBook.HasRemoveAPEffect(spell)
                    || AISpellBook.HasRemoveMPEffect(spell)
                    || AISpellBook.HasRemoveRangeEffect(spell)
                    || AISpellBook.HasPushPullEffect(spell)
                    || AISpellBook.HasUnbewitchEffect(spell));
        }

        private static bool IsControlSpell(AIContext context, SpellLevel spell)
        {
            return IsControlSpell(spell)
                || IsRemoveAPSpell(context, spell)
                || IsRemoveMPSpell(context, spell)
                || IsRemoveRangeSpell(context, spell)
                || IsPushPullSpell(context, spell)
                || IsUnbewitchSpell(context, spell);
        }

        private static bool ContainsSpell(IReadOnlyList<SpellLevel> spells, SpellLevel spell)
        {
            return spell != null
                && spells != null
                && spells.Any(s => s != null && s.SpellId == spell.SpellId);
        }
    }
}
