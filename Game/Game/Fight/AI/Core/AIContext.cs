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

        // Enemigos vivos que la IA NO ve (invisibles sin detectar). No aparecen en Enemies para
        // no hacer trampa con su posicion, pero permiten saber que el combate NO esta vacio y
        // activar el comportamiento de busqueda en vez de pasar turno.
        public IReadOnlyList<AbstractFighter> HiddenEnemies { get; private set; }
        public int CurrentAP => Fighter?.AP ?? 0;
        public int CurrentMP => Fighter?.MP ?? 0;
        public int CurrentCellId => Fighter?.Cell?.Id ?? -1;
        public AISpellBook SpellBook { get; private set; }

        // Presupuesto del turno. Se puede inyectar uno persistente para que la planificacion
        // iterativa (un paso, ejecutar, re-planificar) acumule el gasto a lo largo del turno.
        public AITurnBudget Budget { get; set; }

        // Claves de decisiones que ya fallaron este turno (inyectado por el cerebro, persiste
        // entre re-planificaciones): evita reintentar la misma decision hasta agotar el
        // presupuesto de fallos y permite probar la siguiente mejor opcion.
        public HashSet<string> FailedDecisionKeys { get; set; } = new HashSet<string>();
        public AITurnCache TurnCache { get; private set; }
        public AILastDecisionMemory LastDecisionMemory { get; private set; }

        public IReadOnlyList<AITargetInfo> EnemyTargets { get; private set; }

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
            HiddenEnemies = LoadHiddenEnemies(fighter, Enemies);
            SpellBook = fighter?.AISpellBook ?? new AISpellBook(null);
            Budget = new AITurnBudget();
            TurnCache = new AITurnCache(fighter, Allies, Enemies, SpellBook);


            EnemyTargets = BuildEnemyTargets(fighter, Enemies, TurnCache);
            CurrentPhase = AITurnPhase.Start;
        }

        private static IReadOnlyList<AbstractFighter> LoadAllies(AIFighter fighter)
        {
            if (fighter?.Team?.AliveFighters == null)
                return new List<AbstractFighter>();

            return fighter.Team.AliveFighters.Where(f => f != null && !f.IsFighterDead).ToList();
        }

        private static IReadOnlyList<AbstractFighter> LoadEnemies(AIFighter fighter)
        {
            if (fighter?.Team?.OpponentTeam?.AliveFighters == null)
                return new List<AbstractFighter>();
                
            return fighter.Team.OpponentTeam.AliveFighters.Where(f => f != null && !f.IsFighterDead && IsEnemyDetected(fighter, f)).ToList();
        }
        
        private static IReadOnlyList<AbstractFighter> LoadHiddenEnemies(AIFighter fighter, IReadOnlyList<AbstractFighter> detected)
        {
            if (fighter?.Team?.OpponentTeam?.AliveFighters == null)
                return new List<AbstractFighter>();

            return fighter.Team.OpponentTeam.AliveFighters
                .Where(f => f != null && !f.IsFighterDead && !detected.Contains(f))
                .ToList();
        }
        
        private static bool IsEnemyDetected(AIFighter self, AbstractFighter enemy)
        {
            if (enemy.StateManager == null || !enemy.StateManager.HasState(FighterStateEnum.STATE_STEALTH))
                return true;

            if (self?.Cell == null || enemy.Cell == null || self.Fight?.Map == null)
                return false;

            // Señalo su casilla y no se ha movido (el marcador se limpia al moverse): sabemos donde esta.
            if (enemy.StealthSignalCell >= 0)
                return true;

            // Adyacente: deteccion muy probable al terminar el turno; decision estable por turno.
            if (Map.Pathfinding.GoalDistance(self.Fight.Map, self.Cell.Id, enemy.Cell.Id) <= 1)
            {
                if (self.StealthDetectionThisTurn.TryGetValue(enemy.Id, out var detected))
                    return detected;

                detected = Util.Next(0, 100) < 90;
                self.StealthDetectionThisTurn[enemy.Id] = detected;
                return detected;
            }

            return false;
        }

        private static IReadOnlyList<AITargetInfo> BuildEnemyTargets(
            AIFighter fighter,
            IReadOnlyList<AbstractFighter> enemies,
            AITurnCache cache)
        {
            if (fighter?.Cell == null || enemies == null || cache == null)
                return new List<AITargetInfo>();

            var origin = fighter.Cell.Id;
            return enemies.Where(e => e?.Cell != null).Select(e => new AITargetInfo(e, cache.Cells.GetDistance(origin, e.Cell.Id))).OrderBy(t => t.Distance).ToList();
        }
    }
}
