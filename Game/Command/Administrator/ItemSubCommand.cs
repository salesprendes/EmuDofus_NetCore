using Game.Database.Repository;
using Game.Database.Structure;
using Game.Network;

namespace Game.Command
{
    public sealed partial class CharacterCommand
    {
        public sealed class ItemSubCommand : WorldStaffSubCommand
        {
            private readonly string[] _aliases =
            {
                "item"
            };

            public override string[] Aliases => _aliases;

            public override string Description => "Crea un objeto en tu inventario con stats perfectos. Uso: %templateId% [%cantidad%]";

            protected override StaffRole RequiredRole => StaffRole.Administrator;

            protected override void Process(WorldCommandContext context)
            {
                int templateId;
                if (int.TryParse(context.TextCommandArgument.NextWord(), out templateId))
                {
                    var itemTemplate = ItemTemplateRepository.Instance.GetById(templateId);
                    if (itemTemplate != null)
                    {
                        int quantity = 1;
                        if (!int.TryParse(context.TextCommandArgument.NextWord(), out quantity) || quantity == templateId)
                        {
                            quantity = 1;
                        }

                        var instance = itemTemplate.Create(context.Character.Id, (int)context.Character.Type, quantity, ItemSlotEnum.SLOT_INVENTORY, true);
                        if (instance != null)
                        {
                            context.Character.Inventory.AddItem(instance);
                            context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE($"Item {itemTemplate.Name} added in your inventory"));
                        }
                    }
                    else
                    {
                        context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Unknow templateId"));
                    }
                }
                else
                {
                    context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Command format : character item %templateId%"));
                }
            }
        }
    }
}
