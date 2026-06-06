using Game.Network;

namespace Game.Command
{
    public sealed partial class CharacterCommand
    {
        public sealed class EmoteCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "emote"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Hace que todos en el mapa reproduzcan un emote. Uso: %emoteId%";

            protected override StaffRole RequiredRole => StaffRole.GameMaster;

            protected override void Process(WorldCommandContext context)
            {
                var emoteId = -1;
                if (!int.TryParse(context.TextCommandArgument.NextWord(), out emoteId))
                {
                    context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Command format : character emote %emoteId%"));
                    return;
                }

                foreach (var entity in context.Character.Map.Entities)
                {
                    entity.AddMessage(() => entity.EmoteUse(emoteId));
                }
            }
        }
    }
}
