using Game.Entity;
using Game.Manager;
using Game.Network;
using System;

namespace Game.Command
{
    public sealed partial class CharacterCommand
    {
        public sealed class MorphCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "skin"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Cambia la apariencia de tu personaje o de otro jugador. Uso: %skinId% [%playerName%]";

            protected override StaffRole RequiredRole => StaffRole.Moderator;

            protected override void Process(WorldCommandContext context)
            {
                var firstArgument = context.TextCommandArgument.NextWord();
                if (string.IsNullOrEmpty(firstArgument))
                {
                    SendFormat(context);
                    return;
                }

                int skinId;
                var targetName = string.Empty;
                if (int.TryParse(firstArgument, out skinId))
                {
                    targetName = context.TextCommandArgument.NextWord();
                }
                else
                {
                    targetName = firstArgument;
                    if (!int.TryParse(context.TextCommandArgument.NextWord(), out skinId))
                    {
                        SendFormat(context);
                        return;
                    }
                }

                if (string.IsNullOrEmpty(targetName))
                {
                    ApplySkin(context, context.Character, skinId);
                    return;
                }

                WorldService.Instance.AddMessage(() =>
                {
                    var target = EntityManager.Instance.GetCharacterByName(targetName);
                    if (target == null)
                    {
                        context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Player not found."));
                        return;
                    }

                    if (target.Id != context.Character.Id && target.Account.Power >= context.Character.Account.Power)
                    {
                        context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Unable to change the skin of a staff member with equal or higher power."));
                        return;
                    }

                    target.AddMessage(() => ApplySkin(context, target, skinId));
                });
            }

            private static void ApplySkin(WorldCommandContext context, CharacterEntity target, int skinId)
            {
                target.DatabaseRecord.Skin = skinId;
                target.RefreshOnMap();

                if (target.Id == context.Character.Id)
                    context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Skin changed successfully."));
                else
                    context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Player skin changed successfully."));
            }

            private static void SendFormat(WorldCommandContext context)
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Command format : character skin %skinId% [%playerName%]"));
            }
        }
    }
}
