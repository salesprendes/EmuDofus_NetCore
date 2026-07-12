using Game.Entity;
using Game.Fight.AI.Core;
using System.Collections.Generic;

namespace Game.Fight.AI
{
    public abstract class AIFighter : AbstractFighter
    {
        // Decision "detecto/esquivo esta trampa" por celda, estable durante todo el turno (el
        // caché de celdas se reconstruye en cada paso de planificacion, pero esto persiste en el
        // luchador). Se limpia al empezar el turno.
        public Dictionary<int, bool> TrapAvoidanceThisTurn { get; } = new Dictionary<int, bool>();

        // Decision "detecto a este enemigo invisible adyacente" por id de enemigo, estable durante
        // el turno. Se limpia al empezar el turno.
        public Dictionary<long, bool> StealthDetectionThisTurn { get; } = new Dictionary<long, bool>();


        public override bool TurnReady
        {
            get;
            set;
        }

        public override bool TurnPass
        {
            get;
            set;
        }

        public AIBrain CurrentBrain
        {
            get;
            protected set;
        }

        private AISpellBook m_aiSpellBook;

        // Categorizacion de hechizos por efecto (danio, cura, buff, etc.). Es estatica durante el
        // combate, asi que se calcula una sola vez por luchador y se reutiliza en cada
        // re-planificacion del turno en lugar de reconstruirla en cada paso.
        public AISpellBook AISpellBook => m_aiSpellBook ??= new AISpellBook(this);

        protected AIFighter(EntityTypeEnum type, long id, bool staticInvocation = false) : base(type, id, staticInvocation)
        {
            CurrentBrain = AIBrainFactory.Create(this, AIProfile.Default);
        }

        public override bool CanBeMoved()
        {
            return true;
        }

        public void RefreshBrain()
        {
            var profile = AIProfileResolver.Resolve(this);
            CurrentBrain = AIBrainFactory.Create(this, profile);
        }

        protected void SetBrain(AIProfile profile)
        {
            CurrentBrain = AIBrainFactory.Create(this, profile);
        }

        public override void JoinFight(AbstractFight fight, FightTeam team)
        {
            Life = MaxLife;

            base.JoinFight(fight, team);
        }
    }
}


