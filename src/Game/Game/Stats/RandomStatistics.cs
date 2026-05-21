using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Stats
{
    public sealed class RandomEffect
    {
        public EffectEnum Type { get; }
        public int Minimum { get; }
        public int Maximum { get; }
        public int Value3 { get; }
        public int Random => Util.NextJet(Minimum, Maximum);

        public RandomEffect(EffectEnum type, int min, int max, int value3 = 0)
        {
            Type = type;
            Minimum = min;
            Maximum = max;
            Value3 = value3;
        }
        public void Serialize(StringBuilder sb)
        {
            sb.Append(((int)Type).ToString("X2")).Append('#');
            sb.Append(Minimum.ToString("X2")).Append('#');
            sb.Append(Maximum.ToString("X2"));
            if (Value3 != 0)
                sb.Append('#').Append(Value3.ToString("X2"));
        }
        public static RandomEffect Deserialize(string data)
        {
            var splitted = data.Split('#');
            var effect = (EffectEnum)int.Parse(splitted[0], System.Globalization.NumberStyles.HexNumber);
            var min = int.Parse(splitted[1], System.Globalization.NumberStyles.HexNumber);
            var max = int.Parse(splitted[2], System.Globalization.NumberStyles.HexNumber);
            int value3 = splitted.Length > 3 ? int.Parse(splitted[3], System.Globalization.NumberStyles.HexNumber) : 0;
            return new RandomEffect(effect, min, max, value3);
        }
    }
    public sealed class RandomStatistics : List<RandomEffect>
    {
        public string Serialize()
        {
            var sb = new StringBuilder();
            foreach (var effect in this)
            {
                effect.Serialize(sb);
                sb.Append(',');
            }
            return sb.ToString();
        }
        public static RandomStatistics Deserialize(string data)
        {
            var statistics = new RandomStatistics();
            if(!string.IsNullOrWhiteSpace(data))
                statistics.AddRange(data.Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(RandomEffect.Deserialize));
            return statistics;
        }
    }
}


