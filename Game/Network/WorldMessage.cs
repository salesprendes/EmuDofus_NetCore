using Game.Action;
using Game.Auction;
using Game.Conquest;
using Game.Database.Structure;
using Game.Entity;
using Game.Exchange;
using Game.Fight;
using Game.Guild;
using Game.Interactive;
using Game.Job;
using Game.Manager;
using Game.Mount;
using Game.Quest;
using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Network
{
    public enum ChatChannelEnum
    {
        CHANNEL_GENERAL = '*',
        CHANNEL_RECRUITMENT = '?',
        CHANNEL_DEALING = ':',
        CHANNEL_TEAM = '#',
        CHANNEL_ADMIN = '@',
        CHANNEL_GROUP = '$',
        CHANNEL_GUILD = '%',
        CHANNEL_ALIGNMENT = '!',
        CHANNEL_PRIVATE_SEND = 'T',
        CHANNEL_PRIVATE_RECEIVE = 'F',
    }

    public enum InformationTypeEnum
    {
        INFO = 0,
        ERROR = 1,
        PVP = 2,
    }

    public enum InformationEnum
    {
        INFO_SERVER_MESSAGE = 0,

        INFO_LIFE_RECOVERED = 1,
        INFO_JOB_LEARNT = 2,

        INFO_GAVE_KAMAS_TO_OPEN = 20,

        INFO_CARACTERISTIC_UPGRADED = 15,
        INFO_WON_JOB_XP = 17,

        INFO_EXPERIENCE_GAINED = 8,
        INFO_ENERGY_RECOVERED = 7,

        INFO_WAYPOINT_SAVED = 6,
        INFO_WAYPOINT_REGISTERED = 24,

        INFO_SPELLPOINT_GAINED = 16,

        INFO_JUST_REBORN = 33,
        INFO_ENERGY_LOST = 34,

        INFO_YOU_ARE_AWAY = 37,
        INFO_YOU_ARE_NOT_AWAY_ANYMORE = 38,

        INFO_KAMAS_WON = 45,
        INFO_KAMAS_LOST = 46,

        INFOS_QUEST_NEW = 54,
        INFOS_QUEST_UPDATE = 55,
        INFOS_QUEST_END = 56,

        INFO_YOU_ARE_AWAY_PLAYERS_CANT_RESPOND = 72,

        INFO_ALIGNMENT_DISHONOR_UP = 75,
        INFO_ALIGNMENT_DISHONOR_DOWN = 77,

        INFO_ALIGNMENT_HONOR_UP = 80,
        INFO_ALIGNMENT_HONOR_DOWN = 76,

        INFO_ALIGNMENT_RANK_UP = 82,
        INFO_ALIGNMENT_RANK_DOWN = 83,

        INFO_BASIC_WARNING_BEFORE_SANCTION = 116,
        INFO_BASIC_LAST_CONNECTION = 152,
        INFO_BASIC_CURRENT_IP = 153,

        INFO_FRIEND_ONLINE = 143,

        INFO_CRAFT_FAILED = 118,
        INFO_MAGIC_FAILED = 117,
        INFO_MAGIC_NOT_PERFECT = 194,

        INFO_FIGHT_SPECTATOR_JOINED = 36,
        INFO_FIGHT_TOGGLE_PARTY = 93,
        INFO_FIGHT_UNTOGGLE_PARTY = 94,
        INFO_FIGHT_TOGGLE_PLAYER = 95,
        INFO_FIGHT_UNTOGGLE_PLAYER = 96,
        INFO_FIGHT_TOGGLE_SPECTATOR = 40,
        INFO_FIGHT_UNTOGGLE_SPECTATOR = 39,
        INFO_FIGHT_TOGGLE_HELP = 103,
        INFO_FIGHT_UNTOGGLE_HELP = 104,
        INFO_FIGHT_DISCONNECT_TURN_REMAIN = 162,
        INFO_FIGHT_CHALLENGE_FAILED_DUE_TO = 188,

        INFO_CHAT_SPAM_RESTRICTED = 115,

        INFO_GUILD_KICKED_HIMSELF = 176,
        INFO_GUILD_KICKED = 177,

        INFO_CONTRACT_FAILED = 179,

        INFO_AUCTION_TOO_MANY_ITEMS = 59,
        INFO_AUCTION_RARE = 60,
        INFO_AUCTION_ADD_INVALID_TYPE = 61,
        INFO_ITEM_ALREADY_SOLD = 64,
        INFO_AUCTION_BANK_CREDITED = 65,
        INFO_AUCTION_EXPIRED = 67,
        INFO_AUCTION_LOT_BOUGHT = 68,

        ERROR_UNABLE_LEARN_JOB = 6,
        ERROR_TOO_MUCH_JOB = 9,
        ERROR_ALREADY_JOB = 11,

        ERROR_PLAYER_AWAY_MESSAGE = 14,

        ERROR_UNABLE_LEARN_SPELL = 7,

        ERROR_CONDITIONS_UNSATISFIED = 19,

        ERROR_STORAGE_ALREADY_IN_USE = 20,

        ERROR_CHAT_SAME_MESSAGE = 84,

        ERROR_AUCTION_HOUSE_TOO_MANY_ITEMS = 66,
        ERROR_INVALID_PRICE = 99,
        ERROR_NOT_ENOUGH_KAMAS_FOR_TAXE = 65,
        ERROR_MAX_TAXCOLLECTOR_BY_SUBAREA_REACHED = 168,
        ERROR_NOT_ENOUGH_KAMAS = 128,
        ERROR_YOU_ARE_AWAY = 116,
        ERROR_GUILD_NOT_ENOUGH_RIGHTS = 101,
        ERROR_WORLD_SAVING = 164,
        ERROR_WORLD_SAVING_FINISHED = 165,

        ERROR_MAX_INVOCATION_REACHED = 203,

        ERROR_PLAYER_AWAY_NOT_INVITABLE = 209,

        ERROR_SERVER_BETA = 225,
        ERROR_SERVER_WELCOME = 89,

        ERROR_PET_ALREADY_EQUIPPED = 88,

        INFO_LIVING_ITEM_CANT_EQUIP = 1161,
        INFO_LIVING_ITEM_CANT_ASSOCIATE_INANIMATE = 1162,
        INFO_LIVING_ITEM_WONT_EAT = 1163,

        ERROR_NOT_ENOUGH_KAMAS_TO_PAY_MERCHANT_MODE_TAXE = 76,
        ERROR_NOT_ENOUGH_ITEMS_TO_BE_MERCHANT = 23,
        ERROR_TOO_MANY_MERCHANT_ON_MAP = 25,

        ERROR_FIGHT_SPECTATOR_LOCKED = 57,
        ERROR_FIGHTER_DISCONNECTED = 182,
        ERROR_FIGHTER_RECONNECTED = 184,
        ERROR_FIGHT_WAITING_PLAYERS = 29,
        ERROR_FIGHTER_KICKED_DUE_TO_DISCONNECTION = 30,

        ERROR_MOUNT_MATURITY_LOW = 176,

        ERROR_GUILD_BOSS_LEFT_NEW_BOSS = 199,

        ERROR_PRISM_DISHONOUR = 83,
        ERROR_PRISM_RANK_TOO_LOW = 155,
        ERROR_PRISM_WRONG_ALIGNMENT = 34,
        ERROR_PRISM_WINGS_NOT_ACTIVE = 148,
        ERROR_PRISM_INVALID_MAP = 146,
        ERROR_PRISM_ALREADY_CONQUERED = 149,

        PVP_PRISM_DESTROYED = 154,
    }

    public enum GamePopupTypeEnum
    {
        TYPE_INSTANT = 1,
        TYPE_ON_DISCONNECT = 0,
    }

    public enum GameMessageEnum
    {
        MESSAGE_MUCH_SPAM = 0,
        MESSAGE_MUCH_INACTIVE = 1,
        MESSAGE_BANK_KAMAS_NEEDED = 10,
        MESSAGE_ENERGY_LOW = 11,
        MESSAGE_TOMBESTONE = 12,
        MESSAGE_MAINTENANCE = 13,
        MESSAGE_TRANSFORMED_TO_GHOST_NEED_PHEONIX = 15,
        MESSAGE_MAINTENANCE_SOON = 3,
        MESSAGE_CONNECTION_CLOSED_DUE_TO_MAINTENANCE = 4,
        MESSAGE_KICKED = 18,
    }

    public enum ExchangeMoveEnum
    {
        MOVE_OBJECT = 'O',
        MOVE_GOLD = 'G',
    }

    public enum OperatorEnum
    {
        OPERATOR_ADD = '+',
        OPERATOR_REMOVE = '-',
        OPERATOR_REFRESH = '~',
    }

    public enum MountEquipErrorEnum
    {
        INVENTORY_NOT_EMPTY = '-',
        ALREADY_HAVE_ONE = '+',
        UNKNOW_ERROR = 'r',
    }

    public static class WorldMessage
    {
        public static string IM_INFO_MESSAGE(InformationEnum info, params object[] args)
        {
            return INFORMATION_MESSAGE(InformationTypeEnum.INFO, info, args);
        }

        public static string IM_ERROR_MESSAGE(InformationEnum info, params object[] args)
        {
            return INFORMATION_MESSAGE(InformationTypeEnum.ERROR, info, args);
        }

        public static string IM_PVP_MESSAGE(InformationEnum info, params object[] args)
        {
            return INFORMATION_MESSAGE(InformationTypeEnum.PVP, info, args);
        }

        public static string BASIC_NO_OPERATION()
        {
            return "BN";
        }

        public static string SPELL_UPGRADE_SUCCESS(int spellId, int level)
        {
            return "SUK" + spellId + "~" + level;
        }

        public static string SPELL_UPGRADE_ERROR()
        {
            return "SUE";
        }

        public static string HELLO_GAME()
        {
            return "HG";
        }

        public static string ACCOUNT_TICKET_ERROR()
        {
            return "ATE";
        }

        public static string ACCOUNT_TICKET_SUCCESS()
        {
            return "ATK0";
        }

        public static string ACCOUNT_REGIONAL_VERSION()
        {
            return "AVfr";
        }

        public static string CHARACTER_CREATION_SUCCESS()
        {
            return "AAK";
        }

        public static string CHARACTER_CREATION_ERROR()
        {
            return "AAE";
        }

        public static string CHARACTER_CREATION_ERROR_FULL()
        {
            return "AAEf";
        }

        public static string CHARACTER_CREATION_ERROR_NAME_ALREADY_EXISTS()
        {
            return "AAEa";
        }

        public static string CHARACTER_CREATION_ERROR_BAD_NAME()
        {
            return "AAEn";
        }

        public static string CHARACTER_CREATION_ERROR_SUBSCRIPTION_OUT()
        {
            return "AAEs";
        }

        public static string CHARACTER_DELETION_ERROR() => "ADE";
        public static string CHARACTER_SELECTION_ERROR() => "ASE";
        public static string BASIC_TIME() => "BT" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        public static string BASIC_DATE() => "BD" + (DateTime.Now.Year - 1970) + '|' + (DateTime.Now.Month - 1) + '|' + DateTime.Now.Day;
        public static string BASIC_PONG() => "pong";
        public static string BASIC_QPONG() => "qpong";
        public static string BASIC_RPING(long ticks) => "rping" + ticks;

        public static string CHARACTER_LIST(List<CharacterDAO> characters)
        {
            var message = new StringBuilder("ALK31536000000", characters.Count * 50);
            if (characters.Count > 0)
            {
                message.Append('|').Append(characters.Count);

                foreach (var character in characters)
                {
                    message.Append('|').Append(character.Id);
                    message.Append(';').Append(character.Name);
                    message.Append(';').Append(character.Level);
                    message.Append(';').Append(character.Skin);
                    message.Append(';').Append(character.HexColor1);
                    message.Append(';').Append(character.HexColor2);
                    message.Append(';').Append(character.HexColor3);
                    message.Append(';');
                    character.SerializeAs_ActorLookMessage(message);
                    message.Append(';').Append(character.Merchant ? '1' : '0');
                    message.Append(';').Append(WorldConfig.GAME_ID);
                    message.Append(';').Append(character.Dead ? '1' : '0');
                    message.Append(';').Append(character.DeathCount);
                    message.Append(';').Append(character.MaxLevel);
                }
            }
            return message.ToString();
        }

        public static string GIFT_LIST(Database.Structure.AccountGiftDAO gift)
        {
            var items = new StringBuilder();
            if (!string.IsNullOrEmpty(gift.Items))
            {
                foreach (var entry in gift.Items.Split('|'))
                {
                    var parts = entry.Split(':');
                    if (parts.Length != 2) continue;
                    if (!int.TryParse(parts[0], out int templateId) || !int.TryParse(parts[1], out int quantity))
                        continue;

                    items.Append(';').Append(0).Append('~').Append(templateId.ToString("x")).Append('~').Append(quantity.ToString("x")).Append("~~");
                }
            }

            return new StringBuilder("Ag").Append(gift.GiftType).Append('|').Append(gift.Id).Append('|').Append(Uri.EscapeDataString(gift.Title ?? string.Empty)).Append('|').Append(Uri.EscapeDataString(gift.Description ?? string.Empty)).Append('|').Append(Uri.EscapeDataString(gift.GfxUrl ?? string.Empty)).Append('|').Append(items).ToString();
        }

        public static string GIFT_STORED_SUCCESS()
        {
            return "AG";
        }

        public static string GIFT_STORED_ERROR()
        {
            return "AGE";
        }

        public static string CHARACTER_SELECTION_SUCCESS(CharacterEntity character)
        {
            var message = new StringBuilder("ASK|");
            message.Append(character.Id).Append('|');
            message.Append(character.Name).Append('|');
            message.Append(character.Level).Append('|');
            message.Append((int)character.Breed).Append('|');
            message.Append(character.Sex).Append('|');
            message.Append(character.Skin).Append('|');
            message.Append(character.HexColor1).Append('|');
            message.Append(character.HexColor2).Append('|');
            message.Append(character.HexColor3).Append('|');
            character.Inventory.SerializeAs_BagContent(message);
            message.Append('|');
            return message.ToString();
        }

        public static string SPELLS_LIST(SpellBook book)
        {
            var message = new StringBuilder("SL");
            book.SerializeAs_SpellsListMessage(message);
            return message.ToString();
        }

        public static string AREAS_LIST() => "al|" + ConquestManager.Instance.SerializeAs_SubAreaAlignmentList();
        public static string ACCOUNT_RESTRICTIONS(int restrictions) => "AR" + Util.EncodeBase36(restrictions);
        public static string SPECIALISATION_SET(int alignmentId) => "ZS" + alignmentId;
        public static string SPECIALISATION_CHANGE(int alignmentId) => "ZC" + alignmentId;
        public static string CHAT_ENABLED_CHANNELS() => "cC+i*?:#@$%!TF";

        public static string CHAT_CHANNEL(bool enabled, ChatChannelEnum channel)
        {
            return "cC" + (enabled ? "+" : "-") + (char)channel;
        }

        public static string INVENTORY_WEIGHT(int pods, int maxPods)
        {
            return "Ow" + pods + "|" + maxPods;
        }

        public static string INFORMATION_MESSAGE(InformationTypeEnum type, InformationEnum id, params object[] args)
        {
            return "Im" + (int)type + (int)id + (args.Length > 0 ? ";" : "") + string.Join("~", args.Select(arg => arg.ToString()));
        }

        public static string GAME_CREATION_SUCCESS()
        {
            return "GCK|1|";
        }

        public static string GAME_DATA_MAP(int mapId, string mapDate, string key)
        {
            return "GDM|" + mapId + '|' + mapDate + '|' + key;
        }

        public static string ACCOUNT_STATS(CharacterEntity character)
        {
            var message = new StringBuilder("As", 200);
            message.Append(character.Experience).Append(',').Append(character.ExperienceFloorCurrent).Append(',').Append(character.ExperienceFloorNext).Append('|');

            message.Append(character.Kamas).Append('|');
            message.Append(character.CaractPoint).Append('|');
            message.Append(character.SpellPoint).Append('|');

            message
                .Append(character.AlignmentId).Append('~')
                .Append(character.AlignmentId).Append(',').Append('0').Append(',')
                .Append(character.AlignmentLevel).Append(',')
                .Append(character.Honour).Append(',')
                .Append(character.Dishonour).Append(',')
                .Append(character.AlignmentEnabled ? '1' : '0').Append('|');

            message.Append(character.Life).Append(',').Append(character.MaxLife).Append('|');

            message.Append(character.Energy).Append(',').Append(10000).Append('|');

            message.Append(character.Initiative).Append('|');
            message.Append(character.Prospection).Append('|');

            ReadOnlySpan<EffectEnum> s_stats = [EffectEnum.STAT_MAS_PA, EffectEnum.STAT_MAS_PM, EffectEnum.STAT_MAS_FUERZA, EffectEnum.STAT_MAS_VITALIDAD, EffectEnum.STAT_MAS_SABIDURIA,
                EffectEnum.STAT_MAS_SUERTE, EffectEnum.STAT_MAS_AGILIDAD, EffectEnum.STAT_MAS_INTELIGENCIA, EffectEnum.STAT_MAS_ALCANCE, EffectEnum.STAT_MAS_INVOCACIONES_MAX, EffectEnum.STAT_MAS_DANO,
                EffectEnum.STAT_MAS_DANO_FISICO, EffectEnum.STAT_MAESTRIA, EffectEnum.STAT_MAS_DANO_PORCENTAJE, EffectEnum.STAT_MAS_CURAS, EffectEnum.STAT_MAS_DANO_TRAMPA, EffectEnum.STAT_MAS_DANO_TRAMPA,
                EffectEnum.STAT_MAS_DANO_DEVUELTO_OBJETO, EffectEnum.STAT_MAS_DANO_CRITICO, EffectEnum.STAT_MAS_FALLO_CRITICO, EffectEnum.STAT_MAS_ESQUIVA_PA, EffectEnum.STAT_MAS_ESQUIVA_PM,
                EffectEnum.STAT_MAS_RESISTENCIA_NEUTRAL, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL, EffectEnum.STAT_MAS_RESISTENCIA_PVP_NEUTRAL, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_NEUTRAL,
                EffectEnum.STAT_MAS_RESISTENCIA_TIERRA, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA, EffectEnum.STAT_MAS_RESISTENCIA_PVP_TIERRA, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_TIERRA,
                EffectEnum.STAT_MAS_RESISTENCIA_AGUA, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA, EffectEnum.STAT_MAS_RESISTENCIA_PVP_AGUA, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_AGUA,
                EffectEnum.STAT_MAS_RESISTENCIA_AIRE, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE, EffectEnum.STAT_MAS_RESISTENCIA_PVP_AIRE, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_AIRE,
                EffectEnum.STAT_MAS_RESISTENCIA_FUEGO, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO, EffectEnum.STAT_MAS_RESISTENCIA_PVP_FUEGO, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_FUEGO];

            foreach (var stat in s_stats)
            {
                message.Append(character.Statistics.GetTotalEffect(stat).ToString()).Append('|');
            }

            return message.ToString();
        }

        public static string GAME_MAP_INFORMATIONS(OperatorEnum operation, params AbstractEntity[] entities)
        {
            var message = new StringBuilder("GM", entities.Length * 100);
            foreach (var actor in entities)
            {
                message.Append('|');
                message.Append((char)operation);
                actor.SerializeAs_GameMapInformations(operation, message);
            }
            return message.ToString();
        }

        public static string GAME_DATA_SUCCESS()
        {
            return "GDK";
        }

        public static string GAME_ACTION(GameActionTypeEnum type, long entityId, string args = "")
        {
            StringBuilder message = new StringBuilder("GA");

            switch (type)
            {
                case GameActionTypeEnum.CHANGE_MAP:
                case GameActionTypeEnum.MAP_MOVEMENT:
                case GameActionTypeEnum.FIGHT_SPELL_LAUNCH:
                case GameActionTypeEnum.FIGHT_WEAPON_USE:
                    message.Append((int)type);
                    break;
            }

            message.Append(';');
            message.Append((int)type).Append(';');
            message.Append(entityId).Append(';');
            message.Append(args);

            return message.ToString();
        }

        public static string GAME_ACTION(EffectEnum type, long entityId, string args = "") => "GA" + ';' + (int)type + ';' + entityId + ';' + args;
        public static string GAME_ACTION(int actionId, long entityId, string args = "") => "GA" + ';' + (int)actionId + ';' + entityId + ';' + args;
        public static string GAME_ACTION_FAILED() => "GAE";
        public static string CHAT_MESSAGE(ChatChannelEnum channel, long entityId, string entityName, string message) => "cMK" + (char)channel + '|' + entityId + '|' + entityName + '|' + message;

        public static string CHAT_MESSAGE_ERROR_PLAYER_OFFLINE()
        {
            return "cMEf";
        }

        public static string CHARACTER_NEW_LEVEL(int level)
        {
            return "AN" + level;
        }

        public static string GAME_MESSAGE(GamePopupTypeEnum type, GameMessageEnum message, params object[] args)
        {
            return "M" + (int)type + "" + (int)message + (args.Length > 0 ? "|" : "") + string.Join(";", args.Select(arg => arg.ToString()));
        }

        public static string SERVER_INFO_MESSAGE(string message)
        {
            return IM_INFO_MESSAGE(InformationEnum.INFO_SERVER_MESSAGE, message);
        }

        public static string GAME_OVER()
        {
            return "GO";
        }

        public static string GAME_DATA_ZONE(OperatorEnum op, int cell, int length, int color)
        {
            return "GDZ" + (char)op + cell + ";" + length + ";" + color;
        }

        public static string GAME_DATA_ZONE_CREATE(int cell, string data = "")
        {
            return "GDC" + cell + data;
        }

        public static string GAME_DATA_CELL(int cell, string data, string mask = "", bool permanentLevel = true)
        {
            return "GDC" + cell + ";" + data + mask + ";" + (permanentLevel ? "1" : "0");
        }

        public static string ENTITY_OBJECT_ACTUALIZE(AbstractEntity entity)
        {
            var message = new StringBuilder("Oa");
            message.Append(entity.Id);
            message.Append('|');
            entity.Inventory.SerializeAs_ActorLookMessage(message);

            return message.ToString();
        }

        public static string OBJECT_ADD_SUCCESS(ItemDAO item)
        {
            return "OAKO" + item.ToString();
        }

        public static string LIVING_ITEM_UPDATE(ItemDAO item)
        {
            return "OCO" + item.ToString();
        }

        public static string OBJECT_MOVE_ERROR()
        {
            return "OAE";
        }

        public static string OBJECT_MOVE_ERROR_REQUIRED_LEVEL()
        {
            return "OAEL";
        }

        public static string OBJECT_MOVE_ERROR_ALREADY_EQUIPED()
        {
            return "OAEA";
        }

        public static string OBJECT_MOVE_SUCCESS(long guid, int slot)
        {
            return "OM" + guid + "|" + (slot == (int)ItemSlotEnum.SLOT_INVENTORY ? "" : slot.ToString());
        }

        public static string OBJECT_QUANTITY_UPDATE(long guid, int quantity)
        {
            return "OQ" + guid + "|" + quantity;
        }

        public static string OBJECT_UPDATE(ItemDAO item)
        {
            var message = new StringBuilder("OC;");
            item.SerializeAs_BagContent(message);
            return message.ToString();
        }

        public static string OBJECT_REMOVE_SUCCESS(long guid)
        {
            return "OR" + guid;
        }

        public static string OBJECT_DROP_SUCCESS()
        {
            return "ODK";
        }

        public static string OBJECT_DROP_ERROR_CANT_DROP()
        {
            return "ODEE";
        }

        public static string OBJECT_SELL_ERROR()
        {
            return "OSE";
        }

        public static string EXCHANGE_VALIDATE(long actorId, bool validate)
        {
            return "EK" + (validate ? '1' : '0') + actorId;
        }

        public static string EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum move, OperatorEnum operation, string args)
        {
            return "EMK" + (char)move + (char)operation + args;
        }

        public static string EXCHANGE_DISTANT_MOVEMENT(ExchangeMoveEnum move, OperatorEnum operation, string args)
        {
            return "EmK" + (char)move + (char)operation + args;
        }

        public static string EXCHANGE_STORAGE_MOVEMENT(ExchangeMoveEnum move, OperatorEnum operation, string args)
        {
            return "EsK" + (char)move + (char)operation + args;
        }

        public static string EXCHANGE_CREATE(ExchangeTypeEnum type, string args = "")
        {
            return "ECK" + (int)type + (args != "" ? "|" + args : "");
        }

        public static string EXCHANGE_LEAVE(bool success = false)
        {
            return "EV" + (success ? "a" : "");
        }

        public static string EXCHANGE_BUY_ERROR()
        {
            return "EBE";
        }

        public static string EXCHANGE_SELL_ERROR()
        {
            return "ESE";
        }

        public static string EXCHANGE_SELL_SUCCESS()
        {
            return "ESK";
        }

        public static string EXCHANGE_BUY_SUCCESS()
        {
            return "EBK";
        }


        public static string EXCHANGE_SHOP_LIST(NonPlayerCharacterEntity npc)
        {
            var message = new StringBuilder("EL");
            npc.SerializeAs_ShopItemsListInformations(message);
            return message.ToString();
        }

        public static string EXCHANGE_REQUEST(long characterId, long targetId)
        {
            return "ERK" + characterId + '|' + targetId + "|1";
        }

        /// <summary>
        /// Petición de craft seguro: ERK&lt;iniciadorId&gt;|&lt;invitadoId&gt;|&lt;tipo&gt;. El tipo es el
        /// que mandó el iniciador (12 = artesano invita a un cliente, 13 = cliente pide a un
        /// artesano); el cliente muestra "esperando..." al iniciador y la invitación al invitado.
        /// </summary>
        public static string EXCHANGE_CRAFT_SECURE_REQUEST(long initiatorId, long invitedId, int requestType)
        {
            return "ERK" + initiatorId + '|' + invitedId + "|" + requestType;
        }

        /// <summary>
        /// Movimiento de objeto de pago en el craft seguro: Ep&lt;zona&gt;;O&lt;op&gt;&lt;args&gt;
        /// (zona 1 = pago siempre, zona 2 = pago si éxito).
        /// </summary>
        public static string EXCHANGE_PAY_ITEM(int zone, OperatorEnum operation, string args)
        {
            return "Ep" + zone + ";O" + (char)operation + args;
        }

        /// <summary>
        /// Movimiento de kamas de pago en el craft seguro: Ep&lt;zona&gt;;G&lt;kamas&gt;.
        /// </summary>
        public static string EXCHANGE_PAY_KAMAS(int zone, long kamas)
        {
            return "Ep" + zone + ";G" + kamas;
        }

        /// <summary>
        /// Movimiento en la zona cooperativa del craft seguro (resultado fabricado):
        /// Er&lt;K&gt;&lt;move&gt;&lt;op&gt;&lt;args&gt;.
        /// </summary>
        public static string EXCHANGE_COOP_MOVEMENT(ExchangeMoveEnum move, OperatorEnum operation, string args)
        {
            return "ErK" + (char)move + (char)operation + args;
        }

        /// <summary>
        /// Resultado de un craft seguro: EcK;&lt;tpl&gt;;&lt;rol&gt;&lt;nombre&gt;;&lt;stats&gt;
        /// (rol 'T' = "fabricado para", 'B' = "fabricado por").
        /// </summary>
        public static string CRAFT_SECURE_RESULT(int templateId, char role, string name, string stringEffects)
        {
            return "EcK;" + templateId + ";" + role + name + ";" + stringEffects;
        }

        public static string FIGHT_FLAG_DISPLAY(AbstractFight fight)
        {
            var message = new StringBuilder("Gc+");
            fight.SerializeAs_FightFlag(message);
            return message.ToString();
        }

        public static string FIGHT_FLAG_UPDATE(OperatorEnum ope, long leaderId, params AbstractFighter[] fighters)
        {
            var message = new StringBuilder("Gt").Append(leaderId);
            foreach (var fighter in fighters)
            {
                message.Append('|').Append((char)ope);
                message.Append(fighter.Id).Append(';');
                message.Append(fighter.Name).Append(';');
                message.Append(fighter.Level);
            }
            return message.ToString();
        }

        public static string FIGHT_FLAG_DESTROY(long fightId)
        {
            return "Gc-" + fightId;
        }

        public static string FIGHT_JOIN_SUCCESS(int fightState, bool cancelButton, bool challenge, bool specator, long startTimeOut, int fightType = 4)
        {
            return "GJK" + fightState + "|" + (cancelButton ? "1" : "0") + "|" + (challenge ? "1" : "0") + "|" + (specator ? "1" : "0") + "|" + (startTimeOut == -1 ? "" : startTimeOut.ToString()) + "|" + fightType;
        }

        public static string FIGHT_AVAILABLE_PLACEMENTS(long teamId, string places)
        {
            return "GP" + places + "|" + teamId;
        }

        public static string FIGHT_READY(long actorId, bool ready)
        {
            return "GR" + (ready ? "1" : "0") + actorId;
        }

        public static string FIGHT_COORDINATE_INFORMATIONS(params AbstractFighter[] fighters)
        {
            var message = new StringBuilder("GIC");
            foreach (var fighter in fighters)
            {
                message.Append('|');
                message.Append(fighter.Id).Append(';');
                message.Append(fighter.Cell.Id).Append(';');
                message.Append('1');
            }
            return message.ToString();
        }

        public static string FIGHT_STARTS()
        {
            return "GS";
        }

        public static string FIGHT_TURN_LIST(IEnumerable<AbstractFighter> fighters)
        {
            var message = new StringBuilder("GTL");
            foreach (var fighter in fighters)
            {
                message.Append('|');
                message.Append(fighter.Id);
            }
            return message.ToString();
        }

        public static string FIGHT_TURN_STARTS(long fighterId, long turnTime)
        {
            return "GTS" + fighterId + "|" + turnTime;
        }

        public static string FIGHT_TURN_MIDDLE(IEnumerable<AbstractFighter> fighters)
        {
            var message = new StringBuilder("GTM");

            foreach (var fighter in fighters)
            {
                message.Append('|');
                message.Append(fighter.Id).Append(';');
                message.Append(fighter.IsFighterDead ? '1' : '0').Append(';');
                message.Append(fighter.Life).Append(';');
                message.Append(fighter.AP).Append(';');
                message.Append(fighter.MP).Append(';');
                if (fighter.IsFighterDead || fighter.StateManager.HasState(FighterStateEnum.STATE_STEALTH))
                {
                    message.Append("").Append(";;");
                }
                else
                {
                    message.Append(fighter.Cell.Id).Append(";;");
                }
                message.Append(fighter.MaxLife);
            }

            return message.ToString();
        }

        public static string FIGHT_TURN_READY(long fighterId)
        {
            return "GTR" + fighterId;
        }

        public static string FIGHT_TURN_FINISHED(long fighterId)
        {
            return "GTF" + fighterId;
        }

        public static string FIGHT_LEAVE()
        {
            return "GV";
        }

        public static string FIGHT_COUNT(long count)
        {
            return "fC" + count;
        }

        public static string FIGHT_OPTION(FightOptionTypeEnum type, bool blocked, long teamId)
        {
            return "Go" + (blocked ? "+" : "-") + (char)type + teamId;
        }

        public static string FIGHT_LIST(IEnumerable<AbstractFight> fights)
        {
            var message = new StringBuilder("fL");
            foreach (var fight in fights)
                fight.SerializeAs_FightList(message);
            return message.ToString();
        }

        public static string FIGHT_DETAILS(AbstractFight fight)
        {
            var message = new StringBuilder("fD");
            message.Append(fight.Id);
            message.Append('|');
            foreach (var fighter in fight.Team0.AliveFighters)
            {
                message.Append(fighter.Name);
                message.Append('~');
                message.Append(fighter.Level);
                message.Append(';');
            }
            message.Append('|');
            foreach (var fighter in fight.Team1.AliveFighters)
            {
                message.Append(fighter.Name);
                message.Append('~');
                message.Append(fighter.Level);
                message.Append(';');
            }
            return message.ToString();
        }

        public static string FIGHT_EFFECT_INFORMATION(EffectEnum effect, long entityId, string value1, string value2, string value3, string chance, string duration, string spellId)
        {
            return "GIE" + (int)effect + ";" + entityId + ";" + value1 + ";" + value2 + ";" + value3 + ";" + chance + ";" + duration + ";" + spellId;
        }

        public static string FIGHT_ACTION_START(long entityId)
        {
            return "GAS" + entityId;
        }

        public static string FIGHT_ACTION_FINISHED(long entityId)
        {
            return "GAF2|" + entityId;
        }

        public static string FIGHT_END_RESULT(FightEndResult fightResult)
        {
            return fightResult.Message;
        }

        public static string PARTY_CREATE_SUCCESS(string ownerName)
        {
            return "PCK" + ownerName;
        }

        public static string PARTY_INVITE_SUCCESS(string leaderName, string memberInvited)
        {
            return "PIK" + leaderName + "|" + memberInvited;
        }

        public static string PARTY_REFUSE()
        {
            return "PR";
        }

        public static string PARTY_INVITE_ERROR_FULL()
        {
            return "PIEf";
        }

        public static string PARTY_INVITE_ERROR_PLAYER_OFFLINE(string distantPlayerName)
        {
            return "PIEn" + distantPlayerName;
        }

        public static string PARTY_INVITE_ERROR_ALREADY_IN_PARTY()
        {
            return "PIEa";
        }

        public static string PARTY_CREATE_ERROR_ALREADY_IN_PARTY()
        {
            return "PCEa";
        }

        public static string PARTY_CREATE_ERROR_FULL()
        {
            return "PCEf";
        }

        public static string PARTY_SET_LEADER(long id)
        {
            return "PL" + id;
        }

        public static string PARTY_MEMBER_LEFT(long id)
        {
            return "PM-" + id;
        }

        public static string PARTY_LEAVE(string kickerId = "")
        {
            return "PV" + kickerId;
        }

        public static string PARTY_MEMBER_LIST(params CharacterEntity[] members)
        {
            var message = new StringBuilder("PM+");
            foreach (var member in members)
            {
                member.SerializeAs_PartyMemberInformations(message);
                message.Append('|');
            }
            message.Remove(message.Length - 1, 1);
            return message.ToString();
        }

        public static string FIGHT_CHALLENGE_FAILED(int id)
        {
            return "GdOO" + id;
        }

        public static string FIGHT_CHALLENGE_SUCCESS(int id)
        {
            return "GdKK" + id;
        }

        public static string FIGHT_CHALLENGE_INFORMATIONS(int id, bool showTarget, long targetId, long basicXpBonus, long teamXpBonus, long basicDropBonus, long teamDropBonus, bool success)
        {
            return "Gd" + id + ";" + (showTarget ? "1" : "0") + ";" + targetId + ";" + basicXpBonus + ";" + teamXpBonus + ";" + basicDropBonus + ";" + teamDropBonus + ";" + (success ? "1" : "0");
        }

        public static string FIGHT_CELL_FLAG(int cellId, long fighterId = 0)
        {
            return "Gf" + fighterId + "|" + cellId;
        }

        public static string GUILD_STATS(GuildInstance guild, int power)
        {
            return "gS" + guild.Name + "|" + guild.Emblem + "|" + Util.EncodeBase36(power);
        }

        public static string GUILD_MEMBERS_INFORMATIONS(IEnumerable<GuildMember> members)
        {
            var message = new StringBuilder("gIM+", members.Count() * 40);
            foreach (var member in members)
            {
                member.SerializeAs_GuildMemberInformations(message);
            }
            message.Remove(message.Length - 1, 1);
            return message.ToString();
        }

        public static string GUILD_MEMBERS_INFORMATIONS(GuildMember member)
        {
            var message = new StringBuilder("gIM+");
            member.SerializeAs_GuildMemberInformations(message);
            message.Remove(message.Length - 1, 1);
            return message.ToString();
        }

        public static string GUILD_GENERAL_INFORMATIONS(bool isActive, int level, long experienceFloorCurrent, long experienceFloorNext, long experience)
        {
            return "gIG" + (isActive ? "1" : "0") + "|" + level + "|" + experienceFloorCurrent + "|" + experience + "|" + experienceFloorNext;
        }


        public static string GUILD_JOIN_ERROR_UNKNOW()
        {
            return "gJEu";
        }

        public static string GUILD_JOIN_ERROR_OCCUPIED()
        {
            return "gJEo";
        }

        public static string GUILD_JOIN_ERROR_ALREADY_IN_GUILD()
        {
            return "gJEa";
        }

        public static string GUILD_JOIN_ERROR_RESTRICTED()
        {
            return "gJEd";
        }

        public static string GUILD_JOIN_ERROR_REFUSED_DISTANT(string name)
        {
            return "gJEr" + name;
        }

        public static string GUILD_JOIN_ERROR_REFUSED_LOCAL()
        {
            return "gJEc";
        }

        public static string GUILD_JOIN_REQUEST_DISTANT(long characterId, string characterName, string guildName)
        {
            return "gJr" + characterId + "|" + characterName + "|" + guildName;
        }

        public static string GUILD_JOIN_REQUEST_LOCAL(string distantCharacterName)
        {
            return "gJR" + distantCharacterName;
        }

        public static string GUILD_JOIN_CLOSE()
        {
            return "gJC";
        }

        public static string GUILD_JOIN_ACCEPTED_LOCAL()
        {
            return "gJKj";
        }

        public static string GUILD_JOIN_ACCEPTED_DISTANT(string distantCharacterName)
        {
            return "gJKa" + distantCharacterName;
        }

        public static string GUIL_KICK_SUCCESS(string kicker, string kicked)
        {
            return "gKK" + kicker + "|" + kicked;
        }

        public static string GUILD_MEMBER_REMOVE(long memberId)
        {
            return "gIM-" + memberId;
        }

        public static string GUILD_CREATION_OPEN()
        {
            return "gn";
        }

        public static string GUILD_CREATION_CLOSE()
        {
            return "gV";
        }

        public static string GUILD_CREATION_SUCCESS()
        {
            return "gCK";
        }

        public static string GUILD_CREATION_ERROR_NAME_ALREADY_EXISTS()
        {
            return "gCEan";
        }

        public static string GUILD_CREATION_ERROR_ALREADY_IN_GUILD()
        {
            return "gCEa";
        }

        public static string GUILD_CREATION_ERROR_EMBLEM_ALREADY_EXISTS()
        {
            return "gCEae";
        }

        public static string GUILD_BOOST_INFORMATIONS(int boostPoint, int taxCollectorPrice, GuildStatistics stats)
        {
            var message = new StringBuilder("gIB", 50);
            message.Append(stats.MaxTaxcollector).Append('|');
            message.Append(0).Append('|');
            message.Append(stats.BaseStatistics.GetTotal(EffectEnum.STAT_MAS_VITALIDAD)).Append('|');
            message.Append(stats.BaseStatistics.GetTotal(EffectEnum.STAT_MAS_DANO)).Append('|');
            message.Append(stats.BaseStatistics.GetTotal(EffectEnum.STAT_MAS_PODS)).Append('|');
            message.Append(stats.BaseStatistics.GetTotal(EffectEnum.STAT_MAS_PROSPECCION)).Append('|');
            message.Append(stats.BaseStatistics.GetTotal(EffectEnum.STAT_MAS_SABIDURIA)).Append('|');
            message.Append(stats.MaxTaxcollector).Append('|');
            message.Append(boostPoint).Append('|');
            message.Append(taxCollectorPrice).Append('|');
            stats.Spells.SerializeAs_SpellsList(message);
            return message.ToString();
        }

        public static string GUILD_TAXCOLLECTOR_HIRED(TaxCollectorEntity taxCollector, string owner)
        {
            var message = new StringBuilder("gTS");
            message.Append(taxCollector.Name).Append('|');
            message.Append(taxCollector.Id).Append('|');
            message.Append(taxCollector.Map.X).Append('|');
            message.Append(taxCollector.Map.Y).Append('|');
            message.Append(owner);
            return message.ToString();
        }

        public static string GUILD_TAXCOLLECTOR_REMOVED(TaxCollectorEntity taxCollector, string remover)
        {
            var message = new StringBuilder("gTR");
            message.Append(taxCollector.Name).Append('|');
            message.Append(taxCollector.Id).Append('|');
            message.Append(taxCollector.Map.X).Append('|');
            message.Append(taxCollector.Map.Y).Append('|');
            message.Append(remover);
            return message.ToString();
        }

        public static string GUILD_TAXCOLLECTOR_FARMED(TaxCollectorEntity taxCollector, string farmer)
        {
            var message = new StringBuilder("gTG");
            message.Append(taxCollector.Name).Append('|');
            message.Append(taxCollector.Id).Append('|');
            message.Append(taxCollector.Map.X).Append('|');
            message.Append(taxCollector.Map.Y).Append('|');
            message.Append(farmer).Append('|');
            message.Append(taxCollector.ExperienceGathered);
            foreach (var farmedItem in taxCollector.FarmedItems)
            {
                message.Append(';');
                message.Append(farmedItem.Key).Append(',');
                message.Append(farmedItem.Value);
            }
            return message.ToString();
        }

        public static string GUILD_TAXCOLLECTOR_LIST(IEnumerable<TaxCollectorEntity> taxCollectors)
        {
            var message = new StringBuilder("gITM+");
            foreach (var taxCollector in taxCollectors)
            {
                message.Append(Util.EncodeBase36(taxCollector.Id)).Append(';');
                message.Append(taxCollector.Name).Append(';');
                message.Append(Util.EncodeBase36(taxCollector.MapId)).Append(';');
                if (taxCollector.HasGameAction(GameActionTypeEnum.FIGHT))
                {
                    message.Append('1').Append(';');
                    message.Append(45000 - taxCollector.Fight.UpdateTime).Append(';');
                }
                else
                {
                    message.Append('0').Append(';');
                    message.Append('0').Append(';');
                }
                message.Append(45000).Append(';');
                message.Append('7').Append('|');
            }
            message.Remove(message.Length - 1, 1);
            return message.ToString();
        }

        public static string GUILD_TAXCOLLECTOR_UNDER_ATTACK(string name, int mapX, int mapY)
        {
            return "gAA" + name + "|1|" + mapX + "|" + mapY;
        }

        public static string GUILD_TAXCOLLECTOR_SURVIVED(string name, int mapX, int mapY)
        {
            return "gAS" + name + "|1|" + mapX + "|" + mapY;
        }

        public static string GUILD_TAXCOLLECTOR_DIED(string name, int mapX, int mapY)
        {
            return "gAD" + name + "|1|" + mapX + "|" + mapY;
        }

        public static string GUILD_TAXCOLLECTOR_DEFENDER_LEAVE(long taxCollectorId, long memberId)
        {
            return "gITP-" + Util.EncodeBase36(taxCollectorId) + '|' + Util.EncodeBase36(memberId);
        }

        public static string GUILD_TAXCOLLECTOR_ATTACKER_LEAVE(long taxCollectorId, long attackerId)
        {
            return "gITp-" + Util.EncodeBase36(taxCollectorId) + '|' + Util.EncodeBase36(attackerId);
        }

        public static string GUILD_TAXCOLLECTOR_ATTACKER_JOIN(long taxCollectorId, params AbstractFighter[] attackers)
        {
            var message = new StringBuilder("gITp+").Append(Util.EncodeBase36(taxCollectorId));
            foreach (var attacker in attackers)
            {
                message.Append('|');
                message.Append(Util.EncodeBase36(attacker.Id)).Append(';');
                message.Append(attacker.Name).Append(';');
                message.Append(attacker.Level);
            }
            return message.ToString();
        }

        public static string GUILD_TAXCOLLECTOR_DEFENDER_JOIN(long taxCollectorId, params GuildMember[] members)
        {
            var message = new StringBuilder("gITP+").Append(Util.EncodeBase36(taxCollectorId));
            foreach (var member in members)
            {
                message.Append('|');
                member.SerializeAs_TaxCollectorDefender(message);
            }
            return message.ToString();
        }

        public static string DIALOG_CREATE(long npcId)
        {
            return "DCK" + npcId;
        }

        public static string DIALOG_LEAVE()
        {
            return "DV";
        }

        public static string DIALOG_QUESTION(int questionId, string parameters, IEnumerable<int> responseIds)
        {
            return "DQ" + questionId + ";" + parameters + "|" + string.Join(";", responseIds);
        }

        public static string AUCTION_HOUSE_AUCTION_OWNER_LIST(IEnumerable<AuctionEntry> entries)
        {
            StringBuilder message = new StringBuilder("EL");
            foreach (var entry in entries)
            {
                message.Append(entry.Item.Id).Append(';');
                message.Append(entry.Item.Quantity).Append(';');
                message.Append(entry.Item.TemplateId).Append(';');
                message.Append(entry.Item.StringEffects).Append(';');
                message.Append(entry.Price).Append(';');
                message.Append(entry.HoursLeft).Append('|');
            }
            if (message.Length > 2)
                message.Remove(message.Length - 1, 1);
            return message.ToString();
        }

        public static string AUCTION_HOUSE_TEMPLATE_LIST(int type, IEnumerable<int> templates)
        {
            return "EHL" + type + "|" + String.Join(";", templates);
        }

        public static string AUCTION_HOUSE_AUCTION_LIST(int templateId, IEnumerable<AuctionCategory> entries)
        {
            var message = new StringBuilder("EHl").Append(templateId);
            foreach (var entry in entries)
            {
                message.Append(entry.SerializeAs_BuyExchange());
            }
            return message.ToString();
        }

        public static string AUCTION_HOUSE_TEMPLATE_MOVEMENT(OperatorEnum op, int templateId)
        {
            return "EHM" + (char)op + templateId;
        }

        public static string AUCTION_HOUSE_CATEGORY_MOVEMENT(OperatorEnum op, AuctionCategory category)
        {
            var message = new StringBuilder("EHm").Append((char)op);
            category.SerializeAs_CategoryMovement(op, message);
            return message.ToString();
        }

        public static string AUCTION_HOUSE_MIDDLE_PRICE(int templateId, long middlePrice)
        {
            return "EHP" + templateId + "|" + middlePrice;
        }

        public static string INTERACTIVE_DATA_FRAME(int cellId, int frameId, bool activated)
        {
            return "GDF|" + cellId + ";" + frameId + ";" + (activated ? "1" : "0");
        }

        public static string INTERACTIVE_DATA_FRAME(IEnumerable<InteractiveObject> iobjects)
        {
            var message = new StringBuilder("GDF");
            foreach (var io in iobjects)
                io.SerializeAs_InteractiveListMessage(message);
            message.Remove(message.Length - 1, 1);
            return message.ToString();
        }

        public static string INTERACTIVE_DATA_FRAME_FIGHT(IEnumerable<InteractiveObject> iobjects)
        {
            var message = new StringBuilder("GDF");
            foreach (var io in iobjects)
                io.SerializeAs_FightInteractiveListMessage(message);

            message.Remove(message.Length - 1, 1);
            return message.ToString();
        }

        public static string INTERACTIVE_FARMED_QUANTITY(long characterId, long quantity)
        {
            return "IQ" + characterId + "|" + quantity;
        }

        public static string WAYPOINT_CREATE(CharacterEntity character, int superAreaId)
        {
            var message = new StringBuilder("WC").Append(character.SavedMapId);
            foreach (var waypointMapId in character.Waypoints)
            {
                var waypointMap = MapManager.Instance.GetById(waypointMapId);
                if (waypointMap == null)
                    continue;

                if (waypointMap.SubArea.Area.SuperAreaId != superAreaId)
                    continue;

                var price = 0;
                if (waypointMapId != character.MapId)
                    price = 10 * (Math.Abs(waypointMap.X - character.Map.X) + Math.Abs(waypointMap.Y - character.Map.Y) - 1);
                message.Append('|').Append(waypointMapId).Append(';').Append(price);
            }
            return message.ToString();
        }

        public static string WAYPOINT_LEAVE()
        {
            return "WV";
        }

        public static string WAYPOINT_USE_ERROR()
        {
            return "WUE";
        }

        public static string PRISM_CREATE(CharacterEntity character)
        {
            var currentMapId = character.MapId;
            var message = new StringBuilder("Wp").Append(currentMapId).Append('|').Append(currentMapId);

            foreach (var territory in ConquestManager.Instance.Territories)
            {
                if (territory.PrismType != ConquestPrismType.SubArea)
                    continue;
                if (territory.AlignmentId != character.AlignmentId)
                    continue;
                if (territory.PrismMapId <= 0 || territory.PrismMapId == currentMapId)
                    continue;

                message.Append('|').Append(territory.PrismMapId).Append(";0");
                if (territory.IsUnderAttack)
                    message.Append('*');
            }

            return message.ToString();
        }

        public static string PRISM_LEAVE()
        {
            return "Ww";
        }

        public static string BASIC_CONSOLE_MESSAGE(string message)
        {
            return "BAT2" + message;
        }

        public static string EXCHANGE_PERSONAL_SHOP_ITEMS_LIST(IEnumerable<ItemDAO> items)
        {
            var message = new StringBuilder("EL");
            foreach (var item in items)
            {
                message.Append(item.Id).Append(';');
                message.Append(item.Quantity).Append(';');
                message.Append(item.TemplateId).Append(';');
                message.Append(item.StringEffects).Append(';');
                message.Append(item.MerchantPrice).Append('|');
            }
            return message.ToString();
        }

        public static string EXCHANGE_STORAGE_ITEMS_LIST(IEnumerable<ItemDAO> items, long kamas)
        {
            var message = new StringBuilder("EL");
            foreach (var item in items)
            {
                message.Append('O');
                item.SerializeAs_BagContent(message);
            }
            message.Append(";G").Append(kamas);
            return message.ToString();
        }

        public static string MERCHANT_MODE_TAXE(long value)
        {
            return "Eq1|1|" + value;
        }

        public static string EXCHANGE_STORAGE_KAMAS_VALUE(long value)
        {
            return "EsKG" + value;
        }

        public static string ALIGNMENT_DISABLE_COST(int value)
        {
            return "GIP" + value;
        }

        public static string LIFE_RESTORE_TIME_START(double interval)
        {
            return "ILS" + interval;
        }

        public static string LIFE_RESTORE_TIME_FINISH(int lifeRestored)
        {
            return "ILF" + lifeRestored;
        }

        public static string FRIEND_DELETE_SUCCESS()
        {
            return "FD";
        }

        public static string ENNEMY_DELETE_SUCCESS()
        {
            return "iD";
        }

        public static string FRIEND_ADD(string playerPseudo, SocialRelationDAO friend)
        {
            var message = new StringBuilder("FA");
            message.Append('|');
            message.Append(friend.Pseudo);
            var characterFriend = EntityManager.Instance.GetCharacterByNickname(friend.Pseudo);
            characterFriend?.SerializeAs_FriendInformations(playerPseudo, message);
            return message.ToString();
        }

        public static string FRIEND_ADD_ERROR_UNKNOW_PLAYER()
        {
            return "FAEf";
        }

        public static string FRIEND_ADD_ERROR_ALREADY()
        {
            return "FAEa";
        }

        public static string FRIEND_ADD_ERROR_FULL()
        {
            return "FAEm";
        }

        public static string FRIEND_ADD_ERROR_YOURSELF()
        {
            return "FAEy";
        }

        public static string FRIENDS_LIST_ON_CONNECT(CharacterEntity character, IEnumerable<SocialRelationDAO> friends)
        {
            var message = new StringBuilder("FL");
            foreach (var friend in friends)
            {
                message.Append('|');
                message.Append(friend.Pseudo);
                var characterFriend = EntityManager.Instance.GetCharacterByNickname(friend.Pseudo);
                if (characterFriend != null)
                {
                    characterFriend.SerializeAs_FriendInformations(character.Account.Pseudo, message);
                    if (characterFriend.NotifyOnFriendConnection)
                        characterFriend.SafeDispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_FRIEND_ONLINE, character.Name));
                }
            }
            return message.ToString();
        }

        public static string FRIENDS_LIST(string playerPseudo, IEnumerable<SocialRelationDAO> friends)
        {
            var message = new StringBuilder("FL");
            foreach (var friend in friends)
            {
                message.Append('|');
                message.Append(friend.Pseudo);
                var characterFriend = EntityManager.Instance.GetCharacterByNickname(friend.Pseudo);
                characterFriend?.SerializeAs_FriendInformations(playerPseudo, message);
            }
            return message.ToString();
        }

        public static string ENNEMIES_LIST(string playerPseudo, IEnumerable<SocialRelationDAO> ennemies)
        {
            var message = new StringBuilder("iL");
            foreach (var ennemy in ennemies)
            {
                message.Append('|');
                message.Append(ennemy.Pseudo);
                var characterEnnemy = EntityManager.Instance.GetCharacterByNickname(ennemy.Pseudo);
                characterEnnemy?.SerializeAs_EnnemyInformations(playerPseudo, message);
            }
            return message.ToString();
        }

        public static string ENNEMIES_LIST_ON_CONNECT(CharacterEntity character, IEnumerable<SocialRelationDAO> ennemies)
        {
            var message = new StringBuilder("iL");
            foreach (var ennemy in ennemies)
            {
                message.Append('|');
                message.Append(ennemy.Pseudo);
                var characterEnnemy = EntityManager.Instance.GetCharacterByNickname(ennemy.Pseudo);
                characterEnnemy?.SerializeAs_EnnemyInformations(character.Account.Pseudo, message);
            }
            return message.ToString();
        }

        public static string ENNEMY_ADD_ERROR_UNKNOW_PLAYER()
        {
            return "iAEf";
        }

        public static string ENNEMY_ADD_ERROR_ALREADY()
        {
            return "iAEa";
        }

        public static string ENNEMY_ADD_ERROR_FULL()
        {
            return "iAEm";
        }

        public static string ENNEMY_ADD_ERROR_YOURSELF()
        {
            return "iAEy";
        }

        public static string ENNEMY_ADD(string playerPseudo, SocialRelationDAO ennemy)
        {
            var message = new StringBuilder("iA");
            message.Append('|');
            message.Append(ennemy.Pseudo);
            var characterEnnemy = EntityManager.Instance.GetCharacterByNickname(ennemy.Pseudo);
            characterEnnemy?.SerializeAs_EnnemyInformations(playerPseudo, message);
            return message.ToString();
        }

        public static string EMOTE_DIRECTION(long playerId, int direction)
        {
            return "eD" + playerId + "|" + direction;
        }

        public static string EMOTE_USE(long targetId, int emoteId, long time = 360000)
        {
            return "eUK" + targetId + "|" + emoteId + "|" + time;
        }

        public static string EMOTES_LIST(int capacity)
        {
            return "eL" + capacity + "|" + capacity;
        }

        public static string ITEM_SET(ItemSetDAO set, IEnumerable<ItemDAO> items)
        {
            if (set == null)
                return "";
            var message = new StringBuilder("OS+").Append(set.Id).Append('|');
            message.Append(String.Join(";", items.Select(item => item.TemplateId))).Append('|');
            message.Append(set.GetStats(items.Count()).ToItemStats());
            return message.ToString();
        }

        public static string OBJECT_CHANGE(IEnumerable<ItemDAO> items)
        {
            var message = new StringBuilder("OCK");
            foreach (var item in items)
                message.Append(item.ToString()).Append('*');
            return message.ToString();
        }

        public static string JOB_SKILL(JobBook book)
        {
            var message = new StringBuilder("JSK");
            book.SerializeAs_SkillListMessage(message);
            return message.ToString();
        }

        public static string JOB_SKILL(JobBook book, int jobId)
        {
            var message = new StringBuilder("JSK");
            book.SerializeAs_SkillListMessage(jobId, message);
            return message.ToString();
        }

        public static string JOB_XP(JobBook book)
        {
            var message = new StringBuilder("JXK");
            book.SerializeAs_JobXpMessage(message);
            return message.ToString();
        }

        public static string JOB_XP(JobBook book, int jobId)
        {
            var message = new StringBuilder("JXK");
            book.SerializeAs_JobXpMessage(jobId, message);
            return message.ToString();
        }

        public static string JOB_TOOL_EQUIPPED(string jobId = "n")
        {
            return "OT" + jobId;
        }

        public static string JOB_NEW_LEVEL(int jobId, int level)
        {
            return "JN" + jobId + "|" + level;
        }

        public static string JOB_OPTIONS(int jobIndex, int optionParams, int minSlots)
        {
            return "JO" + jobIndex + "|" + optionParams + "|" + minSlots;
        }

        public static string CRAFT_INTERACTIVE_SUCCESS(long characterId, int templateId)
        {
            return "IO" + characterId + "|+" + templateId;
        }

        public static string CRAFT_INTERACTIVE_FAILED(long characterId, int templateId)
        {
            return "IO" + characterId + "|-" + templateId;
        }

        public static string CRAFT_INTERACTIVE_NOTHING(long characterId)
        {
            return "IO" + characterId + "|-";
        }

        public static string CRAFT_NO_RESULT()
        {
            return "EcEI";
        }

        public static string CRAFT_TEMPLATE_CREATED(int templateId)
        {
            return "EcK;" + templateId;
        }

        public static string CRAFT_TEMPLATE_FAILED(int templateId)
        {
            return "EcEF;" + templateId;
        }

        public static string CRAFT_LOOP_COUNT(int count)
        {
            return "EA" + count;
        }

        public static string CRAFT_LOOP_END(int reason)
        {
            return "Ea" + reason;
        }

        public static string MOUNT_EXPERIENCE_SHARED(int percentage)
    => "Rx" + percentage;

        public static string MOUNT_NAME(string name)
    => "Rn" + name;

        public static string MOUNT_DATA(string informations)
    => "Rd" + informations;

        public static string MOUNT_RIDING_START()
    => "Rr+";

        public static string MOUNT_RIDING_STOP()
    => "Rr-";

        public static string GUILD_MOUNTPARKS(int maxMountParks, IEnumerable<Paddock> paddocks)
        {
            // Formato: gIF<maxParques>|<mapa>;<plazasMontura>;<plazasObjeto>;<monturas>|...
            // (la lista de monturas colocadas va vacia: aun no se modela su colocacion en el mapa).
            var message = new StringBuilder("gIF");
            message.Append(maxMountParks);
            foreach (var paddock in paddocks)
            {
                message.Append('|')
                       .Append(paddock.MapId).Append(';')
                       .Append(paddock.MountPlace).Append(';')
                       .Append(paddock.ItemPlace).Append(';');
            }
            return message.ToString();
        }

        public static string PADDOCK_BUY_START(int defaultPrice)
    => "RD|" + defaultPrice;

        public static string PADDOCK_BUY_LEAVE()
    => "Rv";

        public static string MOUNT_UNEQUIP()
    => "Re-";

        public static string MOUNT_EQUIP(string informations)
    => "Re+" + informations;

        public static string MOUNT_EQUIP_ERROR(MountEquipErrorEnum error)
    => "ReE" + (char)error;

        public static string PADDOCK_INFORMATIONS(int owner, long price, int size, int items, string guildName, string guildEmblem)
    => "Rp" + owner + ";" + price + ";" + size + ";" + items + ";" + guildName + ";" + guildEmblem;


        public static string HOUSE_INFO(bool isOwner, int houseId, bool hasLock, bool isForSale, bool hasGuild)
            => "hL" + (isOwner ? "+" : "-") + "|" + houseId + ";" + (hasLock ? 1 : 0) + ";" + (isForSale ? 1 : 0) + ";" + (hasGuild ? 1 : 0);


        public static string HOUSE_PROPERTIES(int houseId, string ownerName, bool isForSale, string guildName, string guildEmblem)
        {
            var sb = new System.Text.StringBuilder("hP");
            sb.Append(houseId).Append('|').Append(ownerName).Append(';').Append(isForSale ? 1 : 0);
            if (!string.IsNullOrEmpty(guildName))
                sb.Append(';').Append(guildName).Append(';').Append(guildEmblem);
            return sb.ToString();
        }


        public static string HOUSE_LOCK_STATUS(int houseId, bool locked)
            => "hX" + houseId + "|" + (locked ? 1 : 0);


        public static string HOUSE_GUILD_RIGHTS(string data)
            => "hG" + data;


        public static string HOUSE_BUY_DIALOG(int houseId, long price)
            => "hCK" + houseId + "|" + price;


        public static string HOUSE_CLOSE_BUY_DIALOG()
            => "hV";


        public static string HOUSE_SET_PRICE(int houseId, long price)
            => "hSK" + houseId + "|" + price;


        public static string KEY_DIALOG(bool modify, int maxLen) => "KCK" + (modify ? 1 : 0) + "|" + maxLen;


        public static string KEY_ERROR()
            => "KKE";


        public static string KEY_CLOSE()
            => "KV";

        public static string QUEST_LIST(List<CharacterQuest> quests)
        {

            return "QLK" + string.Join("|", quests.Select(q => q.Id + ";" + (q.Done ? "1" : "0") + ";" + "0"));
        }

        public static string QUEST_STEPS(CharacterQuest quest)
        {
            var message = new StringBuilder("QS");
            message.Append(quest.Id).Append('|');
            message.Append(quest.CurrentStepId).Append('|');
            message.Append(string.Join(";", quest.CurrentStep.Objectives.Select(o => o.Id + "," + (o.Done(quest.GetAdvancement(o.Id)) ? "1" : "0"))));
            message.Append('|');
            message.Append(string.Join(";", quest.Template.Steps.Where(s => s.Order < quest.CurrentStep.Order)));
            message.Append('|');
            message.Append(string.Join(";", quest.Template.Steps.Where(s => s.Order > quest.CurrentStep.Order)));
            message.Append('|');
            message.Append(0).Append(';');
            message.Append(",,,");
            return message.ToString();
        }

        public static string SUBAREA_ALIGNMENT_CHANGED(int subAreaId, int alignmentId, bool silent)
        {
            return "am" + subAreaId + "|" + alignmentId + "|" + (silent ? "1" : "0");
        }

        public static string AREA_ALIGNMENT_CHANGED(int areaId, int alignmentId)
        {
            return "aM" + areaId + "|" + (alignmentId == 0 ? -1 : alignmentId);
        }

        public static string CONQUEST_BONUS(int alignedBonus, int rankMultiplicator, int alignedMalus)
        {
            return "CB" + alignedBonus + "," + alignedBonus + "," + alignedBonus + ";" +
                   rankMultiplicator + "," + rankMultiplicator + "," + rankMultiplicator + ";" +
                   alignedMalus + "," + alignedMalus + "," + alignedMalus;
        }

        public static string CONQUEST_BALANCE(int worldBalance, int areaBalance)
        {
            return "Cb" + worldBalance + ";" + areaBalance;
        }

        public static string CONQUEST_WORLD_DATA(string worldData)
        {
            return "CW" + worldData;
        }

        public static string CONQUEST_PRISM_INFOS_JOINED(int timer, int maxTimer, int maxTeamPositions)
        {
            return "CIJ0;" + timer + ";" + maxTimer + ";" + maxTeamPositions;
        }

        public static string CONQUEST_PRISM_INFOS_ERROR(int error)
        {
            return "CIJ" + error;
        }

        public static string CONQUEST_PRISM_INFOS_CLOSING()
        {
            return "CIV";
        }

        public static string CONQUEST_PRISM_ATTACKED(Game.Map.MapInstance map)
        {
            return "CA" + map.Id + "|" + map.X + "|" + map.Y;
        }

        public static string CONQUEST_PRISM_SURVIVED(Game.Map.MapInstance map)
        {
            return "CS" + map.Id + "|" + map.X + "|" + map.Y;
        }

        public static string CONQUEST_PRISM_DEAD(Game.Map.MapInstance map)
        {
            return "CD" + map.Id + "|" + map.X + "|" + map.Y;
        }

        public static string CONQUEST_PRISM_FIGHT_ATTACKERS(AbstractFight fight, params AbstractFighter[] attackers)
        {
            var message = new StringBuilder("Cp+").Append(Util.EncodeBase36(fight.Id));
            foreach (var attacker in attackers)
            {
                message.Append('|');
                message.Append(Util.EncodeBase36(attacker.Id)).Append(';');
                message.Append(attacker.Name).Append(';');
                message.Append(attacker.Level).Append(";0");
            }
            return message.ToString();
        }

        public static string CONQUEST_PRISM_FIGHT_DEFENDERS(AbstractFight fight, params CharacterEntity[] defenders)
        {
            var message = new StringBuilder("CP+").Append(Util.EncodeBase36(fight.Id));
            foreach (var defender in defenders)
            {
                message.Append('|');
                message.Append(Util.EncodeBase36(defender.Id)).Append(';');
                message.Append(defender.Name).Append(';');
                message.Append(defender.SkinBase).Append(';');
                message.Append(defender.Level).Append(';');
                message.Append(defender.HexColor1).Append(';');
                message.Append(defender.HexColor2).Append(';');
                message.Append(defender.HexColor3).Append(";0");
            }
            return message.ToString();
        }

        public static string CONQUEST_PRISM_FIGHT_DEFENDER_LEAVE(AbstractFight fight, CharacterEntity defender)
        {
            return new StringBuilder("CP-").Append(Util.EncodeBase36(fight.Id)).Append('|').Append(Util.EncodeBase36(defender.Id)).ToString();
        }

    }
}
