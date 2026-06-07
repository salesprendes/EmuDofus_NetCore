using Game.Action;
using Game.Condition;
using Game.Database.Repository;
using Game.Database.Structure;
using Game.Entity;
using Game.Fight;
using Game.Manager;
using Game.Network;
using System.Collections.Generic;
using System.Linq;

namespace Game.Map
{
    public sealed class FightManager : MessageDispatcher
    {
        /// <summary>
        /// 
        /// </summary>
        private readonly Dictionary<long, AbstractFight> m_fightList;
        private readonly List<FightActionDAO> m_fightActions;
        private readonly MapInstance m_map;
        private long m_fightId = 1;

        /// <summary>
        /// 
        /// </summary>
        public long FightCount
        {
            get;
            private set;
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<AbstractFight> Fights => m_fightList.Values;

        /// <summary>
        /// 
        /// </summary>
        public FightManager(MapInstance map)
        {
            m_map = map;
            m_map.AddUpdatable(this);
            m_map.SubArea.AddHandler(base.Dispatch);
            m_fightList = new Dictionary<long, AbstractFight>();
            m_fightActions = new List<FightActionDAO>(FightActionRepository.Instance.GetById(ZoneTypeEnum.TYPE_MAP, m_map.Id));
        }


        public bool CanStartFight(CharacterEntity character) => m_map.FightTeam0Cells.Count > 0 && m_map.FightTeam1Cells.Count > 0;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="map"></param>
        /// <param name="attacker"></param>
        /// <param name="defender"></param>
        public void StartChallenge(CharacterEntity attacker, CharacterEntity defender)
        {
            if (CanStartFight(attacker))
                Add(new ChallengerFight(m_map, m_fightId++, attacker, defender));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aggressor"></param>
        /// <param name="victim"></param>
        public void StartAggression(CharacterEntity aggressor, CharacterEntity victim)
        {
            if (CanStartFight(aggressor))
                Add(new AlignmentFight(m_map, m_fightId++, aggressor, victim));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="monsters"></param>
        /// <param name="victim"></param>
        /// <returns></returns>
        public bool StartAggression(MonsterGroupEntity monsters, CharacterEntity victim)
        {
            if (CanStartFight(victim))
            {
                monsters.StopAction(GameActionTypeEnum.MAP_MOVEMENT);
                monsters.StopAction(GameActionTypeEnum.MAP);
                Add(new AlignmentFight(m_map, m_fightId++, monsters, victim));
                return true;
            }
            return false;
        }

        public bool StartMonsterFight(CharacterEntity character, MonsterGroupEntity monsterGroup)
        {
            if (!CanStartFight(character))
                return false;

            if (!character.CanGameAction(GameActionTypeEnum.FIGHT))
            {
                character.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_YOU_ARE_AWAY));
                return false;
            }

            monsterGroup.StopAction(GameActionTypeEnum.MAP_MOVEMENT);
            monsterGroup.StopAction(GameActionTypeEnum.MAP);
            Add(new MonsterFight(m_map, m_fightId++, character, monsterGroup));

            return true;
        }

        public bool StartTaxCollectorAggression(CharacterEntity attacker, TaxCollectorEntity taxCollector)
        {
            if (!CanStartFight(attacker))
                return false;

            Add(new TaxCollectorFight(m_map, m_fightId++, attacker, taxCollector));

            return true;
        }

        public bool StartConquestFight(CharacterEntity attacker, Game.Conquest.ConquestTerritory territory, ConquestPrismEntity prism = null)
        {
            if (territory == null || !ConquestManager.Instance.CanAttack(territory, attacker))
                return false;

            if (!CanStartFight(attacker))
                return false;

            if (!attacker.CanGameAction(GameActionTypeEnum.FIGHT))
            {
                attacker.Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_YOU_ARE_AWAY));
                return false;
            }

            if (prism == null)
                prism = m_map.Entities.OfType<ConquestPrismEntity>().FirstOrDefault(entity => entity.Territory?.SubAreaId == territory.SubAreaId);

            if (prism == null)
                prism = ConquestManager.Instance.CreatePrismEntityForFight(m_map, territory);

            if (prism == null)
                return false;

            Add(new ConquestFight(m_map, m_fightId++, attacker, territory, prism));
            return true;
        }

        public void ExecuteFightActions(FightTypeEnum fightType, FightStateEnum state, CharacterEntity character)
        {
            foreach (var fightAction in m_fightActions.Where(faction => faction.Fight == fightType && faction.State == state))
            {
                if (ConditionParser.Instance.Check(fightAction.Conditions, character))
                {
                    foreach (var action in fightAction.ActionsList)
                    {
                        ActionEffectManager.Instance.ApplyEffect(character, action.Effect, action.Parameters);
                    }
                }
            }
        }

        public AbstractFight GetFight(long fightId) => m_fightList.TryGetValue(fightId, out AbstractFight value) ? value : null;


        private void Add(AbstractFight fight)
        {
            fight.Start();
            FightCount++;
            m_fightList.Add(fight.Id, fight);
            m_map.Dispatch(WorldMessage.FIGHT_COUNT(FightCount));
            AddHandler(fight.Dispatch);
            AddUpdatable(fight);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fight"></param>
        public void Remove(AbstractFight fight)
        {
            FightCount--;
            m_fightList.Remove(fight.Id);
            m_map.Dispatch(WorldMessage.FIGHT_COUNT(FightCount));
            RemoveHandler(fight.Dispatch);
            RemoveUpdatable(fight);
            fight.Dispose();
        }
    }
}



