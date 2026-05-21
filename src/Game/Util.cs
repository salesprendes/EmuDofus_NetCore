using System;
using System.Collections.Generic;
using Protocolo.Framework.Utils;
using Game.Fight;
using Game.Entity;

namespace Game
{
    /// <summary>
    /// 
    /// </summary>
    public static class Util
    {
        /// <summary>
        ///
        /// </summary>
        public static List<char> HASH = new List<char>() {'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's',
                't', 'u', 'v', 'w', 'x', 'y', 'z', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U',
                'V', 'W', 'X', 'Y', 'Z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '_'};

        // O(1) lookup for CharToCell — built once from HASH
        private static readonly Dictionary<char, int> s_hashIndex = BuildHashIndex();
        private static Dictionary<char, int> BuildHashIndex()
        {
            var d = new Dictionary<char, int>(HASH.Count);
            for (int i = 0; i < HASH.Count; i++)
                d[HASH[i]] = i;
            return d;
        }

        /// <summary>
        /// 
        /// </summary>
        private static char[] CHAR_LIST = new char[] 
                                         {
                                             '0', '1','2','3','4','5','6','7','8','9',
                                             'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
                                         };
        
        /// <summary>
        /// 
        /// </summary>
        private static FastRandom Random = new FastRandom();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int Next(int min, int max)
        {
            return Random.Next(min, max);
        }        
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int NextJet(int min, int max)
        {
            max++;
            if (max <= min)
                return min;
            return Next(min, max);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static String EncodeBase36(long input)
        {
            // max base-36 digits for long + optional sign = 13 chars
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

            if (negative) buf[--pos] = '-';

            return new string(buf, pos, 13 - pos);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellId"></param>
        /// <returns></returns>
        public static string CellToChar(int cellId)
        {
            return HASH[cellId / 64].ToString() + HASH[cellId % 64];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellCode"></param>
        /// <returns></returns>
        public static int CharToCell(string cellCode)
        {
            return s_hashIndex[cellCode[0]] * 64 + s_hashIndex[cellCode[1]];
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="winnersLevel"></param>
        /// <param name="losersLevel"></param>
        /// <returns></returns>
        public static int CalculWinHonor(int level, int winnersLevel, int losersLevel)
        {
            var basic = Math.Sqrt(level) * 10;
            var coef = losersLevel / winnersLevel;

            return (int)Math.Floor(basic * coef);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="level"></param>
        /// <param name="winnersLevel"></param>
        /// <param name="losersLevel"></param>
        /// <returns></returns>
        public static int CalculLoseHonor(int level, int winnersLevel, int losersLevel)
        {
            var basic = Math.Sqrt(level) * 10;
            var coef = losersLevel / winnersLevel;

            return (int)Math.Floor(basic * coef);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loot"></param>
        /// <param name="PP"></param>
        /// <param name="winnersTotalPP"></param>
        /// <returns></returns>
        public static long CalculPVMKamas(long loot, int PP, long winnersTotalPP)
        {
            return (long)Math.Round(loot * (PP / (double)winnersTotalPP) * WorldConfig.RATE_KAMAS);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="monsters"></param>
        /// <param name="droppers"></param>
        /// <param name="level"></param>
        /// <param name="wisdom"></param>
        /// <param name="ageBonus"></param>
        /// <returns></returns>
        public static long CalculPVMExperienceTaxCollector(IEnumerable<MonsterEntity> monsters, IEnumerable<AbstractFighter> droppers, int level, int wisdom, int challengeBonus = 0, int ageBonus = 0)
        {
            return (long)(CalculPVMExperience(monsters, droppers, level, wisdom, challengeBonus, ageBonus) * WorldConfig.TAXCOLLECTOR_XP_RATIO);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="monsters"></param>
        /// <param name="droppers"></param>
        /// <param name="level"></param>
        /// <param name="wisdom"></param>
        /// <param name="ageBonus"></param>
        /// <returns></returns>
        public static long CalculPVMExperience(IEnumerable<MonsterEntity> monsters, IEnumerable<AbstractFighter> droppers, int level, int wisdom, int challengeBonus = 0, int ageBonus = 0)
        {
            // single pass over monsters
            long monstersExperience = 0;
            int monstersTotalLevel = 0;
            int monstersMaxLevel = 0;
            int monsterCount = 0;
            foreach (var m in monsters)
            {
                monstersExperience += m.Grade.Experience;
                monstersTotalLevel += m.Grade.Level;
                if (m.Grade.Level > monstersMaxLevel) monstersMaxLevel = m.Grade.Level;
                monsterCount++;
            }
            if (monsterCount == 0) return 0;

            // single pass over droppers
            int playersTotalLevel = 0;
            int dropperCount = 0;
            foreach (var p in droppers)
            {
                playersTotalLevel += p.Level;
                dropperCount++;
            }
            if (dropperCount == 0) return 0;

            double totalLevelDeltaRate = 1;
            if (playersTotalLevel - 5 > monstersTotalLevel)
                totalLevelDeltaRate = monstersTotalLevel / (double)playersTotalLevel;
            else if (playersTotalLevel + 10 < monstersTotalLevel)
                totalLevelDeltaRate = (playersTotalLevel + 10) / (double)monstersTotalLevel;

            double levelDeltaRate = 1;
            if (level - 5 > monstersTotalLevel)
                levelDeltaRate = monstersTotalLevel / (double)level;
            else if (level + 10 < monstersTotalLevel)
                levelDeltaRate = (level + 10) / (double)monstersTotalLevel;

            var a = Math.Min(level, Math.Truncate(2.5 * monstersMaxLevel));
            var b = Math.Truncate(a / (double)level * 100);
            var c = Math.Truncate(a / (double)playersTotalLevel * 100);
            var d = Math.Truncate(monstersExperience * WorldConfig.PVM_RATE_GROUP[0] * levelDeltaRate);
            var e = Math.Truncate(monstersExperience * WorldConfig.PVM_RATE_GROUP[Math.Min(WorldConfig.PVM_RATE_GROUP.Length - 1, dropperCount - 1)] * totalLevelDeltaRate);
            var f = Math.Truncate(b / 100 * d);
            var g = Math.Truncate(c / 100 * e);
            var i = Math.Max(1, ageBonus / 100.0) + challengeBonus;
            var j = Math.Truncate((g * (100 + wisdom) / 100.0) * i);

            return (long)Math.Truncate(j * WorldConfig.RATE_XP);
        }       
    }
}


