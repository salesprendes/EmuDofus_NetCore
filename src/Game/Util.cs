using Game.Entity;
using Game.Fight;
using Protocolo.Framework.Utils;
using System;
using System.Collections.Generic;

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
            long monstersExperience = 0;
            int monstersTotalLevel = 0;
            int monstersMaxLevel = 0;
            int monsterCount = 0;
            foreach (var m in monsters)
            {
                monstersExperience += m.Grade.Experience;
                monstersTotalLevel += m.Grade.Level;
                if (m.Grade.Level > monstersMaxLevel)
                {
                    monstersMaxLevel = m.Grade.Level;
                }

                monsterCount++;
            }

            if (monsterCount == 0)
            {
                return 0;
            }

            int playersTotalLevel = 0;
            int dropperCount = 0;
            foreach (var p in droppers)
            {
                playersTotalLevel += p.Level;
                dropperCount++;
            }
            if (dropperCount == 0)
            {
                return 0;
            }

            double totalLevelDeltaRate = 1;
            if (playersTotalLevel - 5 > monstersTotalLevel)
            {
                totalLevelDeltaRate = monstersTotalLevel / (double)playersTotalLevel;
            }
            else if (playersTotalLevel + 10 < monstersTotalLevel)
            {
                totalLevelDeltaRate = (playersTotalLevel + 10) / (double)monstersTotalLevel;
            }

            double levelDeltaRate = 1;
            if (level - 5 > monstersTotalLevel)
            {
                levelDeltaRate = monstersTotalLevel / (double)level;
            }
            else if (level + 10 < monstersTotalLevel)
            {
                levelDeltaRate = (level + 10) / (double)monstersTotalLevel;
            }

            var a = Math.Min(level, Math.Truncate(2.5 * monstersMaxLevel));
            var b = Math.Truncate(a / (double)level * 100);
            var c = Math.Truncate(a / (double)playersTotalLevel * 100);
            var d = Math.Truncate(monstersExperience * WorldConfig.PVM_RATE_GROUP[0] * levelDeltaRate);
            var e = Math.Truncate(monstersExperience * WorldConfig.PVM_RATE_GROUP[Math.Min(WorldConfig.PVM_RATE_GROUP.Length - 1, dropperCount - 1)] * totalLevelDeltaRate);
            var g = Math.Truncate(c / 100 * e);
            var i = (1.0 + ageBonus / 100.0) * challengeBonus;
            var j = Math.Truncate((g * (100 + wisdom) / 100.0) * i);

            return (long)Math.Truncate(j * WorldConfig.RATE_XP);
        }
    }
}


