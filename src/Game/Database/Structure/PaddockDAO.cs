using Protocolo.Framework.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Structure
{    
    [Table("paddockinstance")]
    public sealed class PaddockDAO : DataAccessObject<PaddockDAO>
    {
        private int _mapId;
        private int _guildId;
        private long _defaultPrice;
        private long _price;
        private int _mountPlace;
        private int _itemPlace;


        [Key]
        public int MapId
        {
            get => _mapId;
            set => SetProperty(ref _mapId, value);
        }
        public int GuildId
        {
            get => _guildId;
            set => SetProperty(ref _guildId, value);
        }
        public long DefaultPrice
        {
            get => _defaultPrice;
            set => SetProperty(ref _defaultPrice, value);
        }
        public long Price
        {
            get => _price;
            set => SetProperty(ref _price, value);
        }
        public int MountPlace
        {
            get => _mountPlace;
            set => SetProperty(ref _mountPlace, value);
        }
        public int ItemPlace
        {
            get => _itemPlace;
            set => SetProperty(ref _itemPlace, value);
        }
    }
}

