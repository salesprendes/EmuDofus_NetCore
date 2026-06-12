using Game.Spell;
using ProtoBuf;

namespace Game.Stats
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public sealed class GenericEffect
    {
        [ProtoIgnore] public int Total => Base + Items + Dons + Boosts;

        [ProtoIgnore] public EffectEnum EffectType => (EffectEnum)Id;

        [ProtoIgnore] public int MergeValue => Value1 != 0 || Value3 == 0 ? Value1 : Value3;

        public int Id;
        public int Value1;
        public int Value2;
        public int Value3;
        public string Args;

        public int Base;
        public int Items;
        public int Dons;
        public int Boosts;

        [ProtoIgnore] public int RandomJet => Util.NextJet(Value1, Value2);

        public GenericEffect(EffectEnum id, int baseValue = 0, int items = 0, int dons = 0, int boosts = 0)
        {
            Id = (int)id;
            Base = baseValue;
            Items = items;
            Dons = dons;
            Boosts = boosts;
        }

        public GenericEffect(EffectEnum id, int value1, int value2, int value3, string args)
        {
            Id = (int)id;
            Value1 = value1;
            Value2 = value2;
            Value3 = value3;
            Args = args;
        }

        public GenericEffect()
        {
        }

        public void Merge(GenericEffect effect)
        {
            Base += effect.Base;
            Boosts += effect.Boosts;
            Dons += effect.Dons;
            Items += effect.Items;
        }

        public void UnMerge(GenericEffect effect)
        {
            Base -= effect.Base;
            Boosts -= effect.Boosts;
            Dons -= effect.Dons;
            Items -= effect.Items;
        }

        public void Merge(StatsType type, GenericEffect effect)
        {
            switch (type)
            {
                case StatsType.TYPE_BASE:
                    Base += effect.MergeValue;
                    break;

                case StatsType.TYPE_BOOST:
                    Boosts += effect.MergeValue;
                    break;

                case StatsType.TYPE_DON:
                    Dons += effect.MergeValue;
                    break;

                case StatsType.TYPE_ITEM:
                    Items += effect.MergeValue;
                    break;
            }
        }

        public void UnMerge(StatsType type, GenericEffect effect)
        {
            switch (type)
            {
                case StatsType.TYPE_BASE:
                    Base -= effect.MergeValue;
                    break;

                case StatsType.TYPE_BOOST:
                    Boosts -= effect.MergeValue;
                    break;

                case StatsType.TYPE_DON:
                    Dons -= effect.MergeValue;
                    break;

                case StatsType.TYPE_ITEM:
                    Items -= effect.MergeValue;
                    break;
            }
        }

        public override string ToString()
        {
            return Base + "," + Items + "," + Dons + "," + Boosts;
        }

        public string ToItemString()
        {
            return Id.ToString("x") + '#' + Value1.ToString("x") + '#' + Value2.ToString("x") + '#' + Value3.ToString("x") + '#' + Args;
        }
    }
}


