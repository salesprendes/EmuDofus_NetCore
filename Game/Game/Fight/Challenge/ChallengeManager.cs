using Protocolo.Framework.Generic;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight.Challenge
{
    public enum ChallengeTypeEnum
    {
        ZOMBIE = 1,
        STATUE = 2,
        APPOINTED_VOLUNTARY = 3,
        REPRIEVE = 4,
        VERSATILE = 6,
        BARBARIRAN = 9,
        CIRCULATE = 21,
        LOST_SIGHT = 23,
        SURVIVOR = 33,
        BOLD = 36,
        TIGHTS = 37,
        ANACHORITE = 39,
        PETULANT = 41,
        ABNEGATION = 43,
    }

    public sealed class ChallengeManager : Singleton<ChallengeManager>
    {
        private List<Func<AbstractChallenge>> m_challengeGenerator;

        public ChallengeManager()
        {
            m_challengeGenerator = new List<Func<AbstractChallenge>>();
            m_challengeGenerator.Add(() => new ZombieChallenge());
            m_challengeGenerator.Add(() => new AnachoriteChallenge());
            m_challengeGenerator.Add(() => new AbnegationChallenge());
            m_challengeGenerator.Add(() => new BarbarianChallenge());
            m_challengeGenerator.Add(() => new CirculateChallenge());
            m_challengeGenerator.Add(() => new TightsChallenge());
            m_challengeGenerator.Add(() => new AppointedVoluntaryChallenge());
            m_challengeGenerator.Add(() => new BoldChallenge());
            m_challengeGenerator.Add(() => new StatueChallenge());
            m_challengeGenerator.Add(() => new ReprieveChallenge());
            m_challengeGenerator.Add(() => new SightLostChallenge());
            m_challengeGenerator.Add(() => new SurvivorChallenge());
            m_challengeGenerator.Add(() => new PetulantChallenge());
        }

        public List<AbstractChallenge> Generate(int length)
        {
            List<AbstractChallenge> challenges = new List<AbstractChallenge>();
            while (challenges.Count < length)
            {
                var challenge = m_challengeGenerator[Util.Next(0, m_challengeGenerator.Count)]();
                if (challenges.Any(chall => chall.Id == challenge.Id))
                    continue;
                challenges.Add(challenge);
            }
            return challenges;
        }
    }
}


