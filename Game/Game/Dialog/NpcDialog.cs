using Game.Database.Structure;
using Game.Condition;
using Game.Entity;
using Game.Manager;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.ActionEffect;

namespace Game.Dialog
{
    public sealed class NpcDialog
    {
        public const string BANK_COST = "%bankCost%";
        public const string NAME = "%name%";

        private CharacterEntity Character
        {
            get;
            set;
        }

        private NonPlayerCharacterEntity Npc
        {
            get;
            set;
        }

        public NpcQuestionDAO CurrentQuestion
        {
            get;
            set;
        }

        private IEnumerable<NpcResponseDAO> m_possibleResponses;

        public NpcDialog(CharacterEntity character, NonPlayerCharacterEntity npc)
        {
            Character = character;
            Npc = npc;
        }

        public void SendQuestion(NpcQuestionDAO question)
        {
            CurrentQuestion = question;
            m_possibleResponses = CurrentQuestion.ResponseList;

            Character.Dispatch(WorldMessage.DIALOG_QUESTION(CurrentQuestion.Id, ApplyParameter(), m_possibleResponses.Select(response => response.Id)));
        }

        public void ProcessResponse(int responseId)
        {
            var response = m_possibleResponses.First(entry => entry.Id == responseId);

            if (response == null || !ConditionParser.Instance.Check(response.Conditions, Character))
            {
                Character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            foreach (ActionEntry action in response.ActionsList)
                ActionEffectManager.Instance.ApplyEffect(Character, action.Effect, action.Parameters);
        }

        private string ApplyParameter()
        {
            switch (CurrentQuestion.Params)
            {
                case BANK_COST:
                    return Character.Bank.Items.GroupBy(item => item.TemplateId).Count().ToString();

                case NAME:
                    return Character.Name;
            }
            return "";
        }
    }
}


