using Protocolo.Framework.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Structure
{
    [Table("characterquest")]
    public sealed class CharacterQuestDAO : DataAccessObject<CharacterQuestDAO>
    {
        private int _id;
        private bool _done;
        private int _currentStepId;
        private string _serializedObjectives;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public bool Done
        {
            get => _done;
            set => SetProperty(ref _done, value);
        }
        public int CurrentStepId
        {
            get => _currentStepId;
            set => SetProperty(ref _currentStepId, value);
        }
        public string SerializedObjectives
        {
            get => _serializedObjectives;
            set => SetProperty(ref _serializedObjectives, value);
        }
    }
}

