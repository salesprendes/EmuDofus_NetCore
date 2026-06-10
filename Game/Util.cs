using Game.Entity;
using Game.Fight;
using Protocolo.Framework.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public static class Util
    {
        public static readonly string HASH = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
        private static ReadOnlySpan<char> Base36Chars => "0123456789abcdefghijklmnopqrstuvwxyz";
        private static readonly FastRandom Random = new FastRandom();
        private static readonly Dictionary<char, int> s_hashIndex = HASH.Select((c, i) => (c, i)).ToDictionary(x => x.c, x => x.i);
        public static int Next(int min, int max) => Random.Next(min, max);
        public static int NextJet(int min, int max) => ++max <= min ? min : Next(min, max);
        public static string CellToChar(int cellId) => string.Create(2, cellId, static (destination, id) => CellToChar(id, destination));
        public static void CellToChar(int cellId, Span<char> destination) => (destination[0], destination[1]) = (HASH[cellId / 64], HASH[cellId % 64]);
        public static int CharToCell(ReadOnlySpan<char> cellCode) => s_hashIndex[cellCode[0]] * 64 + s_hashIndex[cellCode[1]];
        public static int HashIndexOf(char value) => s_hashIndex.TryGetValue(value, out var index) ? index : -1;
        public static long CalculPVMKamas(long loot, int PP, long winnersTotalPP) => (long)Math.Round(loot * (PP / (double)winnersTotalPP) * WorldConfig.RATE_KAMAS);
        public static long CalculPVMExperienceTaxCollector(IEnumerable<MonsterEntity> monsters, IEnumerable<AbstractFighter> droppers, int level, int wisdom, double challengeBonus = 1.0, int ageBonus = 0) => (long)(CalculPVMExperience(monsters, droppers, level, wisdom, challengeBonus, ageBonus) * WorldConfig.TAXCOLLECTOR_XP_RATIO);

        public static string EncodeBase36(long input)
        {
            Span<char> buf = stackalloc char[14];
            int pos = 14;
            ulong value = unchecked(input < 0 ? (ulong)(-input) : (ulong)input);

            do
            {
                buf[--pos] = Base36Chars[(int)(value % 36)];
                value /= 36;
            }
            while (value != 0);

            if (input < 0)
                buf[--pos] = '-';

            return new string(buf.Slice(pos));
        }

        public static long CalculPVMExperience(IEnumerable<MonsterEntity> monsters, IEnumerable<AbstractFighter> droppers, int level, int wisdom, double challengeBonus = 1.0, int ageBonus = 0)
        {
            if (level <= 0 || monsters == null || droppers == null)
                return 0;

            List<MonsterEntity> monstruosValidos = monsters.Where(monstruo => monstruo?.Grade != null).ToList();
            List<AbstractFighter> luchadoresRecompensables = droppers.Where(luchador => luchador != null).ToList();

            if (monstruosValidos.Count == 0 || luchadoresRecompensables.Count == 0)
                return 0;

            List<AbstractFighter> jugadores = luchadoresRecompensables.Where(luchador => luchador.Type == EntityTypeEnum.TYPE_CHARACTER).ToList();
            List<AbstractFighter> participantesGrupo = jugadores.Count != 0 ? jugadores : luchadoresRecompensables;

            long experienciaBase = monstruosValidos.Sum(monstruo => (long)monstruo.Grade.Experience);
            int nivelTotalMonstruos = monstruosValidos.Sum(monstruo => monstruo.Grade.Level);
            int nivelMonstruoMasAlto = monstruosValidos.Max(monstruo => monstruo.Grade.Level);
            int nivelTotalJugadores = participantesGrupo.Sum(jugador => jugador.Level);
            int nivelJugadorMasAlto = participantesGrupo.Max(jugador => jugador.Level);
            int cantidadParticipantesGrupo = participantesGrupo.Count(jugador => jugador.Level * 3 >= nivelJugadorMasAlto);

            if (experienciaBase <= 0 || nivelTotalMonstruos <= 0 || nivelMonstruoMasAlto <= 0 || nivelTotalJugadores <= 0 || cantidadParticipantesGrupo <= 0)
                return 0;

            double coeficienteNivelTotal = nivelTotalMonstruos > nivelTotalJugadores + 10 ? (nivelTotalJugadores + 10) / (double)nivelTotalMonstruos : nivelTotalJugadores > nivelTotalMonstruos + 5 ? nivelTotalMonstruos / (double)nivelTotalJugadores : 1.0;

            var limiteNivelMonstruoMasAlto = Math.Truncate(nivelMonstruoMasAlto * 2.5);
            var coeficienteMonstruoMasAlto = nivelTotalJugadores > limiteNivelMonstruoMasAlto ? limiteNivelMonstruoMasAlto / nivelTotalJugadores : 1.0;

            var indiceCoeficienteGrupo = Math.Min(WorldConfig.PVM_RATE_GROUP.Length - 1, cantidadParticipantesGrupo - 1);
            var coeficienteGrupo = WorldConfig.PVM_RATE_GROUP[indiceCoeficienteGrupo];
            var coeficienteRepartoPersonaje = level / (double)nivelTotalJugadores;

            var xpGrupo = Math.Truncate(experienciaBase * coeficienteGrupo * coeficienteNivelTotal * coeficienteMonstruoMasAlto);
            var xpPersonaje = Math.Truncate(coeficienteRepartoPersonaje * xpGrupo);
            var coeficienteSabiduria = (100 + wisdom) / 100.0;
            var coeficienteBonusCombate = (1.0 + ageBonus / 100.0) * challengeBonus;
            var xpFinal = Math.Truncate(xpPersonaje * coeficienteSabiduria * coeficienteBonusCombate);

            return (long)Math.Truncate(xpFinal * WorldConfig.RATE_XP);
        }
    }
}


