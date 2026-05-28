using Game.Fight.AI.Core;
using System.Collections.Generic;

namespace Game.Fight.AI.Cache
{
    public sealed class AITurnCache
    {
        public AIFighter Fighter { get; private set; }
        public IReadOnlyList<AbstractFighter> LivingAllies { get; private set; }
        public IReadOnlyList<AbstractFighter> LivingEnemies { get; private set; }
        public AISpellBook SpellBook { get; private set; }
        public AICellCache Cells { get; private set; }
        public AILineOfSightCache LineOfSight { get; private set; }

        public AITurnCache(AIFighter fighter, IReadOnlyList<AbstractFighter> allies, IReadOnlyList<AbstractFighter> enemies, AISpellBook spellBook)
        {
            Fighter = fighter;
            LivingAllies = allies ?? new List<AbstractFighter>();
            LivingEnemies = enemies ?? new List<AbstractFighter>();
            SpellBook = spellBook;
            Cells = new AICellCache(fighter);
            LineOfSight = new AILineOfSightCache(fighter?.Fight);
        }
    }
}
