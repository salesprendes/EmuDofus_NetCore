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
    /// <summary>
    /// 
    /// </summary>
    public sealed class SpellBook : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        private const string SPELL_POSITION_CHAR = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";

        /// <summary>
        /// 
        /// </summary>
        public bool Empty => m_monsterSpellById != null
            ? m_monsterSpellById.Count == 0
            : (m_spellById?.Count ?? 0) == 0;

        /// <summary>
        /// 
        /// </summary>
        // Only meaningful for characters — monsters return an empty list.
        public List<SpellBookEntryDAO> Spells => m_spellById?.Values.ToList() ?? new List<SpellBookEntryDAO>();

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<int, SpellBookEntryDAO> m_spellById;
        private Dictionary<int, MonsterSpellDAO> m_monsterSpellById;
        private long m_entityId;
        private int m_entityType;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spellEntries"></param>
        public SpellBook(int type, long id)
        {
            m_entityType = type;
            m_entityId = id;
            Initialize();
        }

        /// <summary>
        /// 
        /// </summary>
        public void Initialize()
        {
            if (m_entityType == (int)EntityTypeEnum.TYPE_MONSTER_FIGHTER)
            {
                int monsterId   = (int)(m_entityId >> 32);
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

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            m_spellById?.Clear();
            m_spellById = null;
            m_monsterSpellById?.Clear();
            m_monsterSpellById = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spellId"></param>
        /// <returns></returns>
        public bool HasSpell(int spellId)
        {
            if (m_monsterSpellById != null)
                return m_monsterSpellById.ContainsKey(spellId);
            return m_spellById.ContainsKey(spellId);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public void AddSpell(int spellId, int level = 1, int position = 25)
        {
            if (!HasSpell(spellId))
            {
                var spellBookEntry = SpellBookEntryRepository.Instance.Create(m_entityType, m_entityId, spellId,  level, position);
                if(spellBookEntry != null)
                    m_spellById.Add(spellId, spellBookEntry);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool LevelUp(int spellId)
        {
            if (HasSpell(spellId))
            {
                m_spellById[spellId].Level++;

                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Reset(CharacterBreedEnum breed)
        {
            SpellBookEntryRepository.Instance.RemoveAll(m_entityType, m_entityId);
            SpellBookEntryRepository.Instance.GenerateForBreed(m_entityId, breed);
            Initialize();
        }

        /// <summary>
        /// 
        /// </summary>
        private static SpellLevel m_basicFist = SpellManager.Instance.GetSpellLevel(0, 1);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<SpellLevel> GetSpells()
        {
            if (m_monsterSpellById != null)
                return m_monsterSpellById.Values
                    .Select(ms => ms.CombatLevel)
                    .Where(sl => sl != null);
            return m_spellById.Values.Select(entry => entry.SpellLevel);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="breed"></param>
        /// <param name="level"></param>
        public void GenerateLevelUpSpell(CharacterBreedEnum breed, int level)
        {
            switch (breed)
            {
                case CharacterBreedEnum.BREED_FECA:
                    if (level == 3)
                        AddSpell(4);//Renvoie de sort
                    if (level == 6)
                        AddSpell(2);//Aveuglement
                    if (level == 9)
                        AddSpell(1);//Armure Incandescente
                    if (level == 13)
                        AddSpell(9);//Attaque nuageuse
                    if (level == 17)
                        AddSpell(18);//Armure Aqueuse
                    if (level == 21)
                        AddSpell(20);//Immunit�
                    if (level == 26)
                        AddSpell(14);//Armure Venteuse
                    if (level == 31)
                        AddSpell(19);//Bulle
                    if (level == 36)
                        AddSpell(5);//Tr�ve
                    if (level == 42)
                        AddSpell(16);//Science du b�ton
                    if (level == 48)
                        AddSpell(8);//Retour du b�ton
                    if (level == 54)
                        AddSpell(12);//glyphe d'Aveuglement
                    if (level == 60)
                        AddSpell(11);//T�l�portation
                    if (level == 70)
                        AddSpell(10);//Glyphe Enflamm�
                    if (level == 80)
                        AddSpell(7);//Bouclier F�ca
                    if (level == 90)
                        AddSpell(15);//Glyphe d'Immobilisation
                    if (level == 100)
                        AddSpell(13);//Glyphe de Silence
                    if (level == 200)
                        AddSpell(1901);//Invocation de Dopeul F�ca
                    break;

                case CharacterBreedEnum.BREED_OSAMODAS:
                    if (level == 3)
                        AddSpell(26);//B�n�diction Animale
                    if (level == 6)
                        AddSpell(22);//D�placement F�lin
                    if (level == 9)
                        AddSpell(35);//Invocation de Bouftou
                    if (level == 13)
                        AddSpell(28);//Crapaud
                    if (level == 17)
                        AddSpell(37);//Invocation de Prespic
                    if (level == 21)
                        AddSpell(30);//Fouet
                    if (level == 26)
                        AddSpell(27);//Piq�re Motivante
                    if (level == 31)
                        AddSpell(24);//Corbeau
                    if (level == 36)
                        AddSpell(33);//Griffe Cinglante
                    if (level == 42)
                        AddSpell(25);//Soin Animal
                    if (level == 48)
                        AddSpell(38);//Invocation de Sanglier
                    if (level == 54)
                        AddSpell(36);//Frappe du Craqueleur
                    if (level == 60)
                        AddSpell(32);//R�sistance Naturelle
                    if (level == 70)
                        AddSpell(29);//Crocs du Mulou
                    if (level == 80)
                        AddSpell(39);//Invocation de Bwork Mage
                    if (level == 90)
                        AddSpell(40);//Invocation de Craqueleur
                    if (level == 100)
                        AddSpell(31);//Invocation de Dragonnet Rouge
                    if (level == 200)
                        AddSpell(1902);//Invocation de Dopeul Osamodas
                    break;

                case CharacterBreedEnum.BREED_ENUTROF:
                    if (level == 3)
                        AddSpell(49);//Pelle Fantomatique
                    if (level == 6)
                        AddSpell(42);//Chance
                    if (level == 9)
                        AddSpell(47);//Bo�te de Pandore
                    if (level == 13)
                        AddSpell(48);//Remblai
                    if (level == 17)
                        AddSpell(45);//Cl� R�ductrice
                    if (level == 21)
                        AddSpell(53);//Force de l'Age
                    if (level == 26)
                        AddSpell(46);//D�sinvocation
                    if (level == 31)
                        AddSpell(52);//Cupidit�
                    if (level == 36)
                        AddSpell(44);//Roulage de Pelle
                    if (level == 42)
                        AddSpell(50);//Maladresse
                    if (level == 48)
                        AddSpell(54);//Maladresse de Masse
                    if (level == 54)
                        AddSpell(55);//Acc�l�ration
                    if (level == 60)
                        AddSpell(56);//Pelle du Jugement
                    if (level == 70)
                        AddSpell(58);//Pelle Massacrante
                    if (level == 80)
                        AddSpell(59);//Corruption
                    if (level == 90)
                        AddSpell(57);//Pelle Anim�e
                    if (level == 100)
                        AddSpell(60);//Coffre Anim�
                    if (level == 200)
                        AddSpell(1903);//Invocation de Dopeul Enutrof
                    break;

                case CharacterBreedEnum.BREED_SRAM:
                    if (level == 3)
                        AddSpell(66);//Poison insidieux
                    if (level == 6)
                        AddSpell(68);//Fourvoiement
                    if (level == 9)
                        AddSpell(63);//Coup Sournois
                    if (level == 13)
                        AddSpell(74);//Double
                    if (level == 17)
                        AddSpell(64);//Rep�rage
                    if (level == 21)
                        AddSpell(79);//Pi�ge de Masse
                    if (level == 26)
                        AddSpell(78);//Invisibilit� d'Autrui
                    if (level == 31)
                        AddSpell(71);//Pi�ge Empoisonn�
                    if (level == 36)
                        AddSpell(62);//Concentration de Chakra
                    if (level == 42)
                        AddSpell(69);//Pi�ge d'Immobilisation
                    if (level == 48)
                        AddSpell(77);//Pi�ge de Silence
                    if (level == 54)
                        AddSpell(73);//Pi�ge r�pulsif
                    if (level == 60)
                        AddSpell(67);//Peur
                    if (level == 70)
                        AddSpell(70);//Arnaque
                    if (level == 80)
                        AddSpell(75);//Pulsion de Chakra
                    if (level == 90)
                        AddSpell(76);//Attaque Mortelle
                    if (level == 100)
                        AddSpell(80);//Pi�ge Mortel
                    if (level == 200)
                        AddSpell(1904);//Invocation de Dopeul Sram
                    break;

                case CharacterBreedEnum.BREED_XELOR:
                    if (level == 3)
                        AddSpell(84);//Gelure
                    if (level == 6)
                        AddSpell(100);//Sablier de X�lor
                    if (level == 9)
                        AddSpell(92);//Rayon Obscur
                    if (level == 13)
                        AddSpell(88);//T�l�portation
                    if (level == 17)
                        AddSpell(93);//Fl�trissement
                    if (level == 21)
                        AddSpell(85);//Flou
                    if (level == 26)
                        AddSpell(96);//Poussi�re Temporelle
                    if (level == 31)
                        AddSpell(98);//Vol du Temps
                    if (level == 36)
                        AddSpell(86);//Aiguille Chercheuse
                    if (level == 42)
                        AddSpell(89);//D�vouement
                    if (level == 48)
                        AddSpell(90);//Fuite
                    if (level == 54)
                        AddSpell(87);//D�motivation
                    if (level == 60)
                        AddSpell(94);//Protection Aveuglante
                    if (level == 70)
                        AddSpell(99);//Momification
                    if (level == 80)
                        AddSpell(95);//Horloge
                    if (level == 90)
                        AddSpell(91);//Frappe de X�lor
                    if (level == 100)
                        AddSpell(97);//Cadran de X�lor
                    if (level == 200)
                        AddSpell(1905);//Invocation de Dopeul X�lor
                    break;

                case CharacterBreedEnum.BREED_ECAFLIP:
                    if (level == 3)
                        AddSpell(109);//Bluff
                    if (level == 6)
                        AddSpell(113);//Perception
                    if (level == 9)
                        AddSpell(111);//Contrecoup
                    if (level == 13)
                        AddSpell(104);//Tr�fle
                    if (level == 17)
                        AddSpell(119);//Tout ou rien
                    if (level == 21)
                        AddSpell(101);//Roulette
                    if (level == 26)
                        AddSpell(107);//Topkaj
                    if (level == 31)
                        AddSpell(116);//Langue R�peuse
                    if (level == 36)
                        AddSpell(106);//Roue de la Fortune
                    if (level == 42)
                        AddSpell(117);//Griffe Invocatrice
                    if (level == 48)
                        AddSpell(108);//Esprit F�lin
                    if (level == 54)
                        AddSpell(115);//Odorat
                    if (level == 60)
                        AddSpell(118);//R�flexes
                    if (level == 70)
                        AddSpell(110);//Griffe Joueuse
                    if (level == 80)
                        AddSpell(112);//Griffe de Ceangal
                    if (level == 90)
                        AddSpell(114);//Rekop
                    if (level == 100)
                        AddSpell(120);//Destin d'Ecaflip
                    if (level == 200)
                        AddSpell(1906);//Invocation de Dopeul Ecaflip
                    break;

                case CharacterBreedEnum.BREED_ENIRIPSA:
                    if (level == 3)
                        AddSpell(124);//Mot Soignant
                    if (level == 6)
                        AddSpell(122);//Mot Blessant
                    if (level == 9)
                        AddSpell(126);//Mot Stimulant
                    if (level == 13)
                        AddSpell(127);//Mot de Pr�vention
                    if (level == 17)
                        AddSpell(123);//Mot Drainant
                    if (level == 21)
                        AddSpell(130);//Mot Revitalisant
                    if (level == 26)
                        AddSpell(131);//Mot de R�g�n�ration
                    if (level == 31)
                        AddSpell(132);//Mot d'Epine
                    if (level == 36)
                        AddSpell(133);//Mot de Jouvence
                    if (level == 42)
                        AddSpell(134);//Mot Vampirique
                    if (level == 48)
                        AddSpell(135);//Mot de Sacrifice
                    if (level == 54)
                        AddSpell(129);//Mot d'Amiti�
                    if (level == 60)
                        AddSpell(136);//Mot d'Immobilisation
                    if (level == 70)
                        AddSpell(137);//Mot d'Envol
                    if (level == 80)
                        AddSpell(138);//Mot de Silence
                    if (level == 90)
                        AddSpell(139);//Mot d'Altruisme
                    if (level == 100)
                        AddSpell(140);//Mot de Reconstitution
                    if (level == 200)
                        AddSpell(1907);//Invocation de Dopeul Eniripsa
                    break;

                case CharacterBreedEnum.BREED_IOP:
                    if (level == 3)
                        AddSpell(144);//Compulsion
                    if (level == 6)
                        AddSpell(145);//Ep�e Divine
                    if (level == 9)
                        AddSpell(146);//Ep�e du Destin
                    if (level == 13)
                        AddSpell(147);//Guide de Bravoure
                    if (level == 17)
                        AddSpell(148);//Amplification
                    if (level == 21)
                        AddSpell(154);//Ep�e Destructrice
                    if (level == 26)
                        AddSpell(150);//Couper
                    if (level == 31)
                        AddSpell(151);//Souffle
                    if (level == 36)
                        AddSpell(155);//Vitalit�
                    if (level == 42)
                        AddSpell(152);//Ep�e du Jugement
                    if (level == 48)
                        AddSpell(153);//Puissance
                    if (level == 54)
                        AddSpell(149);//Mutilation
                    if (level == 60)
                        AddSpell(156);//Temp�te de Puissance
                    if (level == 70)
                        AddSpell(157);//Ep�e C�leste
                    if (level == 80)
                        AddSpell(158);//Concentration
                    if (level == 90)
                        AddSpell(160);//Ep�e de Iop
                    if (level == 100)
                        AddSpell(159);//Col�re de Iop
                    if (level == 200)
                        AddSpell(1908);//Invocation de Dopeul Iop
                    break;

                case CharacterBreedEnum.BREED_CRA:
                    if (level == 3)
                        AddSpell(163);//Fl�che Glac�e
                    if (level == 6)
                        AddSpell(165);//Fl�che enflamm�e
                    if (level == 9)
                        AddSpell(172);//Tir Eloign�
                    if (level == 13)
                        AddSpell(167);//Fl�che d'Expiation
                    if (level == 17)
                        AddSpell(168);//Oeil de Taupe
                    if (level == 21)
                        AddSpell(162);//Tir Critique
                    if (level == 26)
                        AddSpell(170);//Fl�che d'Immobilisation
                    if (level == 31)
                        AddSpell(171);//Fl�che Punitive
                    if (level == 36)
                        AddSpell(166);//Tir Puissant
                    if (level == 42)
                        AddSpell(173);//Fl�che Harcelante
                    if (level == 48)
                        AddSpell(174);//Fl�che Cinglante
                    if (level == 54)
                        AddSpell(176);//Fl�che Pers�cutrice
                    if (level == 60)
                        AddSpell(175);//Fl�che Destructrice
                    if (level == 70)
                        AddSpell(178);//Fl�che Absorbante
                    if (level == 80)
                        AddSpell(177);//Fl�che Ralentissante
                    if (level == 90)
                        AddSpell(179);//Fl�che Explosive
                    if (level == 100)
                        AddSpell(180);//Ma�trise de l'Arc
                    if (level == 200)
                        AddSpell(1909);//Invocation de Dopeul Cra
                    break;

                case CharacterBreedEnum.BREED_SADIDAS:
                    if (level == 3)
                        AddSpell(198);//Sacrifice Poupesque
                    if (level == 6)
                        AddSpell(195);//Larme
                    if (level == 9)
                        AddSpell(182);//Invocation de la Folle
                    if (level == 13)
                        AddSpell(192);//Ronce Apaisante
                    if (level == 17)
                        AddSpell(197);//Puissance Sylvestre
                    if (level == 21)
                        AddSpell(189);//Invocation de la Sacrifi�e
                    if (level == 26)
                        AddSpell(181);//Tremblement
                    if (level == 31)
                        AddSpell(199);//Connaissance des Poup�es
                    if (level == 36)
                        AddSpell(191);//Ronce Multiples
                    if (level == 42)
                        AddSpell(186);//Arbre
                    if (level == 48)
                        AddSpell(196);//Vent Empoisonn�
                    if (level == 54)
                        AddSpell(190);//Invocation de la Gonflable
                    if (level == 60)
                        AddSpell(194);//Ronces Agressives
                    if (level == 70)
                        AddSpell(185);//Herbe Folle
                    if (level == 80)
                        AddSpell(184);//Feu de Brousse
                    if (level == 90)
                        AddSpell(188);//Ronce Insolente
                    if (level == 100)
                        AddSpell(187);//Invocation de la Surpuissante
                    if (level == 200)
                        AddSpell(1910);//Invocation de Dopeul Sadida
                    break;

                case CharacterBreedEnum.BREED_SACRIEUR:
                    if (level == 3)
                        AddSpell(444);//D�robade
                    if (level == 6)
                        AddSpell(449);//D�tour
                    if (level == 9)
                        AddSpell(436);//Assaut
                    if (level == 13)
                        AddSpell(437);//Ch�timent Agile
                    if (level == 17)
                        AddSpell(439);//Dissolution
                    if (level == 21)
                        AddSpell(433);//Ch�timent Os�
                    if (level == 26)
                        AddSpell(443);//Ch�timent Spirituel
                    if (level == 31)
                        AddSpell(440);//Sacrifice
                    if (level == 36)
                        AddSpell(442);//Absorption
                    if (level == 42)
                        AddSpell(441);//Ch�timent Vilatesque
                    if (level == 48)
                        AddSpell(445);//Coop�ration
                    if (level == 54)
                        AddSpell(438);//Transposition
                    if (level == 60)
                        AddSpell(446);//Punition
                    if (level == 70)
                        AddSpell(447);//Furie
                    if (level == 80)
                        AddSpell(448);//Ep�e Volante
                    if (level == 90)
                        AddSpell(435);//Tansfert de Vie
                    if (level == 100)
                        AddSpell(450);//Folie Sanguinaire
                    if (level == 200)
                        AddSpell(1911);//Invocation de Dopeul Sacrieur
                    break;

                case CharacterBreedEnum.BREED_PANDAWA:
                    if (level == 3)
                        AddSpell(689);//Epouvante
                    if (level == 6)
                        AddSpell(690);//Souffle Alcoolis�
                    if (level == 9)
                        AddSpell(691);//Vuln�rabilit� Aqueuse
                    if (level == 13)
                        AddSpell(688);//Vuln�rabilit� Incandescente
                    if (level == 17)
                        AddSpell(693);//Karcham
                    if (level == 21)
                        AddSpell(694);//Vuln�rabilit� Venteuse
                    if (level == 26)
                        AddSpell(695);//Stabilisation
                    if (level == 31)
                        AddSpell(696);//Chamrak
                    if (level == 36)
                        AddSpell(697);//Vuln�rabilit� Terrestre
                    if (level == 42)
                        AddSpell(698);//Souillure
                    if (level == 48)
                        AddSpell(699);//Lait de Bambou
                    if (level == 54)
                        AddSpell(700);//Vague � Lame
                    if (level == 60)
                        AddSpell(701);//Col�re de Zato�shwan
                    if (level == 70)
                        AddSpell(702);//Flasque Explosive
                    if (level == 80)
                        AddSpell(703);//Pandatak
                    if (level == 90)
                        AddSpell(704);//Pandanlku
                    if (level == 100)
                        AddSpell(705);//Lien Spiritueux
                    if (level == 200)
                        AddSpell(1912);//Invocation de Dopeul Pandawa
                    break;
            }
        }
    }
}


