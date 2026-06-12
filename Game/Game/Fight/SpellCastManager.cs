using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Fight
{
    public sealed class SpellCastManager : IDisposable
    {
        private Dictionary<int, List<SpellTarget>> m_targets = new Dictionary<int, List<SpellTarget>>();
        private Dictionary<int, SpellCooldown> m_cooldowns = new Dictionary<int, SpellCooldown>();
        private Dictionary<int, int> m_gameCasts = new Dictionary<int, int>();

        public void Clear()
        {
            m_targets.Clear();
            m_cooldowns.Clear();
            m_gameCasts.Clear();
        }

        public bool CanLaunchSpell(SpellLevel spell, int spellId, long targetId)
        {
            if (spell.Cooldown > 0)
            {
                if (m_cooldowns.ContainsKey(spellId))
                {
                    if (m_cooldowns[spellId] != null)
                    {
                        if (m_cooldowns[spellId].Cooldown > 0)
                            return false;
                    }
                }
            }

            if (spell.MaxLaunchPerGame > 0)
            {
                if (m_gameCasts.TryGetValue(spellId, out int gameCasts) && gameCasts >= spell.MaxLaunchPerGame)
                    return false;
            }

            if (spell.MaxLaunchPerTurn == 0 && spell.MaxLaunchPerTarget == 0)
                return true;

            if (spell.MaxLaunchPerTurn > 0)
            {
                if (m_targets.ContainsKey(spellId))
                {
                    if (m_targets[spellId].Count >= spell.MaxLaunchPerTurn)
                        return false;
                }
            }

            if (targetId == 0)
                return true;

            if (spell.MaxLaunchPerTarget > 0)
            {
                if (m_targets.ContainsKey(spellId))
                {
                    if (m_targets[spellId].Count(spellTarget => spellTarget.TargetId == targetId) >= spell.MaxLaunchPerTarget)
                        return false;
                }
            }

            return true;
        }

        public void Actualize(SpellLevel spell, int spellId, long targetId)
        {
            if (spell.Cooldown > 0)
            {
                if (!m_cooldowns.ContainsKey(spellId))
                {
                    m_cooldowns.Add(spellId, new SpellCooldown(spell.Cooldown));
                }
                else
                {
                    m_cooldowns[spellId].Cooldown = spell.Cooldown;
                }
            }

            if (spell.MaxLaunchPerGame > 0)
            {
                if (!m_gameCasts.ContainsKey(spellId))
                    m_gameCasts[spellId] = 0;
                m_gameCasts[spellId]++;
            }

            if (spell.MaxLaunchPerTurn == 0 && spell.MaxLaunchPerTarget == 0)
                return;

            if (!m_targets.ContainsKey(spellId))
                m_targets.Add(spellId, new List<SpellTarget>());
            m_targets[spellId].Add(new SpellTarget(targetId));
        }

        public void EndTurn()
        {
            foreach (var target in m_targets.Values)
                target.Clear();

            foreach (var cooldown in m_cooldowns.Values)
                cooldown.Decrement();
        }

        public void Dispose()
        {
            m_targets.Clear();
            m_cooldowns.Clear();
            m_gameCasts.Clear();
            m_targets = null;
            m_cooldowns = null;
            m_gameCasts = null;
        }
    }

    public sealed class SpellCooldown
    {
        public int Cooldown
        {
            get;
            set;
        }

        public SpellCooldown(int cooldown)
        {
            Cooldown = cooldown;
        }

        public void Decrement()
        {
            Cooldown--;
        }
    }

    public sealed class SpellTarget
    {
        public long TargetId
        {
            get;
            set;
        }

        public SpellTarget(long targetId)
        {
            TargetId = targetId;
        }
    }
}


