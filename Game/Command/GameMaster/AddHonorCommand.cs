using Game.Manager;
using Game.Network;

namespace Game.Command
{
    public sealed partial class CharacterCommand
    {
        public sealed class AddHonorCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "addhonor"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Da o quita honor a tu personaje o a otro jugador. Uso: %honorValue% [%playerName%]";

            protected override StaffRole RequiredRole => StaffRole.GameMaster;

            protected override void Process(WorldCommandContext context)
            {
                var firstArgument = context.TextCommandArgument.NextWord();
                if (string.IsNullOrEmpty(firstArgument))
                {
                    SendFormat(context);
                    return;
                }

                int honorValue;
                var targetName = string.Empty;
                if (int.TryParse(firstArgument, out honorValue))
                {
                    targetName = context.TextCommandArgument.NextWord();
                }
                else
                {
                    targetName = firstArgument;
                    if (!int.TryParse(context.TextCommandArgument.NextWord(), out honorValue))
                    {
                        SendFormat(context);
                        return;
                    }
                }

                if (string.IsNullOrEmpty(targetName))
                {
                    ApplyHonor(context, context.Character, honorValue);
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
                        context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Unable to change the honor of a staff member with equal or higher power."));
                        return;
                    }

                    target.AddMessage(() => ApplyHonor(context, target, honorValue));
                });
            }

            private static void ApplyHonor(WorldCommandContext context, Entity.CharacterEntity target, int honorValue)
            {
                target.ChangeHonour(honorValue);

                if (target.Id == context.Character.Id)
                    context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Honor changed successfully."));
                else
                    context.Character.SafeDispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Player honor changed successfully."));
            }

            private static void SendFormat(WorldCommandContext context)
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Command format : character addhonor %honorValue% [%playerName%]"));
            }
        }
    }
}
