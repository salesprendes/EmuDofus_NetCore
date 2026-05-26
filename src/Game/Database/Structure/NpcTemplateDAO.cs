using System.Collections.Generic;
using Protocolo.Framework.Database;
using Game.Database.Repository;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
namespace Game.Database.Structure
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class RewardEntry
    {
        /// <summary>
        /// 
        /// </summary>
        public class ItemEntry
        {
            /// <summary>
            /// 
            /// </summary>
            public int TemplateId
            {
                get;
                private set;
            }

            /// <summary>
            /// 
            /// </summary>
            public int Quantity
            {
                get;
                private set;
            }

            /// <summary>
            /// 
            /// </summary>
            public ItemTemplateDAO Template
            {
                get
                {
                    if (m_template == null)
                        m_template = ItemTemplateRepository.Instance.GetById(TemplateId);
                    return m_template;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            private ItemTemplateDAO m_template;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="stringData"></param>
            public ItemEntry(int templateId, int quantity)
            {
                TemplateId = templateId;
                Quantity = quantity;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        public List<ItemEntry> RequiredItems
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long RequiredKamas
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public List<ItemEntry> RewardedItems
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public long RewardedKamas
        {
            get;
            set;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public RewardEntry()
        {
            RequiredItems = new List<ItemEntry>();
            RewardedItems = new List<ItemEntry>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataString"></param>
        public RewardEntry(string dataString)
            : this()
        { 
            var data = dataString.Split(';');
            var requiredData = data[0];
            var rewardedData = data[1];

            foreach (var required in requiredData.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var subData = required.Split(':');
                var type = subData[0];
                var id = int.Parse(subData[1]);
                var quantity = int.Parse(subData[2]);

                switch(type)
                {
                    case "kamas":
                        RequiredKamas = quantity;
                        break;

                    case "item":
                        RequiredItems.Add(new ItemEntry(id, quantity));
                        break;
                }
            }

            foreach (var reward in rewardedData.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var subData = reward.Split(':');
                var type = subData[0];
                var id = int.Parse(subData[1]);
                var quantity = int.Parse(subData[2]);

                switch (type)
                {
                    case "kamas":
                        RewardedKamas = quantity;
                        break;

                    case "item":
                        RewardedItems.Add(new ItemEntry(id, quantity));
                        break;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            StringBuilder serialized = new StringBuilder();
            List<string> data = new List<string>();

            if (RequiredKamas > 0)
                data.Add("kamas:0:" + RequiredKamas);
            foreach(var entry in RequiredItems)
                data.Add("item:" + entry.TemplateId + ":" + entry.Quantity);

            serialized.Append(string.Join(",", data)).Append(';');

            data.Clear();
            if (RewardedKamas > 0)
                data.Add("kamas:0:" + RewardedKamas);
            foreach (var entry in RewardedItems)
                data.Add("item:" + entry.TemplateId + ":" + entry.Quantity);

            serialized.Append(string.Join(",", data));

            return serialized.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateIds"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public bool Match(Dictionary<int, long> templates, long kamas)
        {
            return RequiredKamas == kamas 
                && RequiredItems.All(required => templates.Any(template => required.TemplateId == template.Key && required.Quantity == template.Value))
                && templates.All(template => RequiredItems.Any(required => required.TemplateId == template.Key && required.Quantity == template.Value));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Table("npctemplate")]
    public sealed class NpcTemplateDAO : DataAccessObject<NpcTemplateDAO>
    {
        private int _id;
        private string _name;
        private int _bonusValue;
        private int _gfxID;
        private int _scaleX;
        private int _scaleY;
        private int _sex;
        private int _color1;
        private int _color2;
        private int _color3;
        private string _entityLook;
        private int _extraClip;
        private int _customArtwork;
        private string _sell;
        private string _exchange;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }
        public int BonusValue
        {
            get => _bonusValue;
            set => SetProperty(ref _bonusValue, value);
        }
        public int GfxID
        {
            get => _gfxID;
            set => SetProperty(ref _gfxID, value);
        }
        public int ScaleX
        {
            get => _scaleX;
            set => SetProperty(ref _scaleX, value);
        }
        public int ScaleY
        {
            get => _scaleY;
            set => SetProperty(ref _scaleY, value);
        }
        public int Sex
        {
            get => _sex;
            set => SetProperty(ref _sex, value);
        }
        public int Color1
        {
            get => _color1;
            set => SetProperty(ref _color1, value);
        }
        public int Color2
        {
            get => _color2;
            set => SetProperty(ref _color2, value);
        }
        public int Color3
        {
            get => _color3;
            set => SetProperty(ref _color3, value);
        }

        public string EntityLook
        {
            get => _entityLook;
            set => SetProperty(ref _entityLook, value);
        }

        public int ExtraClip
        {
            get => _extraClip;
            set => SetProperty(ref _extraClip, value);
        }
        public int CustomArtwork
        {
            get => _customArtwork;
            set => SetProperty(ref _customArtwork, value);
        }
        public string Sell
        {
            get => _sell;
            set => SetProperty(ref _sell, value);
        }
        public string Exchange
        {
            get => _exchange;
            set => SetProperty(ref _exchange, value);
        }

        /// <summary>
        /// 
        /// </summary>
        private string m_chatName;
        [Write(false)]
        public string ChatName
        {
            get
            {
                if (m_chatName == null)
                    m_chatName = Regex.Replace(Name, "[^a-zA-Z0-9 ]+", "", RegexOptions.Compiled);
                return m_chatName;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private List<RewardEntry> m_rewards;
        /// <summary>
        /// 
        /// </summary>
        private List<ItemTemplateDAO> m_templatesToSell;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Write(false)]
        public List<RewardEntry> Rewards
        {
            get
            {
                if(m_rewards == null)
                {
                    m_rewards = new List<RewardEntry>();
                    if(Exchange != "-1")
                    {
                        foreach(var reward in Exchange.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            m_rewards.Add(new RewardEntry(reward));
                        }
                    }
                }
                return m_rewards;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Write(false)]
        public List<ItemTemplateDAO> ShopList
        {
            get
            {
                if (m_templatesToSell == null)
                {
                    m_templatesToSell = new List<ItemTemplateDAO>();
                    if (Sell != "" && Sell != "-1")
                    {
                        foreach (var templateId in Sell.Split(','))
                        {
                            var template = ItemTemplateRepository.Instance.GetById(int.Parse(templateId));
                            if (template != null)
                                m_templatesToSell.Add(template);
                        }
                    }
                }
                return m_templatesToSell;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Id + " ( " + Name + " ) ";
        }
    }
}

