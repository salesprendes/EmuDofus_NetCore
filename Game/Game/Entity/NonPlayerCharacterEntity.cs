using Game.Database.Repository;
using Game.Database.Structure;
using Game.Auction;
using Game.Exchange;
using Game.Manager;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Entity
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class NonPlayerCharacterEntity : AbstractEntity
    {
        /// <summary>
        /// 
        /// </summary>
        public override string Name => m_npcRecord.Template.ChatName;

        /// <summary>
        /// 
        /// </summary>
        public override int MapId
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public override int CellId
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public override int Level
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public override int RealLife
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public override int BaseLife => 0;

        /// <summary>
        /// 
        /// </summary>
        public override int Restriction
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string HexColor1
        {
            get
            {
                if (m_npcRecord.Template.Color3 == -1)
                    return "-1";
                return m_npcRecord.Template.Color3.ToString("x");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string HexColor2
        {
            get
            {
                if (m_npcRecord.Template.Color3 == -1)
                    return "-1";
                return m_npcRecord.Template.Color3.ToString("x");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string HexColor3
        {
            get
            {
                if (m_npcRecord.Template.Color3 == -1)
                    return "-1";
                return m_npcRecord.Template.Color3.ToString("x");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public NpcQuestionDAO InitialQuestion
        {
            get
            {
                if (m_initialQuestion == null && m_npcRecord.QuestionId != -1)
                    m_initialQuestion = NpcQuestionRepository.Instance.GetById(m_npcRecord.QuestionId);
                return m_initialQuestion;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public AuctionHouseInstance AuctionHouse
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public int TemplateId => m_npcRecord.TemplateId;

        /// <summary>
        /// 
        /// </summary>
        public List<ItemTemplateDAO> ShopItems
        {
            get;
            private set;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public List<RewardEntry> Rewards
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        private NpcInstanceDAO m_npcRecord;
        private NpcQuestionDAO m_initialQuestion;
        private StringBuilder m_cachedShopListInformations;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="npcDAO"></param>
        /// <param name="id"></param>
        public NonPlayerCharacterEntity(NpcInstanceDAO npcDAO, long id)
            : base(EntityTypeEnum.TYPE_NPC, id)
        {
            m_npcRecord = npcDAO;

            Orientation = m_npcRecord.Orientation;
            MapId = npcDAO.MapId;
            CellId = npcDAO.CellId;

            Rewards = new List<RewardEntry>();
            Rewards.AddRange(npcDAO.Template.Rewards);

            ShopItems = new List<ItemTemplateDAO>();
            ShopItems.AddRange(npcDAO.Template.ShopList);

            AuctionHouse = AuctionHouseManager.Instance.GetByNpcId(m_npcRecord.Id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool CanBeMoved()
        {
            return AuctionHouse == null && m_npcRecord.Template.GfxID <= 121;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exchangeType"></param>
        /// <returns></returns>
        public override bool CanBeExchanged(ExchangeTypeEnum exchangeType)
        {
            switch(exchangeType)
            {
                case ExchangeTypeEnum.EXCHANGE_NPC:
                    return Rewards.Count > 0;

                case ExchangeTypeEnum.EXCHANGE_SHOP:
                    return ShopItems.Count > 0;

                case ExchangeTypeEnum.EXCHANGE_AUCTION_HOUSE_BUY:
                case ExchangeTypeEnum.EXCHANGE_AUCTION_HOUSE_SELL:
                    return AuctionHouse != null;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        private StringBuilder m_serialized;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="message"></param>
        public override void SerializeAs_GameMapInformations(OperatorEnum operation, StringBuilder message)
        {
            message.Append(CellId).Append(';');
            message.Append(Orientation).Append(';');
            if (m_serialized == null)
            {
                m_serialized = new StringBuilder();
                m_serialized.Append('0').Append(';'); // Unknow
                m_serialized.Append(Id).Append(';');
                m_serialized.Append(TemplateId).Append(';');
                m_serialized.Append((int)EntityTypeEnum.TYPE_NPC).Append(';');
                m_serialized.Append(m_npcRecord.Template.GfxID).Append('^');
                m_serialized.Append(m_npcRecord.Template.ScaleX).Append(';'); // size
                m_serialized.Append(m_npcRecord.Template.Sex).Append(';');
                m_serialized.Append(HexColor1 + ';' + HexColor2 + ';' + HexColor3).Append(';');
                m_serialized.Append(m_npcRecord.Template.EntityLook).Append(';');
                m_serialized.Append(m_npcRecord.Template.ExtraClip).Append(';'); // ExtraClip
                m_serialized.Append(m_npcRecord.Template.CustomArtwork);
            }
            message.Append(m_serialized);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void SerializeAs_ShopItemsListInformations(StringBuilder message)
        {
            if (m_cachedShopListInformations == null)
            {
                m_cachedShopListInformations = new StringBuilder();
                foreach(var template in ShopItems)
                {
                    m_cachedShopListInformations.Append(template.Id).Append(';');
                    m_cachedShopListInformations.Append(template.Effects).Append('|');
                }
            }
            message.Append(m_cachedShopListInformations);
        }
    }
}


