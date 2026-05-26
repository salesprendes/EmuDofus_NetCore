using Protocolo.Framework.Database;

namespace Game.Database.Structure
{
    /// <summary>
    /// 
    /// </summary>
    [Table("experiencetemplate")]
    public sealed class ExperienceTemplateDAO : DataAccessObject<ExperienceTemplateDAO>
    {
        private int _level;
        private long _character;
        private long _job;
        private long _mount;
        private long _pvp;
        private int _living;
        private long _guild;


        [Key]
        public int Level
        {
            get => _level;
            set => SetProperty(ref _level, value);
        }
        public long Character
        {
            get => _character;
            set => SetProperty(ref _character, value);
        }
        public long Job
        {
            get => _job;
            set => SetProperty(ref _job, value);
        }
        public long Mount
        {
            get => _mount;
            set => SetProperty(ref _mount, value);
        }
        public long Pvp
        {
            get => _pvp;
            set => SetProperty(ref _pvp, value);
        }
        public int Living
        {
            get => _living;
            set => SetProperty(ref _living, value);
        }
        public long Guild
        {
            get => _guild;
            set => SetProperty(ref _guild, value);
        }
    }
}

