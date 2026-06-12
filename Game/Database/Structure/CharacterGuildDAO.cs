using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    [Table("characterguild")]
    public sealed class CharacterGuildDAO : DataAccessObject<CharacterGuildDAO>
    {
        private long _id;
        private long _guildId;
        private int _rank;
        private int _power;
        private int _xpSharePercent;
        private long _xpGiven;


        [Key]
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public long GuildId
        {
            get => _guildId;
            set => SetProperty(ref _guildId, value);
        }

        public int Rank
        {
            get => _rank;
            set => SetProperty(ref _rank, value);
        }

        public int Power
        {
            get => _power;
            set => SetProperty(ref _power, value);
        }

        public int XPSharePercent
        {
            get => _xpSharePercent;
            set => SetProperty(ref _xpSharePercent, value);
        }

        public long XPGiven
        {
            get => _xpGiven;
            set => SetProperty(ref _xpGiven, value);
        }
    }
}

