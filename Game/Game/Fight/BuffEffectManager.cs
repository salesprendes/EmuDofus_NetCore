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
                var stateBuff = buffList.Find(buff => buff.CastInfos.SubEffect == EffectEnum.AddState && buff.CastInfos.Value3 == state);
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
                var stealthBuff = buffList.Find(buff => buff.CastInfos.EffectType == EffectEnum.Stealth);
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
                var skinBuff = buffList.Find(buff => buff.CastInfos.EffectType == EffectEnum.ChangeSkin);
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

        public FightActionResultEnum BeginTurn()
        {
            var damage = 0;
            foreach (var buff in ActiveBuffs[ActiveType.ACTIVE_BEGINTURN].ToArray())
            {
                var result = buff.ApplyEffect(ref damage);
                if (result != FightActionResultEnum.RESULT_NOTHING)
                    return result;
            }

            foreach (var buff in DecrementBuffs[DecrementType.TYPE_BEGINTURN].ToArray())
            {
                if (buff.DecrementDuration() <= 0)
                {
                    DecrementBuffs[DecrementType.TYPE_BEGINTURN].Remove(buff);
                    var result = buff.RemoveEffect();
                    if (result != FightActionResultEnum.RESULT_NOTHING)
                        return result;
                }
            }

            foreach (var buffList in ActiveBuffs.Values)
                buffList.RemoveAll(buff => buff.DecrementType == DecrementType.TYPE_BEGINTURN && buff.Duration <= 0);

            return m_fighter.Fight.TryKillFighter(m_fighter, m_fighter);
        }

        public FightActionResultEnum EndTurn()
        {
            foreach (var buff in DecrementBuffs[DecrementType.TYPE_ENDTURN].ToArray())
            {
                if (buff.DecrementDuration() <= 0)
                {
                    DecrementBuffs[DecrementType.TYPE_ENDTURN].Remove(buff);
                    var result = buff.RemoveEffect();
                    if (result != FightActionResultEnum.RESULT_NOTHING)
                        return result;
                }
            }

            foreach (var buffList in ActiveBuffs.Values)
                buffList.RemoveAll(buff => buff.DecrementType == DecrementType.TYPE_ENDTURN && buff.Duration <= 0);

            var damage = 0;
            foreach (var buff in ActiveBuffs[ActiveType.ACTIVE_ENDTURN].ToArray())
            {
                var result = buff.ApplyEffect(ref damage);
                if (result != FightActionResultEnum.RESULT_NOTHING)
                    return result;
            }

            return m_fighter.Fight.TryKillFighter(m_fighter, m_fighter);
        }

        public FightActionResultEnum EndMove()
        {
            var damage = 0;
            foreach (var buff in ActiveBuffs[ActiveType.ACTIVE_ENDMOVE].ToArray())
            {
                var result = buff.ApplyEffect(ref damage);
                if (result != FightActionResultEnum.RESULT_NOTHING)
                    return result;
            }

            ActiveBuffs[ActiveType.ACTIVE_ENDMOVE].RemoveAll(buff => buff.DecrementType == DecrementType.TYPE_ENDMOVE && buff.Duration <= 0);

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


