using Game.Dialog;
using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public sealed class GameNpcDialogAction : AbstractGameAction
    {
        public override bool CanAbort => true;

        public NpcDialog Dialog
        {
            get;
            private set;
        }

        public NonPlayerCharacterEntity Npc
        {
            get;
            private set;
        }

        public GameNpcDialogAction(CharacterEntity character, NonPlayerCharacterEntity npc)
    : base(GameActionTypeEnum.NPC_DIALOG, character)
        {
            Npc = npc;
            Dialog = new NpcDialog(character, npc);
        }

        public override void Start()
        {
            Entity.Dispatch(WorldMessage.DIALOG_CREATE(Npc.Id));
            Dialog.SendQuestion(Npc.InitialQuestion);
            base.Start();
        }

        public override void Abort(params object[] args)
        {
            Entity.Dispatch(WorldMessage.DIALOG_LEAVE());
            base.Abort(args);
        }

        public override void Stop(params object[] args)
        {
            Entity.Dispatch(WorldMessage.DIALOG_LEAVE());
            base.Stop(args);
        }
    }
}


