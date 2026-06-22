using Game.Fight.Effect;
using Game.Map;
using Game.Spell;
using Game.Manager;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight
{
    public abstract class AbstractActivableObject : IFightObstacle
    {
        public FightObstacleTypeEnum ObstacleType
        {
            get;
        }

        public int Priority
        {
            get;
        }

        public ActiveType ActivationType
        {
            get;
        }

        public int Color
        {
            get;
            private set;
        }

        public bool CanGoThrough
        {
            get;
            private set;
        }

        public bool CanStack
        {
            get;
        }

        public int Duration
        {
            get;
            protected set;
        }

        public int Length
        {
            get;
            protected set;
        }

        public bool Hide
        {
            get;
            protected set;
        }

        public int ActionId
        {
            get;
            protected set;
        }

        public List<FightCell> AffectedCells
        {
            get;
            protected set;
        }

        public bool Activated
        {
            get;
            protected set;
        }

        public FightCell Cell
        {
            get;
        }

        public List<AbstractFighter> Targets
        {
            get;
        }

        // Lanzador del objeto (glifo/trampa). Permite a la IA distinguir los hostiles de los propios.
        public AbstractFighter Caster => m_caster;

        protected AbstractFight m_fight;
        protected AbstractFighter m_caster;
        protected SpellTemplate m_actionSpell;
        protected SpellLevel m_actionEffect;
        protected int m_spellId;

        public abstract void AppearForAll();

        public abstract void Appear(MessageDispatcher dispatcher);

        public abstract void DisappearForAll();

        protected AbstractActivableObject(FightObstacleTypeEnum type, ActiveType activeType, AbstractFight fight, AbstractFighter caster, CastInfos castInfos, int cell, int duration, int actionId, bool canGoThrough, bool canStack, bool hide = false)
        {
            m_fight = fight;
            m_caster = caster;
            m_spellId = castInfos.SpellId;
            m_actionSpell = SpellManager.Instance.GetTemplate(castInfos.Value1);
            m_actionEffect = m_actionSpell.GetLevel(castInfos.Value2);

            Cell = fight.GetCell(cell);
            ObstacleType = type;
            ActivationType = activeType;
            CanGoThrough = canGoThrough;
            CanStack = canStack;
            Color = castInfos.Value3;
            Targets = new List<AbstractFighter>();
            Length = Pathfinding.GetDirection(castInfos.RangeType[1]);
            AffectedCells = new List<FightCell>();
            Duration = duration;
            ActionId = actionId;
            Hide = hide;

            foreach (var effect in m_actionEffect.Effects)
            {
                if (CastInfos.IsDamageEffect(effect.TypeEnum))
                {
                    Priority--;
                }
            }


            foreach (var cellId in CellZone.GetCircleCells(fight.Map, cell, Length))
            {
                var fightCell = m_fight.GetCell(cellId);
                if (fightCell != null)
                {
                    fightCell.AddObject(this);
                    AffectedCells.Add(fightCell);
                }
            }

            if (Hide)
                Appear(caster.Team);
            else
                AppearForAll();
        }

        public void LoadTargets(AbstractFighter target)
        {
            if (!Targets.Contains(target))
                Targets.Add(target);

            switch (ActivationType)
            {
                case ActiveType.ACTIVE_ENDMOVE:
                    foreach (var cell in AffectedCells)
                    {
                        Targets.AddRange(cell.FightObjects.OfType<AbstractFighter>().Where(fighter => !Targets.Contains(fighter)));
                    }
                    break;
            }
        }

        public void Activate(AbstractFighter activator)
        {
            Activated = true;

            m_fight.CurrentProcessingFighter = activator;
            m_fight.Dispatch(WorldMessage.GAME_ACTION(ActionId, activator.Id, m_spellId + "," + Cell.Id + "," + m_actionSpell.Sprite + "," + m_actionEffect.Level + ",1," + m_caster.Id));

            foreach (var target in Targets)
            {
                if (!target.IsFighterDead)
                {
                    foreach (var effect in m_actionEffect.Effects)
                    {
                        m_fight.AddProcessingTarget(new CastInfos(
                                            effect.TypeEnum,
                                            m_spellId,
                                            Cell.Id,
                                            effect.Value1,
                                            effect.Value2,
                                            effect.Value3,
                                            effect.Chance,
                                            effect.Duration,
                                            m_caster,
                                            target,
                                            "",
                                            target.Cell.Id,
                                            isTrap: ObstacleType == FightObstacleTypeEnum.TYPE_TRAP)
                                         );
                    }
                }
            }

            Targets.Clear();

            if (ObstacleType == FightObstacleTypeEnum.TYPE_TRAP)
                Remove();
        }

        public void DecrementDuration()
        {
            Duration--;

            if (Duration <= 0)
                Remove();
        }

        public void Remove()
        {
            DisappearForAll();

            foreach (var cell in AffectedCells)
            {
                cell.RemoveObject(this);
            }
        }

        public int CompareTo(IFightObstacle obj)
        {
            return Priority.CompareTo(obj.Priority);
        }
    }
}


