using Game.Database.Structure;
using Game.Entity;
using Game.Manager;
using System;
using System.Linq;

namespace Game.Fight.Ending
{
    public sealed class HonorGainBehavior : AbstractEndingBehavior
    {
        // Index = AlignmentLevel (grade 0-10), grades 0-2 use default 1.0
        private static readonly double[] GradeMultiplicator = { 1.0, 1.0, 1.0, 1.5, 2.0, 2.5, 3.0, 4.0, 6.0, 8.0, 12.0 };

        public override void Execute(AbstractFight fight)
        {
            var winnerChars = fight.WinnerFighters.OfType<CharacterEntity>().ToList();
            var loserChars = fight.LoserFighters.OfType<CharacterEntity>().ToList();

            int winnerCount = winnerChars.Count;
            int loserCount = loserChars.Count;

            double winnerAvgLevel = winnerCount > 0 ? winnerChars.Sum(f => (double)f.Level) / winnerCount : 0;
            double loserAvgLevel = loserCount > 0 ? loserChars.Sum(f => (double)f.Level) / loserCount : 0;
            double winnerAvgGrade = winnerCount > 0 ? winnerChars.Sum(f => (double)f.AlignmentLevel) / winnerCount : 0;
            double loserAvgGrade = loserCount > 0 ? loserChars.Sum(f => (double)f.AlignmentLevel) / loserCount : 0;

            double diffLevels = winnerAvgLevel - loserAvgLevel;
            double diffGrades = winnerAvgGrade - loserAvgGrade;

            var isNeutralAggression = fight.IsNeutralAgression;

            // Winner conditions (official retro formula)
            bool winnersAvgTooPowerful = winnerAvgLevel > loserAvgLevel + 20;
            double winnerLevelsSum = winnerAvgLevel * winnerCount;
            double loserLevelsSum = loserAvgLevel * loserCount;
            bool winnerGroupTooPowerful = winnerCount > 0 && (winnerLevelsSum - loserLevelsSum > 20.0 * winnerCount);
            // Cap diffLevels when losers are much stronger
            double winnerDiffLevels = winnerAvgLevel < loserAvgLevel - 20 ? -20 : diffLevels;

            foreach (var fighter in fight.WinnerFighters)
            {
                var honour = 0;
                var dishonour = 0;

                if (fighter is CharacterEntity winner && winner.AlignmentId != (int)ConquestManager.AlignmentTypeEnum.ALIGNMENT_NEUTRAL)
                {
                    if (!isNeutralAggression || winner.Team.AlignmentId == (int)ConquestManager.AlignmentTypeEnum.ALIGNMENT_NEUTRAL)
                    {
                        if (loserCount > 0 && !winnersAvgTooPowerful && !winnerGroupTooPowerful)
                        {
                            honour = (int)Math.Round(100.0 - winnerDiffLevels + 3.0 * -diffGrades + 15.0 * loserCount);

                            if (honour < 0)
                                honour = 0;
                        }
                        winner.ChangeDishonour(-1);
                    }
                    else
                        dishonour = 1;
                    winner.ChangeHonour(honour);
                }

                fight.Result.AddResult(fighter, FightEndTypeEnum.END_WINNER, false, 0, 0, honour, dishonour);
            }

            // Loser conditions (official retro formula)
            bool losersUnderdog = winnerCount > 0 && (winnerLevelsSum - loserLevelsSum > 20.0 * loserCount);
            double loserDiffLevels = diffLevels <= 0 ? 1.0 : diffLevels;

            foreach (var fighter in fight.LoserFighters)
            {
                var honour = 0;
                var dishonour = 0;

                if (fighter is CharacterEntity loser)
                {
                    if (loser.AlignmentId != (int)ConquestManager.AlignmentTypeEnum.ALIGNMENT_NEUTRAL)
                    {
                        if (!isNeutralAggression || loser.Team.AlignmentId != (int)ConquestManager.AlignmentTypeEnum.ALIGNMENT_NEUTRAL)
                        {
                            if (!losersUnderdog && winnerCount > 0)
                            {
                                double playerDiffGrades = loser.AlignmentLevel - winnerAvgGrade;
                                if (playerDiffGrades <= 0) playerDiffGrades = 1;

                                var grade = loser.AlignmentLevel;
                                double multiplicator = grade >= 0 && grade < GradeMultiplicator.Length ? GradeMultiplicator[grade] : 1.0;

                                double raw = (-100.0 - loserDiffLevels - 3.0 * playerDiffGrades - 5.0 * winnerCount) * multiplicator;
                                honour = (int)Math.Abs(Math.Round(raw));
                            }
                        }
                        loser.ChangeHonour(-honour);
                    }
                    else
                        dishonour = 1;
                }

                fight.Result.AddResult(fighter, FightEndTypeEnum.END_LOSER, false, 0, 0, -honour, dishonour);
            }
        }
    }
}
