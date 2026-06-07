using Game.Action;
using Game.Database.Repository;
using Game.Database.Structure;
using Game.Entity;
using Game.Entity.Inventory;
using Game.Manager;
using Game.Network;
using Protocolo.Framework.Generic.Logging;

namespace Game.House
{
    public sealed class HouseInstance
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(HouseInstance));

        private readonly HouseDAO m_record;
        private HouseChestInventory m_chest;

        public int Id => m_record.Id;
        public int MapIdInside => m_record.MapIdInside;
        public int MapIdOutside => m_record.MapIdOutside;
        public int CellIdOutside => m_record.CellIdOutside;
        public int CellIdInside => m_record.CellIdInside;

        public long OwnerId
        {
            get => m_record.OwnerId;
            private set => m_record.OwnerId = value;
        }

        public string LockCode
        {
            get => m_record.LockCode;
            private set => m_record.LockCode = value;
        }

        public long SalePrice
        {
            get => m_record.SalePrice;
            private set => m_record.SalePrice = value;
        }

        public int GuildId
        {
            get => m_record.GuildId;
            private set => m_record.GuildId = value;
        }

        public int GuildRights
        {
            get => m_record.GuildRights;
            private set => m_record.GuildRights = value;
        }

        public bool IsOwned => OwnerId != -1;
        public bool IsForSale => SalePrice > 0;
        public bool IsLocked => LockCode != "-";

        public HouseChestInventory Chest => m_chest ??= new HouseChestInventory(m_record);

        public HouseInstance(HouseDAO record)
        {
            m_record = record;
        }

        public void TryEnter(CharacterEntity character, string code)
        {
            if (!character.CanGameAction(GameActionTypeEnum.MAP_TELEPORT))
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_YOU_ARE_AWAY));
                return;
            }

            if (IsLocked && character.Id != OwnerId)
            {
                if (code == "")
                {
                    character.CurrentHouse = this;
                    character.Dispatch(WorldMessage.KEY_DIALOG(false, 8));
                    return;
                }
                if (code != LockCode)
                {
                    character.Dispatch(WorldMessage.KEY_ERROR());
                    return;
                }
                character.CurrentHouse = null;
                character.Dispatch(WorldMessage.KEY_CLOSE());
            }

            if (MapManager.Instance.GetById(MapIdInside) == null)
            {
                Logger.Warn($"mapa interior {MapIdInside} no existe en MapManager (casa id={Id})");
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.Teleport(MapIdInside, CellIdInside);
        }

        public void OpenProperties(CharacterEntity character)
        {
            character.CurrentHouse = this;
            character.Dispatch(WorldMessage.HOUSE_INFO(character.Id == OwnerId, Id, IsLocked, IsForSale, GuildId != -1));
            SendPropertiesToAll(character);
        }

        public void ShowBuyDialog(CharacterEntity character)
        {
            if (!IsForSale)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }
            character.CurrentHouse = this;
            character.Dispatch(WorldMessage.HOUSE_BUY_DIALOG(Id, SalePrice));
        }

        public void ShowLockDialog(CharacterEntity character)
        {
            if (character.Id != OwnerId)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }
            character.CurrentHouse = this;
            character.Dispatch(WorldMessage.KEY_DIALOG(true, 8));
        }

        public void RemoveLock(CharacterEntity character)
        {
            if (character.Id != OwnerId)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }
            SetLockCode(character, "-");
        }

        public void ShowSellDialog(CharacterEntity character)
        {
            if (character.Id != OwnerId)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }
            character.CurrentHouse = this;
            character.Dispatch(WorldMessage.HOUSE_INFO(true, Id, IsLocked, IsForSale, GuildId != -1));
            SendPropertiesToAll(character);
            character.Dispatch(WorldMessage.HOUSE_BUY_DIALOG(Id, SalePrice));
        }

        public void SendInformationsTo(CharacterEntity character)
        {
            character.Dispatch(WorldMessage.HOUSE_INFO(character.Id == OwnerId, Id, IsLocked, IsForSale, GuildId != -1));
            SendPropertiesToAll(character);
        }

        public void SendPropertiesToAll(CharacterEntity character)
        {
            var ownerName = GetOwnerName();
            var guildName = "";
            var guildEmblem = "";
            if (GuildId != -1)
            {
                var guild = GuildManager.Instance.GetGuild(GuildId);
                if (guild != null)
                {
                    guildName = guild.Name;
                    guildEmblem = guild.Emblem;
                }
            }
            character.Dispatch(WorldMessage.HOUSE_PROPERTIES(Id, ownerName, IsForSale, guildName, guildEmblem));
        }

        public void Buy(CharacterEntity character)
        {
            if (!IsForSale)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (character.Inventory.Kamas < SalePrice)
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_NOT_ENOUGH_KAMAS));
                return;
            }

            character.Inventory.SubKamas(SalePrice);

            var previousOwner = EntityManager.Instance.GetCharacterById(OwnerId);
            previousOwner?.Inventory.AddKamas(SalePrice);

            OwnerId = character.Id;
            LockCode = "-";
            SalePrice = 0;
            GuildId = -1;
            GuildRights = 0;

            character.CurrentHouse = null;
            character.Dispatch(WorldMessage.HOUSE_CLOSE_BUY_DIALOG());
            character.Dispatch(WorldMessage.HOUSE_INFO(true, Id, false, false, false));
        }

        public void SetSalePrice(CharacterEntity character, long price)
        {
            if (character.Id != OwnerId)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }
            SalePrice = price >= 0 ? price : 0;
            character.Dispatch(WorldMessage.HOUSE_SET_PRICE(Id, SalePrice));
            character.Dispatch(WorldMessage.HOUSE_INFO(true, Id, IsLocked, IsForSale, GuildId != -1));
        }

        public void SetLockCode(CharacterEntity character, string code)
        {
            if (character.Id != OwnerId)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }
            LockCode = code.Length > 8 ? code.Substring(0, 8) : code;
            character.CurrentHouse = null;
            character.Dispatch(WorldMessage.KEY_CLOSE());
            character.Dispatch(WorldMessage.HOUSE_INFO(true, Id, IsLocked, IsForSale, GuildId != -1));
        }

        public void SetGuildRights(CharacterEntity character, string data)
        {
            if (character.Id != OwnerId)
            {
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }
            switch (data)
            {
                case "+":
                    if (character.GuildMember == null) return;
                    GuildId = (int)character.GuildMember.GuildId;
                    break;
                case "-":
                case "0":
                    GuildId = -1;
                    GuildRights = 0;
                    break;
                default:
                    if (int.TryParse(data, out var rights))
                        GuildRights = rights;
                    break;
            }
            SendGuildRights(character);
        }

        public void SendGuildRights(CharacterEntity character)
        {
            var guildInfo = "";
            if (GuildId != -1)
            {
                var guild = GuildManager.Instance.GetGuild(GuildId);
                if (guild != null)
                    guildInfo = ";" + guild.Name + ";" + guild.Emblem + ";" + GuildRights;
            }
            character.Dispatch(WorldMessage.HOUSE_GUILD_RIGHTS(Id + guildInfo));
        }

        public void TryOpenChest(CharacterEntity character, string code)
        {
            if (!character.CanGameAction(GameActionTypeEnum.EXCHANGE))
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_YOU_ARE_AWAY));
                return;
            }
            character.ExchangeHouseChest(Chest);
        }

        private string GetOwnerName()
        {
            if (!IsOwned)
                return "";

            return EntityManager.Instance.GetCharacterById(OwnerId)?.Name ?? CharacterRepository.Instance.GetById(OwnerId)?.Name ?? "";
        }
    }
}
