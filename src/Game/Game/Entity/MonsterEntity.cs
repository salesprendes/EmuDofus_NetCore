using Game.Database.Structure;
using Game.Fight;
using Game.Fight.AI;
using Game.Manager;
using Game.Network;
using Game.Spell;
using Game.Stats;
using System.Text;

namespace Game.Entity
{
    public sealed class MonsterEntity : AIFighter
    {
        public override int MapId
        {
            get;
            set;
        }

        public override int BaseLife => Grade.MaxLife;

        public override int CellId
        {
            get;
            set;
        }

        public override string Name => Grade.MonsterId.ToString();

        public override int Level
        {
            get { return Grade.Level; }
            set { }
        }

        public override int SkinBase => Grade.Template.GfxId;
        public override int SkinSizeBase => Grade.Template.SkinSize > 0 ? Grade.Template.SkinSize : 100;
        public override bool CanDrop => Invocator != null && Grade.Id == 285;

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

        public MonsterGradeDAO Grade
        {
            get;
            private set;
        }

        public override int AlignmentId
        {
            get
            {
                if (MapId <= 0)
                {
                    return -1;
                }

                var map = MapManager.Instance.GetById(MapId);
                if (map?.SubArea == null)
                {
                    return -1;
                }

                return ConquestManager.Instance.GetMonsterGroupAlignment(map.SubArea.Id);
            }
        }

        public MonsterEntity(long id, MonsterGradeDAO monsterGrade, AbstractFighter invocator = null, bool staticInvocation = false) : base(EntityTypeEnum.TYPE_MONSTER_FIGHTER, id, staticInvocation)
        {
            Grade = monsterGrade;

            Statistics = new GenericStats(monsterGrade);
            SpellBook = SpellBookFactory.Instance.Create(this);

            RealLife = MaxLife;
            SkinSize = SkinSizeBase;
            Invocator = invocator;
            RefreshBrain();
        }

        public override void JoinFight(AbstractFight fight, FightTeam team)
        {
            Life = MaxLife;
            base.JoinFight(fight, team);
        }

        public override void SerializeAs_GameMapInformations(OperatorEnum operation, StringBuilder message)
        {
            message.Append(Cell.Id).Append(';');
            message.Append(Orientation).Append(';');
            message.Append("0").Append(';');
            message.Append(Id).Append(';');
            message.Append(Name).Append(';');
            message.Append((int)EntityTypeEnum.TYPE_MONSTER_FIGHTER).Append(';');
            message.Append(Skin).Append('^').Append(SkinSize).Append(';');
            message.Append(Grade.Grade).Append(';');
            message.Append(Grade.Template.Colors.Replace(",", ";"));
            message.Append(";0,0,0,0;");
            message.Append(Life).Append(';');
            message.Append(AP).Append(';');
            message.Append(MP).Append(';');
            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentNeutral)).Append(';');
            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentEarth)).Append(';');
            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentFire)).Append(';');
            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentWater)).Append(';');
            message.Append(Statistics.GetTotal(EffectEnum.AddReduceDamagePercentAir)).Append(';');
            message.Append(Statistics.GetTotal(EffectEnum.AddAPDodge)).Append(';');
            message.Append(Statistics.GetTotal(EffectEnum.AddMPDodge)).Append(';');
            message.Append(Team.Id);
        }
    }
}

