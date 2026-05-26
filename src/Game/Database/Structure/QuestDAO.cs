using Protocolo.Framework.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Structure
{
    [Table("quest")]
    public sealed class QuestDAO : DataAccessObject<QuestDAO>
    {
        private int _id;
        private string _description;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        [Write(false)]
        public List<QuestStepDAO> Steps { get; } = new List<QuestStepDAO>();
    }
}

