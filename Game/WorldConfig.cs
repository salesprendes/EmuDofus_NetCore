using Game.Database.Structure;
using Game.Job;
using Game.Network;
using Protocolo.Framework.Configuration;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace Game
{
    public static class WorldConfig
    {
        public static int GetStartCell(CharacterBreedEnum breed)
        {
            return breed switch
            {
                CharacterBreedEnum.BREED_CRA => 219,
                CharacterBreedEnum.BREED_ECAFLIP => 297,
                CharacterBreedEnum.BREED_ENIRIPSA => 270,
                CharacterBreedEnum.BREED_ENUTROF => 272,
                CharacterBreedEnum.BREED_FECA => 321,
                CharacterBreedEnum.BREED_IOP => 235,
                CharacterBreedEnum.BREED_OSAMODAS => 219,
                CharacterBreedEnum.BREED_PANDAWA => 249,
                CharacterBreedEnum.BREED_SACRIEUR => 229,
                CharacterBreedEnum.BREED_SADIDAS => 255,
                CharacterBreedEnum.BREED_SRAM => 219,
                CharacterBreedEnum.BREED_XELOR => 286,
                _ => throw new Exception("BreedId desconocido: " + breed),
            };
        }

        public static int GetStartMap(CharacterBreedEnum breed)
        {
            return breed switch
            {
                CharacterBreedEnum.BREED_CRA => 10285,
                CharacterBreedEnum.BREED_ECAFLIP => 10276,
                CharacterBreedEnum.BREED_ENIRIPSA => 10283,
                CharacterBreedEnum.BREED_ENUTROF => 10299,
                CharacterBreedEnum.BREED_FECA => 10300,
                CharacterBreedEnum.BREED_IOP => 10294,
                CharacterBreedEnum.BREED_OSAMODAS => 10285,
                CharacterBreedEnum.BREED_PANDAWA => 10289,
                CharacterBreedEnum.BREED_SACRIEUR => 10296,
                CharacterBreedEnum.BREED_SADIDAS => 10279,
                CharacterBreedEnum.BREED_SRAM => 10285,
                CharacterBreedEnum.BREED_XELOR => 10298,
                _ => throw new Exception("BreedId desconocido: " + breed),
            };
        }

        public static readonly FrozenDictionary<JobIdEnum, int[]> JOB_TOOLS = new Dictionary<JobIdEnum, int[]>()
        {
            { JobIdEnum.JOB_BUCHERON, new int[] { 454, 8539, 1378, 2608, 478, 2593, 2592, 2600, 2604, 456, 502, 675, 674, 923, 927, 515, 782, 673, 676, 771 } },
            { JobIdEnum.JOB_PAYSAN, new int[] { 577, 765, 8127, 8540, 8992, 12006 } },
            { JobIdEnum.JOB_PECHEUR, new int[] { 596, 1860, 1863, 1865, 1866, 1867, 1868, 2188, 2366, 8541 } },
            { JobIdEnum.JOB_FORGEUR_EPEES, new int[] { 494 } },
            { JobIdEnum.JOB_MINEUR, new int[] { 497 } },
            { JobIdEnum.JOB_ALCHIMISTE, new int[] { 1473 } },
            { JobIdEnum.JOB_TAILLEUR, new int[] { 951 } },
            { JobIdEnum.JOB_BOULANGER, new int[] { 492 } },
            { JobIdEnum.JOB_SCULPTEUR_ARCS, new int[] { 500 } },
            { JobIdEnum.JOB_FORGEUR_DE_MARTEAUX, new int[] { 493 } },
            { JobIdEnum.JOB_FORGEUR_DE_BOUCLIERS, new int[] { 7098 } },
            { JobIdEnum.JOB_CORDONNIER, new int[] { 579 } },
            { JobIdEnum.JOB_BIJOUTIER, new int[] { 491 } },
            { JobIdEnum.JOB_SCULPTEUR_DE_BATONS, new int[] { 498 } },
            { JobIdEnum.JOB_SCULPTEUR_DE_BAGUETTES, new int[] { 499 } },
            { JobIdEnum.JOB_FORGEUR_DE_DAGUES, new int[] { 495 } },
            { JobIdEnum.JOB_FORGEUR_DE_PELLES, new int[] { 496 } },
            { JobIdEnum.JOB_FORGEUR_DE_HACHES, new int[] { 922 } },
            { JobIdEnum.JOB_BRICOLEUR, new int[] { 7650 } },
            { JobIdEnum.JOB_CHASSEUR, new int[] { } },
            { JobIdEnum.JOB_BOUCHER, new int[] { 1945 } },
            { JobIdEnum.JOB_POISSONNIER, new int[] { 1946 } },
        }.ToFrozenDictionary();

        public static readonly FrozenDictionary<int, int> BOOST_ITEMS = new Dictionary<int, int>() { { 8950, 8943 } }.ToFrozenDictionary();


        public static readonly FrozenSet<int> MULTIPLE_INSTANCE_MAP_ID = new[] { 10276, 10279, 10283, 10285, 10289, 10294, 10296, 10298, 10299, 10300 }.ToFrozenSet();

        public static readonly FrozenSet<int> MAPAS_TABERNA = new int[]
        {
            10354, 7573, 7572, 7574, 465, 463, 6064, 461, 462,
            5867, 6197, 6021, 6044, 8196, 6055, 8195, 1905, 1907, 6049
        }.ToFrozenSet();




        public static readonly FrozenDictionary<int, int> BALANCED_INSTANCE_MAPS = new Dictionary<int, int>()
        {

            { 10325, 2 }, { 10326, 2 }, { 10327, 2 }, { 10328, 2 },
            { 10329, 2 }, { 10330, 2 }, { 10331, 2 }, { 10337, 2 },

            { 10347, 2 },

            { 10332, 2 }, { 10333, 2 },

            { 10338, 2 }, { 10339, 2 }, { 10343, 2 }, { 10346, 2 },
            { 10348, 2 }, { 10349, 2 }, { 10351, 2 },

            { 10357, 2 }, { 10358, 2 },

            { 10352, 2 }, { 10359, 2 }, { 10360, 2 }, { 10361, 2 },
            { 10362, 2 }, { 10363, 2 }, { 10364, 2 },

            { 10335, 2 }, { 10354, 2 }, { 10355, 2 }, { 10356, 2 },

            { 10340, 2 }, { 10341, 2 }, { 10342, 2 },

            { 10344, 2 }, { 10345, 2 },
        }.ToFrozenDictionary();

        [Configurable()] public static int SPAWN_MAX_GROUP_PER_MAP = 3;

        [Configurable()] public static int SPAWN_CHECK_INTERVAL = 1 * 60 * 1000;


        [Configurable()] public static int MONSTER_REPOP_DELAY_MIN = 5 * 60 * 1000;
        [Configurable()] public static int MONSTER_REPOP_DELAY_MAX = 10 * 60 * 1000;

        [Configurable()] public static int MAX_AWAY_TIME = 20 * 60 * 1000;
        [Configurable()] public static int INACTIVITY_CHECK_INTERVAL = MAX_AWAY_TIME / 2;

        [Configurable()] public static int RPC_ACCOUNT_TICKET_TIMEOUT = 5000;
        [Configurable()] public static int RPC_ACCOUNT_TICKET_CHECK_INTERVAL = RPC_ACCOUNT_TICKET_TIMEOUT / 2;

        [Configurable()] public static int WORLD_SAVE_INTERVAL = 20 * 60 * 1000;

        [Configurable()]
        public static string WORLD_DB_CONNECTION = "Server=localhost;Database=game_emudofus;Uid=root;Pwd=;SslMode=Disabled;Pooling=true;Min Pool Size=3;Max Pool Size=30;Connection Timeout=30;Connection Lifetime=300;";

        [Configurable()] public static string AUTH_DB_NAME = "login_emudofus";

        [Configurable()] public static string RPC_PASSWORD = "servidorDofus";

        [Configurable()] public static string RPC_IP = "127.0.0.1";

        [Configurable()] public static string RPC_REMOTE_IP = "127.0.0.1";

        [Configurable()] public static int RPC_PORT = 4321;

        [Configurable()] public static int CHARACTER_CREATION_LEVEL = 1;

        [Configurable()] public static int CHARACTER_CREATION_LIFE = 55;

        [Configurable()] public static int CHARACTER_CREATION_EMOTE_CAPACITY = 1376255;

        [Configurable()] public static int PVM_MAX_STAR_BONUS = 1000;
        [Configurable()] public static int PVM_STAR_BONUS_PERCENT_SECONDS = 10;

        [Configurable()] public static int PVM_START_TIMEOUT = 60000;

        [Configurable()] public static int PVM_TURN_TIME = 30000;

        public static double[] PVM_RATE_GROUP = [1, 1.1, 1.5, 2.3, 3.1, 3.6, 4.2, 4.7];

        [Configurable()] public static double RATE_XP = 5;

        [Configurable()] public static double RATE_DROP = 3;

        [Configurable()] public static double RATE_KAMAS = 2;

        [Configurable()] public static double TAXCOLLECTOR_XP_RATIO = 0.05;

        public static int FIGHT_DISCONNECTION_TURN = 20;
        public static int FIGHT_PUSH_CELL_TIME = 270;
        public static int FIGHT_PANDA_LAUNCH_CELL_TIME = 250;
        public static int FIGHT_AI_START_DELAY = 250;
        public static int FIGHT_AI_THINK_DELAY = 150;
        public static int FIGHT_AI_SPELL_LAUNCH_TIME = 1200;
        public static int FIGHT_AI_SPELL_EFFECT_DELAY = 90;
        public static int FIGHT_AI_SPELL_SPECIAL_DELAY = 200;
        public static int FIGHT_AI_MOVE_DELAY = 120;

        public static readonly FrozenDictionary<ChatChannelEnum, long> CHAT_RESTRICTED_DELAY = new Dictionary<ChatChannelEnum, long>()
        {
            { ChatChannelEnum.CHANNEL_GENERAL, 100 },
            { ChatChannelEnum.CHANNEL_DEALING, 10000 },
            { ChatChannelEnum.CHANNEL_RECRUITMENT, 10000 },
            { ChatChannelEnum.CHANNEL_GUILD, 300 },
            { ChatChannelEnum.CHANNEL_GROUP, 300 },
            { ChatChannelEnum.CHANNEL_TEAM, 300 }
        }.ToFrozenDictionary();

        [Configurable()] public static int GAME_ID = 1;

        [Configurable("LogDebug")] public static bool LOG_DEBUG = true;

        [Configurable("LogLevel")] public static string LOG_LEVEL = "Info";

        [Configurable("LogConsoleLevel")] public static string LOG_CONSOLE_LEVEL = "";

        [Configurable("LogFileLevel")] public static string LOG_FILE_LEVEL = "";
    }
}


