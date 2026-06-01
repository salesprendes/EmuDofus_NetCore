using Protocolo.Framework.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Structure
{
    [Table("questobjective")]
    public sealed class QuestObjectiveDAO : DataAccessObject<QuestObjectiveDAO>
    {
        private int _id;
        private int _stepId;
        private int _type;
        private int _x;
        private int _y;
        private string _parameters;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }
        public int StepId
        {
            get => _stepId;
            set => SetProperty(ref _stepId, value);
        }
        public int Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }
        public int X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }
        public int Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }
        public string Parameters
        {
            get => _parameters;
            set => SetProperty(ref _parameters, value);
        }
    }
}

