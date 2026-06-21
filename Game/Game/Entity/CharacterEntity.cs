using Game.Action;
using Game.Database.Repository;
using Game.Database.Structure;
using Game.Entity.Inventory;
using Game.Exchange;
using Game.Fight;
using Game.Frame;
using Game.Guild;
using Game.House;
using Game.Interactive.Type;
using Game.Job;
using Game.Manager;
using Game.Mount;
using Game.Network;
using Game.Quest;
using Game.Spell;
using Game.Stats;
using Protocolo.Framework.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Entity
{
    public enum EmoteTypeEnum
    {
        Sit = 1,
        Bye = 2,
        Applause = 4,
        Angry = 8,
        Fear = 16,
        Weapon = 32,
        Flute = 64,
        Pet = 128,
        Hello = 256,
        Kiss = 512,
        Stone = 1024,
        Sheet = 2048,
        Scissors = 4096,
        CrossArm = 8192,
        Point = 16384,
        Crow = 32768,
        Rest = 262144,
        Champ = 1048576,
        PowerAura = 2097152,
        VampyrAura = 4194304
    }

    public enum DeathTypeEnum
    {
        TYPE_NORMAL = 1,
        TYPE_HEROIC = 2
    }

    public class CharacterEntity : AbstractFighter, IDisposable
    {
        public delegate void OnKick();

        public event OnKick KickEvent;

        private int m_pendingInteractiveSkillMapId = -1;
        private int m_pendingInteractiveSkillCellId = -1;
        private int m_pendingInteractiveSkillId = -1;

        public FrameManager<CharacterEntity, string> FrameManager
        {
            get;
            private set;
        }

        public override string Name => DatabaseRecord.Name;
        public long RttMs { get; set; } = 300;

        public string Ip
        {
            get;
            set;
        }

        public int SavedMapId
        {
            get
            {
                return DatabaseRecord.SavedMapId;
            }
            set
            {
                DatabaseRecord.SavedMapId = value;
            }
        }

        public int SavedCellId
        {
            get
            {
                return DatabaseRecord.SavedCellId;
            }
            set
            {
                DatabaseRecord.SavedCellId = value;
            }
        }

        public override int MapId
        {
            get
            {
                return DatabaseRecord.MapId;
            }
            set
            {
                DatabaseRecord.MapId = value;
            }
        }


        public override int CellId
        {
            get
            {
                return DatabaseRecord.CellId;
            }
            set
            {
                DatabaseRecord.CellId = value;
            }
        }

        public void QueuePendingInteractiveSkill(int mapId, int cellId, int skillId)
        {
            m_pendingInteractiveSkillMapId = mapId;
            m_pendingInteractiveSkillCellId = cellId;
            m_pendingInteractiveSkillId = skillId;
        }

        public bool TryGetPendingInteractiveSkill(int mapId, out int cellId, out int skillId)
        {
            if (m_pendingInteractiveSkillId == -1 || m_pendingInteractiveSkillMapId != mapId)
            {
                cellId = -1;
                skillId = -1;
                return false;
            }

            cellId = m_pendingInteractiveSkillCellId;
            skillId = m_pendingInteractiveSkillId;
            return true;
        }

        public void ClearPendingInteractiveSkill()
        {
            m_pendingInteractiveSkillMapId = -1;
            m_pendingInteractiveSkillCellId = -1;
            m_pendingInteractiveSkillId = -1;
        }

        public override int Level
        {
            get
            {
                return DatabaseRecord.Level;
            }
            set
            {
                DatabaseRecord.Level = value;
            }
        }

        public int LifeBeforeFight
        {
            get;
            private set;
        }

        public override int Restriction
        {
            get
            {
                return DatabaseRecord.Restriction;
            }
            set
            {
                DatabaseRecord.Restriction = value;
            }
        }

        public override long Kamas
        {
            get
            {
                return DatabaseRecord.Kamas;
            }
            set
            {
                long delta = value - DatabaseRecord.Kamas;
                DatabaseRecord.Kamas = value;

                if (IsConnected && delta != 0)
                {
                    if (delta > 0)
                        Dispatch(WorldMessage.IM_INFO_MESSAGE(InformationEnum.INFO_KAMAS_WON, delta));
                    else
                        Dispatch(WorldMessage.IM_INFO_MESSAGE(InformationEnum.INFO_KAMAS_LOST, -delta));
                }

            }
        }

        public int CaractPoint
        {
            get
            {
                return DatabaseRecord.CaracPoint;
            }
            set
            {
                DatabaseRecord.CaracPoint = value;
            }
        }

        public int SpellPoint
        {
            get
            {
                return DatabaseRecord.SpellPoint;
            }
            set
            {
                DatabaseRecord.SpellPoint = value;
            }
        }

        public int EmoteCapacity
        {
            get
            {
                return DatabaseRecord.EmoteCapacity;
            }
            set
            {
                DatabaseRecord.EmoteCapacity = value;
            }
        }

        public long Experience
        {
            get
            {
                return DatabaseRecord.Experience;
            }
            set
            {
                DatabaseRecord.Experience = value;
            }
        }

        public long ExperienceFloorCurrent => ExperienceManager.Instance.GetFloor(Level, ExperienceTypeEnum.CHARACTER);

        public long ExperienceFloorNext
        {
            get
            {
                var next = ExperienceManager.Instance.GetFloor(Level + 1, ExperienceTypeEnum.CHARACTER);
                if (next == -1)
                {
                    return Experience;
                }

                return next;
            }
        }

        public override int AlignmentId => DatabaseRecord.AlignmentId;

        public int Honour
        {
            get
            {
                return DatabaseRecord.AlignmentHonour;
            }
            set
            {
                DatabaseRecord.AlignmentHonour = value;
            }
        }

        public int Dishonour
        {
            get
            {
                return DatabaseRecord.AlignmentDishonour;
            }
            set
            {
                DatabaseRecord.AlignmentDishonour = value;
            }
        }

        public int AlignmentLevel
        {
            get
            {
                return DatabaseRecord.AlignmentLevel;
            }
            set
            {
                DatabaseRecord.AlignmentLevel = value;
            }
        }

        public int AlignmentPromotion
        {
            get
            {
                return DatabaseRecord.AlignmentPromotion;
            }
            set
            {
                DatabaseRecord.AlignmentPromotion = value;
            }
        }

        public bool AlignmentEnabled
        {
            get
            {
                return DatabaseRecord.AlignmentEnabled;
            }
            set
            {
                DatabaseRecord.AlignmentEnabled = value;
            }
        }

        public long AlignmentExperienceFloorNext
        {
            get
            {
                var next = ExperienceManager.Instance.GetFloor(AlignmentLevel + 1, ExperienceTypeEnum.PVP);
                if (next == -1)
                {
                    return Honour;
                }

                return next;
            }
        }

        public long AlignmentExperienceFloorCurrent => ExperienceManager.Instance.GetFloor(AlignmentLevel, ExperienceTypeEnum.PVP);

        public override int RealLife
        {
            get
            {
                return DatabaseRecord.Life;
            }
            set
            {
                DatabaseRecord.Life = value;
            }
        }

        public int Energy
        {
            get
            {
                return DatabaseRecord.Energy;
            }

            set
            {
                int delta = value - DatabaseRecord.Energy;
                DatabaseRecord.Energy = value;

                if (IsConnected && delta != 0)
                {
                    if (delta > 0)
                        Dispatch(WorldMessage.IM_INFO_MESSAGE(InformationEnum.INFO_ENERGY_RECOVERED, delta));
                    else
                        Dispatch(WorldMessage.IM_INFO_MESSAGE(InformationEnum.INFO_ENERGY_LOST, -delta));
                }
            }
        }

        public override int BaseLife => 50 + (Level * 5);

        public int MaxPods => Statistics.GetTotal(EffectEnum.STAT_MAS_PODS) + Statistics.GetTotal(EffectEnum.STAT_MAS_FUERZA) * 5 + CharacterJobs.GetPodsBonus();
        public int CurrentPods => Inventory.Items.Sum(item => (item.Template?.Weight ?? 0) * item.Quantity);
        public string HexColor1 => DatabaseRecord.HexColor1;
        public string HexColor2 => DatabaseRecord.HexColor2;
        public string HexColor3 => DatabaseRecord.HexColor3;
        public override int SkinBase => DatabaseRecord.Skin;
        public override int SkinSizeBase => DatabaseRecord.SkinSize;
        public override bool CanDrop => true;
        public CharacterBreedEnum Breed => (CharacterBreedEnum)DatabaseRecord.Breed;
        public int BreedId => DatabaseRecord.Breed;

        public int Sex
        {
            get
            {
                return DatabaseRecord.Sex ? 1 : 0;
            }
            set
            {
                DatabaseRecord.Sex = value == 1 ? true : false;
            }
        }

        public bool Dead
        {
            get
            {
                return DatabaseRecord.Dead;
            }
            set
            {
                DatabaseRecord.Dead = value;
            }
        }

        public int DeathCount
        {
            get
            {
                return DatabaseRecord.DeathCount;
            }
            set
            {
                DatabaseRecord.DeathCount = value;
            }
        }

        public int MaxLevel
        {
            get
            {
                return DatabaseRecord.MaxLevel;
            }
            set
            {
                DatabaseRecord.MaxLevel = value;
            }
        }

        public CharacterDAO DatabaseRecord
        {
            get;
            private set;
        }

        public long AccountId => DatabaseRecord.AccountId;

        public GuildMember GuildMember
        {
            get;
            private set;
        }

        public HouseInstance CurrentHouse { get; set; }

        public JobBook CharacterJobs
        {
            get;
            private set;
        }

        public List<CharacterQuest> Quests
        {
            get;
            private set;
        }

        public int Aura
        {
            get
            {
                if (Level > 199)
                {
                    return 2;
                }

                if (Level > 100)
                {
                    return 1;
                }

                return 0;
            }
        }

        public override bool TurnReady
        {
            get;
            set;
        }

        public override bool TurnPass
        {
            get;
            set;
        }

        public long PartyId
        {
            get;
            set;
        }

        public long PartyInvitedPlayerId
        {
            get;
            set;
        }

        public long PartyInviterPlayerId
        {
            get;
            set;
        }

        public long GuildInvitedPlayerId
        {
            get;
            set;
        }

        public long GuildInviterPlayerId
        {
            get;
            set;
        }

        public AccountTicket Account
        {
            get;
            set;
        }

        public string Pseudo
        {
            get
            {
                if (Account == null)
                {
                    return "[No Account ?]";
                }

                return Account.Pseudo;
            }
        }

        public List<int> Waypoints
        {
            get;
            private set;
        }

        public PersistentInventory PersonalShop
        {
            get;
            private set;
        }

        public BankInventory Bank
        {
            get;
            private set;
        }

        public long MerchantTaxe
        {
            get;
            private set;
        }

        public bool Merchant
        {
            get
            {
                return DatabaseRecord.Merchant;
            }
            set
            {
                DatabaseRecord.Merchant = value;
            }
        }

        public int TitleId
        {
            get
            {
                return DatabaseRecord.TitleId;
            }
            set
            {
                DatabaseRecord.TitleId = value;
            }
        }

        public string TitleParams
        {
            get
            {
                return DatabaseRecord.TitleParams;
            }
            set
            {
                DatabaseRecord.TitleParams = value;
            }
        }

        public IEnumerable<SocialRelationDAO> Friends
        {
            get
            {
                return Relations.Where(relation => relation.Type == SocialRelationTypeEnum.TYPE_FRIEND);
            }
        }

        public IEnumerable<SocialRelationDAO> Ennemies
        {
            get
            {
                return Relations.Where(relation => relation.Type == SocialRelationTypeEnum.TYPE_ENNEMY);
            }
        }

        public List<SocialRelationDAO> Relations
        {
            get;
            private set;
        }

        public bool NotifyOnFriendConnection
        {
            get;
            set;
        }


        public bool IsGhost => SkinBase == 8004;

        public bool IsTombestone => SkinBase == (BreedId * 10) + 3;

        public DeathTypeEnum DeathType
        {
            get
            {
                return (DeathTypeEnum)DatabaseRecord.DeathType;
            }
            set
            {
                DatabaseRecord.DeathType = (int)value;
            }
        }

        public int EquippedMount
        {
            get
            {
                return DatabaseRecord.EquippedMount;
            }
            set
            {
                DatabaseRecord.EquippedMount = value;
            }
        }

        public bool Away
        {
            get;
            set;
        }

        public bool RidingMount
        {
            get;
            private set;
        }

        protected string m_guildDisplayInfos;
        protected long m_lastRegenTime;
        protected double m_regenTimer;
        protected int m_lastEmoteId;
        protected MountEntity m_mount;

        public CharacterEntity(AccountTicket account, CharacterDAO characterDAO, EntityTypeEnum type = EntityTypeEnum.TYPE_CHARACTER) : base(type, characterDAO.Id)
        {
            m_lastRegenTime = -1;
            m_lastEmoteId = -1;

            Away = false;
            DatabaseRecord = characterDAO;

            Account = account;
            PartyId = -1;
            PartyInvitedPlayerId = -1;
            PartyInviterPlayerId = -1;
            GuildInvitedPlayerId = -1;
            GuildInviterPlayerId = -1;
            NotifyOnFriendConnection = true;

            Quests = new List<CharacterQuest>(characterDAO.Quests.Select(record => new CharacterQuest(this, record)));

            CharacterJobs = new JobBook(this);
            Statistics = new GenericStats(characterDAO);
            SpellBook = SpellBookFactory.Instance.Create(this);
            Waypoints = characterDAO.GetWaypoints();
            FrameManager = new FrameManager<CharacterEntity, string>(this);
            Inventory = new CharacterInventory(this);
            Bank = BankManager.Instance.GetBankByAccountId(AccountId);
            PersonalShop = new PersistentInventory((int)EntityTypeEnum.TYPE_MERCHANT, Id);
            Relations = SocialRelationRepository.Instance.GetByAccountId(AccountId);

            var guildMember = GuildManager.Instance.GetMember(characterDAO.Guild.GuildId, Id);
            if (guildMember != null)
            {
                if (type == EntityTypeEnum.TYPE_CHARACTER)
                {
                    guildMember.CharacterConnected(this);
                }
                else
                {
                    SetCharacterGuild(guildMember);
                }
            }

            SetChatChannel(ChatChannelEnum.CHANNEL_GUILD, () => DispatchGuildMessage);
            SetChatChannel(ChatChannelEnum.CHANNEL_GROUP, () => DispatchPartyMessage);

            RefreshPersonalShopTaxe();
            CheckRestrictions();
            LoadEquippedMount();
        }

        private void LoadEquippedMount()
        {
            if (EquippedMount == -1)
            {
                return;
            }

            MountEntity mount = EntityManager.Instance.GetMountById(EquippedMount);
            if (mount != null)
            {
                if (mount.OwnerId == Id)
                {
                    m_mount = mount;
                }
                else
                {
                    Logger.Info($"CharacterEntity::() montura equipada sin pertenecer al personaje: {Name}");
                }
            }
            else
            {
                Logger.Info($"CharacterEntity::() montura equipada desconocida: {Name}");
                EquippedMount = -1;
            }
        }

        public void SendAccountStats()
        {
            Dispatch(WorldMessage.ACCOUNT_STATS(this));
        }

        public void CheckRestrictions()
        {
            if (IsTombestone)
            {
                SetPlayerRestriction(PlayerRestrictionEnum.RESTRICTION_CANT_EXCHANGE, true);
                SetPlayerRestriction(PlayerRestrictionEnum.RESTRICTION_CANT_USE_OBJECT, true);
                SetPlayerRestriction(PlayerRestrictionEnum.RESTRICTION_CANT_USE_IO, true);
                SetPlayerRestriction(PlayerRestrictionEnum.RESTRICTION_CANT_ASSAULT, true);
                SetPlayerRestriction(PlayerRestrictionEnum.RESTRICTION_CAN_ATTACK, false);
                SetPlayerRestriction(PlayerRestrictionEnum.RESTRICTION_CAN_ATTACK_DUNGEON_MONSTERS_WHEN_MUTANT, false);
                SetPlayerRestriction(PlayerRestrictionEnum.RESTRICTION_CAN_ATTACK_MONSTERS_ANYWHERE_WHEN_MUTANT, false);
                SetPlayerRestriction(PlayerRestrictionEnum.RESTRICTION_CANT_BE_MERCHANT, true);
                SetPlayerRestriction(PlayerRestrictionEnum.RESTRICTION_CANT_CHALLENGE, true);
                SetPlayerRestriction(PlayerRestrictionEnum.RESTRICTION_CANT_INTERACT_WITH_PRISM, true);
                SetPlayerRestriction(PlayerRestrictionEnum.RESTRICTION_CANT_INTERACT_WITH_TAX_COLLECTOR, true);
                SetPlayerRestriction(PlayerRestrictionEnum.RESTRICTION_CAN_MOVE_IN_ALL_DIRECTIONS, false);
                SetEntityRestriction(EntityRestrictionEnum.RESTRICTION_IS_TOMBESTONE, true);
                SafeDispatch(WorldMessage.GAME_MESSAGE(GamePopupTypeEnum.TYPE_INSTANT, GameMessageEnum.MESSAGE_TOMBESTONE));
            }
            else if (IsGhost)
            {
                SetEntityRestriction(EntityRestrictionEnum.RESTRICTION_IS_TOMBESTONE, false);
                SetEntityRestriction(EntityRestrictionEnum.RESTRICTION_SLOWED, true);
                SetEntityRestriction(EntityRestrictionEnum.RESTRICTION_FORCEWALK, true);
                SetPlayerRestriction(PlayerRestrictionEnum.RESTRICTION_CANT_USE_IO, false);
                SetPlayerRestriction(PlayerRestrictionEnum.RESTRICTION_CAN_MOVE_IN_ALL_DIRECTIONS, true);

                SafeDispatch(WorldMessage.GAME_MESSAGE(GamePopupTypeEnum.TYPE_INSTANT, GameMessageEnum.MESSAGE_TRANSFORMED_TO_GHOST_NEED_PHEONIX));
            }
            SafeDispatch(WorldMessage.ACCOUNT_RESTRICTIONS(Restriction));
        }

        public void Reborn()
        {
            DatabaseRecord.Skin = (BreedId * 10) + Sex;
            Energy = 1000;
            Restriction = (int)PlayerRestrictionEnum.RESTRICTION_NEW_CHARACTER;
            EntityRestriction = 0;
            RefreshOnMap();
            CachedBuffer = true;
            Dispatch(WorldMessage.ACCOUNT_RESTRICTIONS(Restriction));
            Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_JUST_REBORN));
            Dispatch(WorldMessage.ACCOUNT_STATS(this));
            CachedBuffer = false;
        }

        public void HardResetSpells()
        {
            SpellBook.Reset(Breed);
            for (var i = 1; i < Level; i++)
            {
                SpellBook.GenerateLevelUpSpell(Breed, i);
            }

            SpellPoint = Level - 1;
            CachedBuffer = true;
            SendAccountStats();
            Dispatch(WorldMessage.SPELLS_LIST(SpellBook));
            CachedBuffer = false;
        }

        public void FreeSoul()
        {
            switch (DeathType)
            {
                case DeathTypeEnum.TYPE_NORMAL:
                    DatabaseRecord.Skin = 8004;
                    CheckRestrictions();
                    if (!DisableAlignment())
                    {
                        RefreshOnMap();
                    }

                    break;

                case DeathTypeEnum.TYPE_HEROIC:
                    Dead = true;
                    DeathCount++;
                    if (Level > MaxLevel)
                    {
                        MaxLevel = Level;
                    }

                    Dispatch(WorldMessage.GAME_OVER());
                    break;
            }
        }

        public void OnLoseFight(DeathTypeEnum type)
        {
            DeathType = type;
            Life = 1;

            switch (type)
            {
                case DeathTypeEnum.TYPE_HEROIC:
                    Energy = 1;
                    LoseEnergy();
                    break;

                case DeathTypeEnum.TYPE_NORMAL:
                    LoseEnergy();
                    if (Energy > 0)
                    {

                        MapId = SavedMapId;
                        CellId = SavedCellId;
                    }
                    break;
            }
        }

        public void LoseEnergy()
        {
            var energyLost = Math.Min(Energy, Level * 10);
            if (energyLost < 1)
            {
                return;
            }

            Energy -= energyLost;

            if (Energy == 0)
            {
                DatabaseRecord.Skin = (BreedId * 10) + 3;
                CheckRestrictions();
            }
            else if (Energy < 1000)
            {
                Dispatch(WorldMessage.GAME_MESSAGE(GamePopupTypeEnum.TYPE_INSTANT, GameMessageEnum.MESSAGE_ENERGY_LOW, Energy));
            }
        }

        public override void JoinFight(AbstractFight fight, FightTeam team)
        {
            LifeBeforeFight = Life;
            Dispatch(WorldMessage.INTERACTIVE_DATA_FRAME_FIGHT(Map.InteractiveObjects));
            base.JoinFight(fight, team);
        }

        public virtual void JoinSpectator(AbstractFight fight)
        {
            Fight = fight;
            IsSpectating = true;

            Fight.SpectatorTeam.AddSpectator(this);
            Fight.SpectatorTeam.AddUpdatable(this);
            Fight.SpectatorTeam.AddHandler(Dispatch);

            SetChatChannel(ChatChannelEnum.CHANNEL_TEAM, () => Fight.SpectatorTeam.Dispatch);
            SetChatChannel(ChatChannelEnum.CHANNEL_GENERAL, () => null);

            StartAction(GameActionTypeEnum.FIGHT);
        }

        public override FightActionResultEnum EndTurn()
        {
            if (IsDisconnected)
            {
                DisconnectedTurnLeft--;
                if (DisconnectedTurnLeft <= 0)
                {
                    Fight.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_FIGHTER_KICKED_DUE_TO_DISCONNECTION, Name));
                    if (Fight.FightQuit(this) == FightActionResultEnum.RESULT_END)
                    {
                        return FightActionResultEnum.RESULT_END;
                    }
                }
                else
                {
                    Fight.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_FIGHT_DISCONNECT_TURN_REMAIN, Name, DisconnectedTurnLeft));
                }
            }
            return base.EndTurn();
        }

        public override void EndFight(bool win = false)
        {
            if (!IsSpectating)
            {
                if (IsFighterDead)
                {
                    switch (Fight.Type)
                    {
                        case FightTypeEnum.TYPE_AGGRESSION:
                        case FightTypeEnum.TYPE_PVM:
                        case FightTypeEnum.TYPE_PVT:
                        case FightTypeEnum.TYPE_PVMA:
                            Life = 1;
                            break;
                    }
                }

                if (!win)
                {
                    switch (Fight.Type)
                    {
                        case FightTypeEnum.TYPE_PVM:
                        case FightTypeEnum.TYPE_AGGRESSION:
                        case FightTypeEnum.TYPE_PVT:
                        case FightTypeEnum.TYPE_PVMA:
                            OnLoseFight(DeathTypeEnum.TYPE_NORMAL);
                            break;




                    }
                }

                switch (Fight.Type)
                {
                    case FightTypeEnum.TYPE_CHALLENGE:
                        Life = LifeBeforeFight;
                        break;

                    default:
                        CachedBuffer = true;
                        var changedItems = new List<ItemDAO>();
                        var items = Inventory.Items.FindAll(item => item.IsBoostEquiped);
                        foreach (var item in items)
                        {
                            if (item.Statistics.HasEffect(EffectEnum.BOOST_MAS))
                            {
                                var effect = item.Statistics.GetEffect(EffectEnum.BOOST_MAS);
                                effect.Value3--;
                                item.SaveStats();
                                changedItems.Add(item);
                                if (effect.Value3 <= 0)
                                {
                                    Inventory.RemoveItem(item.Id);
                                }
                            }
                        }

                        var etherealWeapon = Inventory.Items.Find(item => item.Slot == ItemSlotEnum.SLOT_WEAPON && item.IsEthereal && item.MaxDurability > 0 && item.Durability > 0);

                        if (etherealWeapon != null)
                        {
                            etherealWeapon.DecreaseDurability();

                            if (etherealWeapon.Durability <= 0)
                            {
                                Inventory.RemoveItem(etherealWeapon.Id);
                            }
                            else
                            {
                                changedItems.Add(etherealWeapon);
                            }
                        }

                        if (changedItems.Count > 0)
                        {
                            Dispatch(WorldMessage.OBJECT_CHANGE(changedItems));
                            SendAccountStats();
                        }

                        CachedBuffer = false;
                        break;
                }

            }
            else
            {
                Fight.SpectatorTeam.RemoveSpectator(this);
                Fight.SpectatorTeam.RemoveUpdatable(this);
                Fight.SpectatorTeam.RemoveHandler(Dispatch);
            }

            if (IsDisconnected)
            {
                EntityManager.Instance.RemoveCharacter(this);
            }

            var fightType = Fight.Type;
            AddMessage(() => Map.FightManager.ExecuteFightActions(fightType, FightStateEnum.STATE_ENDED, this));

            base.EndFight(win);
        }

        public override void EmoteUse(int emoteId, int timeout = 360000)
        {
            if (IsTombestone || IsGhost)
            {
                return;
            }

            timeout = emoteId == m_lastEmoteId ? 0 : timeout;
            m_lastEmoteId = emoteId == m_lastEmoteId ? 0 : emoteId;

            // Sentarse (emote 1) acelera la regeneración; cualquier otro estado vuelve al
            // ritmo de pie. StartRegeneration liquida el progreso del ritmo anterior.
            if (m_lastEmoteId == 1)
            {
                StartRegeneration(SITTING_REGEN_RATE);
            }
            else
            {
                StartStandingRegeneration();
            }

            base.EmoteUse(m_lastEmoteId, timeout);
        }

        public void StopEmote()
        {
            var wasSitting = m_lastEmoteId == 1;
            m_lastEmoteId = 0;

            // Al levantarse, liquidar lo regenerado sentado y volver al ritmo de pie.
            if (wasSitting)
            {
                StartStandingRegeneration();
            }
        }

        // Ritmo de regeneración natural de vida, en milisegundos por punto de vida.
        // De pie es lento; sentado (emote 1) es mucho más rápido. Ajustables.
        public const double STANDING_REGEN_RATE = 2000.0;
        public const double SITTING_REGEN_RATE = 300.0;

        /// <summary>
        /// Inicia la regeneración natural de pie (si el personaje está vivo, en mapa y fuera
        /// de combate). Es el ritmo lento; sentarse lo acelera.
        /// </summary>
        public void StartStandingRegeneration()
        {
            if (IsGhost || IsTombestone || HasGameAction(GameActionTypeEnum.FIGHT) || !HasGameAction(GameActionTypeEnum.MAP))
            {
                return;
            }

            StartRegeneration(STANDING_REGEN_RATE);
        }

        public void StartRegeneration(double timer)
        {
            // Liquidar primero lo regenerado por el ciclo anterior (de pie ↔ sentado, cambio
            // de mapa, etc.) para no perder progreso al cambiar de ritmo.
            StopRegeneration();

            if (Life >= MaxLife)
            {
                return;
            }

            m_regenTimer = timer;
            m_lastRegenTime = Environment.TickCount64; // reloj real, no depende del tick del juego
            Dispatch(WorldMessage.LIFE_RESTORE_TIME_START(timer));
        }

        public void StopRegeneration()
        {
            if (m_lastRegenTime == -1)
            {
                return;
            }

            var elapsedMs = Environment.TickCount64 - m_lastRegenTime;
            m_lastRegenTime = -1;

            var lifeRestored = m_regenTimer > 0 ? (int)Math.Floor(elapsedMs / m_regenTimer) : 0;
            if (lifeRestored > 0 && Life + lifeRestored > MaxLife)
            {
                lifeRestored = MaxLife - Life;
            }
            if (lifeRestored < 0)
            {
                lifeRestored = 0;
            }

            CachedBuffer = true;
            if (lifeRestored > 0)
            {
                Life += lifeRestored;
                Dispatch(WorldMessage.ACCOUNT_STATS(this));
            }
            // Siempre avisar al cliente (ILF) para detener su animación de regeneración.
            Dispatch(WorldMessage.LIFE_RESTORE_TIME_FINISH(lifeRestored));
            CachedBuffer = false;
        }

        public void ChangeDishonour(int delta)
        {
            if (delta == 0)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (delta < 0 && Dishonour < 1)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            Dishonour += delta;

            if (Dishonour < 0)
            {
                Dishonour = 0;
            }

            if (Dishonour > 500)
            {
                Dishonour = 500;
            }

            CachedBuffer = true;
            var info = delta > 0 ? InformationEnum.INFO_ALIGNMENT_DISHONOR_UP : InformationEnum.INFO_ALIGNMENT_DISHONOR_DOWN;
            Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, info, Math.Abs(delta)));
            Dispatch(WorldMessage.ACCOUNT_STATS(this));
            CachedBuffer = false;
        }

        public void ChangeHonour(int delta)
        {
            if (delta == 0)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            bool isGain = delta > 0;

            if (isGain && Dishonour > 0)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (delta < 0 && Honour < 1)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            int currentLevel = AlignmentLevel;
            int rankDirection = isGain ? 1 : -1;

            long maxHonour = ExperienceTemplateRepository.Instance.GetMaxPvpExperience();
            if (maxHonour <= 0)
                maxHonour = 18000;

            delta = (int)Math.Clamp(delta, -maxHonour, maxHonour);
            Honour = (int)Math.Clamp((long)Honour + delta, 0, maxHonour);

            while ((isGain && AlignmentLevel < 10 && Honour >= AlignmentExperienceFloorNext) || (!isGain && AlignmentLevel > 1 && Honour < AlignmentExperienceFloorCurrent))
                AlignmentLevel += rankDirection;

            CachedBuffer = true;

            if (currentLevel != AlignmentLevel)
            {
                var rankInfo = isGain ? InformationEnum.INFO_ALIGNMENT_RANK_UP : InformationEnum.INFO_ALIGNMENT_RANK_DOWN;
                Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, rankInfo, AlignmentLevel));
                if (!HasGameAction(GameActionTypeEnum.FIGHT))
                {
                    RefreshOnMap();
                }
            }

            InformationEnum honorInfo = isGain ? InformationEnum.INFO_ALIGNMENT_HONOR_UP : InformationEnum.INFO_ALIGNMENT_HONOR_DOWN;
            Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, honorInfo, delta));
            Dispatch(WorldMessage.ACCOUNT_STATS(this));
            CachedBuffer = false;
        }

        public void EnableAlignment()
        {
            if (AlignmentEnabled)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (AlignmentId == 0)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            AlignmentEnabled = true;
            Dispatch(WorldMessage.ACCOUNT_STATS(this));
            if (!HasGameAction(GameActionTypeEnum.FIGHT))
            {
                RefreshOnMap();
            }
        }

        public bool DisableAlignment(bool force = false)
        {
            if (!AlignmentEnabled)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return false;
            }

            if (Dishonour > 0)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return false;
            }

            AlignmentEnabled = false;

            if (!force)
            {
                ChangeHonour(-((Honour / 100) * 5));
            }

            Dispatch(WorldMessage.ACCOUNT_STATS(this));
            if (!HasGameAction(GameActionTypeEnum.FIGHT))
            {
                RefreshOnMap();
            }
            return true;
        }

        public void SetAlignment(int alignmentId)
        {
            ResetAlignment(alignmentId);
        }

        public void ResetAlignment(int alignmentId = 0)
        {
            DatabaseRecord.AlignmentId = alignmentId;
            AlignmentLevel = 1;
            AlignmentPromotion = 0;
            Honour = 0;
            Dishonour = 0;
            AlignmentEnabled = false;

            Dispatch(WorldMessage.ACCOUNT_STATS(this));
            RefreshOnMap();
        }

        public void RefreshPersonalShopTaxe()
        {
            foreach (var item in PersonalShop.Items)
            {
                MerchantTaxe += item.MerchantPrice * item.Quantity;
            }

            MerchantTaxe /= 1000;
        }

        public void ServerKick(string reason = "")
        {
            SafeKick("[Server]", reason);
        }

        public void SafeKick(string kicker = "", string reason = "")
        {
            AddMessage(() =>
                {
                    if (reason != "")
                    {
                        Dispatch(WorldMessage.GAME_MESSAGE(GamePopupTypeEnum.TYPE_ON_DISCONNECT, GameMessageEnum.MESSAGE_KICKED, kicker, reason));
                    }

                    KickEvent?.Invoke();
                });
        }

        public void RecoverOfflineEnergy()
        {
            if (DatabaseRecord.DisconnectedAt == default)
                return;

            double hoursOffline = (DateTime.Now - DatabaseRecord.DisconnectedAt).TotalHours;
            if (hoursOffline <= 0)
                return;

            bool isEnhanced = DatabaseRecord.Merchant || WorldConfig.MAPAS_TABERNA.Contains(DatabaseRecord.MapId) || Manager.HouseManager.Instance.GetByInsideMapId(DatabaseRecord.MapId) != null;
            double ratePerHour = isEnhanced ? 100.0 : 50.0;
            int recovered = (int)(hoursOffline * ratePerHour);

            if (recovered > 0)
                Energy = Math.Min(10000, Energy + recovered);

            DatabaseRecord.DisconnectedAt = default;
        }

        public bool Disconnected()
        {
            DatabaseRecord.DisconnectedAt = DateTime.Now;

            if (HasGameAction(GameActionTypeEnum.FIGHT))
            {
                if (IsSpectating)
                {
                    Fight.FightQuit(this);
                }
                else
                {
                    if (CurrentAction != null)
                    {
                        AbortAction(CurrentAction.Type);
                    }

                    AbortAction(GameActionTypeEnum.FIGHT);
                    return false;
                }
            }
            StopRegeneration();
            if (CurrentAction != null)
            {
                AbortAction(CurrentAction.Type, Id);
            }

            if (HasGameAction(GameActionTypeEnum.MAP))
            {
                AbortAction(GameActionTypeEnum.MAP);
            }

            GuildMember?.CharacterDisconnected();

            Dispose();
            if (Merchant)
            {
                WorldService.Instance.AddMessage(() => { var merchant = EntityManager.Instance.CreateMerchant(DatabaseRecord); merchant.StartAction(GameActionTypeEnum.MAP); });
            }
            return true;
        }

        public bool HasSkill(SkillIdEnum id)
        {
            return HasSkill((int)id);
        }

        public bool HasSkill(int id)
        {
            return CharacterJobs.HasSkill(id);
        }

        public void SetCharacterGuild(GuildMember characterGuild)
        {
            GuildMember = characterGuild;
            if (GuildMember != null)
            {
                m_guildDisplayInfos = GuildMember.Guild.Name + ";" + GuildMember.Guild.DisplayEmblem;
            }
            else
            {
                m_guildDisplayInfos = null;
            }
        }

        public void SetAway()
        {
            Away = Away == false;
            if (Away)
            {
                Dispatch(WorldMessage.IM_INFO_MESSAGE(InformationEnum.INFO_YOU_ARE_AWAY));
            }
            else
            {
                Dispatch(WorldMessage.IM_INFO_MESSAGE(InformationEnum.INFO_YOU_ARE_NOT_AWAY_ANYMORE));
            }
        }

        public override bool DispatchChatMessage(ChatChannelEnum channel, string message, CharacterEntity whispedCharacter = null)
        {
            if (channel == ChatChannelEnum.CHANNEL_PRIVATE_SEND)
            {
                if (whispedCharacter.Away || whispedCharacter.HasEnnemy(Pseudo))
                {
                    Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_PLAYER_AWAY_MESSAGE, whispedCharacter.Name));
                    return false;
                }
                if (Away)
                {
                    Dispatch(WorldMessage.IM_INFO_MESSAGE(InformationEnum.INFO_YOU_ARE_AWAY_PLAYERS_CANT_RESPOND));
                }
            }
            return base.DispatchChatMessage(channel, message, whispedCharacter);
        }

        public bool HasEnnemy(string pseudo)
        {
            return Ennemies.Any(ennemy => ennemy.Pseudo.Equals(pseudo, StringComparison.OrdinalIgnoreCase));
        }

        public bool HasFriend(string pseudo)
        {
            return Friends.Any(friend => friend.Pseudo.Equals(pseudo, StringComparison.OrdinalIgnoreCase));
        }

        public void DispatchPartyMessage(string message)
        {
            PartyManager.Instance.PartyMessage(PartyId, message);
        }

        public void DispatchGuildMessage(string message)
        {
            GuildMember?.Guild.SafeDispatch(message);
        }

        public void MountRideUnride()
        {
            if (m_mount != null)
            {
                if (!RidingMount)
                {
                    if (Level < 60)
                    {
                        Dispatch(WorldMessage.MOUNT_EQUIP_ERROR(MountEquipErrorEnum.UNKNOW_ERROR));
                        return;
                    }
                    if (!m_mount.Ridable)
                    {
                        Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_MOUNT_MATURITY_LOW));
                        return;
                    }
                    if (Inventory.Items.Any(item => item.Slot == ItemSlotEnum.SLOT_PET))
                    {
                        Dispatch(WorldMessage.IM_ERROR_MESSAGE(InformationEnum.ERROR_PET_ALREADY_EQUIPPED));
                        return;
                    }
                    RidingMount = true;
                    Dispatch(WorldMessage.MOUNT_RIDING_START());
                    Statistics.Merge(StatsType.TYPE_ITEM, m_mount.GetStatistics());
                }
                else
                {
                    RidingMount = false;
                    Dispatch(WorldMessage.MOUNT_RIDING_STOP());
                    Statistics.UnMerge(StatsType.TYPE_ITEM, m_mount.GetStatistics());
                }
                CachedBuffer = true;
                SendAccountStats();
                RefreshOnMap();
                CachedBuffer = false;
            }
        }

        public void SendQuestsList()
        {
            Dispatch(WorldMessage.QUEST_LIST(Quests));
        }

        public void SendQuestsStepsList(int questId)
        {
            var quest = Quests.FirstOrDefault(q => q.Id == questId);
            if (quest != null)
            {
                Dispatch(WorldMessage.QUEST_STEPS(quest));
            }
        }

        public void SendMountEquipped()
        {
            if (m_mount != null)
            {
                Dispatch(WorldMessage.MOUNT_EQUIP(m_mount.SerializeAs_MountInfos()));
            }
        }

        public void SendMountXpShare()
        {
            if (m_mount != null)
            {
                Dispatch(WorldMessage.MOUNT_EXPERIENCE_SHARED(m_mount.XPSharePercent));
            }
        }

        public void RenameMount(string name)
        {
            if (m_mount == null)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            name = name?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 25)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            m_mount.SetName(name);
            Dispatch(WorldMessage.MOUNT_NAME(m_mount.Name));
        }

        public void SetMountXpShare(int percent)
        {
            if (m_mount == null)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            // El cliente solo permite repartir entre 0 y 90 % de la experiencia con la montura.
            percent = Math.Clamp(percent, 0, 90);

            m_mount.XPSharePercent = percent;
            Dispatch(WorldMessage.MOUNT_EXPERIENCE_SHARED(m_mount.XPSharePercent));
        }

        public void SendMountData(long mountId)
        {
            // La montura equipada esta disponible directamente; si no, se busca por id.
            var mount = (m_mount != null && m_mount.UniqueId == mountId)
                ? m_mount
                : EntityManager.Instance.GetMountById(mountId);

            // Solo se entregan los datos de monturas que pertenecen al jugador.
            if (mount == null || mount.OwnerId != Id)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            Dispatch(WorldMessage.MOUNT_DATA(mount.SerializeAs_MountInfos()));
        }

        // "Rf" : liberar la montura equipada (vuelve a estado salvaje y deja de pertenecer al jugador).
        public void FreeMount()
        {
            if (m_mount == null)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            CachedBuffer = true;
            if (RidingMount)
            {
                RidingMount = false;
                Statistics.UnMerge(StatsType.TYPE_ITEM, m_mount.GetStatistics());
                Dispatch(WorldMessage.MOUNT_RIDING_STOP());
            }

            m_mount.SetWild(true);
            m_mount.SetOwner(-1);
            m_mount = null;
            EquippedMount = -1;

            Dispatch(WorldMessage.MOUNT_UNEQUIP());
            SendAccountStats();
            RefreshOnMap();
            CachedBuffer = false;
        }

        // "Rc" : castrar la montura equipada (deja de poder reproducirse).
        public void CastrateMount()
        {
            if (m_mount == null || m_mount.Castrated)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            m_mount.SetCastrated();
            SendMountEquipped();
        }

        // "Rp" : pedir la informacion del enclos del mapa actual.
        public void SendCurrentPaddockInformations()
        {
            if (Map?.Paddock == null)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            Map.SendPaddockInformations(this);
        }

        // "Rv" : cerrar el dialogo de compra/venta del enclos.
        public void PaddockLeave()
        {
            Dispatch(WorldMessage.PADDOCK_BUY_LEAVE());
        }

        // "Rs<precio>" : el propietario fija el precio de venta del enclos (0 = retirar de la venta).
        public void PaddockSetPrice(long price)
        {
            var paddock = Map?.Paddock;
            if (paddock == null)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var member = GuildMember;
            if (member == null || member.Guild.Id != paddock.GuildId
                || !(member.HasRight(GuildRightEnum.ARRANGE_MOUNTPARK) || member.HasRight(GuildRightEnum.BOSS)))
            {
                if (member == null)
                    Dispatch(WorldMessage.BASIC_NO_OPERATION());
                else
                    member.SendHasNotEnoughRights();
                return;
            }

            paddock.SetForSale(price);
            Map.SendPaddockInformations(this);
        }

        // "Rb<precio>" : comprar el enclos (transfiere la propiedad al gremio del comprador).
        public void PaddockBuy(long price)
        {
            var paddock = Map?.Paddock;
            if (paddock == null)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var member = GuildMember;
            if (member == null || !member.HasRight(GuildRightEnum.BOSS))
            {
                if (member == null)
                    Dispatch(WorldMessage.BASIC_NO_OPERATION());
                else
                    member.SendHasNotEnoughRights();
                return;
            }

            // No tiene sentido comprar un enclos que ya pertenece al gremio del comprador.
            if (paddock.GuildId == member.Guild.Id)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            // Enclos disponible (sin gremio) se compra al precio base; en venta, al precio fijado.
            var cost = paddock.OnSale ? paddock.DefaultPrice : paddock.Price;
            if (cost <= 0 || Inventory.Kamas < cost)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            CachedBuffer = true;
            Inventory.SubKamas(cost);
            paddock.TransferTo((int)member.Guild.Id);
            Dispatch(WorldMessage.PADDOCK_BUY_LEAVE());
            Map.SendPaddockInformations(this);
            CachedBuffer = false;
        }

        // "Ro<celda>" : retirar del enclos la montura colocada en la celda indicada.
        public void PaddockRemoveObject(int cellId)
        {
            var paddock = Map?.Paddock;
            if (paddock == null)
            {
                Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var member = GuildMember;
            if (member == null || member.Guild.Id != paddock.GuildId
                || !(member.HasRight(GuildRightEnum.MANAGE_OTHERS_MOUNT)
                     || member.HasRight(GuildRightEnum.ARRANGE_MOUNTPARK)
                     || member.HasRight(GuildRightEnum.BOSS)))
            {
                if (member == null)
                    Dispatch(WorldMessage.BASIC_NO_OPERATION());
                else
                    member.SendHasNotEnoughRights();
                return;
            }

            // La colocacion de monturas dentro del enclos sobre el mapa aun no esta implementada,
            // por lo que no hay ningun objeto que retirar todavia.
            Dispatch(WorldMessage.BASIC_NO_OPERATION());
        }

        public void GuildCreationOpen()
        {
            CurrentAction = new GameGuildCreationAction(this);
            StartAction(GameActionTypeEnum.GUILD_CREATE);
        }

        public void ExchangePaddock(Paddock paddock)
        {
            CurrentAction = new GameMountStorageExchangeAction(this, paddock);
            StartAction(GameActionTypeEnum.EXCHANGE);
        }

        public void WaypointStart(Waypoint waypoint)
        {
            CurrentAction = new GameWaypointAction(this, waypoint);
            StartAction(GameActionTypeEnum.WAYPOINT);
        }

        public void PrismSubwayStart(Game.Conquest.ConquestTerritory territory)
        {
            CurrentAction = new GamePrismSubwayAction(this, territory);
            StartAction(GameActionTypeEnum.PRISM_USE);
        }

        public bool AddWaypoint(int mapId)
        {
            if (Waypoints.Contains(mapId))
            {
                return false;
            }

            Waypoints.Add(mapId);
            DatabaseRecord.SetWaypoints(Waypoints);
            return true;
        }

        public void NpcDialogStart(NonPlayerCharacterEntity npc)
        {
            CurrentAction = new GameNpcDialogAction(this, npc);
            StartAction(GameActionTypeEnum.NPC_DIALOG);
        }

        public void CloseCurrentInteraction()
        {
            if (CurrentAction == null || CurrentAction.IsFinished)
            {
                return;
            }

            switch (CurrentAction.Type)
            {
                case GameActionTypeEnum.NPC_DIALOG:
                case GameActionTypeEnum.WAYPOINT:
                case GameActionTypeEnum.EXCHANGE:
                case GameActionTypeEnum.PRISM_USE:
                case GameActionTypeEnum.MAP_MOVEMENT:
                    AbortAction(CurrentAction.Type);
                    break;
            }
        }

        public void HarvestStart(HarvestableResource resource, int duration)
        {
            CurrentAction = new GameHarvestAction(this, resource, duration);
            StartAction(GameActionTypeEnum.SKILL_HARVEST);
        }

        public void CraftStart(CraftPlan plan, JobSkill skill)
        {


            if (skill is Game.Job.Skill.MagicSkill)
                CurrentAction = new AccionIntercambioForjamagia(this, plan, skill);
            else
                CurrentAction = new GameCraftPlanExchangeAction(this, plan, skill);

            StartAction(GameActionTypeEnum.EXCHANGE);
        }

        public void ExchangeNpc(NonPlayerCharacterEntity npc)
        {
            CurrentAction = new GameNpcExchangeAction(this, npc);
            StartAction(GameActionTypeEnum.EXCHANGE);
        }

        public void ExchangeTaxCollector(TaxCollectorEntity taxCollector)
        {
            CurrentAction = new GameTaxCollectorExchangeAction(this, taxCollector);
            taxCollector.CurrentAction = CurrentAction;

            StartAction(GameActionTypeEnum.EXCHANGE);
        }

        public void ExchangeMerchant(MerchantEntity merchant)
        {
            CurrentAction = new GameMerchantExchangeAction(this, merchant);
            StartAction(GameActionTypeEnum.EXCHANGE);
        }

        public void ExchangePersonalShop()
        {
            CurrentAction = new GamePersonalShopExchangeAction(this);
            StartAction(GameActionTypeEnum.EXCHANGE);
        }

        public void ExchangePlayer(CharacterEntity player)
        {
            CurrentAction = new GamePlayerExchangeAction(this, player);
            player.CurrentAction = CurrentAction;
        }

        /// <summary>
        /// Inicia un craft seguro. <c>this</c> es el iniciador (envía la petición); <paramref
        /// name="invited"/> es quien debe aceptar. Los roles artesano/cliente son independientes
        /// de quién inició (un cliente puede pedir a un artesano y viceversa).
        /// </summary>
        public void RequestCraftSecure(CharacterEntity invited, CharacterEntity artisan, CharacterEntity client, Game.Job.JobSkill skill, int requestType)
        {
            CurrentAction = new GameCraftSecureExchangeAction(this, invited, artisan, client, skill, requestType);
            invited.CurrentAction = CurrentAction;
        }

        public void ChallengePlayer(CharacterEntity player)
        {
            CurrentAction = new GameChallengeRequestAction(this, player);
            player.CurrentAction = CurrentAction;

            StartAction(GameActionTypeEnum.CHALLENGE_REQUEST);
            player.StartAction(GameActionTypeEnum.CHALLENGE_REQUEST);
        }

        public void ExchangeShop(NonPlayerCharacterEntity entity)
        {
            CurrentAction = new GameShopExchangeAction(this, entity);
            StartAction(GameActionTypeEnum.EXCHANGE);
        }

        public void ExchangeStorage(StorageInventory storage)
        {
            CurrentAction = new GameStorageExchangeAction(this, storage);
            StartAction(GameActionTypeEnum.EXCHANGE);
        }

        public void ExchangeHouseChest(HouseChestInventory chest)
        {
            CurrentAction = new GameStorageExchangeAction(this, chest);
            StartAction(GameActionTypeEnum.EXCHANGE);
        }

        public void ExchangeAuctionHouseBuy(NonPlayerCharacterEntity entity)
        {
            CurrentAction = new GameAuctionHouseBuyAction(this, entity);
            StartAction(GameActionTypeEnum.EXCHANGE);
        }

        public void ExchangeAuctionHouseSell(NonPlayerCharacterEntity entity)
        {
            CurrentAction = new GameAuctionHouseSellAction(this, entity);
            StartAction(GameActionTypeEnum.EXCHANGE);
        }

        public void DefendTaxCollector()
        {
            CurrentAction = new GameTaxCollectorDefenderAction(this);
            StartAction(GameActionTypeEnum.TAXCOLLECTOR_AGGRESSION);
        }

        public void DefendConquest(ConquestFight fight)
        {
            CurrentAction = new GameConquestDefenderAction(this, fight);
            StartAction(GameActionTypeEnum.PRISM_AGGRESSION);
        }

        // Reparte la experiencia de combate entre el personaje y, si lleva una montura equipada
        // con porcentaje de reparto, la propia montura. Devuelve la parte que recibio la montura
        // (para mostrarla en el panel de fin de combate). El personaje recibe el resto.
        public long AddFightExperience(long experience)
        {
            if (experience <= 0)
                return 0;

            long mountExperience = 0;

            // La montura equipada se queda con su porcentaje, salvo que ya este al nivel maximo.
            if (m_mount != null && m_mount.XPSharePercent > 0 && !m_mount.IsMaxLevel)
            {
                mountExperience = experience * m_mount.XPSharePercent / 100;
                m_mount.AddExperience(mountExperience);
            }

            AddExperience(experience - mountExperience);
            return mountExperience;
        }

        public void AddExperience(long experience)
        {
            Experience += experience;

            var currentLevel = Level;
            while (Experience > ExperienceFloorNext)
            {
                Level++;
                SpellPoint++;
                CaractPoint += 5;
                Life = MaxLife;

                if (Level == 100)
                    Statistics.AddBase(EffectEnum.STAT_MAS_PA, 1);

                SpellBook?.GenerateLevelUpSpell(Breed, Level);
            }

            if (Level != currentLevel && IsConnected)
            {
                CachedBuffer = true;
                Dispatch(WorldMessage.CHARACTER_NEW_LEVEL(Level));
                Dispatch(WorldMessage.SPELLS_LIST(SpellBook));
                Dispatch(WorldMessage.ACCOUNT_STATS(this));
                CachedBuffer = false;
            }
        }

        public override bool CanBeExchanged(ExchangeTypeEnum exchangeType)
        {
            return base.CanBeExchanged(exchangeType) && (exchangeType == ExchangeTypeEnum.EXCHANGE_PLAYER
                || exchangeType == ExchangeTypeEnum.EXCHANGE_PERSONAL_SHOP_EDIT
                || exchangeType == ExchangeTypeEnum.EXCHANGE_CRAFT_SECURE_ARTISAN
                || exchangeType == ExchangeTypeEnum.EXCHANGE_CRAFT_SECURE_CLIENT);
        }

        public void RefreshOnMap()
        {
            var message = WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_REFRESH, this);
            if (HasGameAction(GameActionTypeEnum.MAP))
            {
                Map.SafeDispatch(message);
            }
            else
            {
                Fight?.SafeDispatch(message);
            }
        }

        public override void StartAction(GameActionTypeEnum actionType)
        {
            base.StartAction(actionType);

            switch (actionType)
            {
                case GameActionTypeEnum.MAP_MOVEMENT:
                    StopEmote();
                    break;

                case GameActionTypeEnum.MAP_TELEPORT:
                    StopEmote();

                    break;

                case GameActionTypeEnum.MAP:
                    if (Map == null)
                    {
                        MapId = SavedMapId;
                        CellId = SavedCellId;
                    }
                    if (HasEntityRestriction(EntityRestrictionEnum.RESTRICTION_IS_TOMBESTONE))
                    {
                        FrameManager.AddFrame(GameTombestoneFrame.Instance);
                    }

                    FrameManager.AddFrame(MapFrame.Instance);
                    FrameManager.AddFrame(InventoryFrame.Instance);
                    FrameManager.AddFrame(ExchangeFrame.Instance);
                    FrameManager.AddFrame(HouseFrame.Instance);
                    FrameManager.AddFrame(GameActionFrame.Instance);
                    FrameManager.AddFrame(Frame.ConquestFrame.Instance);
                    // Al pisar el mapa (login, cambio de mapa, fin de combate) arranca la
                    // regeneración natural de pie.
                    StartStandingRegeneration();
                    break;

                case GameActionTypeEnum.WAYPOINT:
                    FrameManager.RemoveFrame(GameActionFrame.Instance);
                    FrameManager.RemoveFrame(InventoryFrame.Instance);
                    FrameManager.RemoveFrame(MapFrame.Instance);
                    FrameManager.AddFrame(WaypointFrame.Instance);
                    break;

                case GameActionTypeEnum.PRISM_USE:
                    FrameManager.RemoveFrame(GameActionFrame.Instance);
                    FrameManager.RemoveFrame(InventoryFrame.Instance);
                    FrameManager.RemoveFrame(MapFrame.Instance);
                    FrameManager.AddFrame(PrismSubwayFrame.Instance);
                    break;

                case GameActionTypeEnum.NPC_DIALOG:
                    FrameManager.RemoveFrame(GameActionFrame.Instance);
                    FrameManager.RemoveFrame(InventoryFrame.Instance);
                    FrameManager.RemoveFrame(MapFrame.Instance);
                    FrameManager.AddFrame(NpcDialogFrame.Instance);
                    break;

                case GameActionTypeEnum.PRISM_AGGRESSION:
                case GameActionTypeEnum.TAXCOLLECTOR_AGGRESSION:
                case GameActionTypeEnum.GUILD_CREATE:
                case GameActionTypeEnum.EXCHANGE:
                    FrameManager.RemoveFrame(GameActionFrame.Instance);
                    FrameManager.RemoveFrame(InventoryFrame.Instance);
                    FrameManager.RemoveFrame(MapFrame.Instance);
                    break;

                case GameActionTypeEnum.FIGHT:
                    // Sin regeneración en combate: liquidar lo regenerado y detenerla.
                    m_lastEmoteId = 0;
                    StopRegeneration();
                    FrameManager.RemoveFrame(MapFrame.Instance);
                    if (IsSpectating)
                    {
                        FrameManager.AddFrame(FightFrame.Instance);
                    }
                    else
                    {
                        if (Fight.Map.Id != MapId)
                        {
                            Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.MAP_TELEPORT, Id));
                            Dispatch(WorldMessage.GAME_DATA_MAP(Fight.Map.Id, Fight.Map.CreateTime, Fight.Map.DataKey));
                            FrameManager.AddFrame(GameInformationFrame.Instance);
                        }
                        FrameManager.AddFrame(FightPlacementFrame.Instance);
                    }
                    break;
            }
        }

        public override void AbortAction(GameActionTypeEnum actionType, params object[] args)
        {
            base.AbortAction(actionType, args);

            switch (actionType)
            {
                case GameActionTypeEnum.MAP:
                    FrameManager.RemoveFrame(MapFrame.Instance);
                    FrameManager.RemoveFrame(GameActionFrame.Instance);
                    FrameManager.RemoveFrame(ExchangeFrame.Instance);
                    break;

                case GameActionTypeEnum.WAYPOINT:
                    FrameManager.AddFrame(GameActionFrame.Instance);
                    FrameManager.AddFrame(InventoryFrame.Instance);
                    FrameManager.AddFrame(MapFrame.Instance);
                    FrameManager.RemoveFrame(WaypointFrame.Instance);
                    break;

                case GameActionTypeEnum.PRISM_USE:
                    FrameManager.AddFrame(GameActionFrame.Instance);
                    FrameManager.AddFrame(InventoryFrame.Instance);
                    FrameManager.AddFrame(MapFrame.Instance);
                    FrameManager.RemoveFrame(PrismSubwayFrame.Instance);
                    break;

                case GameActionTypeEnum.NPC_DIALOG:
                    FrameManager.AddFrame(GameActionFrame.Instance);
                    FrameManager.AddFrame(InventoryFrame.Instance);
                    FrameManager.AddFrame(MapFrame.Instance);
                    FrameManager.RemoveFrame(NpcDialogFrame.Instance);
                    break;

                case GameActionTypeEnum.PRISM_AGGRESSION:
                case GameActionTypeEnum.TAXCOLLECTOR_AGGRESSION:
                case GameActionTypeEnum.GUILD_CREATE:
                case GameActionTypeEnum.EXCHANGE:
                    FrameManager.AddFrame(GameActionFrame.Instance);
                    FrameManager.AddFrame(InventoryFrame.Instance);
                    FrameManager.AddFrame(MapFrame.Instance);
                    break;
            }
        }

        public override void StopAction(GameActionTypeEnum actionType, params object[] args)
        {
            base.StopAction(actionType, args);

            switch (actionType)
            {
                case GameActionTypeEnum.MAP_TELEPORT:
                    Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.CHANGE_MAP, Id));
                    FrameManager.AddFrame(GameInformationFrame.Instance);
                    Dispatch(WorldMessage.GAME_DATA_MAP(MapId, Map.CreateTime, Map.DataKey));
                    break;

                case GameActionTypeEnum.WAYPOINT:
                    FrameManager.AddFrame(GameActionFrame.Instance);
                    FrameManager.AddFrame(InventoryFrame.Instance);
                    FrameManager.AddFrame(MapFrame.Instance);
                    FrameManager.RemoveFrame(WaypointFrame.Instance);
                    break;

                case GameActionTypeEnum.PRISM_USE:
                    FrameManager.AddFrame(GameActionFrame.Instance);
                    FrameManager.AddFrame(InventoryFrame.Instance);
                    FrameManager.AddFrame(MapFrame.Instance);
                    FrameManager.RemoveFrame(PrismSubwayFrame.Instance);
                    break;

                case GameActionTypeEnum.NPC_DIALOG:
                    FrameManager.AddFrame(GameActionFrame.Instance);
                    FrameManager.AddFrame(InventoryFrame.Instance);
                    FrameManager.AddFrame(MapFrame.Instance);
                    FrameManager.RemoveFrame(NpcDialogFrame.Instance);
                    break;

                case GameActionTypeEnum.PRISM_AGGRESSION:
                case GameActionTypeEnum.TAXCOLLECTOR_AGGRESSION:
                case GameActionTypeEnum.GUILD_CREATE:
                case GameActionTypeEnum.EXCHANGE:
                    FrameManager.AddFrame(GameActionFrame.Instance);
                    FrameManager.AddFrame(InventoryFrame.Instance);
                    FrameManager.AddFrame(MapFrame.Instance);
                    break;

                case GameActionTypeEnum.MAP:
                    FrameManager.RemoveFrame(MapFrame.Instance);
                    FrameManager.RemoveFrame(GameActionFrame.Instance);
                    FrameManager.RemoveFrame(ExchangeFrame.Instance);
                    break;

                case GameActionTypeEnum.FIGHT:
                    if (!IsDisconnected)
                    {
                        Map?.DestroyEntity(this);
                        WorldService.Instance.AddUpdatable(this);
                        FrameManager.AddFrame(GameCreationFrame.Instance);
                        FrameManager.RemoveFrame(FightPlacementFrame.Instance);
                        FrameManager.RemoveFrame(FightFrame.Instance);
                    }
                    break;
            }
        }

        public override void SerializeAs_GameMapInformations(OperatorEnum operation, StringBuilder message)
        {
            switch (operation)
            {
                case OperatorEnum.OPERATOR_REMOVE:
                    message.Append(Id);
                    break;

                case OperatorEnum.OPERATOR_ADD:
                case OperatorEnum.OPERATOR_REFRESH:
                    if (HasGameAction(GameActionTypeEnum.FIGHT))
                    {
                        message.Append(Cell.Id).Append(';');
                    }
                    else
                    {
                        message.Append(CellId).Append(';');
                    }

                    message.Append(Orientation).Append(';');
                    message.Append((int)Type).Append(';');
                    message.Append(Id).Append(';');
                    message.Append(Name).Append(';');
                    message.Append((int)Breed);
                    if (TitleId != 0)
                    {
                        message.Append(',');
                        message.Append(TitleId).Append('*');
                        message.Append(TitleParams);
                    }
                    message.Append(';');
                    if (HasGameAction(GameActionTypeEnum.FIGHT))
                    {
                        message.Append(Skin).Append('^').Append(SkinSize).Append(';');
                    }
                    else
                    {
                        message.Append(SkinBase).Append('^').Append(SkinSizeBase).Append(';');
                    }

                    message.Append(Sex).Append(';');
                    if (HasGameAction(GameActionTypeEnum.FIGHT))
                    {
                        message.Append(Level).Append(';');
                    }

                    message.Append(AlignmentId).Append(',');
                    message.Append(AlignmentId).Append(',');
                    if (AlignmentEnabled)
                    {
                        message.Append(AlignmentLevel).Append(',');
                    }
                    else
                    {
                        message.Append('0').Append(',');
                    }

                    message.Append(Id + Level).Append(';');
                    message.Append(HexColor1).Append(';');
                    message.Append(HexColor2).Append(';');
                    message.Append(HexColor3).Append(';');
                    Inventory.SerializeAs_ActorLookMessage(message);
                    message.Append(';');
                    if (HasGameAction(GameActionTypeEnum.MAP))
                    {
                        message.Append(Aura).Append(';');
                        message.Append(m_lastEmoteId).Append(';');
                        message.Append(360000).Append(';');
                        if (m_guildDisplayInfos != null && GuildMember.Guild.IsActive)
                        {
                            message.Append(m_guildDisplayInfos).Append(';');
                        }
                        else
                        {
                            message.Append("").Append(';');
                            message.Append("").Append(';');
                        }
                        message.Append(Util.EncodeBase36(EntityRestriction)).Append(';');
                    }
                    else if (HasGameAction(GameActionTypeEnum.FIGHT))
                    {
                        message.Append(Life).Append(';');
                        message.Append(AP).Append(';');
                        message.Append(MP).Append(';');
                        switch (Fight.Type)
                        {
                            case FightTypeEnum.TYPE_CHALLENGE:
                            case FightTypeEnum.TYPE_AGGRESSION:
                                message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL) + Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_NEUTRAL)).Append(';');
                                message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA) + Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_TIERRA)).Append(';');
                                message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO) + Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_FUEGO)).Append(';');
                                message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA) + Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_AGUA)).Append(';');
                                message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE) + Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_AIRE)).Append(';');
                                break;

                            default:
                                message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL)).Append(';');
                                message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA)).Append(';');
                                message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO)).Append(';');
                                message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA)).Append(';');
                                message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE)).Append(';');
                                break;
                        }
                        message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_ESQUIVA_PA)).Append(';');
                        message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_ESQUIVA_PM)).Append(';');
                        message.Append(Team.Id).Append(';');
                    }
                    if (m_mount != null && RidingMount)
                    {
                        message.Append(m_mount.SerializeAs_MountLightInfos()).Append(';');
                    }
                    else
                    {
                        message.Append("").Append(';');
                    }

                    break;
            }
        }

        public void SerializeAs_PartyMemberInformations(StringBuilder message)
        {
            message.Append(Id).Append(';');
            message.Append(Name).Append(';');
            message.Append(SkinBase).Append(';');
            message.Append(HexColor1).Append(';');
            message.Append(HexColor2).Append(';');
            message.Append(HexColor3).Append(';');
            Inventory.SerializeAs_ActorLookMessage(message);
            message.Append(';');
            message.Append(Life).Append(',').Append(MaxLife).Append(';');
            message.Append(Level).Append(';');
            message.Append(Initiative).Append(';');
            message.Append(Prospection).Append(';');
            message.Append(0);
        }

        public void SerializeAs_EnnemyInformations(string playerPseudo, StringBuilder message)
        {
            message.Append(';');
            if (HasEnnemy(playerPseudo))
            {
                if (HasGameAction(GameActionTypeEnum.FIGHT))
                {
                    message.Append('2').Append(';');
                }
                else
                {
                    message.Append('1').Append(';');
                }

                message.Append(Name).Append(';');
                message.Append(Level).Append(';');
                message.Append(AlignmentId).Append(';');
            }
            else
            {
                message.Append("?;");
                message.Append(Name).Append(';');
                message.Append("?;");
                message.Append("-1;");
            }
            if (GuildMember != null)
            {
                message.Append(GuildMember.Guild.Name).Append(';');
            }
            else
            {
                message.Append(';');
            }

            message.Append(Sex).Append(';');
            message.Append(SkinBase);
        }

        public void SerializeAs_FriendInformations(string playerPseudo, StringBuilder message)
        {
            message.Append(';');
            if (HasFriend(playerPseudo))
            {
                if (HasGameAction(GameActionTypeEnum.FIGHT))
                {
                    message.Append('2').Append(';');
                }
                else
                {
                    message.Append('1').Append(';');
                }

                message.Append(Name).Append(';');
                message.Append(Level).Append(';');
                message.Append(AlignmentId).Append(';');
            }
            else
            {
                message.Append("?;");
                message.Append(Name).Append(';');
                message.Append("?;");
                message.Append("-1;");
            }
            if (GuildMember != null && GuildMember.Guild != null)
            {
                message.Append(GuildMember.Guild.Name).Append(';');
            }
            else
            {
                message.Append(';');
            }

            message.Append(Sex).Append(';');
            message.Append(SkinBase);
        }

        public override void Dispose()
        {
            GuildMember = null;

            FrameManager.Dispose();
            FrameManager = null;

            base.Dispose();
        }
    }
}


