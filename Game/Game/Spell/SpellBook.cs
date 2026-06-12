using Game.Database.Structure;
using Game.Database.Repository;
using Game.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Entity;

namespace Game.Spell
{
    public sealed class SpellBook : IDisposable
    {
        private const string SPELL_POSITION_CHAR = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";

        public bool Empty => m_monsterSpellById != null ? m_monsterSpellById.Count == 0 : (m_spellById?.Count ?? 0) == 0;


        public List<SpellBookEntryDAO> Spells => m_spellById?.Values.ToList() ?? new List<SpellBookEntryDAO>();

        private Dictionary<int, SpellBookEntryDAO> m_spellById;
        private Dictionary<int, MonsterSpellDAO> m_monsterSpellById;
        private long m_entityId;
        private int m_entityType;

        public SpellBook(int type, long id)
        {
            m_entityType = type;
            m_entityId = id;
            Initialize();
        }

        public void Initialize()
        {
            if (m_entityType == (int)EntityTypeEnum.TYPE_MONSTER_FIGHTER)
            {
                int monsterId = (int)(m_entityId >> 32);
                int gradeNumber = (int)(m_entityId & 0xFFFF_FFFF);
                m_monsterSpellById = new Dictionary<int, MonsterSpellDAO>();
                foreach (var spell in MonsterSpellRepository.Instance.GetByMonsterAndGrade(monsterId, gradeNumber))
                    m_monsterSpellById[spell.SpellId] = spell;
            }
            else
            {
                m_spellById = new Dictionary<int, SpellBookEntryDAO>();
                foreach (var spellEntry in SpellBookEntryRepository.Instance.GetSpellEntries(m_entityType, m_entityId))
                    if (!m_spellById.ContainsKey(spellEntry.SpellId))
                        m_spellById.Add(spellEntry.SpellId, spellEntry);
                    else
                        m_spellById[spellEntry.SpellId] = spellEntry;
            }
        }

        public void Dispose()
        {
            m_spellById?.Clear();
            m_spellById = null;
            m_monsterSpellById?.Clear();
            m_monsterSpellById = null;
        }

        public bool HasSpell(int spellId)
        {
            if (m_monsterSpellById != null)
                return m_monsterSpellById.ContainsKey(spellId);
            return m_spellById.ContainsKey(spellId);
        }

        public void AddSpell(int spellId, int level = 1, int position = 25)
        {
            if (!HasSpell(spellId))
            {
                var spellBookEntry = SpellBookEntryRepository.Instance.Create(m_entityType, m_entityId, spellId, level, position);
                if (spellBookEntry != null)
                    m_spellById.Add(spellId, spellBookEntry);
            }
        }

        public bool LevelUp(int spellId)
        {
            if (HasSpell(spellId))
            {
                m_spellById[spellId].Level++;

                return true;
            }

            return false;
        }

        public void Reset(CharacterBreedEnum breed)
        {
            SpellBookEntryRepository.Instance.RemoveAll(m_entityType, m_entityId);
            SpellBookEntryRepository.Instance.GenerateForBreed(m_entityId, breed);
            Initialize();
        }

        private static SpellLevel m_basicFist = SpellManager.Instance.GetSpellLevel(0, 1);

        public IEnumerable<SpellLevel> GetSpells()
        {
            if (m_monsterSpellById != null)
                return m_monsterSpellById.Values.Select(ms => ms.CombatLevel).Where(sl => sl != null);
            return m_spellById.Values.Select(entry => entry.SpellLevel);
        }

        public SpellLevel GetSpellLevel(int spellId)
        {
            if (spellId == 0)
                return m_basicFist;

            if (m_monsterSpellById != null)
            {
                MonsterSpellDAO ms;
                return m_monsterSpellById.TryGetValue(spellId, out ms) ? ms.CombatLevel : null;
            }

            SpellBookEntryDAO entry;
            return m_spellById.TryGetValue(spellId, out entry) ? entry.SpellLevel : null;
        }

        public bool MoveSpell(int spellId, int position)
        {
            if (HasSpell(spellId))
            {
                foreach (var spell in m_spellById.Values)
                    if (spell.Position == position)
                        spell.Position = 25;
                m_spellById[spellId].Position = position;

                return true;
            }

            return false;
        }

        public void SerializeAs_SpellsListMessage(StringBuilder message)
        {
            foreach (var spellEntry in m_spellById.Values)
            {
                message.Append(spellEntry.SpellId);
                message.Append('~');
                message.Append(spellEntry.Level);
                message.Append('~');
                message.Append(SPELL_POSITION_CHAR[spellEntry.Position]);
                message.Append(';');
            }
        }

        public void GenerateLevelUpSpell(CharacterBreedEnum breed, int level)
        {
            switch (breed)
            {
                case CharacterBreedEnum.BREED_FECA:
                    if (level == 3)
                        AddSpell(4);
                    if (level == 6)
                        AddSpell(2);
                    if (level == 9)
                        AddSpell(1);
                    if (level == 13)
                        AddSpell(9);
                    if (level == 17)
                        AddSpell(18);
                    if (level == 21)
                        AddSpell(20);
                    if (level == 26)
                        AddSpell(14);
                    if (level == 31)
                        AddSpell(19);
                    if (level == 36)
                        AddSpell(5);
                    if (level == 42)
                        AddSpell(16);
                    if (level == 48)
                        AddSpell(8);
                    if (level == 54)
                        AddSpell(12);
                    if (level == 60)
                        AddSpell(11);
                    if (level == 70)
                        AddSpell(10);
                    if (level == 80)
                        AddSpell(7);
                    if (level == 90)
                        AddSpell(15);
                    if (level == 100)
                        AddSpell(13);
                    if (level == 200)
                        AddSpell(1901);
                    break;

                case CharacterBreedEnum.BREED_OSAMODAS:
                    if (level == 3)
                        AddSpell(26);
                    if (level == 6)
                        AddSpell(22);
                    if (level == 9)
                        AddSpell(35);
                    if (level == 13)
                        AddSpell(28);
                    if (level == 17)
                        AddSpell(37);
                    if (level == 21)
                        AddSpell(30);
                    if (level == 26)
                        AddSpell(27);
                    if (level == 31)
                        AddSpell(24);
                    if (level == 36)
                        AddSpell(33);
                    if (level == 42)
                        AddSpell(25);
                    if (level == 48)
                        AddSpell(38);
                    if (level == 54)
                        AddSpell(36);
                    if (level == 60)
                        AddSpell(32);
                    if (level == 70)
                        AddSpell(29);
                    if (level == 80)
                        AddSpell(39);
                    if (level == 90)
                        AddSpell(40);
                    if (level == 100)
                        AddSpell(31);
                    if (level == 200)
                        AddSpell(1902);
                    break;

                case CharacterBreedEnum.BREED_ENUTROF:
                    if (level == 3)
                        AddSpell(49);
                    if (level == 6)
                        AddSpell(42);
                    if (level == 9)
                        AddSpell(47);
                    if (level == 13)
                        AddSpell(48);
                    if (level == 17)
                        AddSpell(45);
                    if (level == 21)
                        AddSpell(53);
                    if (level == 26)
                        AddSpell(46);
                    if (level == 31)
                        AddSpell(52);
                    if (level == 36)
                        AddSpell(44);
                    if (level == 42)
                        AddSpell(50);
                    if (level == 48)
                        AddSpell(54);
                    if (level == 54)
                        AddSpell(55);
                    if (level == 60)
                        AddSpell(56);
                    if (level == 70)
                        AddSpell(58);
                    if (level == 80)
                        AddSpell(59);
                    if (level == 90)
                        AddSpell(57);
                    if (level == 100)
                        AddSpell(60);
                    if (level == 200)
                        AddSpell(1903);
                    break;

                case CharacterBreedEnum.BREED_SRAM:
                    if (level == 3)
                        AddSpell(66);
                    if (level == 6)
                        AddSpell(68);
                    if (level == 9)
                        AddSpell(63);
                    if (level == 13)
                        AddSpell(74);
                    if (level == 17)
                        AddSpell(64);
                    if (level == 21)
                        AddSpell(79);
                    if (level == 26)
                        AddSpell(78);
                    if (level == 31)
                        AddSpell(71);
                    if (level == 36)
                        AddSpell(62);
                    if (level == 42)
                        AddSpell(69);
                    if (level == 48)
                        AddSpell(77);
                    if (level == 54)
                        AddSpell(73);
                    if (level == 60)
                        AddSpell(67);
                    if (level == 70)
                        AddSpell(70);
                    if (level == 80)
                        AddSpell(75);
                    if (level == 90)
                        AddSpell(76);
                    if (level == 100)
                        AddSpell(80);
                    if (level == 200)
                        AddSpell(1904);
                    break;

                case CharacterBreedEnum.BREED_XELOR:
                    if (level == 3)
                        AddSpell(84);
                    if (level == 6)
                        AddSpell(100);
                    if (level == 9)
                        AddSpell(92);
                    if (level == 13)
                        AddSpell(88);
                    if (level == 17)
                        AddSpell(93);
                    if (level == 21)
                        AddSpell(85);
                    if (level == 26)
                        AddSpell(96);
                    if (level == 31)
                        AddSpell(98);
                    if (level == 36)
                        AddSpell(86);
                    if (level == 42)
                        AddSpell(89);
                    if (level == 48)
                        AddSpell(90);
                    if (level == 54)
                        AddSpell(87);
                    if (level == 60)
                        AddSpell(94);
                    if (level == 70)
                        AddSpell(99);
                    if (level == 80)
                        AddSpell(95);
                    if (level == 90)
                        AddSpell(91);
                    if (level == 100)
                        AddSpell(97);
                    if (level == 200)
                        AddSpell(1905);
                    break;

                case CharacterBreedEnum.BREED_ECAFLIP:
                    if (level == 3)
                        AddSpell(109);
                    if (level == 6)
                        AddSpell(113);
                    if (level == 9)
                        AddSpell(111);
                    if (level == 13)
                        AddSpell(104);
                    if (level == 17)
                        AddSpell(119);
                    if (level == 21)
                        AddSpell(101);
                    if (level == 26)
                        AddSpell(107);
                    if (level == 31)
                        AddSpell(116);
                    if (level == 36)
                        AddSpell(106);
                    if (level == 42)
                        AddSpell(117);
                    if (level == 48)
                        AddSpell(108);
                    if (level == 54)
                        AddSpell(115);
                    if (level == 60)
                        AddSpell(118);
                    if (level == 70)
                        AddSpell(110);
                    if (level == 80)
                        AddSpell(112);
                    if (level == 90)
                        AddSpell(114);
                    if (level == 100)
                        AddSpell(120);
                    if (level == 200)
                        AddSpell(1906);
                    break;

                case CharacterBreedEnum.BREED_ENIRIPSA:
                    if (level == 3)
                        AddSpell(124);
                    if (level == 6)
                        AddSpell(122);
                    if (level == 9)
                        AddSpell(126);
                    if (level == 13)
                        AddSpell(127);
                    if (level == 17)
                        AddSpell(123);
                    if (level == 21)
                        AddSpell(130);
                    if (level == 26)
                        AddSpell(131);
                    if (level == 31)
                        AddSpell(132);
                    if (level == 36)
                        AddSpell(133);
                    if (level == 42)
                        AddSpell(134);
                    if (level == 48)
                        AddSpell(135);
                    if (level == 54)
                        AddSpell(129);
                    if (level == 60)
                        AddSpell(136);
                    if (level == 70)
                        AddSpell(137);
                    if (level == 80)
                        AddSpell(138);
                    if (level == 90)
                        AddSpell(139);
                    if (level == 100)
                        AddSpell(140);
                    if (level == 200)
                        AddSpell(1907);
                    break;

                case CharacterBreedEnum.BREED_IOP:
                    if (level == 3)
                        AddSpell(144);
                    if (level == 6)
                        AddSpell(145);
                    if (level == 9)
                        AddSpell(146);
                    if (level == 13)
                        AddSpell(147);
                    if (level == 17)
                        AddSpell(148);
                    if (level == 21)
                        AddSpell(154);
                    if (level == 26)
                        AddSpell(150);
                    if (level == 31)
                        AddSpell(151);
                    if (level == 36)
                        AddSpell(155);
                    if (level == 42)
                        AddSpell(152);
                    if (level == 48)
                        AddSpell(153);
                    if (level == 54)
                        AddSpell(149);
                    if (level == 60)
                        AddSpell(156);
                    if (level == 70)
                        AddSpell(157);
                    if (level == 80)
                        AddSpell(158);
                    if (level == 90)
                        AddSpell(160);
                    if (level == 100)
                        AddSpell(159);
                    if (level == 200)
                        AddSpell(1908);
                    break;

                case CharacterBreedEnum.BREED_CRA:
                    if (level == 3)
                        AddSpell(163);
                    if (level == 6)
                        AddSpell(165);
                    if (level == 9)
                        AddSpell(172);
                    if (level == 13)
                        AddSpell(167);
                    if (level == 17)
                        AddSpell(168);
                    if (level == 21)
                        AddSpell(162);
                    if (level == 26)
                        AddSpell(170);
                    if (level == 31)
                        AddSpell(171);
                    if (level == 36)
                        AddSpell(166);
                    if (level == 42)
                        AddSpell(173);
                    if (level == 48)
                        AddSpell(174);
                    if (level == 54)
                        AddSpell(176);
                    if (level == 60)
                        AddSpell(175);
                    if (level == 70)
                        AddSpell(178);
                    if (level == 80)
                        AddSpell(177);
                    if (level == 90)
                        AddSpell(179);
                    if (level == 100)
                        AddSpell(180);
                    if (level == 200)
                        AddSpell(1909);
                    break;

                case CharacterBreedEnum.BREED_SADIDAS:
                    if (level == 3)
                        AddSpell(198);
                    if (level == 6)
                        AddSpell(195);
                    if (level == 9)
                        AddSpell(182);
                    if (level == 13)
                        AddSpell(192);
                    if (level == 17)
                        AddSpell(197);
                    if (level == 21)
                        AddSpell(189);
                    if (level == 26)
                        AddSpell(181);
                    if (level == 31)
                        AddSpell(199);
                    if (level == 36)
                        AddSpell(191);
                    if (level == 42)
                        AddSpell(186);
                    if (level == 48)
                        AddSpell(196);
                    if (level == 54)
                        AddSpell(190);
                    if (level == 60)
                        AddSpell(194);
                    if (level == 70)
                        AddSpell(185);
                    if (level == 80)
                        AddSpell(184);
                    if (level == 90)
                        AddSpell(188);
                    if (level == 100)
                        AddSpell(187);
                    if (level == 200)
                        AddSpell(1910);
                    break;

                case CharacterBreedEnum.BREED_SACRIEUR:
                    if (level == 3)
                        AddSpell(444);
                    if (level == 6)
                        AddSpell(449);
                    if (level == 9)
                        AddSpell(436);
                    if (level == 13)
                        AddSpell(437);
                    if (level == 17)
                        AddSpell(439);
                    if (level == 21)
                        AddSpell(433);
                    if (level == 26)
                        AddSpell(443);
                    if (level == 31)
                        AddSpell(440);
                    if (level == 36)
                        AddSpell(442);
                    if (level == 42)
                        AddSpell(441);
                    if (level == 48)
                        AddSpell(445);
                    if (level == 54)
                        AddSpell(438);
                    if (level == 60)
                        AddSpell(446);
                    if (level == 70)
                        AddSpell(447);
                    if (level == 80)
                        AddSpell(448);
                    if (level == 90)
                        AddSpell(435);
                    if (level == 100)
                        AddSpell(450);
                    if (level == 200)
                        AddSpell(1911);
                    break;

                case CharacterBreedEnum.BREED_PANDAWA:
                    if (level == 3)
                        AddSpell(689);
                    if (level == 6)
                        AddSpell(690);
                    if (level == 9)
                        AddSpell(691);
                    if (level == 13)
                        AddSpell(688);
                    if (level == 17)
                        AddSpell(693);
                    if (level == 21)
                        AddSpell(694);
                    if (level == 26)
                        AddSpell(695);
                    if (level == 31)
                        AddSpell(696);
                    if (level == 36)
                        AddSpell(697);
                    if (level == 42)
                        AddSpell(698);
                    if (level == 48)
                        AddSpell(699);
                    if (level == 54)
                        AddSpell(700);
                    if (level == 60)
                        AddSpell(701);
                    if (level == 70)
                        AddSpell(702);
                    if (level == 80)
                        AddSpell(703);
                    if (level == 90)
                        AddSpell(704);
                    if (level == 100)
                        AddSpell(705);
                    if (level == 200)
                        AddSpell(1912);
                    break;
            }
        }
    }
}


