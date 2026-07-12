using Game.Database.Structure;
using Game.Entity;
using Game.Manager;
using System;
using System.Collections.Generic;
using System.Linq;

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

            var droppers = fight.WinnerTeam.Fighters.Where(fighter => fighter.CanDrop).Concat(GetAdditionalDroppers(fight)).ToList();

            var losers = fight.LoserTeam.Fighters.OfType<T>().Where(fighter => fighter.Invocator == null).ToList();

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
                long mountXpWon = 0;
                switch (fighter.Type)
                {
                    case EntityTypeEnum.TYPE_CHARACTER:
                        var character = (CharacterEntity)fighter;
                        foreach (var item in itemWon)
                            character.Inventory.AddItem(item);
                        character.Inventory.AddKamas(kamasWon);
                        // Reparte la XP: la montura equipada se queda con su porcentaje y el
                        // personaje con el resto. xpWon pasa a ser solo lo que gana el personaje.
                        mountXpWon = character.AddFightExperience(xpWon);
                        xpWon -= mountXpWon;
                        break;

                    case EntityTypeEnum.TYPE_MONSTER_FIGHTER:
                        var monsterFight = fight as MonsterFight;


                        if (monsterFight != null && fight.WinnerTeam == fight.Team0)
                        {
                            foreach (var item in itemWon)
                                monsterFight.MonsterGroup.Inventory.AddItem(item);
                            monsterFight.MonsterGroup.Inventory.AddKamas(kamasWon);
                        }
                        else
                        {
                            // Subir hasta el invocador raíz: la condición del while original
                            // (fighter.Invocator, constante) era un bucle infinito que congelaba
                            // el hilo de la subárea. El abono de kamas va una sola vez, no por ítem.
                            var invocator = fighter.Invocator;
                            while (invocator?.Invocator != null)
                                invocator = invocator.Invocator;

                            foreach (var item in itemWon)
                                invocator?.Inventory?.AddItem(item);
                            invocator?.Inventory?.AddKamas(kamasWon);
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
                    mountXpWon,
                    itemWon
                        .GroupBy(item => item.TemplateId)
                        .Select(g => new { TemplateId = g.Key, Count = g.Count() })
                        .ToDictionary(g => g.TemplateId, g => g.Count));
                fighter.CachedBuffer = false;
            }

            foreach (var loserCharacter in fight.LoserTeam.Fighters.OfType<CharacterEntity>().Where(f => f.Invocator == null))
            {
                if (fight.Result.HasResult(loserCharacter)) continue;

                var xpWon = GetLoserExperienceWon(arguments, loserCharacter);
                if (xpWon <= 0) continue;
                loserCharacter.CachedBuffer = true;
                var mountXpWon = loserCharacter.AddFightExperience(xpWon);
                xpWon -= mountXpWon;
                fight.Result.AddResult(loserCharacter, FightEndTypeEnum.END_LOSER, false, 0, xpWon, 0, 0, 0, mountXpWon);
                loserCharacter.CachedBuffer = false;
            }
        }
    }
}

