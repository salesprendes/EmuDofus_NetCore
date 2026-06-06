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
        public static List<char> HASH = new List<char>() {'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's',
            't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U',
            'V', 'W', 'X', 'Y', 'Z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '_'};

        private static char[] CHAR_LIST = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
        private static readonly Dictionary<char, int> s_hashIndex = BuildHashIndex();
        private static FastRandom Random = new FastRandom();

        private static Dictionary<char, int> BuildHashIndex()
        {
            var d = new Dictionary<char, int>(HASH.Count);
            for (int i = 0; i < HASH.Count; i++)
            {
                d[HASH[i]] = i;
            }

            return d;
        }

        public static int Next(int min, int max)
        {
            return Random.Next(min, max);
        }

        public static int NextJet(int min, int max)
        {
            max++;
            if (max <= min)
            {
                return min;
            }

            return Next(min, max);
        }

        public static String EncodeBase36(long input)
        {
            var buf = new char[13];
            int pos = 13;
            bool negative = input < 0;
            input = Math.Abs(input);

            do
            {
                buf[--pos] = CHAR_LIST[input % 36];
                input /= 36;
            }
            while (input != 0);

            if (negative)
            {
                buf[--pos] = '-';
            }

            return new string(buf, pos, 13 - pos);
        }

        public static string CellToChar(int cellId)
        {
            return HASH[cellId / 64].ToString() + HASH[cellId % 64];
        }

        public static int CharToCell(string cellCode)
        {
            return s_hashIndex[cellCode[0]] * 64 + s_hashIndex[cellCode[1]];
        }

        public static int CalculWinHonor(int level, int winnersLevel, int losersLevel)
        {
            var basic = Math.Sqrt(level) * 10;
            var coef = losersLevel / winnersLevel;

            return (int)Math.Floor(basic * coef);
        }

        public static int CalculLoseHonor(int level, int winnersLevel, int losersLevel)
        {
            var basic = Math.Sqrt(level) * 10;
            var coef = losersLevel / winnersLevel;

            return (int)Math.Floor(basic * coef);
        }

        public static long CalculPVMKamas(long loot, int PP, long winnersTotalPP)
        {
            return (long)Math.Round(loot * (PP / (double)winnersTotalPP) * WorldConfig.RATE_KAMAS);
        }

        public static long CalculPVMExperienceTaxCollector(IEnumerable<MonsterEntity> monsters, IEnumerable<AbstractFighter> droppers, int level, int wisdom, double challengeBonus = 1.0, int ageBonus = 0)
        {
            return (long)(CalculPVMExperience(monsters, droppers, level, wisdom, challengeBonus, ageBonus) * WorldConfig.TAXCOLLECTOR_XP_RATIO);
        }

        public static long CalculPVMExperience(IEnumerable<MonsterEntity> monsters, IEnumerable<AbstractFighter> droppers, int level, int wisdom, double challengeBonus = 1.0, int ageBonus = 0)
        {
            if (level <= 0 || monsters == null || droppers == null)
            {
                return 0;
            }

            List<MonsterEntity> monstruosValidos = monsters.Where(monstruo => monstruo?.Grade != null).ToList();
            List<AbstractFighter> luchadoresRecompensables = droppers.Where(luchador => luchador != null).ToList();

            if (!monstruosValidos.Any() || !luchadoresRecompensables.Any())
            {
                return 0;
            }

            List<AbstractFighter> jugadores = luchadoresRecompensables.Where(luchador => luchador.Type == EntityTypeEnum.TYPE_CHARACTER).ToList();

            List<AbstractFighter> participantesGrupo = jugadores.Any() ? jugadores : luchadoresRecompensables;

            long experienciaBase = monstruosValidos.Sum(monstruo => (long)monstruo.Grade.Experience);
            int nivelTotalMonstruos = monstruosValidos.Sum(monstruo => monstruo.Grade.Level);
            int nivelMonstruoMasAlto = monstruosValidos.Max(monstruo => monstruo.Grade.Level);
            int nivelTotalJugadores = participantesGrupo.Sum(jugador => jugador.Level);
            int nivelJugadorMasAlto = participantesGrupo.Max(jugador => jugador.Level);
            int cantidadParticipantesGrupo = participantesGrupo.Count(jugador => jugador.Level * 3 >= nivelJugadorMasAlto);

            if (experienciaBase <= 0 || nivelTotalMonstruos <= 0 || nivelMonstruoMasAlto <= 0 || nivelTotalJugadores <= 0 || cantidadParticipantesGrupo <= 0)
            {
                return 0;
            }

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


