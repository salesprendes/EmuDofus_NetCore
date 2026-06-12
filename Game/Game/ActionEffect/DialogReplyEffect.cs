using Game.Database.Repository;
using Game.Database.Structure;
using Game.Action;
using Game.Entity;
using Game.Stats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.ActionEffect
{
    public sealed class DialogReplyEffect : AbstractActionEffect<DialogReplyEffect>
    {
        public override bool ProcessItem(CharacterEntity character, ItemDAO item, GenericEffect effect, long targetId, int targetCell)
        {
            throw new NotImplementedException();
        }

        public override bool Process(CharacterEntity character, Dictionary<string, string> parameters)
        {
            var question = NpcQuestionRepository.Instance.GetById(int.Parse(parameters["questionId"]));
            if (question == null)
                return false;
            ((GameNpcDialogAction)character.CurrentAction).Dialog.SendQuestion(question);
            return true;
        }
    }
}


