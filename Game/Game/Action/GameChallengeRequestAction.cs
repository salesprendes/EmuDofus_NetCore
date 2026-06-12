using Game.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Manager;
using Game.Network;

namespace Game.Action
{
    public sealed class GameChallengeRequestAction : AbstractGameAction
    {
        public override bool CanAbort => true;

        public CharacterEntity Attacker
        {
            get;
            private set;
        }

        public CharacterEntity Defender
        {
            get;
            private set;
        }

        public GameChallengeRequestAction(CharacterEntity attacker, CharacterEntity defender)
    : base(GameActionTypeEnum.CHALLENGE_REQUEST, attacker)
        {
            Attacker = attacker;
            Defender = defender;
            Attacker.Map.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.CHALLENGE_REQUEST, Attacker.Id, SerializeAs_GameAction()));
        }

        public override void Stop(params object[] args)
        {
            Finish(GameActionTypeEnum.CHALLENGE_ACCEPT);
            Attacker.Map.FightManager.StartChallenge(Attacker, Defender);
            base.Stop(args);
        }

        public override void Abort(params object[] args)
        {
            Finish(GameActionTypeEnum.CHALLENGE_DECLINE);
            base.Abort(args);
        }

        private void Finish(GameActionTypeEnum result)
        {
            var message = WorldMessage.GAME_ACTION(result, Attacker.Id, Defender.Id.ToString());
            Attacker.Dispatch(message);
            Defender.Dispatch(message);
        }

        public override string SerializeAs_GameAction()
        {
            return Defender.Id.ToString();
        }
    }
}


