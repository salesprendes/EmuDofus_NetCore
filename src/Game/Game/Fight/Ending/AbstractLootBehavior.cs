using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Database.Structure;
using Game.Entity;
using Game.Spell;
using Game.Manager;

namespace Game.Fight.Ending
{
    public abstract class AbstractLootBehavior<T> : AbstractEndingBehavior
        where T : AbstractFighter
    {
        protected abstract long GetAdditionalKamas(AbstractFight fight);
        protected abstract IEnumerable<ItemDAO> GetAdditionalLoot(AbstractFight fight); 
        protected abstract IEnumerable<AbstractFighter> GetAdditionalDroppers(AbstractFight fight); 
        protected abstract long GetTargetKamas(EndingArguments<T> arguments, T fighter);
        protected abstract IEnumerable<ItemDAO> GetTargetItems(EndingArguments<T> arguments, T fighter);
        protected abstract long GetExperienceWon(EndingArguments<T> arguments, AbstractFighter fighter);
        protected abstract long GetKamasWon(EndingArguments<T> arguments, AbstractFighter fighter);
        protected virtual long GetLoserExperienceWon(EndingArguments<T> arguments, CharacterEntity fighter) => 0;

        public override void Execute(AbstractFight fight)
        {
            long kamasLoot = GetAdditionalKamas(fight);
            var itemLoot = new List<ItemDAO>(GetAdditionalLoot(fight));
            
            var droppers = fight
                .WinnerTeam
                .Fighters
                .Where(fighter => fighter.CanDrop)
                .Concat(GetAdditionalDroppers(fight))
                .ToList();

            var losers = fight
                .LoserTeam
                .Fighters
                .OfType<T>()
                .Where(fighter => fighter.Invocator == null)
                .ToList();

            var droppersTotalPP = droppers.Sum(fighter => fighter.Prospection);

            var arguments = new EndingArguments<T>(fight, droppers, losers, droppersTotalPP, itemLoot, kamasLoot);

            foreach (var loser in losers)
            {
                kamasLoot += GetTargetKamas(arguments, loser);
                itemLoot.AddRange(GetTargetItems(arguments, loser));
            }

            var distributedDrop = DropManager.Instance.Distribute(droppers, droppersTotalPP, itemLoot);

            foreach (var fighter in droppers)
            {
                fighter.CachedBuffer = true;
                var itemWon = distributedDrop[fighter];
                var kamasWon = GetKamasWon(arguments, fighter);
                var xpWon = GetExperienceWon(arguments, fighter);
                switch (fighter.Type)
                {
                    case EntityTypeEnum.TYPE_CHARACTER:
                        var character = (CharacterEntity)fighter;
                        foreach (var item in itemWon)
                            character.Inventory.AddItem(item);
                        character.Inventory.AddKamas(kamasWon);
                        character.AddExperience(xpWon);
                     break;

                    case EntityTypeEnum.TYPE_MONSTER_FIGHTER:
                        var monsterFight = fight as MonsterFight;

                        // case of monster winning
                        if (monsterFight != null && fight.WinnerTeam == fight.Team0)
                        {
                            foreach (var item in itemWon)
                                monsterFight.MonsterGroup.Inventory.AddItem(item);
                            monsterFight.MonsterGroup.Inventory.AddKamas(kamasWon);
                        }
                        else
                        {
                            foreach (var item in itemWon)
                            {
                                var invocator = fighter.Invocator;
                                while (fighter.Invocator != null)
                                    invocator = fighter.Invocator;
                                invocator?.Inventory?.AddItem(item);
                                invocator?.Inventory?.AddKamas(kamasWon);
                            }
                        }
                        break;

                    case EntityTypeEnum.TYPE_TAX_COLLECTOR:
                        var taxCollector = (TaxCollectorEntity)fighter;
                        taxCollector.Storage.AddKamas(kamasWon);
                        taxCollector.ExperienceGathered += xpWon;
                        foreach (var item in itemWon)
                            taxCollector.Storage.AddItem(item);
                        break;
                }
                fight.Result.AddResult(fighter,
                    FightEndTypeEnum.END_WINNER,
                    false,
                    kamasWon,
                    xpWon,
                    0,
                    0,
                    0,
                    0,
                    itemWon
                        .GroupBy(item => item.TemplateId)
                        .Select(g => new { TemplateId = g.Key, Count = g.Count() })
                        .ToDictionary(g => g.TemplateId, g => g.Count));
                fighter.CachedBuffer = false;
            }

            foreach (var loserCharacter in fight.LoserTeam.Fighters
                .OfType<CharacterEntity>()
                .Where(f => f.Invocator == null))
            {
                var xpWon = GetLoserExperienceWon(arguments, loserCharacter);
                if (xpWon <= 0) continue;
                loserCharacter.CachedBuffer = true;
                loserCharacter.AddExperience(xpWon);
                fight.Result.AddResult(loserCharacter, FightEndTypeEnum.END_LOSER, false, 0, xpWon);
                loserCharacter.CachedBuffer = false;
            }
        }
    }
}


