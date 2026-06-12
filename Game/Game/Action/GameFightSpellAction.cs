using Game.Fight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Action
{
    public sealed class GameFightSpellAction : AbstractGameFightAction
    {
        public System.Action Callback
        {
            get;
            private set;
        }

        public int SpellLevel
        {
            get;
            private set;
        }

        public int CellId
        {
            get;
            private set;
        }

        public int SpellId
        {
            get;
            private set;
        }

        public string Sprite
        {
            get;
            private set;
        }

        public string SpriteInfos
        {
            get;
            private set;
        }

        public GameFightSpellAction(AbstractFighter fighter, int cellId, int spellId, int spellLevel, string sprite, string spriteInfos, long duration, System.Action callback)
    : base(GameActionTypeEnum.FIGHT_SPELL_LAUNCH, fighter, duration)
        {
            Callback = callback;
            CellId = cellId;
            SpellId = spellId;
            SpellLevel = spellLevel;
            Sprite = sprite;
            SpriteInfos = spriteInfos;
        }

        public override void Stop(params object[] args)
        {
            Callback();
            base.Stop(args);
        }

        public override string SerializeAs_GameAction()
        {
            return SpellId + "," + CellId + "," + Sprite + "," + SpellLevel + "," + SpriteInfos;
        }
    }
}


