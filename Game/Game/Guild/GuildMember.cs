using Game.Database.Structure;
using Game.Entity;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Guild
{
    public sealed class GuildMember : MessageDispatcher
    {
        static GuildRightEnum[] RIGHTS = (GuildRightEnum[])Enum.GetValues(typeof(GuildRightEnum));

        public long Id => m_character.Id;

        public long TaxCollectorJoinedId
        {
            get;
            set;
        }

        public CharacterEntity Character
        {
            get;
            private set;
        }

        public long GuildId
        {
            get
            {
                return m_character.Guild.GuildId;
            }
            set
            {
                m_character.Guild.GuildId = value;
            }
        }

        public string Name => m_character.Name;

        public long XPGiven
        {
            get
            {
                return m_character.Guild.XPGiven;
            }
            set
            {
                m_character.Guild.XPGiven = value;
            }
        }

        public int XPSharePercent
        {
            get
            {
                return m_character.Guild.XPSharePercent;
            }
            set
            {
                m_character.Guild.XPSharePercent = value;
            }
        }

        public int Power
        {
            get
            {
                return m_character.Guild.Power;
            }
            set
            {
                m_character.Guild.Power = value;
            }
        }

        public GuildRankEnum Rank
        {
            get
            {
                return (GuildRankEnum)m_character.Guild.Rank;
            }
            set
            {
                m_character.Guild.Rank = (int)value;
            }
        }

        public GuildInstance Guild
        {
            get;
            private set;
        }

        private readonly CharacterDAO m_character;

        public GuildMember(GuildInstance guild, CharacterDAO character)
        {
            m_character = character;
            TaxCollectorJoinedId = -1;
            Guild = guild;
        }

        public void MemberProfilUpdate(long profilId, int rank, int percent, int power)
        {
            Guild.AddMessage(() => { Guild.MemberProfilUpdate(this, profilId, rank, percent, power); });
        }

        public void MemberKick(string kickedMemberName)
        {
            Guild.AddMessage(() => { Guild.MemberKick(this, kickedMemberName); });
        }

        public void MemberKick(ReadOnlySpan<char> kickedMemberName)
        {
            MemberKick(kickedMemberName.ToString());
        }

        public void HireTaxCollector()
        {
            Guild.HireTaxCollector(this);
        }

        public void BoostGuildStats(char statId)
        {
            Guild.AddMessage(() => { Guild.BoostStats(this, statId); });
        }

        public void BoostGuildSpell(int spellId)
        {
            Guild.AddMessage(() => { Guild.BoostSpell(this, spellId); });
        }

        public void CharacterConnected(CharacterEntity character)
        {
            AddHandler(character.SafeDispatch);
            Character = character;
            Character.SetCharacterGuild(this);
        }

        public void MemberKick()
        {
            Guild.AddMessage(() => { Guild.MemberKick(this, Name); });
        }

        public void SetBoss()
        {
            Rank = GuildRankEnum.BOSS;
            foreach (var right in RIGHTS)
                SetRight(right, true);
        }

        public void GuildLeave()
        {
            GuildId = -1;
            XPGiven = 0;
            XPSharePercent = 0;
            Power = 0;
            if (Character != null)
            {
                Character.SetCharacterGuild(null);
                Character.RefreshOnMap();

                CharacterDisconnected();
            }
        }

        public void CharacterDisconnected()
        {
            if (Character != null)
                RemoveHandler(Character.SafeDispatch);
            Character = null;
        }

        public void SendGuildStats()
        {
            Dispatch(WorldMessage.GUILD_STATS(Guild, Power));
        }

        public bool HasRight(GuildRightEnum right)
        {
            return (Power & (int)right) == (int)right;
        }

        public void SetRight(GuildRightEnum right, bool value)
        {
            if (value)
                Power = Power | (int)right;
            else
                Power = Power ^ (int)right;
        }

        public void SendHasNotEnoughRights()
        {
            Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_GUILD_NOT_ENOUGH_RIGHTS));
        }

        public void SendMembersInformations()
        {
            Guild.AddMessage(() => { Guild.SendMembersInformations(this); });
        }

        public void SendBoostInformations()
        {
            Guild.AddMessage(() => { Guild.SendBoostInformations(this); });
        }

        public void TaxCollectorsInterfaceJoin()
        {
            Guild.AddMessage(() => { Guild.AddTaxCollectorListener(this); });
        }

        public void TaxCollectorsInterfaceLeave()
        {
            Guild.AddMessage(() => { Guild.RemoveTaxCollectorListener(this); });
        }

        public void RemoveTaxCollector(TaxCollectorEntity taxCollector)
        {
            Guild.AddMessage(() => { Guild.RemoveTaxCollector(this, taxCollector); });
        }

        public void FarmTaxCollector(TaxCollectorEntity taxCollector)
        {
            Guild.FarmTaxCollector(this, taxCollector);
        }

        public void SendTaxCollectorsList()
        {
            Guild.AddMessage(() => { Guild.SendTaxCollectorsList(this); });
        }

        public void SendGeneralInformations()
        {
            Guild.AddMessage(() => { Guild.SendGeneralInformations(this); });
        }

        public void TaxCollectorJoin(long id)
        {
            Guild.AddMessage(() => { Guild.TaxCollectorJoin(this, id); });
        }

        public void TaxCollectorLeave()
        {
            Guild.AddMessage(() => { Guild.TaxCollectorLeave(this); });
        }

        public void SerializeAs_GuildMemberInformations(StringBuilder message)
        {
            message.Append(m_character.Id).Append(";");
            message.Append(m_character.Name).Append(";");
            message.Append(m_character.Level).Append(";");
            message.Append(m_character.Skin).Append(";");
            message.Append((int)Rank).Append(";");
            message.Append(XPGiven).Append(";");
            message.Append(XPSharePercent).Append(";");
            message.Append(Power).Append(";");
            if (Character != null)
                message.Append("1").Append(";");
            else
                message.Append("0").Append(";");
            message.Append(m_character.AlignmentId).Append(";");
            message.Append("-1").Append('|');
        }

        public void SerializeAs_TaxCollectorDefender(StringBuilder message)
        {
            message.Append(Util.EncodeBase36(m_character.Id)).Append(';');
            message.Append(m_character.Name).Append(';');
            message.Append(m_character.Skin).Append(';');
            message.Append(m_character.Level).Append(';');
            message.Append(m_character.HexColor1).Append(';');
            message.Append(m_character.HexColor2).Append(';');
            message.Append(m_character.HexColor3);
        }
    }
}


