namespace Game.Fight.AI.Cache
{
    // Perfil de amenaza de un enemigo, precalculado una vez por turno: hasta donde puede llegar a
    // golpear este turno (movimiento + alcance de hechizo) y cuanto danio estimado puede infligir.
    public sealed class AIEnemyThreat
    {
        public AbstractFighter Fighter { get; }

        // Distancia maxima a la que puede alcanzar a golpear: PM + alcance del hechizo de danio.
        public int Reach { get; }

        // Danio estimado del mejor hechizo de danio contra nuestro luchador.
        public int Damage { get; }

        // El enemigo ataca a distancia (alcance de hechizo > 1).
        public bool Ranged { get; }

        public AIEnemyThreat(AbstractFighter fighter, int reach, int damage, bool ranged)
        {
            Fighter = fighter;
            Reach = reach;
            Damage = damage;
            Ranged = ranged;
        }
    }
}
