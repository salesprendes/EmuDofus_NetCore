using Game.Database.Structure;
using Game.Action;
using Game.Exchange;
using Game.Fight;
using Game.Fight.AI;
using Game.Guild;
using Game.Manager;
using Game.Spell;
using Game.Stats;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entity.Inventory;

namespace Game.Entity
{
    public sealed class TaxCollectorEntity : AIFighter, IDisposable
    {
        public override int MapId
        {
            get;
            set;
        }

        public override int CellId
        {
            get;
            set;
        }

        public override string Name => Util.EncodeBase36(DatabaseRecord.FirstName) + "," + Util.EncodeBase36(DatabaseRecord.Name);

        public override int Level
        {
            get
            {
                return Guild.Level;
            }
            set
            {
            }
        }

        public override int BaseLife => Statistics.GetTotal(EffectEnum.STAT_MAS_VITALIDAD);

        public override int RealLife
        {
            get;
            set;
        }

        public override int Restriction
        {
            get;
            set;
        }

        public override int SkinBase => DatabaseRecord.Skin;

        public override int SkinSizeBase => DatabaseRecord.SkinSize;

        public TaxCollectorDAO DatabaseRecord
        {
            get;
            private set;
        }

        public GuildInstance Guild
        {
            get;
            private set;
        }


        public List<GuildMember> Defenders
        {
            get;
            private set;
        }

        public bool CanDefend
        {
            get
            {
                if (!HasGameAction(GameActionTypeEnum.FIGHT))
                    return false;
                return ((TaxCollectorFight)Fight).CanDefend;
            }
        }

        public override long Kamas
        {
            get
            {
                return DatabaseRecord.Kamas;
            }
            set
            {
                DatabaseRecord.Kamas = value;
            }
        }

        public long ExperienceGathered
        {
            get
            {
                return DatabaseRecord.Experience;
            }
            set
            {
                DatabaseRecord.Experience = value;
            }
        }

        public TaxCollectorInventory Storage
        {
            get;
            private set;
        }

        public Dictionary<int, int> FarmedItems
        {
            get;
            private set;
        }

        public override int AlignmentId => (int)ConquestManager.AlignmentTypeEnum.ALIGNMENT_NEUTRAL;

        public override bool CanDrop => true;

        public TaxCollectorEntity(GuildInstance guild, TaxCollectorDAO record) : base(EntityTypeEnum.TYPE_TAX_COLLECTOR, record.Id)
        {
            DatabaseRecord = record;
            Guild = guild;

            MapId = DatabaseRecord.MapId;
            CellId = DatabaseRecord.CellId;

            Defenders = new List<GuildMember>();
            FarmedItems = new Dictionary<int, int>();

            Statistics = new GenericStats();
            Statistics.Merge(guild.Statistics.BaseStatistics);
            SpellBook = SpellBookFactory.Instance.Create(this);
            Storage = new TaxCollectorInventory(this);
            RefreshBrain();
        }

        public override bool CanBeMoved()
        {
            return true;
        }

        public override void Dispose()
        {
            Guild = null;

            Defenders.Clear();
            Defenders = null;

            base.Dispose();
        }

        public override void JoinFight(Fight.AbstractFight fight, Fight.FightTeam team)
        {
            base.JoinFight(fight, team);

            Guild.SafeDispatch(WorldMessage.GUILD_TAXCOLLECTOR_UNDER_ATTACK(Name, Map.X, Map.Y));
        }

        public override void EndFight(bool win = false)
        {
            base.EndFight(win);

            if (win)
            {
                Guild.AddMessage(() => { if (Guild.IsDeleted) return; StartAction(GameActionTypeEnum.MAP); Guild.Dispatch(WorldMessage.GUILD_TAXCOLLECTOR_SURVIVED(Name, Map.X, Map.Y)); });
            }
            else
            {
                Guild.AddMessage(() => { if (Guild.IsDeleted) return; Guild.RemoveTaxCollector(this); Guild.Dispatch(WorldMessage.GUILD_TAXCOLLECTOR_DIED(Name, Map.X, Map.Y)); });
            }

            Defenders.Clear();
        }

        public void DefenderJoin(GuildMember member)
        {
            Defenders.Add(member);
        }

        public void DefenderLeft(GuildMember member)
        {
            Defenders.Remove(member);
        }

        public override bool CanBeExchanged(Exchange.ExchangeTypeEnum exchangeType)
        {
            return base.CanBeExchanged(exchangeType) && exchangeType == ExchangeTypeEnum.EXCHANGE_TAXCOLLECTOR;
        }

        private StringBuilder m_serialized;

        public override void SerializeAs_GameMapInformations(OperatorEnum operation, StringBuilder message)
        {
            switch (operation)
            {
                case OperatorEnum.OPERATOR_REMOVE:
                    message.Append(Id);
                    break;

                case OperatorEnum.OPERATOR_ADD:
                case OperatorEnum.OPERATOR_REFRESH:
                    if (HasGameAction(GameActionTypeEnum.MAP))
                    {
                        message.Append(CellId).Append(';');
                        message.Append(Orientation).Append(';');
                        if (m_serialized == null)
                        {
                            m_serialized = new StringBuilder();
                            m_serialized.Append(0).Append(';');
                            m_serialized.Append(Id).Append(';');
                            m_serialized.Append(Name).Append(';');
                            m_serialized.Append((int)Type).Append(';');
                            m_serialized.Append(SkinBase).Append('^');
                            m_serialized.Append(SkinSizeBase).Append(';');
                            m_serialized.Append(Level).Append(';');
                            m_serialized.Append(Guild.Name).Append(';');
                            m_serialized.Append(Guild.DisplayEmblem);
                        }
                        message.Append(m_serialized.ToString());
                    }
                    else if (HasGameAction(GameActionTypeEnum.FIGHT))
                    {
                        message.Append(Cell.Id).Append(';');
                        message.Append(Orientation).Append(';');
                        message.Append('0').Append(';');
                        message.Append(Id).Append(';');
                        message.Append(Name).Append(';');
                        message.Append((int)Type).Append(';');
                        message.Append(Skin).Append('^');
                        message.Append(SkinSize).Append(';');
                        message.Append(Level).Append(';');
                        message.Append(Life).Append(';');
                        message.Append(AP).Append(';');
                        message.Append(MP).Append(';');
                        message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL)).Append(';');
                        message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA)).Append(';');
                        message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO)).Append(';');
                        message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA)).Append(';');
                        message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE)).Append(';');
                        message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_ESQUIVA_PA)).Append(';');
                        message.Append(Statistics.GetTotal(EffectEnum.STAT_MAS_ESQUIVA_PM)).Append(';');
                        message.Append(Team.Id);
                    }
                    break;
            }
        }
    }
}


