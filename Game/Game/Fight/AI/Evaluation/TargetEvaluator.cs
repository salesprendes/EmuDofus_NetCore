using Game.Fight.AI.Core;
using Game.Fight;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Evaluation
{
    // Coleccion de helpers de puntuacion/seleccion de objetivos. No es un evaluador (no produce
    // decisiones por si mismo): lo usan los evaluadores reales a traves de sus metodos estaticos.
    public static class TargetEvaluator
    {
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

        /// <summary>
        /// Peligrosidad del enemigo contra NOSOTROS (0-100), del perfil de amenazas del turno:
        /// prioriza eliminar antes a quien más daño nos puede hacer.
        /// </summary>
        public static int ScoreThreat(AIContext context, AbstractFighter enemy)
        {
            if (context == null || enemy == null)
                return 0;

            foreach (var threat in RiskEvaluator.GetEnemyThreats(context))
            {
                if (threat.Fighter == enemy)
                    return System.Math.Min(threat.Damage, 400) / 4;
            }

            return 0;
        }

        public static AbstractFighter GetNearestEnemy(AIContext context)
        {


            if (context?.EnemyTargets == null || context.EnemyTargets.Count == 0)
                return null;

            return context.EnemyTargets[0].Target;
        }

        public static AbstractFighter GetWeakestEnemy(AIContext context)
        {
            if (context?.EnemyTargets == null)
                return null;


            return context.EnemyTargets.Select(t => t.Target).Where(e => e != null).OrderBy(e => e.MaxLife > 0 ? (double)e.Life / e.MaxLife : 1.0).ThenBy(e => e.Life).FirstOrDefault();
        }

        public static AbstractFighter GetMostDangerousEnemy(AIContext context)
        {
            if (context?.EnemyTargets == null)
                return null;

            return context.EnemyTargets.Select(t => t.Target).Where(e => e != null).OrderByDescending(ScorePriorityTarget).FirstOrDefault();
        }

        public static AbstractFighter GetBestAllyToHeal(AIContext context)
        {
            if (context?.Allies == null)
                return null;

            return context.Allies.Where(a => a != null && !a.IsFighterDead && a.MaxLife > 0 && a.Life < a.MaxLife).OrderBy(a => (double)a.Life / a.MaxLife).FirstOrDefault();
        }
    }
}
