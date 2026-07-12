using Game.Action;
using Game.Database.Structure;
using Game.Entity;
using Game.Fight.AI;
using Game.Fight.Effect;
using Game.Fight.Ending;
using Game.Frame;
using Game.Manager;
using Game.Map;
using Game.Network;
using Game.Spell;
using Game.Stats;
using Protocolo.Framework.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Fight
{
    public sealed class FightCell : IDisposable
    {
        public int Id
        {
            get;
            private set;
        }

        public bool Walkable
        {
            get;
            private set;
        }

        public bool LineOfSight
        {
            get;
            private set;
        }

        public int GroundLevel
        {
            get;
            private set;
        }

        public PriorityQueue<IFightObstacle> FightObjects
        {
            get;
            private set;
        }

        public bool CanWalk
        {
            get
            {
                return Walkable && FightObjects.All(obj => obj.CanGoThrough);
            }
        }

        public bool CanPutObject
        {
            get
            {
                return Walkable && FightObjects.Where(obj => obj.Cell.Id == Id).All(obj => obj.CanStack);
            }
        }

        public FightCell(int id, bool walkable, bool los, int groundLevel = 7)
        {
            Id = id;
            Walkable = walkable;
            LineOfSight = los;
            GroundLevel = groundLevel;
            FightObjects = new PriorityQueue<IFightObstacle>();
        }

        public bool HasObject(FightObstacleTypeEnum type)
        {
            return FightObjects.Any(obj => obj.ObstacleType == type);
        }

        public FightActionResultEnum AddObject(IFightObstacle fightObject)
        {
            FightObjects.Add(fightObject);

            if (fightObject.ObstacleType == FightObstacleTypeEnum.TYPE_FIGHTER)
            {
                var fighter = (AbstractFighter)fightObject;

                for (int i = FightObjects.Count - 1; i > -1; i--)
                {
                    var activableObject = FightObjects[i] as AbstractActivableObject;
                    if (activableObject != null)
                    {
                        if (activableObject.ActivationType == ActiveType.ACTIVE_ENDMOVE)
                        {
                            if (!fighter.IsFighterDead)
                            {
                                activableObject.LoadTargets(fighter);
                                activableObject.Activate(fighter);
                            }
                        }
                    }
                }
            }

            return FightActionResultEnum.RESULT_NOTHING;
        }

        public FightActionResultEnum RemoveObject(IFightObstacle obstacle)
        {
            FightObjects?.Remove(obstacle);

            return FightActionResultEnum.RESULT_NOTHING;
        }

        public FightActionResultEnum BeginTurn(AbstractFighter fighter)
        {
            for (int i = FightObjects.Count - 1; i > -1; i--)
            {
                var activableObject = FightObjects[i] as AbstractActivableObject;
                if (activableObject != null)
                {
                    if (activableObject.ActivationType == ActiveType.ACTIVE_BEGINTURN)
                    {
                        activableObject.LoadTargets(fighter);
                        activableObject.Activate(fighter);
                    }
                }
            }

            return FightActionResultEnum.RESULT_NOTHING;
        }

        public void Dispose()
        {
            FightObjects.Clear();
            FightObjects = null;
        }
    }
}
