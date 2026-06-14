using Game.Map;
using Game.Spell;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Effect.Type
{
    public sealed class PandaLaunchEffect : AbstractSpellEffect
    {
        public override FightActionResultEnum ApplyEffect(CastInfos CastInfos)
        {
            var infos = CastInfos.Caster.StateManager.FindState(FighterStateEnum.STATE_CARRIER);

            if (infos != null)
            {
                var target = infos.Target;

                if (target.StateManager.HasState(FighterStateEnum.STATE_CARRIED))
                {
                    var cell = target.Fight.GetCell(CastInfos.CellId);
                    if (cell.CanWalk)
                    {
                        var sleepTime = 1 + (WorldConfig.FIGHT_PANDA_LAUNCH_CELL_TIME * Pathfinding.GoalDistance(target.Fight.Map, target.Cell.Id, CastInfos.CellId));
                        var caster = CastInfos.Caster;

                        target.Fight.Dispatch(WorldMessage.GAME_ACTION(EffectEnum.PANDA_LANZAR, CastInfos.Caster.Id, CastInfos.CellId.ToString()));

                        target.Fight.SetSubAction(() =>
                        {
                            var result = target.SetCell(cell);
                            target.BuffManager.RemoveState((int)FighterStateEnum.STATE_CARRIED);
                            caster.BuffManager.RemoveState((int)FighterStateEnum.STATE_CARRIER);
                            return result;
                        }, sleepTime);
                    }
                }
            }

            return FightActionResultEnum.RESULT_NOTHING;
        }
    }
}


