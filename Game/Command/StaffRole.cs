using Game.Entity;

namespace Game.Command
{
    public enum StaffRole
    {
        Player = 0,
        Moderator = 1,
        GameMaster = 2,
        Administrator = 3
    }

    public static class WorldCommandPermissions
    {
        public static bool HasRole(WorldCommandContext context, StaffRole role)
        {
            return context != null && HasRole(context.Character, role);
        }

        public static bool HasRole(CharacterEntity character, StaffRole role)
        {
            return character != null &&
                   character.Account != null &&
                   character.Account.Power >= (int)role;
        }

        public static bool CanUseStaffConsole(CharacterEntity character)
        {
            return HasRole(character, StaffRole.Moderator);
        }

        public static string GetDisplayName(StaffRole role)
        {
            switch (role)
            {
                case StaffRole.Moderator:
                    return "Moderador";

                case StaffRole.GameMaster:
                    return "Game Master";

                case StaffRole.Administrator:
                    return "Administrador";

                default:
                    return "Jugador";
            }
        }
    }
}
