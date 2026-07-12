using Game.Action;
using Game.Entity;
using Game.Map;
using Game.Network;
using Game.Spell;
using System;
using System.Collections.Generic;
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

        // GetTotal(STAT_MAS_ESQUIVA_PA/PM) ya incorpora sabiduría/4 (ver GenericStats.GetTotal),
        // así que NO se vuelve a sumar aquí; hacerlo contaba la sabiduría dos veces (esquiva doble).
        public int APDodge => Statistics.GetTotal(EffectEnum.STAT_MAS_ESQUIVA_PA);

        public int MPDodge => Statistics.GetTotal(EffectEnum.STAT_MAS_ESQUIVA_PM);

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

        // Valores de los buckets Boosts/Dons antes de entrar en combate, para restaurarlos al
        // salir sin borrar los boosts legítimos de objetos/caramelos ni dejar armaduras eternas.
        private Dictionary<EffectEnum, (int Boosts, int Dons)> m_preFightBuckets;

        public virtual void JoinFight(AbstractFight fight, FightTeam team)
        {
            m_preFightBuckets = Statistics.CaptureCombatBuckets();

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
            StealthSignalCell = -1;
            LastKnownStealthCell = -1;

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

                // Revierte exactamente las mutaciones de combate (buffs de stats en Boosts,
                // armaduras en Dons) preservando los boosts de objetos previos al combate.
                Statistics.RestoreCombatBuckets(m_preFightBuckets);
                m_preFightBuckets = null;
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

            // Nuevo turno: el marcador de posicion señalada caduca.
            ClearStealthSignal();

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

        // Característica que potencia cada elemento de daño/robo.
        private static readonly Dictionary<EffectEnum, EffectEnum> DamageStatByElement = new Dictionary<EffectEnum, EffectEnum>
        {
            { EffectEnum.DANO_TIERRA, EffectEnum.STAT_MAS_FUERZA },
            { EffectEnum.ROBO_VIDA_TIERRA, EffectEnum.STAT_MAS_FUERZA },
            { EffectEnum.DANO_NEUTRAL, EffectEnum.STAT_MAS_FUERZA },
            { EffectEnum.ROBO_VIDA_NEUTRAL, EffectEnum.STAT_MAS_FUERZA },
            { EffectEnum.DANO_FUEGO, EffectEnum.STAT_MAS_INTELIGENCIA },
            { EffectEnum.ROBO_VIDA_FUEGO, EffectEnum.STAT_MAS_INTELIGENCIA },
            { EffectEnum.DANO_AIRE, EffectEnum.STAT_MAS_AGILIDAD },
            { EffectEnum.ROBO_VIDA_AIRE, EffectEnum.STAT_MAS_AGILIDAD },
            { EffectEnum.DANO_AGUA, EffectEnum.STAT_MAS_SUERTE },
            { EffectEnum.ROBO_VIDA_AGUA, EffectEnum.STAT_MAS_SUERTE },
        };

        // Resistencias (porcentual, fija, y sus variantes PvP) de cada elemento.
        private static readonly Dictionary<EffectEnum, (EffectEnum Percent, EffectEnum Fixed, EffectEnum PvpPercent, EffectEnum PvpFixed)> ResistanceByElement = new Dictionary<EffectEnum, (EffectEnum, EffectEnum, EffectEnum, EffectEnum)>
        {
            { EffectEnum.DANO_NEUTRAL, (EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL, EffectEnum.STAT_MAS_RESISTENCIA_NEUTRAL, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_NEUTRAL, EffectEnum.STAT_MAS_RESISTENCIA_PVP_NEUTRAL) },
            { EffectEnum.ROBO_VIDA_NEUTRAL, (EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL, EffectEnum.STAT_MAS_RESISTENCIA_NEUTRAL, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_NEUTRAL, EffectEnum.STAT_MAS_RESISTENCIA_PVP_NEUTRAL) },
            { EffectEnum.DANO_TIERRA, (EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA, EffectEnum.STAT_MAS_RESISTENCIA_TIERRA, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_TIERRA, EffectEnum.STAT_MAS_RESISTENCIA_PVP_TIERRA) },
            { EffectEnum.ROBO_VIDA_TIERRA, (EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA, EffectEnum.STAT_MAS_RESISTENCIA_TIERRA, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_TIERRA, EffectEnum.STAT_MAS_RESISTENCIA_PVP_TIERRA) },
            { EffectEnum.DANO_FUEGO, (EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO, EffectEnum.STAT_MAS_RESISTENCIA_FUEGO, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_FUEGO, EffectEnum.STAT_MAS_RESISTENCIA_PVP_FUEGO) },
            { EffectEnum.ROBO_VIDA_FUEGO, (EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO, EffectEnum.STAT_MAS_RESISTENCIA_FUEGO, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_FUEGO, EffectEnum.STAT_MAS_RESISTENCIA_PVP_FUEGO) },
            { EffectEnum.DANO_AIRE, (EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE, EffectEnum.STAT_MAS_RESISTENCIA_AIRE, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_AIRE, EffectEnum.STAT_MAS_RESISTENCIA_PVP_AIRE) },
            { EffectEnum.ROBO_VIDA_AIRE, (EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE, EffectEnum.STAT_MAS_RESISTENCIA_AIRE, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_AIRE, EffectEnum.STAT_MAS_RESISTENCIA_PVP_AIRE) },
            { EffectEnum.DANO_AGUA, (EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA, EffectEnum.STAT_MAS_RESISTENCIA_AGUA, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_AGUA, EffectEnum.STAT_MAS_RESISTENCIA_PVP_AGUA) },
            { EffectEnum.ROBO_VIDA_AGUA, (EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA, EffectEnum.STAT_MAS_RESISTENCIA_AGUA, EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_PVP_AGUA, EffectEnum.STAT_MAS_RESISTENCIA_PVP_AGUA) },
        };

        public void CalculDamages(EffectEnum effect, ref int jet, bool isMelee = false)
        {
            if (!DamageStatByElement.TryGetValue(effect, out var elementStat))
                return;

            // "Físico" = golpes de arma y "mágico" = hechizos, independientemente del elemento
            // (antes se asignaba por elemento: un arco de aire nunca aprovechaba +daños físicos).
            var originDamage = isMelee ? EffectEnum.STAT_MAS_DANO_FISICO : EffectEnum.STAT_MAS_DANO_MAGICO;

            jet = (int)Math.Floor((double)jet * (100 + Statistics.GetTotal(elementStat) + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO_PORCENTAJE)) / 100
                + Statistics.GetTotal(originDamage)
                + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO)
                + Statistics.GetTotal(EffectEnum.STAT_MAS_DANO_BIS)
                - Statistics.GetTotal(EffectEnum.STAT_MENOS_DANO_FIJO)
                - Statistics.GetTotal(EffectEnum.DANO_SIN_BOOST));
        }

        public void CalculReduceDamages(EffectEnum effect, ref int damages, bool isMelee = false, bool versusPlayer = false)
        {
            if (!ResistanceByElement.TryGetValue(effect, out var resist))
                return;

            var percent = Statistics.GetTotal(resist.Percent);
            var fixedResist = Statistics.GetTotal(resist.Fixed);

            // Las resistencias PvP solo cuentan contra golpes de origen jugador (antes el equipo
            // de alineamiento reducía también el daño de los monstruos).
            if (versusPlayer)
            {
                percent += Statistics.GetTotal(resist.PvpPercent);
                fixedResist += Statistics.GetTotal(resist.PvpFixed);
            }

            var reduction = isMelee ? EffectEnum.STAT_MAS_REDUCCION_DANO_FISICO : EffectEnum.STAT_MAS_REDUCCION_DANO_MAGICO;

            var coef = damages * (100 - percent) / 100 - fixedResist;
            damages = (int)(coef - Statistics.GetTotal(reduction));
        }

        public void CalculHeal(ref int heal)
        {
            heal = (int)Math.Floor((double)heal * (100 + Statistics.GetTotal(EffectEnum.STAT_MAS_INTELIGENCIA)) / 100 + Statistics.GetTotal(EffectEnum.STAT_MAS_CURAS));

            // Con -curas mayor que el jet, la cura podría volverse negativa (daño encubierto).
            if (heal < 0)
                heal = 0;
        }

        public void CalculCriticalHitRate(ref int cHitRate)
        {
            // Fórmula del cliente 1.29 (GameManager.critique): la agilidad solo puede MEJORAR la
            // tasa (menor = más crítico), nunca empeorarla, y se acota a 0 para no producir
            // NaN/negativos con agilidad baja o negativa (antes: 50% de crítico garantizado).
            var agility = Math.Max(0, Statistics.GetTotal(EffectEnum.STAT_MAS_AGILIDAD));
            if (agility <= 0)
                return;

            var scaled = (int)(cHitRate * Math.E * 1.1 / Math.Log(agility + 12));
            if (scaled < cHitRate)
                cHitRate = scaled;
        }

        // Tirada de golpe crítico. baseRate es el CSRate del arma o del hechizo. Devuelve true si
        // sale crítico. Fórmula única (antes duplicada en TryUseWeapon y TryLaunchSpell): resta el
        // dominio de crítico, aplica la escala por agilidad y acota al mínimo configurado.
        public bool RollCriticalHit(int baseRate)
        {
            if (baseRate == 0)
                return false;

            var rate = baseRate - Math.Max(0, Statistics.GetTotal(EffectEnum.STAT_MAS_DANO_CRITICO));
            CalculCriticalHitRate(ref rate);

            if (rate < WorldConfig.FIGHT_CRITICAL_RATE_FLOOR)
                rate = WorldConfig.FIGHT_CRITICAL_RATE_FLOOR;

            return Util.Next(0, rate) == 0;
        }

        // Tirada de fallo crítico. baseRate es el CFRate del arma o el ECSRate del hechizo. El
        // mínimo de 2 evita el fallo garantizado (1/1). Devuelve true si sale fallo crítico.
        public bool RollCriticalFailure(int baseRate)
        {
            if (baseRate == 0)
                return false;

            var rate = baseRate - Statistics.GetTotal(EffectEnum.STAT_MAS_FALLO_CRITICO);
            if (rate < 2)
                rate = 2;

            return Util.Next(0, rate) == 0;
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

                    // Tope oficial 90% (antes, 90-100 se colaba sin recortar).
                    percentChance = Math.Clamp(percentChance, 10, 90);

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

                    // Tope oficial 90% (antes, 90-100 se colaba sin recortar).
                    percentChance = Math.Clamp(percentChance, 10, 90);

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

        public int StealthSignalCell { get; set; } = -1;
        public int LastKnownStealthCell { get; set; } = -1;
        public void ClearStealthSignal() => StealthSignalCell = -1;

        public FightActionResultEnum SetCell(FightCell cell)
        {
            if (IsFighterDead)
                return FightActionResultEnum.RESULT_DEATH;

            if (Cell != null)
            {
                if (Cell == cell)
                    return FightActionResultEnum.RESULT_NOTHING;

                ClearStealthSignal();

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


