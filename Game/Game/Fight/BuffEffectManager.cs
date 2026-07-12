using Game.Fight.Effect;
using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight
{
    public sealed class BuffEffectManager
    {
        private readonly AbstractFighter m_fighter;

        private Dictionary<ActiveType, List<AbstractSpellBuff>> ActiveBuffs = new Dictionary<ActiveType, List<AbstractSpellBuff>>()
        {
            { ActiveType.ACTIVE_ATTACKED_AFTER_JET, new List<AbstractSpellBuff>() },
            { ActiveType.ACTIVE_ATTACKED_BEFORE_JET, new List<AbstractSpellBuff>() },
            { ActiveType.ACTIVE_ATTACK_AFTER_JET, new List<AbstractSpellBuff>() },
            { ActiveType.ACTIVE_ATTACK_BEFORE_JET, new List<AbstractSpellBuff>() },
            { ActiveType.ACTIVE_BEGINTURN, new List<AbstractSpellBuff>() },
            { ActiveType.ACTIVE_ENDTURN, new List<AbstractSpellBuff>() },
            { ActiveType.ACTIVE_ENDMOVE, new List<AbstractSpellBuff>() },
            { ActiveType.ACTIVE_STATS, new List<AbstractSpellBuff>() },
        };

        private Dictionary<DecrementType, List<AbstractSpellBuff>> DecrementBuffs = new Dictionary<DecrementType, List<AbstractSpellBuff>>()
        {
            { DecrementType.TYPE_BEGINTURN, new List<AbstractSpellBuff>()},
            { DecrementType.TYPE_ENDTURN, new List<AbstractSpellBuff>()},
            { DecrementType.TYPE_ENDMOVE, new List<AbstractSpellBuff>()},
        };

        public BuffEffectManager(AbstractFighter fighter)
        {
            m_fighter = fighter;
        }

        public void AddBuff(AbstractSpellBuff Buff)
        {
            ActiveBuffs[Buff.ActiveType].Add(Buff);
            DecrementBuffs[Buff.DecrementType].Add(Buff);
        }

        public void RemoveState(int state)
        {
            foreach (var buffList in ActiveBuffs.Values)
            {
                var stateBuff = buffList.Find(buff => buff.CastInfos.SubEffect == EffectEnum.ESTADO_MAS && buff.CastInfos.Value3 == state);
                if (stateBuff != null)
                {
                    stateBuff.RemoveEffect();

                    RemoveBuff(stateBuff);

                    return;
                }
            }
        }

        public void RemoveStealth()
        {
            foreach (var buffList in ActiveBuffs.Values)
            {
                var stealthBuff = buffList.Find(buff => buff.CastInfos.EffectType == EffectEnum.ESTADO_INVISIBILIDAD);
                if (stealthBuff != null)
                {
                    stealthBuff.RemoveEffect();

                    RemoveBuff(stealthBuff);

                    return;
                }
            }
        }

        public void RemoveSkin()
        {
            foreach (var buffList in ActiveBuffs.Values)
            {
                var skinBuff = buffList.Find(buff => buff.CastInfos.EffectType == EffectEnum.APARIENCIA_CAMBIAR);
                if (skinBuff != null)
                {
                    skinBuff.RemoveEffect();

                    RemoveBuff(skinBuff);

                    return;
                }
            }
        }

        public void RemoveBuff(AbstractSpellBuff buff)
        {
            ActiveBuffs[buff.ActiveType].Remove(buff);
            DecrementBuffs[buff.DecrementType].Remove(buff);
        }

        /// <summary>
        /// Retira las instancias previas de UN castigo concreto (y su erosión asociada, que
        /// comparte SpellId). Pueden convivir castigos distintos a la vez; relanzar el mismo
        /// lo refresca en lugar de apilarse consigo mismo (duplicaría la ganancia por golpe).
        /// </summary>
        public void RemovePunishment(int spellId)
        {
            foreach (var buffList in ActiveBuffs.Values)
            {
                foreach (var buff in buffList.ToArray())
                {
                    if (buff.CastInfos.SpellId != spellId)
                        continue;

                    if (buff is Effect.Type.PunishmentBuff || buff.CastInfos.EffectType == EffectEnum.CASTIGO_EROSION)
                    {
                        buff.RemoveEffect();
                        RemoveBuff(buff);
                    }
                }
            }
        }

        // Combina resultados sin perder señales: RESULT_END (fin de combate) siempre gana; en su
        // defecto se conserva el primer resultado significativo (p.ej. una muerte por veneno).
        private static FightActionResultEnum Combine(FightActionResultEnum current, FightActionResultEnum next)
        {
            if (current == FightActionResultEnum.RESULT_END || next == FightActionResultEnum.RESULT_END)
                return FightActionResultEnum.RESULT_END;
            return current != FightActionResultEnum.RESULT_NOTHING ? current : next;
        }

        // Aplica una lista ACTIVE. Deja de aplicar en cuanto un buff devuelve algo != NOTHING
        // (p.ej. el objetivo muere y no debe recibir más ticks), pero NO corta el decremento de
        // duraciones posterior: así los demás buffs siguen expirando en su turno correcto.
        private FightActionResultEnum ApplyActive(ActiveType type)
        {
            var damage = 0;
            foreach (var buff in ActiveBuffs[type].ToArray())
            {
                var result = buff.ApplyEffect(ref damage);
                if (result != FightActionResultEnum.RESULT_NOTHING)
                    return result;
            }
            return FightActionResultEnum.RESULT_NOTHING;
        }

        // Decrementa las duraciones de una lista y revierte los buffs expirados. Recorre siempre
        // toda la lista (salvo RESULT_END) para que ninguna duración se quede sin decrementar.
        private FightActionResultEnum DecrementExpire(DecrementType type)
        {
            var pending = FightActionResultEnum.RESULT_NOTHING;

            foreach (var buff in DecrementBuffs[type].ToArray())
            {
                if (buff.DecrementDuration() > 0)
                    continue;

                DecrementBuffs[type].Remove(buff);
                var result = buff.RemoveEffect();
                if (result == FightActionResultEnum.RESULT_END)
                    return FightActionResultEnum.RESULT_END;
                pending = Combine(pending, result);
            }

            foreach (var buffList in ActiveBuffs.Values)
                buffList.RemoveAll(buff => buff.DecrementType == type && buff.Duration <= 0);

            return pending;
        }

        public FightActionResultEnum BeginTurn()
        {
            var pending = ApplyActive(ActiveType.ACTIVE_BEGINTURN);
            if (pending == FightActionResultEnum.RESULT_END)
                return pending;

            pending = Combine(pending, DecrementExpire(DecrementType.TYPE_BEGINTURN));
            if (pending != FightActionResultEnum.RESULT_NOTHING)
                return pending;

            return m_fighter.Fight.TryKillFighter(m_fighter, m_fighter);
        }

        public FightActionResultEnum EndTurn()
        {
            // Aplicar ANTES de decrementar (espejo de BeginTurn): si no, un buff de duración N
            // expiraría antes de su último tick (y los de 1 turno no tickearían nunca).
            var pending = ApplyActive(ActiveType.ACTIVE_ENDTURN);
            if (pending == FightActionResultEnum.RESULT_END)
                return pending;

            pending = Combine(pending, DecrementExpire(DecrementType.TYPE_ENDTURN));
            if (pending != FightActionResultEnum.RESULT_NOTHING)
                return pending;

            return m_fighter.Fight.TryKillFighter(m_fighter, m_fighter);
        }

        public FightActionResultEnum EndMove()
        {
            var pending = ApplyActive(ActiveType.ACTIVE_ENDMOVE);
            if (pending == FightActionResultEnum.RESULT_END)
                return pending;

            // Mantener sincronizadas ambas listas al purgar buffs ENDMOVE expirados.
            ActiveBuffs[ActiveType.ACTIVE_ENDMOVE].RemoveAll(buff => buff.DecrementType == DecrementType.TYPE_ENDMOVE && buff.Duration <= 0);
            DecrementBuffs[DecrementType.TYPE_ENDMOVE].RemoveAll(buff => buff.Duration <= 0);

            if (pending != FightActionResultEnum.RESULT_NOTHING)
                return pending;

            return m_fighter.Fight.TryKillFighter(m_fighter, m_fighter);
        }

        public FightActionResultEnum OnAttackBeforeJet(CastInfos castInfos, ref int damageValue)
        {
            foreach (var buff in ActiveBuffs[ActiveType.ACTIVE_ATTACK_BEFORE_JET].ToArray())
            {
                var result = buff.ApplyEffect(ref damageValue, castInfos);
                if (result != FightActionResultEnum.RESULT_NOTHING)
                {
                    return result;
                }
            }

            return m_fighter.Fight.TryKillFighter(m_fighter, m_fighter);
        }

        public FightActionResultEnum OnAttackAfterJet(CastInfos castInfos, ref int damageValue)
        {
            foreach (var buff in ActiveBuffs[ActiveType.ACTIVE_ATTACK_AFTER_JET].ToArray())
            {
                var result = buff.ApplyEffect(ref damageValue, castInfos);
                if (result != FightActionResultEnum.RESULT_NOTHING)
                {
                    return result;
                }
            }

            return m_fighter.Fight.TryKillFighter(m_fighter, m_fighter);
        }

        public FightActionResultEnum OnAttackedBeforeJet(CastInfos castInfos, ref int damageValue)
        {
            foreach (var buff in ActiveBuffs[ActiveType.ACTIVE_ATTACKED_BEFORE_JET].ToArray())
            {
                var result = buff.ApplyEffect(ref damageValue, castInfos);
                if (result != FightActionResultEnum.RESULT_NOTHING)
                    return result;
            }

            return m_fighter.Fight.TryKillFighter(m_fighter, m_fighter);
        }

        public FightActionResultEnum OnAttackedAfterJet(CastInfos castInfos, ref int damageValue)
        {
            foreach (var buff in ActiveBuffs[ActiveType.ACTIVE_ATTACKED_AFTER_JET].ToArray())
            {
                var result = buff.ApplyEffect(ref damageValue, castInfos);
                if (result != FightActionResultEnum.RESULT_NOTHING)
                    return result;
            }

            return m_fighter.Fight.TryKillFighter(m_fighter, m_fighter);
        }

        public IEnumerable<AbstractSpellBuff> GetAllBuffs()
        {
            foreach (var buffList in ActiveBuffs.Values)
                foreach (var buff in buffList)
                    yield return buff;
        }

        public FightActionResultEnum Debuff()
        {
            foreach (var buff in DecrementBuffs[DecrementType.TYPE_BEGINTURN].ToArray())
                if (buff.IsDebuffable)
                {
                    var result = buff.RemoveEffect();
                    if (result != FightActionResultEnum.RESULT_NOTHING)
                        return result;
                }

            foreach (var buff in DecrementBuffs[DecrementType.TYPE_ENDTURN].ToArray())
                if (buff.IsDebuffable)
                {
                    var result = buff.RemoveEffect();
                    if (result != FightActionResultEnum.RESULT_NOTHING)
                        return result;
                }

            // Simétrico con las otras dos listas: sin esto, un futuro buff ENDMOVE debufeable se
            // borraría de la lista sin revertir su efecto.
            foreach (var buff in DecrementBuffs[DecrementType.TYPE_ENDMOVE].ToArray())
                if (buff.IsDebuffable)
                {
                    var result = buff.RemoveEffect();
                    if (result != FightActionResultEnum.RESULT_NOTHING)
                        return result;
                }

            foreach (var buffList in ActiveBuffs.Values)
                buffList.RemoveAll(buff => buff.IsDebuffable);
            foreach (var buffList in DecrementBuffs.Values)
                buffList.RemoveAll(buff => buff.IsDebuffable);

            return m_fighter.Fight.TryKillFighter(m_fighter, m_fighter);
        }

        public void Dispose()
        {
            DecrementBuffs[DecrementType.TYPE_BEGINTURN].Clear();
            DecrementBuffs[DecrementType.TYPE_ENDTURN].Clear();
            DecrementBuffs[DecrementType.TYPE_ENDMOVE].Clear();
            DecrementBuffs.Clear();
            DecrementBuffs = null;
            foreach (var activeBuffList in ActiveBuffs)
                activeBuffList.Value.Clear();
            ActiveBuffs.Clear();
            ActiveBuffs = null;
        }
    }
}


