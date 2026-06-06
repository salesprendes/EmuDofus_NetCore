using Game.Network;
using Protocolo.Framework.Command;

namespace Game.Command
{
    public abstract class WorldStaffCommand : Command<WorldCommandContext>
    {
        protected abstract StaffRole RequiredRole { get; }

        protected override bool CanExecute(WorldCommandContext context)
        {
            return WorldCommandPermissions.HasRole(context, RequiredRole);
        }

        protected override void OnCanExecuteFailed(WorldCommandContext context)
        {
            context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE(
                "Not enough permissions. Required role : " + WorldCommandPermissions.GetDisplayName(RequiredRole)));
        }
    }

    public abstract class WorldStaffSubCommand : SubCommand<WorldCommandContext>
    {
        protected abstract StaffRole RequiredRole { get; }

        protected override bool CanExecute(WorldCommandContext context)
        {
            return WorldCommandPermissions.HasRole(context, RequiredRole);
        }

        protected override void OnCanExecuteFailed(WorldCommandContext context)
        {
            context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE(
                "Not enough permissions. Required role : " + WorldCommandPermissions.GetDisplayName(RequiredRole)));
        }
    }
}
