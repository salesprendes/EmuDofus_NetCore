using Protocolo.Framework.Command;
using Game.Entity;

namespace Game.Command
{
    public class WorldCommandContext : CommandContext
    {
        public WorldCommandContext(CharacterEntity character, string line) : base(line)
        {
            Character = character;
        }

        public CharacterEntity Character
        {
            get; 
            set;
        }
    }
}


