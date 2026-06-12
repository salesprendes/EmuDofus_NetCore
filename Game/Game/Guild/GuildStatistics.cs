using Game.Database.Structure;
using Game.Spell;
using Game.Stats;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Guild
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public sealed class GuildStatistics
    {
        public static GuildStatistics Deserialize(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                return Serializer.Deserialize<GuildStatistics>(stream);
            }
        }

        public byte[] Serialize()
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize<GuildStatistics>(stream, this);

                return stream.ToArray();
            }
        }

        public static GuildStatistics Create(GuildDAO guild)
        {
            return new GuildStatistics { Spells = GuildSpellBook.Create(), BaseStatistics = new GenericStats(guild), MaxTaxcollector = 1 };
        }

        public GuildSpellBook Spells
        {
            get;
            private set;
        }

        public GenericStats BaseStatistics
        {
            get;
            private set;
        }

        public int MaxTaxcollector
        {
            get;
            set;
        }
    }
}


