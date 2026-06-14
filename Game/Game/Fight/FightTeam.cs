using Game.Action;
using Game.Database.Structure;
using Game.Entity;
using Game.Fight.Challenge;
using Game.Fight.Effect;
using Game.Manager;
using Game.Network;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight
{
    public enum FightOptionTypeEnum
    {
        TYPE_NEW_PLAYER_BIS = 'A',
        TYPE_NEW_PLAYER = 'N',
        TYPE_HELP = 'H',
        TYPE_PARTY = 'P',
        TYPE_SPECTATOR = 'S',
    }

    public sealed class SpectatorTeam : MessageDispatcher
    {
        public List<CharacterEntity> Spectators => m_spectators;
        private List<CharacterEntity> m_spectators;
        private AbstractFight m_fight;
        public bool CanJoin => m_fight.State == FightStateEnum.STATE_FIGHTING && !m_fight.Team0.IsOptionLocked(FightOptionTypeEnum.TYPE_SPECTATOR) && !m_fight.Team1.IsOptionLocked(FightOptionTypeEnum.TYPE_SPECTATOR);

        public SpectatorTeam(AbstractFight fight)
        {
            m_fight = fight;
            m_spectators = new List<CharacterEntity>();
        }

        public void AddSpectator(CharacterEntity fighter)
        {
            m_spectators.Add(fighter);
        }

        public void RemoveSpectator(CharacterEntity fighter)
        {
            m_spectators.Remove(fighter);
        }

        public override void Dispose()
        {
            m_fight = null;
            m_spectators.Clear();
            m_spectators = null;

            base.Dispose();
        }
    }

    public sealed class FightTeam : MessageDispatcher
    {
        public List<AbstractFighter> Fighters => m_fighters;

        public IEnumerable<AbstractFighter> AliveFighters => Fighters.Where(fighter => !fighter.IsFighterDead);

        public int Id
        {
            get;
            private set;
        }

        public AbstractFight Fight
        {
            get;
            private set;
        }

        public AbstractFighter Leader => m_fighters.FirstOrDefault();

        public long LeaderId
        {
            get;
            set;
        }

        public int FlagCellId
        {
            get;
            set;
        }

        public FightTeam OpponentTeam
        {
            get;
            set;
        }

        public FightCell FreePlace
        {
            get
            {
                return m_places.Find(cell => cell.CanWalk);
            }
        }

        public int PlacesCount => m_places.Count;

        public bool HasSomeoneAlive
        {
            get
            {
                switch (Fight.Type)
                {
                    case FightTypeEnum.TYPE_PVT:
                        if (m_fighters[0].IsFighterDead)
                            return false;
                        break;

                    case FightTypeEnum.TYPE_PVMA:
                        if (m_fighters[0] is ConquestPrismEntity)
                        {

                            var charDefenders = m_fighters.OfType<CharacterEntity>();
                            if (charDefenders.Any())
                                return charDefenders.Any(f => !f.IsFighterDead);

                            return !m_fighters[0].IsFighterDead;
                        }

                        if (m_fighters[0].IsFighterDead)
                            return false;
                        break;
                }

                return m_fighters.Any(fighter => !fighter.IsFighterDead && fighter.Invocator == null);
            }
        }

        public string Places
        {
            get
            {
                return m_placesCache ?? (m_placesCache = string.Create(m_places.Count * 2, m_places, static (destination, places) =>
                {
                    for (int i = 0; i < places.Count; i++)
                    {
                        Util.CellToChar(places[i].Id, destination.Slice(i * 2, 2));
                    }
                }));
            }
        }

        public int AlignmentId
        {
            get;
            private set;
        }

        public IEnumerable<AbstractChallenge> SucceededChallenges
        {
            get
            {
                return m_challenges.Where(challenge => challenge.Success);
            }
        }

        private Dictionary<FightOptionTypeEnum, bool> m_blockedOption;
        private List<AbstractFighter> m_fighters;
        private List<FightCell> m_places;
        private List<AbstractChallenge> m_challenges;
        private string m_placesCache;

        public FightTeam(int id, long leaderId, int alignment, int flagCell, AbstractFight fight, List<FightCell> places)
        {
            Id = id;
            Fight = fight;
            LeaderId = leaderId;
            AlignmentId = alignment;
            FlagCellId = flagCell;

            m_challenges = new List<AbstractChallenge>();
            m_fighters = new List<AbstractFighter>();
            m_places = places;
            m_blockedOption = new Dictionary<FightOptionTypeEnum, bool>()
            {
                { FightOptionTypeEnum.TYPE_NEW_PLAYER_BIS, false },
                { FightOptionTypeEnum.TYPE_HELP, false },
                { FightOptionTypeEnum.TYPE_PARTY, false },
                { FightOptionTypeEnum.TYPE_SPECTATOR, false },
            };
        }

        public void AddChallenge(AbstractChallenge challenge)
        {
            challenge.AddHandler(base.Dispatch);
            m_challenges.Add(challenge);
        }

        public void AddFighter(AbstractFighter fighter)
        {
            m_fighters.Add(fighter);
        }

        public void RemoveFighter(AbstractFighter fighter)
        {
            m_fighters.Remove(fighter);
        }

        public AbstractFighter GetFighter(long fighterId)
        {
            return m_fighters.Find(fighter => fighter.Id == fighterId);
        }

        public bool CanJoinBeforeStart(CharacterEntity character)
        {
            if (LeaderId < 0 && AlignmentId == -1)
                return false;


            if (FreePlace == null || IsOptionLocked(FightOptionTypeEnum.TYPE_NEW_PLAYER_BIS))
            {
                character.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_JOIN, character.Id, "f"));
                return false;
            }

            if (IsOptionLocked(FightOptionTypeEnum.TYPE_PARTY) && character.PartyId != ((CharacterEntity)m_fighters[0]).PartyId)
            {
                character.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_JOIN, character.Id, "f"));
                return false;
            }


            if (FreePlace == null)
            {
                character.Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_JOIN, character.Id, "c"));
                return false;
            }

            switch (Fight.Type)
            {
                case FightTypeEnum.TYPE_PVT:
                    var taxCollector = OpponentTeam.Fighters[0] as TaxCollectorEntity;
                    if (taxCollector == null)
                        return false;

                    if (character.GuildMember != null && character.GuildMember.GuildId == taxCollector.Guild.Id)
                        return false;
                    break;

                case FightTypeEnum.TYPE_PVM:
                case FightTypeEnum.TYPE_AGGRESSION:
                    return AlignmentId <= (int)ConquestManager.AlignmentTypeEnum.ALIGNMENT_NEUTRAL || character.AlignmentId == AlignmentId;
            }

            return true;
        }

        public void SendChallengeInfos()
        {
            foreach (var challenge in m_challenges)
            {
                challenge.StartFight(this);
                Dispatch(WorldMessage.FIGHT_CHALLENGE_INFORMATIONS(challenge.Id,
                    challenge.ShowTarget,
                    challenge.TargetId,
                    challenge.BasicXpBonus,
                    challenge.TeamXpBonus,
                    challenge.BasicDropBonus,
                    challenge.TeamDropBonus,
                    challenge.Success));
            }
        }

        public void SendChallengeInfos(AbstractFighter fighter)
        {
            foreach (var challenge in m_challenges)
                fighter.Dispatch(WorldMessage.FIGHT_CHALLENGE_INFORMATIONS(challenge.Id, challenge.ShowTarget, challenge.TargetId, challenge.BasicXpBonus, challenge.TeamXpBonus, challenge.BasicDropBonus, challenge.TeamDropBonus, challenge.Success));
        }

        public void BeginTurn(AbstractFighter fighter)
        {
            foreach (var challenge in m_challenges)
                challenge.BeginTurn(fighter);
        }

        public void CheckSpell(AbstractFighter fighter, CastInfos castInfos)
        {
            foreach (var challenge in m_challenges)
                challenge.CheckSpell(fighter, castInfos);
        }

        public void CheckMovement(AbstractFighter fighter, int beginCell, int endCell, int movementLength)
        {
            foreach (var challenge in m_challenges)
                challenge.CheckMovement(fighter, beginCell, endCell, movementLength);
        }

        public void CheckWeapon(AbstractFighter fighter, ItemTemplateDAO weapon)
        {
            foreach (var challenge in m_challenges)
                challenge.CheckWeapon(fighter, weapon);
        }

        public void CheckDeath(AbstractFighter fighter)
        {
            if (!HasSomeoneAlive)
                return;

            foreach (var challenge in m_challenges)
                challenge.CheckDeath(fighter);
        }

        public void EndTurn(AbstractFighter fighter)
        {
            foreach (var challenge in m_challenges)
                challenge.EndTurn(fighter);
        }

        public void FightEnd()
        {
            foreach (var challenge in m_challenges)
            {
                if (!challenge.Success && !challenge.Failed && HasSomeoneAlive)
                    challenge.OnSuccess();
            }
        }

        public void OptionLock(FightOptionTypeEnum type)
        {
            AddMessage(() =>
            {
                if (type == FightOptionTypeEnum.TYPE_NEW_PLAYER)
                    type = FightOptionTypeEnum.TYPE_NEW_PLAYER_BIS;

                m_blockedOption[type] = m_blockedOption[type] == false;

                var value = m_blockedOption[type];

                var infoType = InformationEnum.INFO_FIGHT_TOGGLE_PARTY;

                if (Fight.State == FightStateEnum.STATE_PLACEMENT)
                {
                    Fight.Map.Dispatch(WorldMessage.FIGHT_OPTION(type, value, LeaderId));
                }

                switch (type)
                {
                    case FightOptionTypeEnum.TYPE_HELP:
                        infoType = value ? InformationEnum.INFO_FIGHT_TOGGLE_HELP : InformationEnum.INFO_FIGHT_UNTOGGLE_HELP;
                        break;

                    case FightOptionTypeEnum.TYPE_NEW_PLAYER_BIS:
                        infoType = value ? InformationEnum.INFO_FIGHT_TOGGLE_PLAYER : InformationEnum.INFO_FIGHT_UNTOGGLE_PLAYER;
                        break;

                    case FightOptionTypeEnum.TYPE_PARTY:
                        infoType = value ? InformationEnum.INFO_FIGHT_TOGGLE_PARTY : InformationEnum.INFO_FIGHT_UNTOGGLE_PARTY;
                    break;

                    case FightOptionTypeEnum.TYPE_SPECTATOR:
                        if (value)
                        {
                            Fight.KickSpectators();
                            Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_FIGHT_TOGGLE_SPECTATOR));
                        }
                        else
                        {
                            Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_FIGHT_UNTOGGLE_SPECTATOR));
                        }
                    return;
                }

                Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, infoType));
            });
        }

        public void SendMapFightInfos(AbstractEntity entity)
        {
            entity.Dispatch(WorldMessage.FIGHT_FLAG_UPDATE(OperatorEnum.OPERATOR_ADD, LeaderId, Fighters.ToArray()));
            foreach (var option in m_blockedOption)
                entity.Dispatch(WorldMessage.FIGHT_OPTION(option.Key, option.Value, LeaderId));
        }

        public bool IsOptionLocked(FightOptionTypeEnum toggle)
        {
            return m_blockedOption[toggle];
        }

        public override void Dispose()
        {
            Fight = null;
            OpponentTeam = null;

            foreach (var challenge in m_challenges) challenge.Dispose();
            m_challenges.Clear();
            m_challenges = null;

            m_places.Clear();
            m_places = null;

            m_fighters.Clear();
            m_fighters = null;

            m_blockedOption.Clear();
            m_blockedOption = null;

            base.Dispose();
        }
    }
}


