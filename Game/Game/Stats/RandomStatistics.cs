using Game.Spell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Stats
{
    public sealed class RandomEffect(EffectEnum type, int min, int max, int value3 = 0)
    {
        public EffectEnum Type { get; } = type;
        public int Minimum { get; } = min;
        public int Maximum { get; } = max;
        public int Value3 { get; } = value3;
        public int Random => Util.NextJet(Minimum, Maximum);

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
            if (!string.IsNullOrWhiteSpace(data))
                statistics.AddRange(data.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(RandomEffect.Deserialize));
            return statistics;
        }
    }
}


