using Game.Fight.Effect;
using Game.Map;
using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.AI.Action.Type
{
    public enum AttackStateEnum
    { 
        STATE_CALCULATE_CELLS,
        STATE_CALCULATE_EFFECT_TARGETS,
        STATE_CALCULATE_BEST_SPELL,
        STATE_LAUNCH_ATTACK,
        STATE_ATTACKING,
    }

    public class AttackAction : AIAction
    {
        private AttackStateEnum AttackState
        {
            get;
            set;
        }

        private Dictionary<int, List<SpellLevel>> CastCellList
        {
            get;
            set;
        }

        private Dictionary<int, Dictionary<int, Dictionary<SpellEffect, List<AbstractFighter>>>> TargetList
        {
            get;
            set;
        }

        private IEnumerable<AbstractFighter> WeakestEnnemies
        {
            get;
            set;
        }

        private int TargetCell
        {
            get;
            set;
        }

        private int SpellId
        {
            get;
            set;
        }

        private SpellLevel SelectedSpellLevel
        {
            get;
            set;
        }

        public AttackAction(AIFighter fighter)
            : base(fighter)
        {
            AttackState = AttackStateEnum.STATE_CALCULATE_CELLS;
        }

        public override AIActionResult Initialize()
        {
            TargetCell = 0;
            SpellId = 0;
            SelectedSpellLevel = null;
            AttackState = AttackStateEnum.STATE_CALCULATE_CELLS;

            return Fighter.AP > 0 && Fighter.SpellBook.GetSpells().Any(spell => spell.APCost <= Fighter.AP) ? AIActionResult.RUNNING : AIActionResult.FAILURE;
        }

        public override AIActionResult Execute()
        {
            switch (AttackState)
            {
                case AttackStateEnum.STATE_CALCULATE_CELLS:
                    CastCellList = new Dictionary<int,List<SpellLevel>>();
                    WeakestEnnemies = Fighter.Team.OpponentTeam.AliveFighters.OrderBy(fighter => Pathfinding.GoalDistance(Map, Fighter.Cell.Id, fighter.Cell.Id));

                    foreach(var spellLevel in Fighter.SpellBook.GetSpells())
                    {
                        foreach (var castCell in CellZone.GetCircleCells(Map, Fighter.Cell.Id, spellLevel.MaxPO))
                        {
                            if (Fight.CanLaunchSpell(Fighter, spellLevel, spellLevel.SpellId, Fighter.Cell.Id, castCell) == FightSpellLaunchResultEnum.RESULT_OK)
                            {
                                if (!CastCellList.ContainsKey(castCell))
                                    CastCellList.Add(castCell, new List<SpellLevel>());
                                CastCellList[castCell].Add(spellLevel);
                            }
                        }
                    }

                    if (CastCellList.Count == 0)
                        return AIActionResult.FAILURE;

                    AttackState = AttackStateEnum.STATE_CALCULATE_EFFECT_TARGETS;
                    return AIActionResult.RUNNING;

                case AttackStateEnum.STATE_CALCULATE_EFFECT_TARGETS:
                    TargetList = new Dictionary<int, Dictionary<int, Dictionary<SpellEffect, List<AbstractFighter>>>>();
                    foreach(var castInfos in CastCellList)
                    {
                        var castCell = castInfos.Key;
                        TargetList.Add(castCell, new Dictionary<int, Dictionary<SpellEffect, List<AbstractFighter>>>());
                        foreach(var spellLevel in castInfos.Value)
                        {
                            if (spellLevel == null || spellLevel.Effects == null)
                                continue;

                            TargetList[castCell].Add(spellLevel.SpellId, new Dictionary<SpellEffect, List<AbstractFighter>>());

                            int effectIndex = 0;
                            foreach (var effect in spellLevel.Effects)
                            {
                                TargetList[castCell][spellLevel.SpellId].Add(effect, new List<AbstractFighter>());

                                var targetType = spellLevel.Template.Targets != null ? spellLevel.Template.Targets.Count > effectIndex ? spellLevel.Template.Targets[effectIndex] : -1 : -1;

                                if (effect.TypeEnum != EffectEnum.UseGlyph && effect.TypeEnum != EffectEnum.UseTrap)
                                {
                                    foreach (var currentCellId in CellZone.GetCells(Map, castCell, Fighter.Cell.Id, spellLevel.RangeType))
                                    {
                                        var fightCell = Fight.GetCell(currentCellId);
                                        if (fightCell != null)
                                        {
                                            foreach (var fighterObject in fightCell.FightObjects.OfType<AbstractFighter>())
                                            {
                                                if (targetType != -1)
                                                {
                                                    if (((((targetType >> 5) & 1) == 1) && (Fighter.Id != fighterObject.Id)))
                                                    {
                                                        if (!TargetList[castCell][spellLevel.SpellId][effect].Contains(Fighter))
                                                            TargetList[castCell][spellLevel.SpellId][effect].Add(Fighter);
                                                        continue;
                                                    }
                                                    if (((targetType & 1) == 1) && Fighter.Team == fighterObject.Team)
                                                        continue;
                                                    if ((((targetType >> 1) & 1) == 1) && Fighter == fighterObject)
                                                        continue;
                                                    if ((((targetType >> 2) & 1) == 1) && Fighter.Team != fighterObject.Team)
                                                        continue;
                                                    if (((((targetType >> 3) & 1) == 1) && (fighterObject.Invocator == null)))
                                                        continue;
                                                    if (((((targetType >> 4) & 1) == 1) && (fighterObject.Invocator != null)))
                                                        continue;                                                                                                   
                                                }

                                                if (!TargetList[castCell][spellLevel.SpellId][effect].Contains(fighterObject))
                                                    TargetList[castCell][spellLevel.SpellId][effect].Add(fighterObject);
                                            }
                                        }
                                    }
                                }
                                effectIndex++;
                            }
                        }
                    }
                        
                    AttackState = AttackStateEnum.STATE_CALCULATE_BEST_SPELL;

                    return AIActionResult.RUNNING;

                case AttackStateEnum.STATE_CALCULATE_BEST_SPELL:
                    int bestScore = 0;
                    foreach(var target in TargetList)
                    {
                        var castCell = target.Key;

                        foreach (var spell in target.Value)
                        {
                            var spellId = spell.Key;
                            var currentScore = -1;
                            int enemyHitCount = 0;
                            int allyHitCount = 0;

                            foreach (var levelInfos in spell.Value)
                            {
                                var effect = levelInfos.Key;
                                foreach (var fighter in levelInfos.Value)
                                {
                                    bool isEnemy = fighter.Team.Id != Fighter.Team.Id;

                                    if (CastInfos.IsDamageEffect(effect.TypeEnum))
                                    {
                                        if (isEnemy)
                                        {
                                            int baseDmg = effect.Value1 + effect.Value2 + effect.Value3;
                                            currentScore += 200 + baseDmg;
                                            enemyHitCount++;

                                            // Big bonus for killing blow
                                            if (fighter.Life <= baseDmg * 2)
                                                currentScore += 500;

                                            // Bonus for targeting already-weakened enemies
                                            if (fighter.MaxLife > 0)
                                                currentScore += (int)(150 * (1.0 - (double)fighter.Life / fighter.MaxLife));
                                        }
                                        else
                                        {
                                            // Heavy penalty for friendly fire
                                            currentScore -= 500 + effect.Value1 + effect.Value2 + effect.Value3;
                                            allyHitCount++;
                                        }
                                    }
                                    else if (CastInfos.IsMalusEffect(effect.TypeEnum))
                                    {
                                        if (isEnemy)
                                        {
                                            // AP/MP steal is extremely valuable – disables enemy actions
                                            int malusValue = ScoreDebuff(effect.TypeEnum, effect.Value1 + effect.Value2);
                                            currentScore += malusValue;
                                        }
                                        else
                                        {
                                            currentScore -= 80 + effect.Value1 + effect.Value2 + effect.Value3;
                                        }
                                    }
                                    else if(CastInfos.IsBonusEffect(effect.TypeEnum) || CastInfos.IsFriendlyEffect(effect.TypeEnum))
                                    {
                                        if (isEnemy)
                                            currentScore -= 50 + effect.Value1 + effect.Value2 + effect.Value3;
                                        else
                                        {
                                            if (effect.TypeEnum == EffectEnum.Heal)
                                            {
                                                int missingHpPct = fighter.MaxLife > 0
                                                    ? (int)(100 * (1.0 - (double)fighter.Life / fighter.MaxLife))
                                                    : 0;
                                                currentScore += 50 + (effect.Value1 + effect.Value2 + effect.Value3) * missingHpPct / 100;
                                            }
                                            else
                                                currentScore += 50 + effect.Value1 + effect.Value2 + effect.Value3;
                                        }
                                    }
                                }

                                if(levelInfos.Value.Count == 0)
                                {
                                    switch(effect.TypeEnum)
                                    {
                                        case EffectEnum.UseTrap:
                                        case EffectEnum.UseGlyph:
                                        case EffectEnum.Teleport:
                                            foreach(var ennemy in WeakestEnnemies)
                                            {
                                                currentScore += 50;
                                                currentScore -= Pathfinding.GoalDistance(Map, castCell, ennemy.Cell.Id);
                                            }
                                            break;

                                        case EffectEnum.Invocation:
                                        case EffectEnum.InvocationStatic:
                                        case EffectEnum.InvocDouble:
                                            currentScore += 80;
                                            break;
                                    }
                                }
                            }

                            // Bonus for hitting multiple enemies with one cast (zone spells)
                            if (enemyHitCount > 1)
                                currentScore += (enemyHitCount - 1) * 150;

                            // Drop score if we hit more allies than enemies (bad zone placement)
                            if (allyHitCount > enemyHitCount)
                                currentScore -= allyHitCount * 200;

                            if (currentScore > bestScore)
                            {
                                bestScore = currentScore;
                                SpellId = spellId;
                                TargetCell = castCell;
                                SelectedSpellLevel = Fighter.SpellBook.GetSpellLevel(spellId);
                            }
                        }
                    }

                    if (SpellId == 0)
                        return AIActionResult.FAILURE;

                    AttackState = AttackStateEnum.STATE_LAUNCH_ATTACK;

                    return AIActionResult.RUNNING;

                case AttackStateEnum.STATE_LAUNCH_ATTACK:
                    var actionTime = GetSpellActionTime(SelectedSpellLevel);
                    Fight.TryLaunchSpell(Fighter, SpellId, TargetCell, actionTime);
                    Timeout = actionTime + GetActionThinkTime();

                    AttackState = AttackStateEnum.STATE_ATTACKING;

                    return AIActionResult.RUNNING;

                case AttackStateEnum.STATE_ATTACKING:
                    if (!Timedout)
                        return AIActionResult.RUNNING;
                    
                    return Initialize();

                default:
                    throw new Exception("AI Attack action invalid state.");
            }
        }

        // Returns a weighted score for a debuff effect — AP/MP removal is most impactful
        private static int ScoreDebuff(EffectEnum effect, int value)
        {
            switch (effect)
            {
                case EffectEnum.SubAP:
                case EffectEnum.SubAPDodgeable:
                    return 250 + value * 80; // losing AP = losing entire actions
                case EffectEnum.SubMP:
                case EffectEnum.SubMPDodgeable:
                    return 150 + value * 50; // losing MP = losing mobility
                case EffectEnum.SubStrength:
                case EffectEnum.SubIntelligence:
                case EffectEnum.SubAgility:
                case EffectEnum.SubChance:
                    return 80 + value * 2;
                case EffectEnum.SubReduceDamageAir:
                case EffectEnum.SubReduceDamageEarth:
                case EffectEnum.SubReduceDamageFire:
                case EffectEnum.SubReduceDamageWater:
                case EffectEnum.SubReduceDamageNeutral:
                    return 60 + value;
                default:
                    return 60;
            }
        }
    }
}


