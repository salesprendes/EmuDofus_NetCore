using Game.Fight;
using Game.Fight.AI;
using Game.Fight.AI.Core;
using Game.Spell;
using Game.Stats;
using Game.Network;
using System.Text;

namespace Game.Entity
{
    /// <summary>
    /// Sram double.
    /// Internally it behaves like an AI summon, but it is serialized as a character
    /// so the client renders the clone with the summoner's appearance.
    /// </summary>
    public sealed class DoubleFighter : AIFighter
    {
        private readonly CharacterEntity m_source;
        private readonly int m_baseLife;
        private readonly int m_skinBase;
        private readonly int m_skinSizeBase;
        private int m_realLife;
        private int m_restriction;

        public override int MapId
        {
            get { return Fight?.Map?.Id ?? m_source.MapId; }
            set { }
        }

        public override int BaseLife => m_baseLife;

        public override int CellId
        {
            get { return Cell?.Id ?? m_source.CellId; }
            set { }
        }

        public override string Name => m_source.Name;

        public override int Level
        {
            get { return m_source.Level; }
            set { }
        }

        public override int SkinBase => m_skinBase;

        public override int SkinSizeBase => m_skinSizeBase;

        public override bool CanDrop => false;

        public override int RealLife
        {
            get { return m_realLife; }
            set { m_realLife = value; }
        }

        public override int Restriction
        {
            get { return m_restriction; }
            set { m_restriction = value; }
        }

        public override int AlignmentId => m_source.AlignmentId;

        public override EffectEnum SummonEffectType => EffectEnum.InvocDouble;

        public DoubleFighter(long id, CharacterEntity source) : base(EntityTypeEnum.TYPE_MONSTER_FIGHTER, id)
        {
            m_source = source;
            m_baseLife = source.BaseLife;
            m_skinBase = source.Skin;
            m_skinSizeBase = source.SkinSize;
            m_realLife = source.RealLife;
            m_restriction = source.Restriction;

            Statistics = new GenericStats();
            Statistics.Merge(source.Statistics);

            // The double does not cast spells, but the AI expects a spellbook instance.
            SpellBook = new SpellBook((int)EntityTypeEnum.TYPE_MONSTER_FIGHTER, 0);

            Invocator = source;
            SetBrain(AIProfile.Passive);
        }

        public override void JoinFight(AbstractFight fight, FightTeam team)
        {
            base.JoinFight(fight, team);
            Life = m_source.Life;
        }

        public override void SerializeAs_GameMapInformations(OperatorEnum operation, StringBuilder message)
        {
            switch (operation)
            {
                case OperatorEnum.OPERATOR_REMOVE:
                    message.Append(Id);
                    break;

                case OperatorEnum.OPERATOR_ADD:
                case OperatorEnum.OPERATOR_REFRESH:
                    message.Append(Cell.Id).Append(';');
                    message.Append(Orientation).Append(';');
                    message.Append((int)EntityTypeEnum.TYPE_CHARACTER).Append(';');
                    message.Append(Id).Append(';');
                    message.Append(Name).Append(';');
                    message.Append(m_source.BreedId);
                    if (m_source.TitleId != 0)
                    {
                        message.Append(",");
                        message.Append(m_source.TitleId).Append('*');
                        message.Append(m_source.TitleParams);
                    }
                    message.Append(';');
                    message.Append(Skin).Append('^').Append(SkinSize).Append(';');
                    message.Append(m_source.Sex).Append(';');
                    message.Append(Level).Append(';');
                    message.Append(m_source.AlignmentId).Append(',');
                    message.Append(m_source.AlignmentId).Append(',');
                    if (m_source.AlignmentEnabled)
                        message.Append(m_source.AlignmentLevel).Append(',');
                    else
                        message.Append('0').Append(',');
                    message.Append(Id + Level).Append(';');
                    message.Append(m_source.HexColor1).Append(';');
                    message.Append(m_source.HexColor2).Append(';');
                    message.Append(m_source.HexColor3).Append(';');
                    m_source.Inventory.SerializeAs_ActorLookMessage(message);
                    message.Append(';');
                    message.Append(Life).Append(';');
                    message.Append(AP).Append(';');
                    message.Append(MP).Append(';');
                    switch (Fight.Type)
                    {
                        case FightTypeEnum.TYPE_CHALLENGE:
                        case FightTypeEnum.TYPE_AGGRESSION:
                            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentNeutral) + Statistics.GetTotal(EffectEnum.AddReduceDamagePercentPvPNeutral)).Append(';');
                            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentEarth) + Statistics.GetTotal(EffectEnum.AddReduceDamagePercentPvPEarth)).Append(';');
                            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentFire) + Statistics.GetTotal(EffectEnum.AddReduceDamagePercentPvPFire)).Append(';');
                            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentWater) + Statistics.GetTotal(EffectEnum.AddReduceDamagePercentPvPWater)).Append(';');
                            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentAir) + Statistics.GetTotal(EffectEnum.AddReduceDamagePercentPvPAir)).Append(';');
                            break;

                        default:
                            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentNeutral)).Append(';');
                            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentEarth)).Append(';');
                            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentFire)).Append(';');
                            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentWater)).Append(';');
                            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentAir)).Append(';');
                            break;
                    }
                    message.Append(Statistics.GetTotal(EffectEnum.AddAPDodge)).Append(';');
                    message.Append(Statistics.GetTotal(EffectEnum.AddMPDodge)).Append(';');
                    message.Append(Team.Id).Append(';');
                    message.Append("");
                    break;
            }
        }
    }
}


