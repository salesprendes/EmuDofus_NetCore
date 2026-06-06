using Game.Action;
using Game.Manager;
using Game.Network;
using Game.Spell;
using System.Collections.Generic;

namespace Game.Command
{
    public sealed partial class CharacterCommand
    {
        public sealed class EffectCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "effect"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Aplica un efecto directo a tu personaje. Uso: %effectId%";

            protected override StaffRole RequiredRole => StaffRole.Administrator;

            protected override void Process(WorldCommandContext context)
            {
                int effectId = 0;
                if (!int.TryParse(context.TextCommandArgument.NextWord(), out effectId))
                {
                    context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Command format : character effect %effectId%"));
                    return;
                }

                var parameters = new Dictionary<string, string>();
                foreach (var parameter in context.TextCommandArgument.NextWord().Split(','))
                {
                    if (parameter.Contains('='))
                    {
                        var data = parameter.Split('=');
                        parameters.Add(data[0], data[1]);
                    }
                }

                ActionEffectManager.Instance.ApplyEffect(context.Character, (EffectEnum)effectId, parameters);
            }
        }
    }
}
