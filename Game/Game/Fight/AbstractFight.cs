using Game.Action;
using Game.Database.Structure;
using Game.Entity;
using Game.Fight.AI;
using Game.Fight.Effect;
using Game.Fight.Ending;
using Game.Frame;
using Game.Manager;
using Game.Map;
using Game.Network;
using Game.Spell;
using Game.Stats;
using Protocolo.Framework.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Fight
{
    public enum FightTypeEnum
    {
        TYPE_CHALLENGE = 0,
        TYPE_AGGRESSION = 1,
        TYPE_PVMA = 2,
        TYPE_MXVM = 3,
        TYPE_PVM = 4,
        TYPE_PVT = 5,
        TYPE_PVMU = 6,
    }

    public enum FightStateEnum
    {
        STATE_PLACEMENT = 2,
        STATE_FIGHTING = 3,
        STATE_ENDED = 4,
    }

    public enum FightLoopStateEnum
    {
        STATE_INIT,
        STATE_WAIT_START,
        STATE_WAIT_TURN,
        STATE_WAIT_SUBACTION,
        STATE_WAIT_ACTION,
        STATE_PROCESS_EFFECT,
        STATE_WAIT_READY,
        STATE_WAIT_END,
        STATE_WAIT_AI,
        STATE_ENDED,
    }

    public enum FightEndStateEnum
    {
        STATE_END_INITIALIZE,
        STATE_END_EXECUTE_BEHAVIORS,
        STATE_END_SUCCESS,
        STATE_END_ERROR,
        STATE_ENDED,
    }

    public enum FightActionResultEnum
    {
        RESULT_NOTHING,
        RESULT_END_TURN,
        RESULT_PROCESS_EFFECT,
        RESULT_DEATH,
        RESULT_END,
    }

    public enum FightSpellLaunchResultEnum
    {
        RESULT_NO_AP,
        RESULT_NEED_MOVE,
        RESULT_WRONG_TARGET,
        RESULT_OK,
        RESULT_NO_LOS,
        RESULT_ERROR,
    }

    public enum FightEndTypeEnum
    {
        END_LOSER = 0,
        END_WINNER = 2,
        END_TAXCOLLECTOR = 5,
    }

    public abstract class AbstractFight : MessageDispatcher, IMovementHandler, IDisposable
    {
        public FightTypeEnum Type
        {
            get;
        }

        public FieldTypeEnum FieldType => FieldTypeEnum.TYPE_FIGHT;

        public bool CancelButton
        {
            get;
            private set;
        }

        public FightStateEnum State
        {
            get;
            private set;
        }

        public FightLoopStateEnum LoopState
        {
            get;
            set;
        }

        // El combate esta cerrandose (fase final) o ya terminado: en este estado no se aceptan
        // acciones (mover, lanzar, arma...) y se responde con BASIC_NO_OPERATION.
        public bool IsFightEnding => LoopState == FightLoopStateEnum.STATE_WAIT_END || LoopState == FightLoopStateEnum.STATE_ENDED;

        public FightLoopStateEnum NextLoopState
        {
            get;
            set;
        }

        public FightEndStateEnum LoopEndState
        {
            get;
            set;
        }

        public long Id
        {
            get;
        }

        public MapInstance Map
        {
            get;
            private set;
        }

        public AbstractFighter CurrentFighter
        {
            get;
            private set;
        }

        public AbstractFighter CurrentProcessingFighter
        {
            get;
            set;
        }

        public FightTeam Team0
        {
            get;
            private set;
        }

        public FightTeam Team1
        {
            get;
            private set;
        }

        public Dictionary<int, FightCell> Cells
        {
            get;
            private set;
        }

        public FightTurnProcessor TurnProcessor
        {
            get;
            private set;
        }

        public SpectatorTeam SpectatorTeam
        {
            get;
            private set;
        }

        public long TurnTime
        {
            get;
        }

        public long StartTime
        {
            get;
        }

        // Momento (UpdateTime, en ms) en que el combate paso de colocacion a lucha.
        // Sirve para calcular la duracion mostrada en el panel de fin de combate.
        public long CombatStartTime
        {
            get;
            private set;
        }

        public long TurnTimeLeft
        {
            get
            {
                if (NextTurnTimeout < UpdateTime)
                {
                    return 0;
                }

                return NextTurnTimeout - UpdateTime;
            }
        }

        public bool LoopTimedout => NextLoopTimeout <= UpdateTime;

        public long CurrentLoopTimeout
        {
            get
            {
                if (NextLoopTimeout < UpdateTime)
                {
                    return 0;
                }

                return NextLoopTimeout - UpdateTime;
            }
        }

        public long NextLoopTimeout
        {
            get
            {
                return m_loopTimeout;
            }
            set
            {
                m_loopTimeout = UpdateTime + value;
            }
        }

        public bool TurnTimedout => NextTurnTimeout <= UpdateTime;

        public long NextTurnTimeout
        {
            get
            {
                return m_turnTimeout;
            }
            set
            {
                m_turnTimeout = UpdateTime + value;
            }
        }

        public bool SubActionTimedout => NextSubActionTimeout <= UpdateTime;

        public long CurrentSubActionTimeout
        {
            get
            {
                if (NextSubActionTimeout < UpdateTime)
                {
                    return 0;
                }

                return NextSubActionTimeout - UpdateTime;
            }
        }

        public long NextSubActionTimeout
        {
            get
            {
                return m_subActionTimeout;
            }
            set
            {
                m_subActionTimeout = UpdateTime + value;
            }
        }

        public bool ActionTimedout
        {
            get
            {
                if (CurrentAction == null)
                {
                    return true;
                }

                return CurrentAction.Timeout <= UpdateTime;
            }
        }

        public bool SynchronizationTimedout => NextSynchroTimeout <= UpdateTime;

        public long NextSynchroTimeout
        {
            get
            {
                return m_synchronizationTimeout;
            }
            set
            {
                m_synchronizationTimeout = UpdateTime + value;
            }
        }

        public string FightPlaces => Team0.Places + "|" + Team1.Places;

        // Un desconectado nunca envía "listo": sin excluirlo, cada transición de turno agotaba
        // el timeout de sincronización (5 s) hasta expulsarlo.
        private bool IsAllReady => Fighters.OfType<CharacterEntity>().All(fighter => fighter.TurnReady || fighter.IsFighterDead || fighter.IsDisconnected);


        private bool IsAllReadyToStart
        {
            get
            {
                switch (Type)
                {
                    case FightTypeEnum.TYPE_PVT:
                    case FightTypeEnum.TYPE_PVMA:
                        return false;

                    case FightTypeEnum.TYPE_AGGRESSION:
                        if (Team0.Fighters.First().Type == EntityTypeEnum.TYPE_MONSTER_FIGHTER)
                        {
                            return false;
                        }

                        break;
                }

                return IsAllReady;
            }
        }

        public IEnumerable<AbstractFighter> Fighters => Team0.Fighters.Concat(Team1.Fighters);

        public IEnumerable<AbstractFighter> AliveFighters => Fighters.Where(fighter => !fighter.IsFighterDead);

        public AbstractGameFightAction CurrentAction => CurrentFighter?.CurrentAction as AbstractGameFightAction;

        public Func<FightActionResultEnum> CurrentSubAction
        {
            get;
            private set;
        }

        public FightEndResult Result
        {
            get;
            private set;
        }

        public IEnumerable<int> Obstacles
        {
            get
            {
                return Cells.Values.Where(cell => !cell.CanWalk).Select(cell => cell.Id);
            }
        }

        public long NextFighterId
        {
            get
            {
                return Fighters.Min(fighter => fighter.Id) - 1;
            }
        }

        public bool IsNeutralAgression
        {
            get;
            protected set;
        }

        public double ChallengeXpBonus => Math.Max(1.0, (100.0 + WinnerTeam.SucceededChallenges.Sum(challenge => challenge.BasicXpBonus + challenge.TeamXpBonus)) / 100.0);
        public double ChallengeLootBonus => Math.Max(1.0, (100.0 + WinnerTeam.SucceededChallenges.Sum(challenge => challenge.BasicDropBonus + challenge.TeamDropBonus)) / 100.0);
        public List<AbstractFighter> WinnerFighters { get; private set; }
        public List<AbstractFighter> LoserFighters { get; private set; }
        public FightTeam WinnerTeam { get; private set; }
        public FightTeam LoserTeam { get; private set; }

        private long m_loopTimeout, m_turnTimeout, m_subActionTimeout, m_synchronizationTimeout;
        private Dictionary<AbstractFighter, List<AbstractActivableObject>> m_activableObjects;
        private LinkedList<CastInfos> m_processingTargets;
        private int m_currentApCost;
        private readonly Queue<AbstractEndingBehavior> m_endingBehaviors;

        protected AbstractFight(FightTypeEnum type,
            MapInstance mapInstance,
            long id,
            long team0LeaderId,
            int team0Alignment,
            int team0FlagCell,
            long team1LeaderId,
            int team1Alignment,
            int team1FlagCell,
            long startTimeout,
            long turnTime,
            bool cancelButton = false,
            bool canWinHonor = false,
            params AbstractEndingBehavior[] endingBehaviors)
        {
            m_endingBehaviors = new Queue<AbstractEndingBehavior>(endingBehaviors);
            m_activableObjects = new Dictionary<AbstractFighter, List<AbstractActivableObject>>();
            m_processingTargets = new LinkedList<CastInfos>();
            m_currentApCost = -1;

            Type = type;
            Id = id;
            Map = mapInstance;
            State = FightStateEnum.STATE_PLACEMENT;
            LoopState = FightLoopStateEnum.STATE_INIT;
            CancelButton = cancelButton;
            TurnTime = turnTime;
            StartTime = startTimeout;
            NextLoopTimeout = startTimeout;
            Result = new FightEndResult(Id, canWinHonor);
            Cells = new Dictionary<int, FightCell>();
            TurnProcessor = new FightTurnProcessor();

            foreach (var cell in mapInstance.Cells)
            {
                Cells.Add(cell.Id, new FightCell(cell.Id, cell.Walkable, cell.LineOfSight, cell.GroundLevel));
            }

            SpectatorTeam = new SpectatorTeam(this);
            Team0 = new FightTeam(0, team0LeaderId, team0Alignment, team0FlagCell, this, new List<FightCell>(Cells.Values.Where(cell => mapInstance.FightTeam0Cells.Contains(cell.Id))));
            Team1 = new FightTeam(1, team1LeaderId, team1Alignment, team1FlagCell, this, new List<FightCell>(Cells.Values.Where(cell => mapInstance.FightTeam1Cells.Contains(cell.Id))));
            Team0.OpponentTeam = Team1;
            Team1.OpponentTeam = Team0;

            AddUpdatable(SpectatorTeam);
            AddUpdatable(Team0);
            AddUpdatable(Team1);
            AddHandler(SpectatorTeam.Dispatch);
            AddHandler(Team0.Dispatch);
            AddHandler(Team1.Dispatch);
        }

        public void Start()
        {
            AddMessage(() =>
                {
                    LoopState = FightLoopStateEnum.STATE_WAIT_START;
                    Map.CachedBuffer = true;
                    Map.Dispatch(WorldMessage.FIGHT_FLAG_DISPLAY(this));
                    Map.Dispatch(WorldMessage.FIGHT_FLAG_UPDATE(OperatorEnum.OPERATOR_ADD, Team0.LeaderId, Team0.Fighters.ToArray()));
                    Map.Dispatch(WorldMessage.FIGHT_FLAG_UPDATE(OperatorEnum.OPERATOR_ADD, Team1.LeaderId, Team1.Fighters.ToArray()));
                    Map.CachedBuffer = false;
                });
        }

        public void SetSubAction(Func<FightActionResultEnum> action, int timeout)
        {
            CurrentSubAction = action;
            NextSubActionTimeout = timeout;
        }

        public void AddProcessingTarget(CastInfos infos)
        {
            if (infos.Target == null)
            {
                Logger.Debug("AddProcessingTarget: se procesa primero porque el objetivo es nulo.");
                m_processingTargets.AddFirst(infos);
            }
            else if (CurrentProcessingFighter == infos.Target)
            {
                Logger.Debug($"AddProcessingTarget: se procesa primero porque coincide con el luchador en curso: {infos.Target.Name}");
                m_processingTargets.AddFirst(infos);
            }
            else if (CurrentProcessingFighter == null && CurrentFighter == infos.Target)
            {
                Logger.Debug($"AddProcessingTarget: se procesa primero porque coincide con el luchador actual: {infos.Target.Name}");
                m_processingTargets.AddFirst(infos);
            }
            else
            {
                Logger.Debug($"AddProcessingTarget: se envia al final de la cola: {infos.Target.Name}");
                m_processingTargets.AddLast(infos);
            }
        }

        public FightCell GetCell(int cellId)
        {
            if (Cells.ContainsKey(cellId))
            {
                return Cells[cellId];
            }

            return null;
        }

        public void KickSpectators()
        {
            AddMessage(() =>
            {
                if (IsFightEnding)
                {
                    return;
                }

                for (int i = SpectatorTeam.Spectators.Count() - 1; i > -1; i--)
                {
                    FightQuit(SpectatorTeam.Spectators.ElementAt(i), true);
                }
            });
        }

        public void TrySpectate(CharacterEntity character)
        {
            AddMessage(() =>
                    {
                        if (IsFightEnding)
                        {
                            character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                            return;
                        }

                        if (State != FightStateEnum.STATE_FIGHTING)
                        {
                            Logger.Debug($"FightBase::TrySpectate no se puede espectar durante la fase de colocacion: {character.Name}");
                            character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_FIGHT_SPECTATOR_LOCKED));
                            return;
                        }

                        if (!SpectatorTeam.CanJoin)
                        {
                            character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_FIGHT_SPECTATOR_LOCKED));
                            return;
                        }

                        character.JoinSpectator(this);

                        SendFightJoinInfos(character);
                    });
        }

        public void TryJoin(CharacterEntity character, long teamId)
        {
            AddMessage(() =>
                {
                    if (IsFightEnding)
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    if (State != FightStateEnum.STATE_PLACEMENT)
                    {
                        Logger.Debug($"FightBase::TryJoin el combate ya ha comenzado: {character.Name}");
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    if (!CanJoin(character))
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    var team = teamId == Team0.LeaderId ? Team0 : Team1;

                    if (!team.CanJoinBeforeStart(character))
                    {
                        Logger.Debug($"FightBase::TryJoin no puede unirse a ese equipo antes de empezar: {character.Name}");
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    JoinFight(character, team);
                });
        }

        public void JoinFight(AbstractFighter fighter, FightTeam team)
        {
            if (team.FreePlace == null)
            {
                return;
            }

            if (!fighter.IsDisconnected)
            {
                if (fighter.Type == EntityTypeEnum.TYPE_CHARACTER)
                {
                    OnCharacterJoin(fighter as CharacterEntity, team);
                }

                fighter.JoinFight(this, team);
                Dispatch(WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_ADD, fighter));
            }

            if (fighter.MapId == Map.Id)
            {
                SendFightJoinInfos(fighter);
            }
        }

        public FightActionResultEnum SummonFighter(AbstractFighter fighter, FightTeam team, int cellId)
        {
            fighter.JoinFight(this, team);
            fighter.TurnReady = true;

            var result = fighter.SetCell(GetCell(cellId));
            if (result != FightActionResultEnum.RESULT_NOTHING)
            {
                return result;
            }

            var message = new StringBuilder("+");
            fighter.SerializeAs_GameMapInformations(OperatorEnum.OPERATOR_ADD, message);

            if (fighter.Invocator != null)
            {
                Dispatch(WorldMessage.GAME_ACTION(fighter.SummonEffectType, fighter.Invocator.Id, message.ToString()));
            }
            else
            {
                Dispatch("GM|" + message.ToString());
            }

            switch (State)
            {
                case FightStateEnum.STATE_PLACEMENT:

                    break;

                case FightStateEnum.STATE_FIGHTING:
                    fighter.TurnReady = true;
                    TurnProcessor.SummonFighter(fighter);
                    Dispatch(WorldMessage.FIGHT_TURN_LIST(TurnProcessor.FighterOrder));
                    break;
            }

            return result;
        }

        public void FighterDisconnect(AbstractFighter fighter)
        {
            AddMessage(() =>
            {
                if (IsFightEnding)
                {
                    return;
                }

                fighter.IsDisconnected = true;

                if (fighter.IsSpectating)
                {
                    FightQuit((CharacterEntity)fighter, true);
                    return;
                }

                if (WorldConfig.LOG_DEBUG)
                {
                    Logger.Debug($"Fight::Disconnect luchador desconectado: {fighter.Name}");
                }

                if (fighter.DisconnectedTurnLeft == 0)
                {
                    fighter.DisconnectedTurnLeft = WorldConfig.FIGHT_DISCONNECTION_TURN;
                }

                Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_FIGHTER_DISCONNECTED, fighter.Name, fighter.DisconnectedTurnLeft));
            });
        }

        public FightActionResultEnum TryKillFighter(AbstractFighter fighter, AbstractFighter killer, bool force = false, bool quit = false)
        {
            if (LoopState == FightLoopStateEnum.STATE_ENDED ||
                LoopState == FightLoopStateEnum.STATE_WAIT_END ||
                LoopState == FightLoopStateEnum.STATE_INIT)
            {
                return FightActionResultEnum.RESULT_NOTHING;
            }

            if (fighter.DeclaredDead)
            {
                return FightActionResultEnum.RESULT_DEATH;
            }

            if (force)
            {
                fighter.Life = 0;
            }

            if (fighter.IsFighterDead)
            {
                Logger.Debug($"FightBase::KillFighter eliminando a: {fighter.Name}");

                if (quit)
                {
                    Dispatch(WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_REMOVE, fighter));
                }
                else
                {
                    Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_KILL, killer.Id, fighter.Id.ToString()));
                }

                fighter.OnDeath(killer);
                killer.OnKill(fighter);

                // En 1.29 los glifos/trampas desaparecen al morir su lanzador: si no, seguirían
                // activándose para siempre con un caster cuyos managers ya fueron destruidos (NRE).
                RemoveActivableObjects(fighter);

                if (!quit)
                {
                    Team0.CheckDeath(fighter);
                    Team1.CheckDeath(fighter);
                }

                foreach (var invocation in fighter.Team.AliveFighters.Where(ally => ally.Invocator == fighter))
                {
                    TryKillFighter(invocation, invocation, true);
                }

                if (fighter.Invocator != null)
                {
                    TurnProcessor.RemoveFighter(fighter);
                    Dispatch(WorldMessage.FIGHT_TURN_LIST(TurnProcessor.FighterOrder));
                }

                // Pausa del bucle tras una muerte
                if (State != FightStateEnum.STATE_PLACEMENT)
                {
                    NextLoopTimeout = CurrentLoopTimeout + 1300;
                }

                if (WillFinish())
                {
                    if (State == FightStateEnum.STATE_PLACEMENT)
                    {
                        NextLoopTimeout = -1;
                    }

                    return FightActionResultEnum.RESULT_END;
                }

                return FightActionResultEnum.RESULT_DEATH;
            }

            if (WillFinish())
            {
                return FightActionResultEnum.RESULT_END;
            }

            return FightActionResultEnum.RESULT_NOTHING;
        }


        public void FighterReady(AbstractFighter fighter)
        {
            AddMessage(() => { fighter.TurnReady = fighter.TurnReady == false; Dispatch(WorldMessage.FIGHT_READY(fighter.Id, fighter.TurnReady)); });
        }

        public void FighterPlacementChange(AbstractFighter fighter, int cellId)
        {
            AddMessage(() =>
            {
                if (State != FightStateEnum.STATE_PLACEMENT)
                {
                    fighter.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var cell = GetCell(cellId);
                if (cell != null)
                {
                    if (cell.CanWalk)
                    {
                        fighter.SetCell(cell);
                        Dispatch(WorldMessage.FIGHT_COORDINATE_INFORMATIONS(fighter));
                    }
                }
            });
        }

        private void SetAllUnReady()
        {
            foreach (var fighter in Fighters)
            {
                fighter.TurnReady = false;
            }
        }

        private void StartFight()
        {
            AddMessage(() =>
            {
                CombatStartTime = UpdateTime;

                OnFightStart();

                TurnProcessor.InitTurns(Fighters);

                Map.Dispatch(WorldMessage.FIGHT_FLAG_DESTROY(Id));

                CachedBuffer = true;
                Dispatch(WorldMessage.FIGHT_STARTS());
                Dispatch(WorldMessage.FIGHT_TURN_MIDDLE(Fighters));
                Dispatch(WorldMessage.FIGHT_COORDINATE_INFORMATIONS(AliveFighters.ToArray()));
                Dispatch(WorldMessage.FIGHT_TURN_LIST(TurnProcessor.FighterOrder));
                Team0.SendChallengeInfos();
                Team1.SendChallengeInfos();
                CachedBuffer = false;

                State = FightStateEnum.STATE_FIGHTING;
                NextLoopTimeout = -1;

                SetAllUnReady();

                BeginTurn();
            });
        }

        private void BeginTurn()
        {
            AddMessage(() =>
                {
                    CurrentFighter = TurnProcessor.NextFighter;
                    if (CurrentFighter == null)
                    {
                        LoopState = FightLoopStateEnum.STATE_WAIT_END;
                        LoopEndState = FightEndStateEnum.STATE_END_ERROR;
                        return;
                    }

                    base.Dispatch(WorldMessage.FIGHT_TURN_STARTS(CurrentFighter.Id, TurnTime));

                    NextTurnTimeout = TurnTime;

                    CurrentFighter.Team.BeginTurn(CurrentFighter);

                    switch (CurrentFighter.BeginTurn())
                    {
                        case FightActionResultEnum.RESULT_END:
                            return;

                        case FightActionResultEnum.RESULT_END_TURN:
                        case FightActionResultEnum.RESULT_DEATH:
                            CurrentFighter.TurnPass = true;
                            EndTurn();
                            return;
                    }

                    LoopState = FightLoopStateEnum.STATE_PROCESS_EFFECT;

                    switch (CurrentFighter.Type)
                    {
                        case EntityTypeEnum.TYPE_CHARACTER:
                            NextLoopState = FightLoopStateEnum.STATE_WAIT_TURN;
                            if (CurrentFighter.IsDisconnected)
                            {
                                CurrentFighter.TurnPass = true;
                            }
                            break;

                        default:
                            NextLoopState = FightLoopStateEnum.STATE_WAIT_AI;
                            if (CurrentFighter is AIFighter)
                            {
                                ((AIFighter)CurrentFighter).CurrentBrain.OnTurnStart();
                            }
                            break;
                    }
                });
        }

        private void MiddleTurn()
        {
            AddMessage(() => { if (!HasLeft(CurrentFighter)) { CurrentFighter.MiddleTurn(); } base.Dispatch(WorldMessage.FIGHT_TURN_MIDDLE(Fighters)); });
        }

        public bool HasLeft(AbstractFighter fighter)
        {
            return !Fighters.Contains(fighter);
        }

        private void EndTurn()
        {
            AddMessage(() =>
            {
                if (!HasLeft(CurrentFighter))
                {
                    if (!CurrentFighter.IsFighterDead)
                    {
                        CurrentFighter.Team.EndTurn(CurrentFighter);

                        if (CurrentFighter.EndTurn() == FightActionResultEnum.RESULT_END)
                        {
                            Logger.Debug("Fight::EndTurn el turno ha terminado y eso ha cerrado el combate.");
                            return;
                        }
                    }

                    if (m_activableObjects.ContainsKey(CurrentFighter))
                    {
                        foreach (var glyph in m_activableObjects[CurrentFighter].OfType<FightGlyph>())
                        {
                            glyph.DecrementDuration();
                        }

                        m_activableObjects[CurrentFighter].RemoveAll(fightObject => fightObject.ObstacleType == FightObstacleTypeEnum.TYPE_GLYPH && fightObject.Duration <= 0);
                    }
                }
                else
                {
                    // El que abandona pierde sus glifos: hay que retirarlos de la celda (Remove),
                    // no solo de la lista, o seguirían activándose con un caster ya liberado.
                    RemoveActivableObjects(CurrentFighter, FightObstacleTypeEnum.TYPE_GLYPH);
                }

                CachedBuffer = true;
                Dispatch(WorldMessage.FIGHT_TURN_FINISHED(CurrentFighter.Id));
                Dispatch(WorldMessage.FIGHT_TURN_READY(CurrentFighter.Id));
                CachedBuffer = false;

                SetAllUnReady();

                LoopState = FightLoopStateEnum.STATE_PROCESS_EFFECT;
                NextLoopState = FightLoopStateEnum.STATE_WAIT_READY;

                NextSynchroTimeout = 5000;
            });
        }

        public bool WillFinish()
        {
            if (LoopState == FightLoopStateEnum.STATE_WAIT_END)
            {
                return true;
            }

            if (GetWinners() != null)
            {
                WinnerTeam = GetWinners();
                WinnerFighters = WinnerTeam.Fighters.Where(fighter => fighter.Invocator == null).ToList();

                LoserTeam = WinnerTeam.OpponentTeam;
                LoserFighters = LoserTeam.Fighters.Where(fighter => fighter.Invocator == null).ToList();

                LoopState = FightLoopStateEnum.STATE_WAIT_END;

                if (CurrentAction != null)
                {
                    Dispatch(WorldMessage.FIGHT_ACTION_FINISHED(CurrentFighter.Id));
                }

                return true;
            }

            return false;
        }

        public AbstractFighter GetFighterOnCell(int cellId)
        {
            return AliveFighters.FirstOrDefault(fighter => fighter.Cell != null && fighter.Cell.Id == cellId);
        }

        public FightTeam GetWinners()
        {
            if (!Team0.HasSomeoneAlive)
            {
                return Team1;
            }

            if (!Team1.HasSomeoneAlive)
            {
                return Team0;
            }

            return null;
        }

        public override void Update(long updateDelta)
        {
            try
            {
                switch (LoopState)
                {
                    case FightLoopStateEnum.STATE_WAIT_START:
                        if (IsAllReadyToStart || LoopTimedout)
                        {
                            StartFight();
                        }
                        break;

                    case FightLoopStateEnum.STATE_WAIT_READY:
                        if (IsAllReady)
                        {
                            MiddleTurn();
                            BeginTurn();
                        }
                        else if (SynchronizationTimedout)
                        {
                            var fighters = AliveFighters.OfType<CharacterEntity>().Where(fighter => !fighter.TurnReady);
                            var fightersName = string.Join(", ", fighters.Select(fighter => fighter.Name));

                            Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_FIGHT_WAITING_PLAYERS, fightersName));

                            MiddleTurn();
                            BeginTurn();
                        }
                        break;

                    case FightLoopStateEnum.STATE_WAIT_TURN:
                        if (LoopTimedout)
                        {
                            if (TurnTimedout || HasLeft(CurrentFighter) || CurrentFighter.TurnPass || CurrentFighter.IsFighterDead)
                            {
                                EndTurn();
                            }
                            else if (CurrentFighter is AIFighter)
                            {
                                LoopState = FightLoopStateEnum.STATE_WAIT_AI;
                            }
                        }
                        break;

                    case FightLoopStateEnum.STATE_PROCESS_EFFECT:
                        if (m_processingTargets.Count > 0)
                        {
                            var castInfos = m_processingTargets.First.Value;
                            m_processingTargets.RemoveFirst();

                            CurrentProcessingFighter = castInfos.Target;

                            if (CurrentProcessingFighter != null)
                            {
                                if (!CurrentProcessingFighter.IsFighterDead)
                                {
                                    Logger.Debug($"Procesando efecto de: {CurrentProcessingFighter.Name}");
                                    var effectResult = EffectManager.Instance.TryApplyEffect(castInfos);
                                    if (effectResult == FightActionResultEnum.RESULT_END)
                                    {
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                var effectResult = EffectManager.Instance.TryApplyEffect(castInfos);
                                if (effectResult == FightActionResultEnum.RESULT_END)
                                {
                                    break;
                                }
                            }

                            LoopState = FightLoopStateEnum.STATE_WAIT_SUBACTION;
                        }
                        else
                        {
                            CurrentProcessingFighter = null;
                            LoopState = NextLoopState;
                        }
                        break;

                    case FightLoopStateEnum.STATE_WAIT_ACTION:
                        if (ActionTimedout || CurrentAction.IsFinished)
                        {
                            if (CurrentAction != null && !CurrentAction.IsFinished)
                            {
                                CurrentFighter.StopAction(CurrentAction.Type);
                            }
                            
                            if (CurrentSubAction != null)
                            {
                                LoopState = FightLoopStateEnum.STATE_WAIT_SUBACTION;
                                Logger.Debug("FightBase::Update esperando a que termine una subaccion.");
                                break;
                            }

                            if (m_processingTargets.Count > 0 && LoopState != FightLoopStateEnum.STATE_WAIT_END)
                            {
                                LoopState = FightLoopStateEnum.STATE_PROCESS_EFFECT;
                                NextLoopState = FightLoopStateEnum.STATE_WAIT_ACTION;
                                break;
                            }

                            if (m_currentApCost != -1)
                            {
                                Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_PA_LOST, CurrentFighter.Id, CurrentFighter.Id + ",-" + m_currentApCost));
                                m_currentApCost = -1;
                            }

                            Dispatch(WorldMessage.FIGHT_ACTION_FINISHED(CurrentFighter.Id));

                            if (LoopState == FightLoopStateEnum.STATE_WAIT_END)
                            {
                                break;
                            }

                            switch (CurrentFighter.Type)
                            {
                                case EntityTypeEnum.TYPE_CHARACTER:
                                    LoopState = FightLoopStateEnum.STATE_WAIT_TURN;
                                    break;

                                default:
                                    LoopState = FightLoopStateEnum.STATE_WAIT_AI;
                                    break;
                            }
                        }
                        break;

                    case FightLoopStateEnum.STATE_WAIT_SUBACTION:
                        if (SubActionTimedout)
                        {
                            if (CurrentSubAction == null)
                            {
                                LoopState = FightLoopStateEnum.STATE_PROCESS_EFFECT;
                                NextLoopState = FightLoopStateEnum.STATE_WAIT_ACTION;
                            }
                            else
                            {
                                var currentAction = CurrentSubAction;
                                var result = currentAction();
                                switch (result)
                                {
                                    case FightActionResultEnum.RESULT_END:
                                        Logger.Debug("FightBase::Update el combate ha terminado tras la subaccion.");
                                        return;

                                    case FightActionResultEnum.RESULT_DEATH:
                                        if (CurrentFighter.IsFighterDead)
                                        {
                                            CurrentFighter.TurnPass = true;
                                        }

                                        break;
                                }

                                if (CurrentSubAction == currentAction)
                                {
                                    CurrentSubAction = null;
                                    LoopState = FightLoopStateEnum.STATE_PROCESS_EFFECT;
                                    NextLoopState = FightLoopStateEnum.STATE_WAIT_ACTION;
                                }
                            }
                        }
                        break;

                    case FightLoopStateEnum.STATE_WAIT_AI:
                        var aiFighter = CurrentFighter as AIFighter;
                        if (aiFighter != null)
                        {
                            try
                            {
                                aiFighter.CurrentBrain.OnUpdate();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex.ToString());
                                CurrentFighter.TurnPass = true;
                            }
                        }
                        LoopState = FightLoopStateEnum.STATE_WAIT_TURN;
                        break;

                    case FightLoopStateEnum.STATE_WAIT_END:
                        switch (LoopEndState)
                        {
                            case FightEndStateEnum.STATE_END_INITIALIZE:
                                LoopEndState = FightEndStateEnum.STATE_END_EXECUTE_BEHAVIORS;
                                Team0.FightEnd();
                                Team1.FightEnd();
                                break;

                            case FightEndStateEnum.STATE_END_EXECUTE_BEHAVIORS:
                                if (m_endingBehaviors.Count > 0)
                                {
                                    m_endingBehaviors.Dequeue().Execute(this);
                                }
                                else
                                {
                                    LoopEndState = FightEndStateEnum.STATE_END_SUCCESS;
                                }
                                break;

                            case FightEndStateEnum.STATE_END_SUCCESS:
                                if (LoopTimedout)
                                {
                                    FightEnd();
                                    LoopEndState = FightEndStateEnum.STATE_ENDED;
                                }
                                break;

                            case FightEndStateEnum.STATE_END_ERROR:
                                FightEndError();
                                LoopEndState = FightEndStateEnum.STATE_ENDED;
                                break;
                        }
                        break;

                    case FightLoopStateEnum.STATE_ENDED:
                        FightEnded();
                        break;
                }

                base.Update(updateDelta);
            }
            catch (Exception ex)
            {
                if (LoopState != FightLoopStateEnum.STATE_ENDED)
                {
                    LoopState = FightLoopStateEnum.STATE_WAIT_END;
                    LoopEndState = FightEndStateEnum.STATE_END_ERROR;
                }
                Logger.Error($"Error al cerrar el combate: tipo={Type} detalle={ex}");
            }
        }

        public FightSpellLaunchResultEnum CanLaunchSpell(AbstractFighter fighter, SpellLevel spellLevel, int spellId, int cellId, int castCell)
        {
            if (fighter == null
                || spellLevel == null
                || Map == null
                || fighter.Cell == null
                || fighter.Statistics == null
                || fighter.StateManager == null)
            {
                return FightSpellLaunchResultEnum.RESULT_ERROR;
            }

            if (LoopState != FightLoopStateEnum.STATE_WAIT_TURN && LoopState != FightLoopStateEnum.STATE_WAIT_AI)
            {
                return FightSpellLaunchResultEnum.RESULT_ERROR;
            }

            if (fighter.IsFighterDead)
            {
                return FightSpellLaunchResultEnum.RESULT_ERROR;
            }

            if (CurrentFighter != fighter)
            {
                return FightSpellLaunchResultEnum.RESULT_ERROR;
            }

            if (GetCell(castCell) == null)
            {
                return FightSpellLaunchResultEnum.RESULT_ERROR;
            }

            if (fighter.AP < spellLevel.APCost)
            {
                return FightSpellLaunchResultEnum.RESULT_NO_AP;
            }

            if (spellLevel.RequiredLevel > 0 && fighter.Level < spellLevel.RequiredLevel)
            {
                return FightSpellLaunchResultEnum.RESULT_ERROR;
            }

            if (fighter.StateManager.HasState(FighterStateEnum.STATE_WEAKENED))
            {
                return FightSpellLaunchResultEnum.RESULT_ERROR;
            }


            if (spellLevel.Conditions != null)
            {
                foreach (var stateId in spellLevel.Conditions)
                {
                    if (!fighter.StateManager.HasState((FighterStateEnum)stateId))
                    {
                        Logger.Debug($"[CanLaunchSpell] Bloqueado: hechizo={spellLevel.SpellId} requiere estado={stateId} luchador={fighter.Id}");
                        return FightSpellLaunchResultEnum.RESULT_ERROR;
                    }
                }
            }

            if (spellLevel.TargetZones != null)
            {
                foreach (var stateId in spellLevel.TargetZones)
                {
                    if (fighter.StateManager.HasState((FighterStateEnum)stateId))
                    {
                        Logger.Debug($"[CanLaunchSpell] Bloqueado: hechizo={spellLevel.SpellId} estado prohibido={stateId} luchador={fighter.Id}");
                        return FightSpellLaunchResultEnum.RESULT_ERROR;
                    }
                }
            }

            var distance = Pathfinding.GoalDistance(Map, cellId, castCell);
            var maxPo = spellLevel.AllowPOBoost && spellLevel.MaxPO != 0 ? spellLevel.MaxPO + fighter.Statistics.GetTotal(EffectEnum.STAT_MAS_ALCANCE) : spellLevel.MaxPO;

            if (maxPo < spellLevel.MinPO)
            {
                maxPo = spellLevel.MinPO;
            }

            if (distance > maxPo || distance < spellLevel.MinPO)
            {
                return FightSpellLaunchResultEnum.RESULT_NEED_MOVE;
            }

            if (spellLevel.EmptyCell && !GetCell(castCell).CanWalk)
            {
                return FightSpellLaunchResultEnum.RESULT_WRONG_TARGET;
            }

            // Una sola trampa por celda: si el hechizo coloca una trampa y la celda ya tiene una
            // (propia o enemiga oculta), el lanzamiento se rechaza AQUI, antes de gastar PA (el
            // efecto tambien lo rechazaba, pero en silencio y con los PA ya consumidos).
            if (spellLevel.Effects != null
                && spellLevel.Effects.Any(effect => effect.TypeEnum == EffectEnum.COMBATE_COLOCAR_TRAMPA)
                && GetCell(castCell).HasObject(FightObstacleTypeEnum.TYPE_TRAP))
            {
                return FightSpellLaunchResultEnum.RESULT_WRONG_TARGET;
            }

            if (spellLevel.InLine && !Pathfinding.InLine(Map, cellId, castCell))
            {
                return FightSpellLaunchResultEnum.RESULT_NEED_MOVE;
            }

            if (spellLevel.EmptyCell && GetCell(castCell).HasObject(FightObstacleTypeEnum.TYPE_FIGHTER))
            {
                return FightSpellLaunchResultEnum.RESULT_NO_LOS;
            }

            if (spellLevel.LOS && !Pathfinding.CheckView(this, cellId, castCell))
            {
                return FightSpellLaunchResultEnum.RESULT_NO_LOS;
            }

            if (spellLevel.Effects != null)
            {
                if (spellLevel.Effects.Any(effect => effect.TypeEnum == EffectEnum.INVOCACION_CRIATURA || effect.TypeEnum == EffectEnum.INVOCACION_DOBLE))
                {
                    var invocationCount = fighter.Team?.AliveFighters?.Count(f => f.Invocator == fighter && !f.StaticInvocation) ?? 0;
                    if (invocationCount >= fighter.Statistics.GetTotal(EffectEnum.STAT_MAS_INVOCACIONES_MAX))
                    {
                        fighter.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_MAX_INVOCATION_REACHED, fighter.Statistics.GetTotal(EffectEnum.STAT_MAS_INVOCACIONES_MAX)));
                        return FightSpellLaunchResultEnum.RESULT_ERROR;
                    }
                }
            }

            var target = GetFighterOnCell(castCell);
            long targetId = 0;
            if (target != null)
            {
                targetId = target.Id;
            }

            if (fighter.SpellManager == null || !fighter.SpellManager.CanLaunchSpell(spellLevel, spellId, targetId))
            {
                return FightSpellLaunchResultEnum.RESULT_WRONG_TARGET;
            }

            return FightSpellLaunchResultEnum.RESULT_OK;
        }

        public bool CanUseWeapon(AbstractFighter fighter, ItemDAO weapon, int cellId)
        {
            if (fighter == null || weapon?.Template == null || fighter.Cell == null || fighter.Statistics == null)
            {
                return false;
            }

            var template = weapon.Template;

            if (LoopState != FightLoopStateEnum.STATE_WAIT_TURN)
            {
                Logger.Debug($"Fight::CanUseWeapon se ha intentado usar un arma fuera de la fase de espera del turno: {fighter.Name}");
                return false;
            }

            if (CurrentFighter != fighter)
            {
                Logger.Debug($"Fight::CanUseWeapon el luchador ha intentado usar un arma fuera de su turno: {fighter.Name}");
                return false;
            }

            // Mismas guardas que el lanzamiento de hechizo: un muerto (murió en su propio turno
            // por trampa/veneno) o debilitado no puede atacar en la ventana previa al fin de turno.
            if (fighter.IsFighterDead || fighter.StateManager == null || fighter.StateManager.HasState(FighterStateEnum.STATE_WEAKENED))
            {
                Logger.Debug($"Fight::CanUseWeapon el luchador no puede actuar (muerto/debilitado): {fighter.Name}");
                return false;
            }

            if (GetCell(cellId) == null)
            {
                Logger.Debug($"Fight::CanUseWeapon la celda de lanzamiento es nula: {fighter.Name}");
                return false;
            }

            if (fighter.AP < template.APCost)
            {
                Logger.Debug($"Fight::CanUseWeapon no tiene PA suficientes: {fighter.Name}");
                return false;
            }



            if (weapon.IsEthereal && weapon.MaxDurability > 0 && weapon.Durability == 0)
            {
                Logger.Debug($"Fight::CanUseWeapon el arma eterea ya no tiene durabilidad: {fighter.Name}");
                return false;
            }

            var distance = Pathfinding.GoalDistance(Map, fighter.Cell.Id, cellId);

            // En 1.29 las armas tienen alcance FIJO: el +alcance (STAT_MAS_ALCANCE) no aplica
            // (el cliente devuelve canBoostRange=false para armas), así que no se suma.
            if (distance > template.POMax || distance < template.POMin)
            {
                Logger.Debug($"Fight::CanUseWeapon la celda objetivo esta fuera de alcance: {fighter.Name}");
                return false;
            }

            // Las armas requieren línea de visión (como el hechizo con LOS): sin esto, un arco o
            // varita podría golpear a través de muros con un cliente modificado.
            if (!Pathfinding.CheckView(this, fighter.Cell.Id, cellId))
            {
                Logger.Debug($"Fight::CanUseWeapon sin linea de vision al objetivo: {fighter.Name}");
                return false;
            }

            return true;
        }

        public void TryUseWeapon(AbstractFighter fighter, int cellId, int actionTime = 5000)
        {
            AddMessage(() =>
                {
                    if (IsFightEnding)
                    {
                        fighter.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    if (State != FightStateEnum.STATE_FIGHTING)
                    {
                        Logger.Debug($"Fight::TryUseWeapon el combate no esta en estado de lucha: {fighter.Name}");
                        fighter.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    if (m_currentApCost != -1)
                    {
                        Logger.Debug($"Fight::TryUseWeapon el combate ya esta procesando otro lanzamiento y aun no ha terminado: {fighter.Name}");
                        fighter.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    var weapon = fighter.Inventory.Items.Find(item => item.Slot == ItemSlotEnum.SLOT_WEAPON);

                    if (weapon == null)
                    {
                        TryLaunchSpell(fighter, 0, cellId);
                        return;
                    }

                    if (!CanUseWeapon(fighter, weapon, cellId))
                    {
                        Logger.Debug($"Fight::TryUseWeapon no se puede usar el arma: {fighter.Name}");
                        fighter.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    var weaponTemplate = weapon.Template;

                    CurrentFighter.Team.CheckWeapon(fighter, weaponTemplate);

                    var isMelee = Pathfinding.GoalDistance(Map, fighter.Cell.Id, cellId) == 1;

                    fighter.UsedAP += weaponTemplate.APCost;

                    // Atacar con arma (daga, bastón, arco...) revela por completo al Sram invisible.
                    if (fighter.StateManager != null && fighter.StateManager.HasState(FighterStateEnum.STATE_STEALTH))
                        fighter.BuffManager.RemoveStealth();

                    Dispatch(WorldMessage.FIGHT_ACTION_START(CurrentFighter.Id));

                    var failure = fighter.RollCriticalFailure(weaponTemplate.CFRate);

                    if (failure)
                    {
                        CachedBuffer = true;
                        Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_WEAPON_FAILURE, fighter.Id, weaponTemplate.Id.ToString()));
                        Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_PA_LOST, fighter.Id, fighter.Id + ",-" + weaponTemplate.APCost));
                        Dispatch(WorldMessage.FIGHT_ACTION_FINISHED(CurrentFighter.Id));
                        CachedBuffer = false;

                        CurrentFighter.TurnPass = true;
                        return;
                    }

                    var criticalHit = fighter.RollCriticalHit(weaponTemplate.CSRate);

                    if (criticalHit)
                    {
                        Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_CRITICAL_HIT, fighter.Id, "0"));
                    }

                    var effects = weapon.Statistics.WeaponEffects;
                    var targetLists = new List<Tuple<GenericEffect, List<AbstractFighter>>>();

                    foreach (var effect in effects)
                    {
                        var targetList = new List<AbstractFighter>();
                        foreach (var currentCellId in CellZone.GetCells(Map, cellId, fighter.Cell.Id, weaponTemplate.RangeType))
                        {
                            var fightCell = GetCell(currentCellId);
                            if (fightCell != null)
                            {
                                foreach (var fighterObject in fightCell.FightObjects.OfType<AbstractFighter>())
                                {
                                    if (fighter == fighterObject)
                                    {
                                        continue;
                                    }

                                    targetList.Add(fighterObject);
                                }
                            }
                        }
                        targetLists.Add(Tuple.Create(effect.Value, targetList));
                    }

                    LoopState = FightLoopStateEnum.STATE_WAIT_ACTION;

                    fighter.UseWeapon(cellId, actionTime, () =>
                    {
                        if (IsFightEnding)
                        {
                            fighter.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                            return;
                        }

                        foreach (var targetsByEffect in targetLists)
                        {
                            targetsByEffect.Item2.RemoveAll(affectedTarget => affectedTarget.IsFighterDead);
                            var effectType = targetsByEffect.Item1.EffectType;
                            var value1 = criticalHit && CastInfos.IsDamageEffect(effectType) ? targetsByEffect.Item1.Value1 + weaponTemplate.CSBonus : targetsByEffect.Item1.Value1;
                            var value2 = criticalHit && CastInfos.IsDamageEffect(effectType) ? targetsByEffect.Item1.Value2 + weaponTemplate.CSBonus : targetsByEffect.Item1.Value2;

                            if (targetsByEffect.Item2.Count == 0)
                            {
                                AddProcessingTarget(
                                        new CastInfos(
                                                        effectType,
                                                        -1,
                                                        cellId,
                                                        value1,
                                                        value2,
                                                        -1,
                                                        -1,
                                                        0,
                                                        fighter,
                                                        null,
                                                        weaponTemplate.RangeType,
                                                        0,
                                                        -1,
                                                        isMelee)
                                                     );
                            }
                            else
                            {
                                foreach (var target in targetsByEffect.Item2)
                                {
                                    AddProcessingTarget(new CastInfos(
                                                        effectType,
                                                        -1,
                                                        cellId,
                                                        value1,
                                                        value2,
                                                        -1,
                                                        -1,
                                                        0,
                                                        fighter,
                                                        target,
                                                        weaponTemplate.RangeType,
                                                        target.Cell.Id,
                                                        -1,
                                                        isMelee));
                                }
                            }

                        }

                        m_currentApCost = weaponTemplate.APCost;
                    });
                });
        }

        /// <summary>
        /// Un hechizo REVELA al Sram invisible si es un ataque DIRECTO: tiene algún efecto de daño
        /// instantáneo (duración 0). Los venenos (daño con duración), trampas/glifos y utilitarios
        /// (Miedo, boosts, invocaciones) NO revelan: solo señalan la casilla.
        /// </summary>
        private static bool SpellRevealsStealth(SpellLevel spellLevel)
        {
            if (spellLevel?.Effects == null)
                return false;

            foreach (var effect in spellLevel.Effects)
            {
                if (effect != null && effect.Duration == 0 && CastInfos.IsDamageEffect(effect.TypeEnum))
                    return true;
            }

            return false;
        }
        
        public void SignalStealthPosition(AbstractFighter fighter)
        {
            if (fighter?.Cell == null)
                return;

            // Reemplaza cualquier marcador anterior del mismo luchador.
            fighter.ClearStealthSignal();

            // El propio cliente pone la bandera (flag.swf) en la celda y escribe "X ha señalado
            // la posición Y" en el chat al recibir Gf (Game::onFlag).
            Dispatch(WorldMessage.FIGHT_CELL_FLAG(fighter.Id, fighter.Cell.Id));

            fighter.StealthSignalCell = fighter.Cell.Id;
            fighter.LastKnownStealthCell = fighter.Cell.Id;
        }

        public void TryLaunchSpell(AbstractFighter fighter, int spellId, int castCellId, int actionTime = 5000)
        {
            if (fighter == null)
            {
                return;
            }

            AddMessage(() =>
            {
                if (IsFightEnding)
                {
                    fighter.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (State != FightStateEnum.STATE_FIGHTING)
                {
                    if (WorldConfig.LOG_DEBUG)
                    {
                        Logger.Debug($"Fight::TryLaunchSpell el combate no esta en estado de lucha: {fighter.Name}");
                    }

                    fighter.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (m_currentApCost != -1)
                {
                    if (WorldConfig.LOG_DEBUG)
                    {
                        Logger.Debug($"Fight::TryLaunchSpell el combate ya esta procesando otro lanzamiento y aun no ha terminado: {fighter.Name}");
                    }

                    fighter.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (fighter.SpellBook == null)
                {
                    if (WorldConfig.LOG_DEBUG)
                    {
                        Logger.Debug($"Fight::TryLaunchSpell el grimorio esta vacio: {fighter.Name}");
                    }

                    fighter.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var spellLevel = fighter.SpellBook.GetSpellLevel(spellId);

                if (spellLevel == null)
                {
                    if (WorldConfig.LOG_DEBUG)
                    {
                        Logger.Debug($"Fight::TryLaunchSpell hechizo desconocido: {fighter.Name}");
                    }

                    fighter.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (fighter.Cell == null)
                {
                    if (WorldConfig.LOG_DEBUG)
                    {
                        Logger.Debug($"Fight::TryLaunchSpell la celda del luchador es nula: {fighter.Name}");
                    }

                    fighter.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var launchResult = CanLaunchSpell(fighter, spellLevel, spellId, fighter.Cell.Id, castCellId);
                if (launchResult != FightSpellLaunchResultEnum.RESULT_OK)
                {
                    if (WorldConfig.LOG_DEBUG)
                    {
                        Logger.Debug($"Fight::TryLaunchSpell no se puede lanzar el hechizo: {fighter.Name} motivo={launchResult}");
                    }

                    fighter.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var isMelee = Pathfinding.GoalDistance(Map, fighter.Cell.Id, castCellId) == 1;

                fighter.UsedAP += spellLevel.APCost;

                // Invisibilidad del Sram: un ataque directo lo revela por completo; cualquier otro
                // hechizo (trampa, veneno, Miedo, boost, invocación) solo señala su casilla actual.
                if (fighter.StateManager != null && fighter.StateManager.HasState(FighterStateEnum.STATE_STEALTH))
                {
                    if (SpellRevealsStealth(spellLevel))
                        fighter.BuffManager.RemoveStealth();
                    else
                        SignalStealthPosition(fighter);
                }

                base.Dispatch(WorldMessage.FIGHT_ACTION_START(CurrentFighter.Id));

                if (fighter.RollCriticalFailure(spellLevel.ECSRate))
                {
                    CachedBuffer = true;
                    Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_CRITICAL_FAILURE, fighter.Id, spellId.ToString()));
                    Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_PA_LOST, fighter.Id, fighter.Id + ",-" + spellLevel.APCost));
                    Dispatch(WorldMessage.FIGHT_ACTION_FINISHED(CurrentFighter.Id));
                    CachedBuffer = false;

                    if (spellLevel.IsECSEndTurn == 1)
                    {
                        CurrentFighter.TurnPass = true;
                    }
                    return;
                }

                var target = GetFighterOnCell(castCellId);
                fighter.SpellManager.Actualize(spellLevel, spellId, target?.Id ?? 0);

                var isCritic = spellLevel.CriticalEffects.Count > 0 && fighter.RollCriticalHit(spellLevel.CSRate);

                if (isCritic)
                {
                    Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_CRITICAL_HIT, fighter.Id, spellId.ToString()));
                }

                var effects = isCritic ? spellLevel.CriticalEffects : spellLevel.Effects;
                var targetLists = new Dictionary<SpellEffect, List<AbstractFighter>>();
                var effectIndex = 0;

                // Las celdas de cada zona se calculan una única vez aunque varios efectos
                // compartan el mismo par (forma, tamaño).
                var zoneCellsCache = new Dictionary<string, List<int>>();

                foreach (var effect in effects)
                {
                    targetLists.Add(effect, new List<AbstractFighter>());

                    // Zona y máscara PROPIAS de cada efecto (como el cliente): un hechizo puede
                    // golpear en área con un efecto y aplicar otro solo al objetivo puntual.
                    var targetType = spellLevel.GetEffectTarget(effectIndex, isCritic);
                    var effectZone = spellLevel.GetEffectZone(effectIndex, isCritic);

                    if (!zoneCellsCache.TryGetValue(effectZone, out var zoneCells))
                    {
                        zoneCells = CellZone.GetCells(Map, castCellId, fighter.Cell.Id, effectZone).ToList();
                        zoneCellsCache[effectZone] = zoneCells;
                    }

                    if (effect.TypeEnum != EffectEnum.COMBATE_COLOCAR_GLIFO && effect.TypeEnum != EffectEnum.COMBATE_COLOCAR_TRAMPA)
                    {
                        if (targetType != -1 && ((targetType >> 5) & 1) == 1)
                        {
                            // Bit 5 = el efecto solo afecta al lanzador: se añade directamente,
                            // aunque la zona no contenga ningún luchador (antes se perdía).
                            targetLists[effect].Add(fighter);
                        }
                        else
                        {
                            foreach (var currentCellId in zoneCells)
                            {
                                var fightCell = GetCell(currentCellId);
                                if (fightCell == null)
                                    continue;

                                foreach (var fighterObject in fightCell.FightObjects.OfType<AbstractFighter>())
                                {
                                    if (targetType != -1)
                                    {
                                        if (((targetType & 1) == 1) && fighter.Team == fighterObject.Team)
                                        {
                                            continue;
                                        }

                                        if ((((targetType >> 1) & 1) == 1) && fighter.Id == fighterObject.Id)
                                        {
                                            continue;
                                        }

                                        if ((((targetType >> 2) & 1) == 1) && fighter.Team != fighterObject.Team)
                                        {
                                            continue;
                                        }

                                        if (((((targetType >> 3) & 1) == 1) && (fighterObject.Invocator == null)))
                                        {
                                            continue;
                                        }

                                        if (((((targetType >> 4) & 1) == 1) && (fighterObject.Invocator != null)))
                                        {
                                            continue;
                                        }
                                    }

                                    if (!targetLists[effect].Contains(fighterObject))
                                    {
                                        targetLists[effect].Add(fighterObject);
                                    }
                                }
                            }
                        }
                    }
                    effectIndex++;
                }

                LoopState = FightLoopStateEnum.STATE_WAIT_ACTION;

                var template = SpellManager.Instance.GetTemplate(spellId);

                fighter.LaunchSpell(castCellId, spellId, spellLevel.Level, template.Sprite.ToString(), template.SpriteInfos, actionTime, () =>
                {
                    if (IsFightEnding)
                    {
                        fighter.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    // Grupos de probabilidad: los efectos consecutivos con Chance > 0 comparten
                    // UNA tirada y se aplica exactamente aquel cuyo rango acumulado contiene la
                    // tirada (Ruleta, Siega...). Tirar por efecto sesgaba las probabilidades y el
                    // residuo negativo corrompía los grupos siguientes.
                    int? groupRoll = null;
                    var groupAccumulated = 0;
                    var applyIndex = -1;

                    foreach (var effect in effects)
                    {
                        applyIndex++;

                        if (effect.Chance > 0)
                        {
                            groupRoll ??= Util.Next(0, 100);

                            var lowerBound = groupAccumulated;
                            groupAccumulated += effect.Chance;

                            if (groupRoll.Value < lowerBound || groupRoll.Value >= groupAccumulated)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            // Efecto sin probabilidad: cierra el grupo anterior.
                            groupRoll = null;
                            groupAccumulated = 0;
                        }

                        // Zona PROPIA del efecto (trampas/glifos la usan para su área y
                        // Percepción para su radio de revelado).
                        var appliedZone = spellLevel.GetEffectZone(applyIndex, isCritic);

                        targetLists[effect].RemoveAll(affectedTarget => affectedTarget.IsFighterDead);

                        if (targetLists[effect].Count == 0)
                        {
                            var castInfos = new CastInfos(
                                                    effect.TypeEnum,
                                                    spellId,
                                                    castCellId,
                                                    effect.Value1,
                                                    effect.Value2,
                                                    effect.Value3,
                                                    effect.Chance,
                                                    effect.Duration,
                                                    fighter,
                                                    null,
                                                    appliedZone,
                                                    0,
                                                    spellLevel.Level,
                                                    isMelee);
                            AddProcessingTarget(castInfos);
                            CurrentFighter.Team.CheckSpell(CurrentFighter, castInfos);
                        }
                        else
                        {
                            foreach (var effectTarget in targetLists[effect])
                            {
                                var castInfos = new CastInfos(
                                                    effect.TypeEnum,
                                                    spellId,
                                                    castCellId,
                                                    effect.Value1,
                                                    effect.Value2,
                                                    effect.Value3,
                                                    effect.Chance,
                                                    effect.Duration,
                                                    fighter,
                                                    effectTarget,
                                                    appliedZone,
                                                    effectTarget.Cell.Id,
                                                    spellLevel.Level,
                                                    isMelee);
                                AddProcessingTarget(castInfos);
                                CurrentFighter.Team.CheckSpell(CurrentFighter, castInfos);
                            }
                        }

                    }

                    m_currentApCost = spellLevel.APCost;
                });
            });
        }


        protected virtual void FightEnd()
        {
            if (State == FightStateEnum.STATE_PLACEMENT)
            {
                Map.Dispatch(WorldMessage.FIGHT_FLAG_DESTROY(Id));
            }

            State = FightStateEnum.STATE_ENDED;
            LoopState = FightLoopStateEnum.STATE_ENDED;

            foreach (var fighter in WinnerFighters)
            {
                if (!Result.HasResult(fighter))
                {
                    Result.AddResult(fighter, FightEndTypeEnum.END_WINNER);
                }
            }

            foreach (var fighter in LoserFighters)
            {
                if (!Result.HasResult(fighter))
                {
                    Result.AddResult(fighter);
                }
            }

            Result.Duration = CombatStartTime > 0 ? UpdateTime - CombatStartTime : 0;
            Dispatch(WorldMessage.FIGHT_END_RESULT(Result));

            foreach (var fighter in WinnerTeam.Fighters.ToArray())
            {
                fighter.EndFight(true);
            }

            foreach (var fighter in LoserTeam.Fighters.ToArray())
            {
                fighter.EndFight();
            }

            foreach (var spectator in SpectatorTeam.Spectators.ToArray())
            {
                spectator.EndFight();
            }
        }

        protected virtual void FightEndError()
        {
            if (State == FightStateEnum.STATE_PLACEMENT)
            {
                Map.Dispatch(WorldMessage.FIGHT_FLAG_DESTROY(Id));
            }

            State = FightStateEnum.STATE_ENDED;
            LoopState = FightLoopStateEnum.STATE_ENDED;

            Result.Duration = CombatStartTime > 0 ? UpdateTime - CombatStartTime : 0;
            Dispatch(WorldMessage.FIGHT_END_RESULT(Result));

            foreach (var fighter in Fighters.ToArray())
            {
                fighter.EndFight(true);
            }

            foreach (var spectator in SpectatorTeam.Spectators.ToArray())
            {
                spectator.EndFight();
            }
        }

        private void FightEnded()
        {
            Map.FightManager.Remove(this);
        }

        public override void Dispose()
        {
            foreach (var cell in Cells)
            {
                cell.Value.Dispose();
            }

            Cells.Clear();
            Cells = null;

            SpectatorTeam = null;
            Team0 = null;
            Team1 = null;

            CurrentFighter = null;
            CurrentProcessingFighter = null;
            CurrentSubAction = null;

            TurnProcessor.Dispose();
            TurnProcessor = null;

            Result.Dispose();
            Result = null;

            Map = null;

            m_activableObjects.Clear();
            m_activableObjects = null;
            WinnerTeam = null;
            LoserTeam = null;
            WinnerFighters.Clear();
            WinnerFighters = null;
            LoserFighters.Clear();
            LoserFighters = null;
            m_processingTargets.Clear();
            m_processingTargets = null;

            base.Dispose();
        }

        public bool HasObjectOnCell(FightObstacleTypeEnum type, int cell)
        {
            var fightCell = GetCell(cell);
            if (fightCell == null)
            {
                return false;
            }

            return fightCell.HasObject(type);
        }

        public bool CanPutObject(int cellId)
        {
            var cell = GetCell(cellId);
            if (cell == null)
            {
                return false;
            }

            return cell.CanPutObject;
        }

        public void AddActivableObject(AbstractFighter caster, AbstractActivableObject obj)
        {
            if (!m_activableObjects.ContainsKey(caster))
            {
                m_activableObjects.Add(caster, new List<AbstractActivableObject>());
            }

            m_activableObjects[caster].Add(obj);
        }

        // Retira de la celda (y de la lista del lanzador) los objetos activables indicados, o
        // todos si no se especifica tipo. Se usa cuando el lanzador muere o abandona el combate.
        private void RemoveActivableObjects(AbstractFighter caster, FightObstacleTypeEnum? type = null)
        {
            if (caster == null || m_activableObjects == null || !m_activableObjects.TryGetValue(caster, out var objects))
                return;

            for (int i = objects.Count - 1; i >= 0; i--)
            {
                var obj = objects[i];
                if (type.HasValue && obj.ObstacleType != type.Value)
                    continue;

                obj.Remove();
                objects.RemoveAt(i);
            }

            if (objects.Count == 0)
                m_activableObjects.Remove(caster);
        }

        public bool CanAbortMovement => false;

        public void Move(AbstractEntity entity, int cellId, string path)
        {
            if (entity == null)
            {
                return;
            }

            AddMessage(() =>
            {
                if (LoopState != FightLoopStateEnum.STATE_WAIT_TURN)
                {
                    Logger.Debug($"Fight::Move se ha intentado mover fuera de la fase de espera del turno: {entity.Name}");
                    return;
                }

                if (entity != CurrentFighter)
                {
                    Logger.Debug($"Fight::Move no es su turno: {entity.Name}");
                    return;
                }

                var fighter = entity as AbstractFighter;
                if (fighter?.Cell == null)
                {
                    Logger.Debug($"Fight::Move luchador no valido: {entity.Name}");
                    return;
                }

                if (fighter.IsFighterDead)
                {
                    Logger.Debug($"Fight::Move luchador muerto intenta moverse: {entity.Name}");
                    return;
                }

                var movementPath = Pathfinding.IsValidPath(this, fighter, fighter.Cell.Id, path);

                if (movementPath == null)
                {
                    Logger.Debug($"Fight::Move la ruta de movimiento es nula: {entity.Name}");
                    return;
                }

                if (movementPath.MovementLength <= 0 || movementPath.EndCell == fighter.Cell.Id)
                {
                    Logger.Debug($"Fight::Move la ruta de movimiento esta vacia: {entity.Name}");
                    return;
                }


                if (movementPath.MovementLength > fighter.MP)
                {
                    Logger.Debug($"Fight::Move no tiene PM suficientes para moverse: {entity.Name}");
                    return;
                }

                var tacledChance = Pathfinding.TryTacle(fighter);

                if (tacledChance != -1 && !CurrentFighter.StateManager.HasState(FighterStateEnum.STATE_ROOTED))
                {
                    CachedBuffer = true;

                    Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_TACLE, fighter.Id));

                    var lostAP = (fighter.AP * tacledChance / 100) - 1;

                    if (lostAP < 0)
                    {
                        lostAP = 1;
                    }

                    if (lostAP > fighter.AP)
                    {
                        lostAP = fighter.AP;
                    }

                    fighter.UsedAP += lostAP;

                    Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_PA_LOST, fighter.Id, fighter.Id + ",-" + lostAP));

                    var lostMP = fighter.MP;

                    if (lostMP < 0)
                    {
                        lostMP = 0;
                    }

                    if (lostMP > fighter.MP)
                    {
                        lostMP = fighter.MP;
                    }

                    fighter.UsedMP += lostMP;

                    Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_PM_LOST, fighter.Id, fighter.Id + ",-" + lostMP));
                    CachedBuffer = false;

                    return;
                }

                LoopState = FightLoopStateEnum.STATE_WAIT_ACTION;

                Dispatch(WorldMessage.FIGHT_ACTION_START(CurrentFighter.Id));

                CurrentFighter.Team.CheckMovement(fighter, fighter.Cell.Id, movementPath.EndCell, movementPath.MovementLength);

                fighter.Move(movementPath);
            });
        }

        public void MovementFinish(AbstractEntity entity, MovementPath movementPath, int cellId)
        {
            var fighter = (AbstractFighter)entity;

            if (fighter.IsFighterDead)
                return;

            fighter.UsedMP += movementPath.MovementLength;

            Dispatch(WorldMessage.GAME_ACTION(GameActionTypeEnum.FIGHT_PM_LOST, fighter.Id, fighter.Id + ",-" + movementPath.MovementLength));

            fighter.Orientation = movementPath.GetDirection(movementPath.LastStep);
            fighter.SetCell(GetCell(cellId));
        }

        public void SendMapFightInfos(AbstractEntity entity)
        {
            if (State == FightStateEnum.STATE_PLACEMENT)
            {
                entity.Dispatch(WorldMessage.FIGHT_FLAG_DISPLAY(this));
                Team0.SendMapFightInfos(entity);
                Team1.SendMapFightInfos(entity);
            }
        }

        public void SendFightJoinInfos(AbstractFighter fighter)
        {
            if (fighter.Type == EntityTypeEnum.TYPE_CHARACTER)
            {
                fighter.CachedBuffer = true;
                fighter.Dispatch(fighter.IsSpectating ? WorldMessage.FIGHT_JOIN_SUCCESS((int)FightStateEnum.STATE_FIGHTING, false, false, true, 0, (int)Type) : WorldMessage.FIGHT_JOIN_SUCCESS((int)State, CancelButton, true, false, StartTime - UpdateTime, (int)Type));
                fighter.Dispatch(WorldMessage.GAME_MAP_INFORMATIONS(OperatorEnum.OPERATOR_ADD, AliveFighters.ToArray()));

                switch (State)
                {
                    case FightStateEnum.STATE_PLACEMENT:
                        fighter.Dispatch(WorldMessage.FIGHT_AVAILABLE_PLACEMENTS(fighter.Team.Id, FightPlaces));
                        if (fighter.IsDisconnected)
                        {
                            fighter.IsDisconnected = false;
                            fighter.Dispatch(WorldMessage.GAME_DATA_SUCCESS());
                            Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_FIGHTER_RECONNECTED, fighter.Name));
                        }
                        else
                        {
                            Map.Dispatch(WorldMessage.FIGHT_FLAG_UPDATE(OperatorEnum.OPERATOR_ADD, fighter.Team.LeaderId, fighter));
                            if (fighter.MapId != Map.Id)
                            {
                                fighter.Dispatch(WorldMessage.GAME_DATA_SUCCESS());
                            }
                        }
                        break;

                    case FightStateEnum.STATE_FIGHTING:
                        if (fighter.IsSpectating)
                        {
                            Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_FIGHT_SPECTATOR_JOINED, fighter.Name));
                        }
                        else if (fighter.IsDisconnected)
                        {
                            fighter.IsDisconnected = false;
                            fighter.Team.SendChallengeInfos(fighter);
                            fighter.Dispatch(WorldMessage.GAME_DATA_SUCCESS());
                            Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_FIGHTER_RECONNECTED, fighter.Name));
                        }
                        fighter.Dispatch(WorldMessage.FIGHT_COORDINATE_INFORMATIONS(AliveFighters.ToArray()));
                        fighter.Dispatch(WorldMessage.FIGHT_STARTS());
                        fighter.Dispatch(WorldMessage.FIGHT_TURN_LIST(TurnProcessor.FighterOrder));
                        fighter.Dispatch(WorldMessage.FIGHT_TURN_STARTS(CurrentFighter.Id, TurnTimeLeft));

                        foreach (var aliveFighter in AliveFighters)
                        {
                            foreach (var buff in aliveFighter.BuffManager.GetAllBuffs())
                            {
                                buff.SendTo(fighter.Dispatch);
                            }
                        }

                        break;
                }
                fighter.CachedBuffer = false;
            }
        }

        public virtual void OnFightStart()
        {
            foreach (var character in Fighters.OfType<CharacterEntity>())
            {
                character.FrameManager.RemoveFrame(FightPlacementFrame.Instance);
                character.FrameManager.RemoveFrame(InventoryFrame.Instance);
                character.FrameManager.AddFrame(GameActionFrame.Instance);
                character.FrameManager.AddFrame(FightFrame.Instance);

                Map.FightManager.ExecuteFightActions(Type, FightStateEnum.STATE_PLACEMENT, character);
            }

            foreach (var fighter in Fighters.OfType<AIFighter>())
            {
                fighter.TurnReady = true;
            }
        }

        public virtual void OnCharacterJoin(CharacterEntity character, FightTeam team)
        {
        }


        public abstract bool CanJoin(CharacterEntity character);
        public abstract FightActionResultEnum FightQuit(CharacterEntity character, bool kick = false);
        public abstract void SerializeAs_FightList(StringBuilder message);
        public abstract void SerializeAs_FightFlag(StringBuilder message);
    }
}


