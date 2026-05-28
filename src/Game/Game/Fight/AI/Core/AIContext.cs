using Game.Fight.AI.Cache;
using Game.Fight;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Core
{
    public sealed class AIContext
    {
        public AIFighter Fighter { get; private set; }
        public AbstractFight Fight => Fighter?.Fight;
        public IReadOnlyList<AbstractFighter> Allies { get; private set; }
        public IReadOnlyList<AbstractFighter> Enemies { get; private set; }
        public int CurrentAP => Fighter?.AP ?? 0;
        public int CurrentMP => Fighter?.MP ?? 0;
        public int CurrentCellId => Fighter?.Cell?.Id ?? -1;
        public AISpellBook SpellBook { get; private set; }
        public AITurnBudget Budget { get; private set; }
        public AITurnCache TurnCache { get; private set; }
        public AILastDecisionMemory LastDecisionMemory { get; private set; }

        /// <summary>
        /// Lista de objetivos enemigos enriquecida con distancia pre-calculada,
        /// ordenada por distancia ascendente.  Se construye una vez por turno.
        /// Consume <see cref="Cache.AICellCache.GetDistance"/> para aprovechar
        /// el caché interno y evitar llamadas redundantes a Pathfinding.GoalDistance.
        /// </summary>
        public IReadOnlyList<AITargetInfo> EnemyTargets { get; private set; }

        /// <summary>
        /// Fase semántica del turno en curso.
        /// <see cref="AIBrain"/> la actualiza antes de vincular cada decisión.
        /// Los cerebros de boss pueden establecerla a <see cref="AITurnPhase.CriticalMechanic"/>
        /// cuando están evaluando mecánicas críticas.
        /// </summary>
        public AITurnPhase CurrentPhase { get; set; }

        public AIContext(AIFighter fighter)
            : this(fighter, new AILastDecisionMemory())
        {
        }

        public AIContext(AIFighter fighter, AILastDecisionMemory memory)
        {
            Fighter = fighter;
            LastDecisionMemory = memory ?? new AILastDecisionMemory();
            Allies = LoadAllies(fighter);
            Enemies = LoadEnemies(fighter);
            SpellBook = new AISpellBook(fighter);
            Budget = new AITurnBudget();
            TurnCache = new AITurnCache(fighter, Allies, Enemies, SpellBook);

            // EnemyTargets se construye después de TurnCache para usar GetDistance cacheado.
            EnemyTargets = BuildEnemyTargets(fighter, Enemies, TurnCache);
            CurrentPhase = AITurnPhase.Start;
        }

        private static IReadOnlyList<AbstractFighter> LoadAllies(AIFighter fighter)
        {
            if (fighter?.Team?.AliveFighters == null)
                return new List<AbstractFighter>();

            return fighter.Team.AliveFighters
                .Where(f => f != null && !f.IsFighterDead)
                .ToList();
        }

        private static IReadOnlyList<AbstractFighter> LoadEnemies(AIFighter fighter)
        {
            if (fighter?.Team?.OpponentTeam?.AliveFighters == null)
                return new List<AbstractFighter>();

            return fighter.Team.OpponentTeam.AliveFighters
                .Where(f => f != null && !f.IsFighterDead)
                .ToList();
        }

        private static IReadOnlyList<AITargetInfo> BuildEnemyTargets(
            AIFighter fighter,
            IReadOnlyList<AbstractFighter> enemies,
            AITurnCache cache)
        {
            if (fighter?.Cell == null || enemies == null || cache == null)
                return new List<AITargetInfo>();

            var origin = fighter.Cell.Id;
            return enemies
                .Where(e => e?.Cell != null)
                .Select(e => new AITargetInfo(e, cache.Cells.GetDistance(origin, e.Cell.Id)))
                .OrderBy(t => t.Distance)
                .ToList();
        }
    }
}
