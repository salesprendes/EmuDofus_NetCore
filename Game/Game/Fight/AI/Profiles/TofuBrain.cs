using Game.Entity;
using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using Game.Spell;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Profiles
{
    public sealed class TofuBrain : AIBrain
    {
        public TofuBrain(AIFighter fighter) : base(fighter) { }

        protected override IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            foreach (var decision in EvaluarAtaque(context))
                yield return decision;

            foreach (var decision in EvaluarAutoProteccion(context))
                yield return decision;

            foreach (var decision in EvaluarHuida(context))
                yield return decision;
        }

        private IEnumerable<AIDecision> EvaluarAtaque(AIContext context)
        {
            foreach (var decision in new AttackEvaluator().Evaluate(context))
            {
                decision.Score += BonusTipoObjetivo(context, decision.TargetId);

                if (decision.Priority < AIDecisionPriority.High)
                    decision.Priority = AIDecisionPriority.High;

                yield return decision;
            }
        }

        private static int BonusTipoObjetivo(AIContext context, long? targetId)
        {
            if (targetId == null)
                return 0;

            var target = context.Enemies?.FirstOrDefault(e => e != null && e.Id == targetId.Value);
            if (target == null)
                return 0;

            // Jugador real (no es una invocacion y es un personaje).
            if (target.Invocator == null && target.Type == EntityTypeEnum.TYPE_CHARACTER)
                return 300;

            // Invocacion estatica (la menos prioritaria).
            if (target.StaticInvocation)
                return 20;

            // Invocacion dinamica.
            if (target.Invocator != null)
                return 120;

            // Otros monstruos.
            return 60;
        }

        private IEnumerable<AIDecision> EvaluarAutoProteccion(AIContext context)
        {
            var self = context.Fighter;
            if (self?.Cell == null)
                yield break;

            if (context.LastDecisionMemory.GetUsageCountThisTurn(AIDecisionType.Buff) > 0)
                yield break;

            foreach (var spell in HechizosDeProteccion(context))
            {
                if (spell == null || spell.APCost > context.CurrentAP)
                    continue;

                if (!SpellEvaluator.CanCastFromCurrentCell(context, spell, self.Cell.Id))
                    continue;

                yield return new AIDecision
                {
                    Type = AIDecisionType.Buff,
                    Priority = AIDecisionPriority.Normal,
                    Score = 90,
                    SpellId = spell.SpellId,
                    TargetId = self.Id,
                    CellId = (short)self.Cell.Id,
                    Reason = "Tofuescapada: bonus de huida sobre si mismo"
                };
            }
        }

        private static IEnumerable<SpellLevel> HechizosDeProteccion(AIContext context)
        {
            return context.SpellBook.DefensiveSpells
                .Concat(context.SpellBook.BuffSpells)
                .Distinct();
        }
        
        private IEnumerable<AIDecision> EvaluarHuida(AIContext context)
        {
            var fleeCell = new MovementEvaluator().GetBestCellAwayFromEnemies(context);
            if (fleeCell.HasValue)
                yield return AIDecision.Move(fleeCell.Value, 70, AIDecisionPriority.Low, "Tofu huye del enemigo mas cercano");
        }
    }
}
