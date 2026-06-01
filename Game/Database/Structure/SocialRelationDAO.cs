using Protocolo.Framework.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Structure
{
    /// <summary>
    /// 
    /// </summary>
    public enum SocialRelationTypeEnum
    {
        TYPE_FRIEND = 0,
        TYPE_ENNEMY = 1,
    }

    /// <summary>
    /// 
    /// </summary>
    [Table("socialrelation")]
    public sealed class SocialRelationDAO : DataAccessObject<SocialRelationDAO>
    {
        private long _accountId;
        private string _pseudo;
        private int _typeId;


        [Key]
        public long AccountId
        {
            get => _accountId;
            set => SetProperty(ref _accountId, value);
        }

        [Key]
        public string Pseudo
        {
            get => _pseudo;
            set => SetProperty(ref _pseudo, value);
        }

        public int TypeId
        {
            get => _typeId;
            set => SetProperty(ref _typeId, value);
        }
        
        [Write(false)]
        public SocialRelationTypeEnum Type => (SocialRelationTypeEnum)TypeId;
    }
}

