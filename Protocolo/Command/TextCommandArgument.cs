using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Protocolo.Framework.Command
{
    public class TextCommandArgument
    {
        private readonly string[] m_data;
        private int m_position;

        public int Count => m_data.Length;

        public int RemainingCount => Math.Max(0, Count - Position);

        public bool HasNext => Position < Count;

        public int Position
        {
            get => m_position;
            set => m_position = Math.Clamp(value, 0, Count);
        }

        public TextCommandArgument(string line)
        {
            m_data = Parse(line ?? string.Empty).ToArray();
            Position = 0;
        }

        public string NextWord()
        {
            return TryReadWord(out var word) ? word : string.Empty;
        }

        public bool TryReadWord(out string word)
        {
            if (!HasNext)
            {
                word = string.Empty;
                return false;
            }

            word = m_data[Position++];
            return true;
        }

        public string PeekWord()
        {
            return TryPeekWord(out var word) ? word : string.Empty;
        }

        public bool TryPeekWord(out string word)
        {
            if (!HasNext)
            {
                word = string.Empty;
                return false;
            }

            word = m_data[Position];
            return true;
        }

        public bool TryReadInt(out int value)
        {
            if (!TryPeekWord(out var word) ||
                !int.TryParse(word, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                value = 0;
                return false;
            }

            Position++;
            return true;
        }

        public bool TryReadLong(out long value)
        {
            if (!TryPeekWord(out var word) ||
                !long.TryParse(word, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                value = 0;
                return false;
            }

            Position++;
            return true;
        }

        public string ReadRemainingText(string separator = " ")
        {
            if (!HasNext)
                return string.Empty;

            var remaining = string.Join(separator, m_data, Position, RemainingCount);
            Position = Count;
            return remaining;
        }

        public void Rewind(int count = 1)
        {
            Position -= count;
        }

        private static List<string> Parse(string line)
        {
            var words = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;
            var escaped = false;

            foreach (var character in line)
            {
                if (escaped)
                {
                    current.Append(character);
                    escaped = false;
                    continue;
                }

                if (character == '\\' && inQuotes)
                {
                    escaped = true;
                    continue;
                }

                if (character == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(character) && !inQuotes)
                {
                    AddWord(words, current);
                    continue;
                }

                current.Append(character);
            }

            if (escaped)
                current.Append('\\');

            AddWord(words, current);
            return words;
        }

        private static void AddWord(ICollection<string> words, StringBuilder current)
        {
            if (current.Length == 0)
                return;

            words.Add(current.ToString());
            current.Clear();
        }
    }
}
