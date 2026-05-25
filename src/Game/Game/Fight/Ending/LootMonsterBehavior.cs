using System;
using System.Collections.Generic;
using System.Linq;
using Game.Database.Structure;
using Game.Entity;
using Game.Spell;
using Game.Manager;

namespace Game.Fight.Ending
{
    public sealed class LootMonsterBehavior : AbstractLootBehavior<MonsterEntity>
    {
        protected override long GetAdditionalKamas(AbstractFight fight)
        {
            var monsterFight = fight as MonsterFight;
            if (monsterFight != null)
                if (fight.WinnerTeam == fight.Team0)
                    return monsterFight.MonsterGroup.Kamas;
            return 0;
        }

        protected override IEnumerable<ItemDAO> GetAdditionalLoot(AbstractFight fight)
        {
             var monsterFight = fight as MonsterFight;
            if (monsterFight != null)
                if (fight.WinnerTeam == fight.Team0)
                    return monsterFight.MonsterGroup.Inventory.RemoveItems();
            return Enumerable.Empty<ItemDAO>();
        }

        protected override IEnumerable<AbstractFighter> GetAdditionalDroppers(AbstractFight fight)
        {
            if (fight.Map.TaxCollector != null)
                yield return fight.Map.TaxCollector;
        }

        protected override long GetTargetKamas(EndingArguments<MonsterEntity> arguments, MonsterEntity fighter)
        {
            return (long)Math.Round(
                    Util.Next(fighter.Grade.Template.MinKamas, fighter.Grade.Template.MaxKamas)
                    * WorldConfig.RATE_KAMAS
                    * arguments.Fight.ChallengeLootBonus);
        }

        protected override IEnumerable<ItemDAO> GetTargetItems(EndingArguments<MonsterEntity> arguments, MonsterEntity fighter)
        {
            return DropManager.Instance.GetDrops
                (
                    arguments.DroppersTotalPP,
                    fighter,
                    WorldConfig.RATE_DROP * arguments.Fight.ChallengeLootBonus
                );
        }

        protected override long GetExperienceWon(EndingArguments<MonsterEntity> arguments, AbstractFighter fighter)
        {
            var monsterFight = arguments.Fight as MonsterFight;
            switch (fighter.Type)
            {
                case EntityTypeEnum.TYPE_CHARACTER:
                    return Util.CalculPVMExperience(
                                   arguments.Losers,
                                   arguments.Droppers,
                                   fighter.Level,
                                   fighter.Statistics.GetTotal(EffectEnum.AddWisdom),
                                   arguments.Fight.ChallengeXpBonus,
                                   monsterFight?.MonsterGroup.AgeBonus ?? 0);

                case EntityTypeEnum.TYPE_TAX_COLLECTOR:
                    return Util.CalculPVMExperienceTaxCollector(
                                   arguments.Losers,
                                   arguments.Droppers, 
                                   fighter.Level,
                                   fighter.Statistics.GetTotal(EffectEnum.AddWisdom),
                                   arguments.Fight.ChallengeXpBonus,
                                   monsterFight?.MonsterGroup.AgeBonus ?? 0);

            }
            return 0;
        }

        protected override long GetKamasWon(EndingArguments<MonsterEntity> arguments, AbstractFighter fighter)
        {
            return Util.CalculPVMKamas(arguments.KamasLoot, fighter.Prospection, arguments.DroppersTotalPP);
        }

        protected override long GetLoserExperienceWon(EndingArguments<MonsterEntity> arguments, CharacterEntity fighter)
        {
            if (arguments.Fight.WinnerTeam != arguments.Fight.Team1) return 0;
            var monsterFight = arguments.Fight as MonsterFight;
            var winnerMonsters = arguments.Fight.WinnerTeam.Fighters
                .OfType<MonsterEntity>()
                .Where(f => f.Invocator == null);
            var loserFighters = arguments.Fight.LoserTeam.Fighters
                .Where(f => f.Invocator == null);
            var fullXp = Util.CalculPVMExperience(
                winnerMonsters,
                loserFighters,
                fighter.Level,
                fighter.Statistics.GetTotal(EffectEnum.AddWisdom),
                1,
                monsterFight?.MonsterGroup.AgeBonus ?? 0);

            return (long)(fullXp * 0.1);
        }
    }
}


