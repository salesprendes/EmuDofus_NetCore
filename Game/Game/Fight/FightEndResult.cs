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
    public sealed class FightEndResult : IDisposable
    {
        public long FightId
        {
            get;
            private set;
        }

        public bool CanWinHonor
        {
            get;
            private set;
        }

        // Duracion del combate en milisegundos, mostrada en el panel de fin de combate.
        public long Duration
        {
            get;
            set;
        }

        // Bonus de estrellas del grupo de monstruos (solo PvM); -1 = no enviar.
        public int StarsBonus
        {
            get;
            set;
        } = -1;

        public string Message
        {
            get
            {
                // Primer campo: "<duracion>" o "<duracion>;<estrellas>" si hay bonus de grupo.
                var message = new StringBuilder("GE");
                message.Append(Duration);
                if (StarsBonus >= 0)
                {
                    message.Append(';').Append(StarsBonus);
                }
                message.Append('|').Append(m_fightId).Append('|').Append(CanWinHonor ? '1' : '0');
                message.Append(m_message);
                return message.ToString();
            }
        }

        private readonly long m_fightId;

        private StringBuilder m_message;

        private HashSet<long> m_resultFighterIds;

        public FightEndResult(long fightId, bool canWinHonor)
        {
            CanWinHonor = canWinHonor;
            m_fightId = fightId;

            // m_message acumula solo la parte de cada luchador (cada una empieza por '|').
            // La cabecera (con duracion y estrellas) se construye al leer Message.
            m_message = new StringBuilder();
            m_resultFighterIds = new HashSet<long>();
        }

        public bool HasResult(AbstractFighter fighter)
        {
            return fighter != null && m_resultFighterIds.Contains(fighter.Id);
        }

        public void AddResult(AbstractFighter fighter,
    FightEndTypeEnum type = FightEndTypeEnum.END_LOSER,
    bool leave = false,
    long kamas = 0,
    long exp = 0,
    long honour = 0,
    long dishonour = 0,
    long guildxp = 0,
    long mountxp = 0,
    Dictionary<int, int> items = null)
        {
            if (fighter == null)
            {
                return;
            }

            m_resultFighterIds.Add(fighter.Id);

            m_message.Append('|').Append((int)type).Append(';');
            m_message.Append(fighter.Id).Append(';');
            m_message.Append(fighter.Name).Append(';');
            m_message.Append(fighter.Level).Append(';');
            m_message.Append((fighter.IsFighterDead || leave) ? '1' : '0').Append(';');

            if (CanWinHonor)
            {
                switch (fighter.Type)
                {
                    case EntityTypeEnum.TYPE_CHARACTER:
                        CharacterEntity character = (CharacterEntity)fighter;
                        if (character.AlignmentId != (int)ConquestManager.AlignmentTypeEnum.ALIGNMENT_NEUTRAL)
                        {
                            m_message.Append(character.AlignmentExperienceFloorCurrent).Append(';');
                            m_message.Append(character.Honour).Append(';');
                            m_message.Append(character.AlignmentExperienceFloorNext).Append(';');
                            m_message.Append(honour).Append(';');
                            m_message.Append(character.AlignmentLevel).Append(';');
                            m_message.Append(character.Dishonour).Append(';');
                            m_message.Append(dishonour).Append(';');
                        }
                        else
                        {
                            m_message.Append("0;0;0;0;0;0;0;");
                        }
                        if (items != null && items.Count > 0)
                        {
                            m_message.Append(string.Join(",", items.Select(itemEntry => itemEntry.Key + "~" + itemEntry.Value))).Append(';');
                        }
                        else
                        {
                            m_message.Append("").Append(';');
                        }

                        m_message.Append(kamas > 0 ? kamas.ToString() : "").Append(';');
                        m_message.Append(character.ExperienceFloorCurrent).Append(';');
                        m_message.Append(character.Experience).Append(';');
                        m_message.Append(character.ExperienceFloorNext).Append(';');
                        m_message.Append(exp);
                        break;

                    case EntityTypeEnum.TYPE_PRISM:
                    case EntityTypeEnum.TYPE_MONSTER_FIGHTER:
                        m_message.Append("0;0;0;0;0;0;0;");
                        if (items != null && items.Count > 0)
                        {
                            m_message.Append(string.Join(",", items.Select(itemEntry => itemEntry.Key + "~" + itemEntry.Value))).Append(';');
                        }
                        else
                        {
                            m_message.Append("").Append(';');
                        }

                        m_message.Append(kamas > 0 ? kamas.ToString() : "").Append(';');
                        m_message.Append(0).Append(';');
                        m_message.Append(0).Append(';');
                        m_message.Append(0).Append(';');
                        m_message.Append(0);
                        break;
                }
            }
            else
            {
                switch (fighter.Type)
                {
                    case EntityTypeEnum.TYPE_CHARACTER:
                        var character = (CharacterEntity)fighter;
                        m_message.Append(character.ExperienceFloorCurrent).Append(';');
                        m_message.Append(character.Experience).Append(';');
                        m_message.Append(character.ExperienceFloorNext).Append(';');
                        m_message.Append(exp).Append(';');
                        m_message.Append(guildxp).Append(';');
                        m_message.Append(mountxp).Append(';');
                        break;

                    case EntityTypeEnum.TYPE_TAX_COLLECTOR:
                        var taxCollector = (TaxCollectorEntity)fighter;
                        m_message.Append(taxCollector.Level).Append(';');
                        m_message.Append("").Append(';');
                        m_message.Append("").Append(';');
                        m_message.Append("").Append(';');
                        m_message.Append(guildxp).Append(';');
                        m_message.Append("").Append(';');
                        break;

                    default:
                        m_message.Append(";;;;;;");
                        break;
                }

                if (items != null && items.Count > 0)
                {
                    m_message.Append(string.Join(",", items.Select(itemEntry => itemEntry.Key + "~" + itemEntry.Value))).Append(';');
                }
                else
                {
                    m_message.Append("").Append(';');
                }

                m_message.Append(kamas > 0 ? kamas.ToString() : "");
            }
        }

        public void Dispose()
        {
            m_message.Clear();
            m_message = null;
            m_resultFighterIds.Clear();
            m_resultFighterIds = null;
        }
    }
}
