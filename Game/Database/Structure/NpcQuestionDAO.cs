using Protocolo.Framework.Database;
using Game.Database.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Database.Structure
{
    [Table("npcquestion")]
    public sealed class NpcQuestionDAO : DataAccessObject<NpcQuestionDAO>
    {
        private int _id;
        private string _params;
        private string _responses;


        [Key]
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Params
        {
            get => _params;
            set => SetProperty(ref _params, value);
        }

        public string Responses
        {
            get => _responses;
            set => SetProperty(ref _responses, value);
        }

        private List<NpcResponseDAO> m_responses;

        [Write(false)]
        public List<NpcResponseDAO> ResponseList
        {
            get
            {
                if (m_responses == null)
                {
                    m_responses = new List<NpcResponseDAO>();
                    if (Responses != string.Empty)
                    {
                        foreach (var response in Responses.Split(','))
                        {
                            m_responses.Add(NpcResponseRepository.Instance.GetById(int.Parse(response)));
                        }
                    }
                }
                return m_responses;
            }
        }

        public override string ToString()
        {
            return Id + " ( " + Params + " - " + Responses + " )";
        }
    }
}

