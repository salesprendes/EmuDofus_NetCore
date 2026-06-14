using Game.Action;
using Game.Entity;
using Game.Map;
using Game.Network;
using Game.Spell;
using System;
using System.Text;

namespace Game.Fight
{
    public abstract class AbstractFighter : AbstractEntity, IFightObstacle
    {

        #region IFightObstacle
        public FightObstacleTypeEnum ObstacleType => FightObstacleTypeEnum.TYPE_FIGHTER;

        public int Priority => 0;

        public bool CanGoThrough => false;

        public bool CanStack => false;

        #endregion

        #region AbstractEntity

        public abstract override int MapId
        {
            get;
            set;
        }


        public abstract override int BaseLife
        {
            get;
        }

        public abstract override int CellId
        {
            get;
            set;
        }

        public abstract override string Name
        {
            get;
        }

        public abstract override int Level
        {
            get;
        }

        #endregion

        #region AbstractFighter

        public abstract bool TurnReady
        {
            get;
            set;
        }

        public abstract bool TurnPass
        {
            get;
            set;
        }

        public abstract int SkinBase
        {
            get;
        }

        public abstract int SkinSizeBase
        {
            get;
        }

        public int Skin
        {
            get;
            set;
        }

        public int SkinSize
        {
            get;
            set;
        }

        #endregion

        #region Fighter

        public bool IsDisconnected
        {
            get;
            set;
        }

        public int DisconnectedTurnLeft
        {
            get;
            set;
        }

        public bool IsSpectating
        {
            get;
            set;
        }

        public FightCell Cell
        {
            get;
            private set;
        }

        #endregion

        public AbstractFight Fight
        {
            get;
            protected set;
        }

        public FightTeam Team
        {
            get;
            private set;
        }

        public bool IsLeader => Team?.LeaderId == Id;

        public bool IsFighterDead => DeclaredDead || Life <= 0;

        public bool CanBeginTurn => !IsFighterDead && Fight != null;

        public abstract bool CanDrop { get; }

        public int UsedAP
        {
            get;
            set;
        }

        public int UsedMP
        {
            get;
            set;
        }

        public int MaxAP => Statistics.GetTotal(EffectEnum.STAT_MAS_PA);

        public int MaxMP => Statistics.GetTotal(EffectEnum.STAT_MAS_PM);

        public int AP => MaxAP - UsedAP;

        public int MP => MaxMP - UsedMP;

        public int APDodge => (int)Math.Floor((double)Statistics.GetTotal(EffectEnum.STAT_MAS_SABIDURIA) / 4) + Statistics.GetTotal(EffectEnum.STAT_MAS_ESQUIVA_PA);

        public int MPDodge => (int)Math.Floor((double)Statistics.GetTotal(EffectEnum.STAT_MAS_SABIDURIA) / 4) + Statistics.GetTotal(EffectEnum.STAT_MAS_ESQUIVA_PM);

        public AbstractFighter Invocator
        {
            get;
            set;
        }

        public bool StaticInvocation
        {
            get;
            set;
        }

        public virtual EffectEnum SummonEffectType => StaticInvocation ? EffectEnum.INVOCACION_ESTATICA : EffectEnum.INVOCACION_CRIATURA;

        public BuffEffectManager BuffManager
        {
            get;
            private set;
        }

        public FighterStateManager StateManager
        {
            get;
            private set;
        }

        public SpellCastManager SpellManager
        {
            get;
            private set;
        }

        public override IMovementHandler MovementHandler
        {
            get
            {
                if (Fight != null)
                    return Fight;
                return base.MovementHandler;
            }
        }

        public abstract int AlignmentId { get; }

        public bool DeclaredDead { get; private set; }

        protected AbstractFighter(EntityTypeEnum type, long id, bool staticInvocation = false)
    : base(type, id)
        {
            StaticInvocation = staticInvocation;
        }

        public new void Move(MovementPath path)
        {
            CurrentAction = new GameFightMovementAction(this, path);

            StartAction(GameActionTypeEnum.MAP_MOVEMENT);
        }

        public void LaunchSpell(int cellId, int spellId, int spellLevel, string sprite, string spriteInfos, long duration, System.Action callback)
        {
            CurrentAction = new GameFightSpellAction(this, cellId, spellId, spellLevel, sprite, spriteInfos, duration, callback);

            StartAction(GameActionTypeEnum.FIGHT_SPELL_LAUNCH);
        }

        public void UseWeapon(int cellId, long duration, System.Action callback)
        {
            CurrentAction = new GameFightWeaponAction(this, cellId, duration, callback);

            StartAction(GameActionTypeEnum.FIGHT_WEAPON_USE);
        }

        public virtual void JoinFight(AbstractFight fight, FightTeam team)
        {
            BuffManager = new BuffEffectManager(this);
            StateManager = new FighterStateManager(this);
            SpellManager = new SpellCastManager();

            DeclaredDead = false;
            Orientation = 1;
            Skin = SkinBase;
            SkinSize = SkinSizeBase;
            UsedAP = 0;
            UsedMP = 0;

            Fight = fight;

            Team = team;
            TurnReady = false;
            TurnPass = false;

            Team.AddFighter(this);
            Team.AddUpdatable(this);
            Team.AddHandler(Dispatch);

            if (Life < 1)
                Life = 1;

            if (Fight.State == FightStateEnum.STATE_PLACEMENT)
                SetCell(Team.FreePlace);

            SetChatChannel(ChatChannelEnum.CHANNEL_TEAM, () => Team.Dispatch);
            StartAction(GameActionTypeEnum.FIGHT);
        }

        public virtual void EndFight(bool win = false)
        {
            if (!IsSpectating)
            {
                Team.RemoveFighter(this);
                Team.RemoveUpdatable(this);
                Team.RemoveHandler(Dispatch);

                Fight.TurnProcessor.RemoveFighter(this);

                Statistics.ClearBoosts();
            }

            SetChatChannel(ChatChannelEnum.CHANNEL_TEAM, () => null);
            SetChatChannel(ChatChannelEnum.CHANNEL_GENERAL, () => MovementHandler == null ? default(Action<string>) : MovementHandler.Dispatch);

            CurrentAction = null;
            StopAction(GameActionTypeEnum.FIGHT);

            if (SpellManager != null)
            {
                SpellManager.Dispose();
                SpellManager = null;
            }

            if (StateManager != null)
            {
                StateManager.Dispose();
                StateManager = null;
            }

            if (BuffManager != null)
            {
                BuffManager.Dispose();
                BuffManager = null;
            }

            Skin = SkinBase;
            SkinSize = SkinSizeBase;

            SetCell(null);
            Cell = null;
            Team = null;
            Fight = null;
            IsSpectating = false;
            IsDisconnected = false;
            TurnPass = false;
            TurnReady = false;
            Invocator = null;
        }

        public virtual FightActionResultEnum BeginTurn()
        {
            TurnPass = false;

            var buffResult = BuffManager.BeginTurn();
            if (buffResult != FightActionResultEnum.RESULT_NOTHING)
                return buffResult;

            return Cell.BeginTurn(this);
        }

        public virtual FightActionResultEnum MiddleTurn()
        {
            UsedAP = 0;
            UsedMP = 0;

            return FightActionResultEnum.RESULT_NOTHING;
        }

        public virtual FightActionResultEnum EndTurn()
        {
            SpellManager.EndTurn();

            return BuffManager.EndTurn();
        }

        public void CalculDamages(EffectEnum effect, ref int jet)
        {
            switch (effect)
            {
                case EffectEnum.DANO_TIERRA:
                case EffectEnum.ROBO_VIDA_TIERRA:
                case EffectEnum.DANO_NEUTRAL:
                case EffectEnum.ROBO_VIDA_NEUTRAL:
                    jet = (int)Math.Floor((double)jet * (100 + Statistics.GetTotal(EffectEnum.STAT_MAS_FUERZA) + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO_PORCENTAJE)) / 100 + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO_FISICO) + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO) + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO_BIS) - Statistics.GetTotal(EffectEnum.STAT_MENOS_DANO_FIJO) - Statistics.GetTotal(EffectEnum.DANO_SIN_BOOST));
                    break;

                case EffectEnum.DANO_FUEGO:
                case EffectEnum.ROBO_VIDA_FUEGO:
                    jet = (int)Math.Floor((double)jet * (100 + Statistics.GetTotal(EffectEnum.STAT_MAS_INTELIGENCIA) + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO_PORCENTAJE)) / 100 + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO_MAGICO) + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO) + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO_BIS) - Statistics.GetTotal(EffectEnum.STAT_MENOS_DANO_FIJO) - Statistics.GetTotal(EffectEnum.DANO_SIN_BOOST));
                    break;

                case EffectEnum.DANO_AIRE:
                case EffectEnum.ROBO_VIDA_AIRE:
                    jet = (int)Math.Floor((double)jet * (100 + Statistics.GetTotal(EffectEnum.STAT_MAS_AGILIDAD) + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO_PORCENTAJE)) / 100 + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO_MAGICO) + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO) + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO_BIS) - Statistics.GetTotal(EffectEnum.STAT_MENOS_DANO_FIJO) - Statistics.GetTotal(EffectEnum.DANO_SIN_BOOST));
                    break;

                case EffectEnum.DANO_AGUA:
                case EffectEnum.ROBO_VIDA_AGUA:
                    jet = (int)Math.Floor((double)jet * (100 + Statistics.GetTotal(EffectEnum.STAT_MAS_SUERTE) + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO_PORCENTAJE)) / 100 + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO_MAGICO) + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO) + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO_BIS) - Statistics.GetTotal(EffectEnum.STAT_MENOS_DANO_FIJO) - Statistics.GetTotal(EffectEnum.DANO_SIN_BOOST));
                    break;
            }
        }

        public void CalculReduceDamages(EffectEnum effect, ref int damages)
        {
            var coef = damages;
            switch (effect)
            {
                case EffectEnum.DANO_NEUTRAL:
                case EffectEnum.ROBO_VIDA_NEUTRAL:
                    coef = damages * (100 - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL) - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_NEUTRAL)) / 100
                                             - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_NEUTRAL) - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PVP_NEUTRAL);
                    damages = (int)(coef - Statistics.GetTotal(EffectEnum.STAT_MAS_REDUCCION_DANO_FISICO));
                    break;

                case EffectEnum.DANO_TIERRA:
                case EffectEnum.ROBO_VIDA_TIERRA:
                    coef = damages * (100 - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA) - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_TIERRA)) / 100
                                             - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_TIERRA) - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PVP_TIERRA);
                    damages = (int)(coef - Statistics.GetTotal(EffectEnum.STAT_MAS_REDUCCION_DANO_FISICO));
                    break;

                case EffectEnum.DANO_FUEGO:
                case EffectEnum.ROBO_VIDA_FUEGO:
                    coef = damages * (100 - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO) - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_FUEGO)) / 100
                                             - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_FUEGO) - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PVP_FUEGO);
                    damages = (int)(coef - Statistics.GetTotal(EffectEnum.STAT_MAS_REDUCCION_DANO_MAGICO));
                    break;

                case EffectEnum.DANO_AIRE:
                case EffectEnum.ROBO_VIDA_AIRE:
                    coef = damages * (100 - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE) - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_AIRE)) / 100
                                             - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_AIRE) - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PVP_AIRE);
                    damages = (int)(coef - Statistics.GetTotal(EffectEnum.STAT_MAS_REDUCCION_DANO_MAGICO));
                    break;

                case EffectEnum.DANO_AGUA:
                case EffectEnum.ROBO_VIDA_AGUA:
                    coef = damages * (100 - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA) - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_AGUA)) / 100
                                             - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_AGUA) - Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PVP_AGUA);
                    damages = (int)(coef - Statistics.GetTotal(EffectEnum.STAT_MAS_REDUCCION_DANO_MAGICO));
                    break;
            }
        }

        public void CalculHeal(ref int heal)
        {
            heal = (int)Math.Floor((double)heal * (100 + Statistics.GetTotal(EffectEnum.STAT_MAS_INTELIGENCIA)) / 100 + Statistics.GetTotal(EffectEnum.STAT_MAS_CURAS));
        }

        public void CalculCriticalHitRate(ref int cHitRate)
        {
            cHitRate = (int)(cHitRate * Math.E * 1.1 / Math.Log(Statistics.GetTotal(EffectEnum.STAT_MAS_AGILIDAD) + 12));
        }

        public int CalculArmor(EffectEnum damageEffect)
        {
            switch (damageEffect)
            {
                case EffectEnum.DANO_TIERRA:
                case EffectEnum.ROBO_VIDA_TIERRA:
                case EffectEnum.DANO_NEUTRAL:
                case EffectEnum.ROBO_VIDA_NEUTRAL:
                    return (Statistics.GetTotal(EffectEnum.STAT_MAS_ARMADURA_TIERRA) * Math.Max(1 + Statistics.GetTotal(EffectEnum.STAT_MAS_FUERZA) / 100, 1 + Statistics.GetTotal(EffectEnum.STAT_MAS_FUERZA) / 200 + Statistics.GetTotal(EffectEnum.STAT_MAS_INTELIGENCIA) / 200)) + (Statistics.GetTotal(EffectEnum.STAT_MAS_ARMADURA) * Math.Max(1 + Statistics.GetTotal(EffectEnum.STAT_MAS_FUERZA) / 100, 1 + Statistics.GetTotal(EffectEnum.STAT_MAS_FUERZA) / 200 + Statistics.GetTotal(EffectEnum.STAT_MAS_INTELIGENCIA) / 200));

                case EffectEnum.DANO_FUEGO:
                case EffectEnum.ROBO_VIDA_FUEGO:
                    return (Statistics.GetTotal(EffectEnum.STAT_MAS_ARMADURA_FUEGO) * Math.Max(1 + Statistics.GetTotal(EffectEnum.STAT_MAS_INTELIGENCIA) / 100, 1 + Statistics.GetTotal(EffectEnum.STAT_MAS_INTELIGENCIA) / 200 + Statistics.GetTotal(EffectEnum.STAT_MAS_INTELIGENCIA) / 200)) + (Statistics.GetTotal(EffectEnum.STAT_MAS_ARMADURA) * Math.Max(1 + Statistics.GetTotal(EffectEnum.STAT_MAS_INTELIGENCIA) / 100, 1 + Statistics.GetTotal(EffectEnum.STAT_MAS_INTELIGENCIA) / 200 + Statistics.GetTotal(EffectEnum.STAT_MAS_INTELIGENCIA) / 200));

                case EffectEnum.DANO_AIRE:
                case EffectEnum.ROBO_VIDA_AIRE:
                    return (Statistics.GetTotal(EffectEnum.STAT_MAS_ARMADURA_AIRE) * Math.Max(1 + Statistics.GetTotal(EffectEnum.STAT_MAS_AGILIDAD) / 100, 1 + Statistics.GetTotal(EffectEnum.STAT_MAS_AGILIDAD) / 200 + Statistics.GetTotal(EffectEnum.STAT_MAS_INTELIGENCIA) / 200)) + (Statistics.GetTotal(EffectEnum.STAT_MAS_ARMADURA) * Math.Max(1 + Statistics.GetTotal(EffectEnum.STAT_MAS_AGILIDAD) / 100, 1 + Statistics.GetTotal(EffectEnum.STAT_MAS_AGILIDAD) / 200 + Statistics.GetTotal(EffectEnum.STAT_MAS_INTELIGENCIA) / 200));

                case EffectEnum.DANO_AGUA:
                case EffectEnum.ROBO_VIDA_AGUA:
                    return (Statistics.GetTotal(EffectEnum.STAT_MAS_ARMADURA_AGUA) * Math.Max(1 + Statistics.GetTotal(EffectEnum.STAT_MAS_SUERTE) / 100, 1 + Statistics.GetTotal(EffectEnum.STAT_MAS_SUERTE) / 200 + Statistics.GetTotal(EffectEnum.STAT_MAS_INTELIGENCIA) / 200)) + (Statistics.GetTotal(EffectEnum.STAT_MAS_ARMADURA) * Math.Max(1 + Statistics.GetTotal(EffectEnum.STAT_MAS_SUERTE) / 100, 1 + Statistics.GetTotal(EffectEnum.STAT_MAS_SUERTE) / 200 + Statistics.GetTotal(EffectEnum.STAT_MAS_INTELIGENCIA) / 200));
            }

            return 0;
        }

        public int CalculDodgeAPMP(AbstractFighter caster, int lostPoint, bool mp = false)
        {
            var reality = 0;

            if (!mp)
            {
                var dodgeAPCaster = caster.APDodge;
                var dodgeAPTarget = APDodge;
                if (dodgeAPTarget == 0)
                    dodgeAPTarget = 1;

                for (int i = 0; i < lostPoint; i++)
                {
                    var actualAP = AP - reality;
                    var realAP = AP;
                    if (realAP == 0)
                        realAP = 1;

                    var percentLastAP = (double)actualAP / realAP;
                    var chance = 0.5 * ((double)dodgeAPCaster / dodgeAPTarget) * percentLastAP;
                    var percentChance = chance * 100;

                    if (percentChance > 100)
                        percentChance = 90;
                    else if (percentChance < 10)
                        percentChance = 10;

                    if (Util.Next(0, 100) < percentChance)
                        reality++;
                }
            }
            else
            {
                var dodgeMPCaster = caster.MPDodge;
                var dodgeMPTarget = MPDodge;
                if (dodgeMPTarget == 0)
                    dodgeMPTarget = 1;

                for (int i = 0; i < lostPoint; i++)
                {
                    var actualMP = MP - reality;
                    var realMP = MP;
                    if (realMP == 0)
                        realMP = 1;

                    var percentLastMP = (double)actualMP / realMP;
                    var chance = 0.5 * ((double)dodgeMPCaster / dodgeMPTarget) * percentLastMP;
                    var percentChance = chance * 100;

                    if (percentChance > 100)
                        percentChance = 90;
                    else if (percentChance < 10)
                        percentChance = 10;

                    if (Util.Next(0, 100) < percentChance)
                        reality++;
                }
            }

            return reality;
        }

        public virtual void OnKill(AbstractFighter target)
        {
            FireEvent(EntityEventType.FIGHT_KILL, target);
        }

        public void OnDeath(AbstractFighter killer)
        {
            if (!DeclaredDead)
            {
                DeclaredDead = true;
                if (Cell != null)
                {
                    Cell.RemoveObject(this);

                    Cell = null;
                }
            }
        }

        public FightActionResultEnum SetCell(FightCell cell)
        {
            if (IsFighterDead)
                return FightActionResultEnum.RESULT_DEATH;

            if (Cell != null)
            {
                if (Cell == cell)
                    return FightActionResultEnum.RESULT_NOTHING;

                var removeResult = Cell.RemoveObject(this);
                if (removeResult != FightActionResultEnum.RESULT_NOTHING)
                    return removeResult;
            }

            Cell = cell;

            if (Cell != null)
            {
                var moveResult = Cell.AddObject(this);
                if (moveResult != FightActionResultEnum.RESULT_NOTHING)
                    return moveResult;

                var buffResult = BuffManager.EndMove();
                if (buffResult != FightActionResultEnum.RESULT_NOTHING)
                    return buffResult;

                if (Fight.LoopState != FightLoopStateEnum.STATE_ENDED)
                    return Fight.TryKillFighter(this, this);
            }

            if (Fight.State != FightStateEnum.STATE_FIGHTING)
                return FightActionResultEnum.RESULT_NOTHING;

            return FightActionResultEnum.RESULT_NOTHING;
        }

        public override void StartAction(GameActionTypeEnum actionType)
        {
            switch (actionType)
            {
                case GameActionTypeEnum.FIGHT:
                    StopAction(GameActionTypeEnum.MAP);
                    break;

                case GameActionTypeEnum.FIGHT_WEAPON_USE:
                case GameActionTypeEnum.FIGHT_SPELL_LAUNCH:
                    Fight.Dispatch(WorldMessage.GAME_ACTION(CurrentAction.Type, Id, CurrentAction.SerializeAs_GameAction()));
                    break;

                case GameActionTypeEnum.MAP_MOVEMENT:
                    if (HasGameAction(GameActionTypeEnum.FIGHT))
                    {
                        if (StateManager.HasState(FighterStateEnum.STATE_STEALTH))
                        {
                            Team.Dispatch(WorldMessage.GAME_ACTION(actionType, Id, CurrentAction.SerializeAs_GameAction()));
                            return;
                        }
                    }
                    break;
            }

            base.StartAction(actionType);
        }

        public override void AbortAction(GameActionTypeEnum actionType, params object[] args)
        {
            switch (actionType)
            {
                case GameActionTypeEnum.FIGHT:
                    if (Fight != null)
                        Fight.FighterDisconnect(this);
                    break;
            }

            base.AbortAction(actionType, args);
        }

        public abstract override void SerializeAs_GameMapInformations(OperatorEnum operation, StringBuilder message);

        public int CompareTo(IFightObstacle obj)
        {
            return Priority.CompareTo(obj.Priority);
        }

        public override void Dispose()
        {
            Fight = null;
            Team = null;
            Cell = null;
            Invocator = null;

            if (SpellManager != null)
            {
                SpellManager.Dispose();
                SpellManager = null;
            }

            if (StateManager != null)
            {
                StateManager.Dispose();
                StateManager = null;
            }

            if (BuffManager != null)
            {
                BuffManager.Dispose();
                BuffManager = null;
            }

            base.Dispose();
        }
    }
}


