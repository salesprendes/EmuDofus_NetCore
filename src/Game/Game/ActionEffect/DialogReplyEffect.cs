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
    /// <summary>
    /// 
    /// </summary>
    public sealed class DialogReplyEffect : AbstractActionEffect<DialogReplyEffect>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="item"></param>
        /// <param name="effect"></param>
        /// <param name="targetId"></param>
        /// <param name="targetCell"></param>
        /// <returns></returns>
        public override bool ProcessItem(CharacterEntity character, ItemDAO item, GenericEffect effect, long targetId, int targetCell)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="parameters"></param>
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


