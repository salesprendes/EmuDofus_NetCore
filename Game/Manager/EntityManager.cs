using System.Collections.Generic;
using Protocolo.Framework.Generic;
using Game.Database.Structure;
using Game.Action;
using Game.Entity;
using Game.Frame;
using Game.Guild;
using Game.Database.Repository;

namespace Game.Manager
{
    public sealed class EntityManager : Singleton<EntityManager>
    {
        private readonly Dictionary<long, MerchantEntity> m_merchantById;
        private readonly Dictionary<long, MerchantEntity> m_merchantByAccount;
        private readonly Dictionary<string, MerchantEntity> m_merchantByName;

        private readonly Dictionary<long, CharacterEntity> m_characterById;
        private readonly Dictionary<long, CharacterEntity> m_characterByAccount;
        private readonly Dictionary<string, CharacterEntity> m_characterByNickname;
        private readonly Dictionary<string, CharacterEntity> m_characterByName;
        
        private readonly Dictionary<long, TaxCollectorEntity> m_taxCollectorById;
        private readonly Dictionary<long, MountEntity> m_mountById;

        public EntityManager()
        {
            m_merchantById = new Dictionary<long, MerchantEntity>();
            m_merchantByAccount = new Dictionary<long, MerchantEntity>();
            m_merchantByName = new Dictionary<string, MerchantEntity>();

            m_characterById = new Dictionary<long, CharacterEntity>();
            m_characterByAccount = new Dictionary<long, CharacterEntity>();
            m_characterByName = new Dictionary<string, CharacterEntity>();
            m_characterByNickname = new Dictionary<string, CharacterEntity>();
            
            m_taxCollectorById = new Dictionary<long, TaxCollectorEntity>();

            m_mountById = new Dictionary<long, MountEntity>();
        }

        public void Initialize()
        {
            foreach(var character in CharacterRepository.Instance.All)
                if(character.Merchant)                
                    CreateMerchant(character).StartAction(GameActionTypeEnum.MAP);
            foreach (var mount in MountRepository.Instance.All)
                CreateMount(mount);
        }

        public TaxCollectorEntity CreateTaxCollector(GuildInstance guild, TaxCollectorDAO taxCollectorDAO)
        {
            var taxCollector = new TaxCollectorEntity(guild, taxCollectorDAO);
            taxCollector.StartAction(GameActionTypeEnum.MAP);
            taxCollector.Map.SubArea.TaxCollector = taxCollector;
            m_taxCollectorById.Add(taxCollector.Id, taxCollector);
            return taxCollector;
        }

        public void RemoveTaxCollector(TaxCollectorEntity taxCollector)
        {
            m_taxCollectorById.Remove(taxCollector.Id);
        }

        public CharacterEntity CreateCharacter(AccountTicket account, CharacterDAO characterDAO)
        {
            var merchant = GetMerchantByAccount(characterDAO.AccountId);
            if(merchant != null)            
                RemoveMerchant(merchant);
            
            var character = new CharacterEntity(account, characterDAO);         
            m_characterById.Add(character.Id, character);
            m_characterByName.Add(character.Name.ToLower(), character);
            m_characterByAccount.Add(character.AccountId, character);
            m_characterByNickname.Add(account.Pseudo.ToLower(), character);        
            return character;
        }

        public MerchantEntity CreateMerchant(CharacterDAO characterDAO)
        {
            var merchant = new MerchantEntity(characterDAO);
            m_merchantById.Add(merchant.Id, merchant);
            m_merchantByName.Add(merchant.Name.ToLower(), merchant);
            m_merchantByAccount.Add(merchant.AccountId, merchant);
            return merchant;
        }

        public MountEntity CreateMount(MountDAO mountDAO)
        {
            var mount = new MountEntity(mountDAO);
            m_mountById.Add(mount.UniqueId, mount);
            return mount;
        }

        public void CharacterDisconnected(CharacterEntity character)
        {
            if (character.PartyId != -1)
                PartyManager.Instance.PartyLeave(character);
            if (character.PartyInvitedPlayerId != -1 || character.PartyInviterPlayerId != -1)
                BasicFrame.Instance.PartyRefuse(character, "");
            if (character.GuildInvitedPlayerId != -1 || character.GuildInviterPlayerId != -1)
                BasicFrame.Instance.GuildJoinRefuse(character, "");
                
            character.AddMessage(() =>
            {
                if (character.Disconnected())
                {
                    RemoveCharacter(character);
                }
            });
        }

        public void RemoveMerchant(MerchantEntity merchant)
        {
            merchant.AddMessage(() =>
            {
                foreach (var buyer in merchant.Buyers)
                    buyer.AddMessage(() => buyer.AbortAction(GameActionTypeEnum.EXCHANGE));
                merchant.StopAction(GameActionTypeEnum.MAP);
                merchant.Dispose();
                merchant.Merchant = false;

                WorldService.Instance.AddMessage(() =>
                {
                    m_merchantById.Remove(merchant.Id);
                    m_merchantByName.Remove(merchant.Name.ToLower());
                    m_merchantByAccount.Remove(merchant.AccountId);
                });
            });
        }

        public void RemoveCharacter(CharacterEntity character)
        {
            WorldService.Instance.AddMessage(() =>
                {
                    m_characterById.Remove(character.Id);
                    m_characterByName.Remove(character.Name.ToLower());
                    m_characterByAccount.Remove(character.AccountId);
                    m_characterByNickname.Remove(character.Account.Pseudo.ToLower());
                });
        }

        public IEnumerable<CharacterEntity> OnlineCharacters => m_characterById.Values;

        public CharacterEntity GetCharacterById(long id)
        {
            if (m_characterById.ContainsKey(id))
                return m_characterById[id];
            return null;
        }

        public CharacterEntity GetCharacterByAccount(long accountId)
        {
            if (m_characterByAccount.ContainsKey(accountId))
                return m_characterByAccount[accountId];
            return null;
        }

        public CharacterEntity GetCharacterByNickname(string nickname)
        {
            nickname = nickname.ToLower();
            if (m_characterByNickname.ContainsKey(nickname))
                return m_characterByNickname[nickname];
            return null;
        }

        public CharacterEntity GetCharacterByName(string name)
        {
            name = name.ToLower();
            if (m_characterByName.ContainsKey(name))
                return m_characterByName[name];
            return null;
        }

        public MerchantEntity GetMerchantById(long id)
        {
            if (m_merchantById.ContainsKey(id))
                return m_merchantById[id];
            return null;
        }

        public MerchantEntity GetMerchantByAccount(long accountId)
        {
            if (m_merchantByAccount.ContainsKey(accountId))
                return m_merchantByAccount[accountId];
            return null;
        }

        public MerchantEntity GetMerchantByName(string name)
        {
            name = name.ToLower();
            if (m_merchantByName.ContainsKey(name))
                return m_merchantByName[name];
            return null;
        }

        public MountEntity GetMountById(long id)
        {
            if (m_mountById.ContainsKey(id))
                return m_mountById[id];
            return null;
        }
    }
}


