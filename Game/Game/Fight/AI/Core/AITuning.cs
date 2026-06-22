namespace Game.Fight.AI.Core
{
    // Punto central de ajuste de la economia de la IA. Centraliza los limites del presupuesto de
    // turno para poder tunearlos (por servidor o dificultad) sin tocar la logica. Los tiempos de
    // animacion estan en WorldConfig (FIGHT_AI_*); las puntuaciones concretas de cada evaluador
    // permanecen en linea a proposito, por claridad de cada heuristica.
    public static class AITuning
    {
        // Numero maximo de acciones que la IA encadena en un turno.
        public const int MaxActionsPerTurn = 6;

        // Tope de lanzamientos de hechizo por turno.
        public const int MaxSpellCastsPerTurn = 6;

        // Tope de movimientos por turno.
        public const int MaxMovementsPerTurn = 2;

        // Acciones fallidas toleradas antes de cerrar el turno (evita bucles).
        public const int MaxFailedActionsPerTurn = 3;
    }
}
