using Game.Fight.AI.Core;
using Game.Fight;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Evaluation
{
    public sealed class TargetEvaluator : IAIEvaluator
    {
        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            yield break;
        }

        public static int ScoreLowHp(AbstractFighter target)
        {
            if (target == null || target.MaxLife <= 0)
                return 0;

            return (int)(200 * (1.0 - (double)target.Life / target.MaxLife));
        }

        public static int ScoreKillChance(AbstractFighter attacker, AbstractFighter target)
        {
            return ScoreKillChance(attacker, target, 0);
        }

        public static int ScoreKillChance(AbstractFighter attacker, AbstractFighter target, int estimatedDamage)
        {
            if (target == null || target.IsFighterDead)
                return 0;

            if (estimatedDamage > 0 && target.Life <= estimatedDamage)
                return 1000;

            if (target.MaxLife > 0 && target.Life <= target.MaxLife / 5)
                return 250;

            return 0;
        }

        public static int ScorePriorityTarget(AbstractFighter target)
        {
            if (target == null)
                return 0;

            var score = target.Level;
            score += target.AP * 8;
            score += target.MP * 4;
            if (target.Invocator != null)
                score -= 25;
            return score;
        }

        public static AbstractFighter GetNearestEnemy(AIContext context)
        {
            // EnemyTargets ya está ordenada por distancia ascendente desde la
            // construcción del contexto — no hay que volver a calcular.
            if (context?.EnemyTargets == null || context.EnemyTargets.Count == 0)
                return null;

            return context.EnemyTargets[0].Target;
        }

        public static AbstractFighter GetWeakestEnemy(AIContext context)
        {
            if (context?.EnemyTargets == null)
                return null;

            // EnemyTargets filtra muertos en la construcción; solo quedan vivos.
            return context.EnemyTargets
                .Select(t => t.Target)
                .Where(e => e != null)
                .OrderBy(e => e.MaxLife > 0 ? (double)e.Life / e.MaxLife : 1.0)
                .ThenBy(e => e.Life)
                .FirstOrDefault();
        }

        public static AbstractFighter GetMostDangerousEnemy(AIContext context)
        {
            if (context?.EnemyTargets == null)
                return null;

            return context.EnemyTargets
                .Select(t => t.Target)
                .Where(e => e != null)
                .OrderByDescending(ScorePriorityTarget)
                .FirstOrDefault();
        }

        public static AbstractFighter GetBestAllyToHeal(AIContext context)
        {
            if (context?.Allies == null)
                return null;

            return context.Allies
                .Where(a => a != null && !a.IsFighterDead && a.MaxLife > 0 && a.Life < a.MaxLife)
                .OrderBy(a => (double)a.Life / a.MaxLife)
                .FirstOrDefault();
        }
    }
}
