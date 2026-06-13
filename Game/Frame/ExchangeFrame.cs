using System;
using System.Linq;
using Protocolo.Framework.Network;
using Game;
using Game.Action;
using Game.Entity;
using Game.Exchange;
using Game.Network;
using Game.Manager;

namespace Game.Frame
{
    public sealed class ExchangeFrame : AbstractNetworkFrame<ExchangeFrame, CharacterEntity, string>
    {
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message.Length < 2)
                return null;

            switch (message[0])
            {
                case 'E':
                    switch (message[1])
                    {
                        case 'Q':
                            return MerchantModeProcess;

                        case 'q':
                            return MerchantModeTaxe;

                        case 'H':
                            if (message.Length < 3)
                                return null;

                            switch (message[2])
                            {
                                case 'T':
                                    return AuctionHouseGetTemplatesList;

                                case 'l':
                                    return AuctionHouseGetItemsList;

                                case 'B':
                                    return AuctionHouseBuyItem;


                                case 'S':
                                    return null;

                                case 'P':
                                    return AuctionHouseMiddlePrice;
                            }
                            break;

                        case 'A':
                            return ExchangeAccept;

                        case 'R':
                            return ExchangeRequest;

                        case 'V':
                            return ExchangeLeave;

                        case 'K':
                            return ExchangeValidate;

                        case 'B':
                            return ExchangeBuy;

                        case 'S':
                            return ExchangeSell;

                        case 'M':
                            if (message.Length < 3)
                                return null;

                            return message[2] switch { 'G' => ExchangeMoveGold, 'O' => ExchangeMoveObject, 'R' => ExchangeRetry, 'r' => ExchangeCancelRetry, _ => null, };

                        case 'P': // craft seguro: movimiento de pago (kamas/objetos)
                            return ExchangePayMovement;
                    }
                    break;
            }

            return null;
        }

        private void MerchantModeProcess(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
                {
                    if (character.Inventory.Kamas < character.MerchantTaxe)
                    {
                        character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_NOT_ENOUGH_KAMAS_TO_PAY_MERCHANT_MODE_TAXE));
                        return;
                    }

                    if (character.Map.Entities.OfType<MerchantEntity>().Count() >= 5)
                    {
                        character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_TOO_MANY_MERCHANT_ON_MAP, 5));
                        return;
                    }

                    if (character.PersonalShop.Items.Count == 0)
                    {
                        character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_NOT_ENOUGH_ITEMS_TO_BE_MERCHANT));
                        return;
                    }

                    if (character.HasPlayerRestriction(PlayerRestrictionEnum.RESTRICTION_CANT_BE_MERCHANT))
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    character.Inventory.SubKamas(character.MerchantTaxe);
                    character.Merchant = true;
                    character.ServerKick("Modo Mercante");
                });
        }

        private void MerchantModeTaxe(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
                {
                    if (character.Map.Entities.OfType<MerchantEntity>().Count() >= 5)
                    {
                        character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_TOO_MANY_MERCHANT_ON_MAP, 5));
                        return;
                    }

                    if (character.PersonalShop.Items.Count == 0)
                    {
                        character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_NOT_ENOUGH_ITEMS_TO_BE_MERCHANT));
                        return;
                    }

                    character.Dispatch(WorldMessage.MERCHANT_MODE_TAXE(character.MerchantTaxe));
                });
        }

        private void AuctionHouseMiddlePrice(CharacterEntity character, string message)
        {
            int templateId = -1;
            if (!int.TryParse(message.AsSpan(3), out templateId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.EXCHANGE))
                {
                    Logger.Debug("ExchangeFrame::Leave la entidad no esta en un intercambio: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var exchangeAction = character.CurrentAction as AbstractGameAuctionHouseAction;
                if (exchangeAction == null)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var middlePrice = exchangeAction.AuctionExchange.Npc.AuctionHouse.GetMiddlePrice(templateId);

                character.Dispatch(WorldMessage.AUCTION_HOUSE_MIDDLE_PRICE(templateId, middlePrice));
            });
        }

        private void AuctionHouseBuyItem(CharacterEntity character, string message)
        {
            var data = message.AsSpan(3);
            Span<Range> parts = stackalloc Range[4];
            var partCount = data.Split(parts, '|');
            if (partCount < 3)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            int categoryId = -1;
            if (!int.TryParse(data[parts[0]], out categoryId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            int quantity = -1;
            if (!int.TryParse(data[parts[1]], out quantity))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            long price = -1;
            if (!long.TryParse(data[parts[2]], out price))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.EXCHANGE))
                {
                    Logger.Debug("ExchangeFrame::Leave la entidad no esta en un intercambio: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var exchangeAction = character.CurrentAction as AbstractGameAuctionHouseAction;
                if (exchangeAction == null)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                exchangeAction.AuctionExchange.Npc.AuctionHouse.TryBuy(character, categoryId, quantity, price);
            });
        }

        private void AuctionHouseGetItemsList(CharacterEntity character, string message)
        {
            int templateId = -1;
            if (!int.TryParse(message.AsSpan(3), out templateId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.EXCHANGE))
                {
                    Logger.Debug("ExchangeFrame::Leave la entidad no esta en un intercambio: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var exchangeAction = character.CurrentAction as AbstractGameAuctionHouseAction;
                if (exchangeAction == null)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                exchangeAction.AuctionExchange.Npc.AuctionHouse.SendCategoriesByTemplate(character, templateId);
            });
        }

        private void AuctionHouseGetTemplatesList(CharacterEntity character, string message)
        {
            int type = -1;
            if (!int.TryParse(message.AsSpan(3), out type))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
                {
                    if (!character.HasGameAction(GameActionTypeEnum.EXCHANGE))
                    {
                        Logger.Debug("ExchangeFrame::Leave la entidad no esta en un intercambio, posible trampa: " + character.Name);
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    var exchangeAction = character.CurrentAction as AbstractGameAuctionHouseAction;
                    if (exchangeAction == null)
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    exchangeAction.AuctionExchange.Npc.AuctionHouse.SendTemplatesByTypeList(character, type);
                });
        }

        private void ExchangeRequest(CharacterEntity character, string message)
        {
            var exchangeData = message.AsSpan(2);
            Span<Range> exchangeParts = stackalloc Range[3];
            var exchangePartCount = exchangeData.Split(exchangeParts, '|', StringSplitOptions.RemoveEmptyEntries);
            int exchangeTypeId = -1;

            if (exchangePartCount < 1 || !int.TryParse(exchangeData[exchangeParts[0]], out exchangeTypeId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var exchangeActorId = -1;
            if (exchangePartCount > 1 && !int.TryParse(exchangeData[exchangeParts[1]], out exchangeActorId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            // Craft seguro (ER12|clienteId|skill): tercer campo = id de la habilidad de craft.
            var craftSecureSkillId = -1;
            if (exchangePartCount > 2)
                int.TryParse(exchangeData[exchangeParts[2]], out craftSecureSkillId);

            if (!Enum.IsDefined(typeof(ExchangeTypeEnum), exchangeTypeId))
            {
                Logger.Debug("ExchangeFrame::Request tipo de intercambio desconocido: " + exchangeTypeId + " " + character.Name);
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
            {
                var exchangeType = (ExchangeTypeEnum)exchangeTypeId;
                if (!character.CanGameAction(GameActionTypeEnum.EXCHANGE))
                {
                    Logger.Debug("ExchangeFrame::Request el personaje no puede iniciar un intercambio en este momento: " + character.Name);
                    character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_YOU_ARE_AWAY));
                    return;
                }

                var distantEntity = character.Map.GetEntity(exchangeActorId);
                if (exchangeType == ExchangeTypeEnum.EXCHANGE_PERSONAL_SHOP_EDIT)
                    distantEntity = character;

                if (distantEntity == null)
                {
                    var entityIds = string.Join(", ", character.Map.Entities.Select(e => e.Id + "(" + e.Type + ")"));
                    Logger.Debug("ExchangeFrame::Request entidad desconocida " + exchangeActorId + " mapa=" + character.Map.Id + " entidades=[" + entityIds + "] jugador=" + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (!distantEntity.CanGameAction(GameActionTypeEnum.EXCHANGE) || !distantEntity.CanBeExchanged(exchangeType))
                {
                    if (distantEntity.Type == EntityTypeEnum.TYPE_CHARACTER)
                        character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_PLAYER_AWAY_NOT_INVITABLE));
                    else
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                switch (distantEntity.Type)
                {
                    case EntityTypeEnum.TYPE_TAX_COLLECTOR:
                        var taxCollector = (TaxCollectorEntity)distantEntity;
                        if (character.GuildMember == null || taxCollector.Guild.Id != character.GuildMember.GuildId)
                        {
                            character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                            return;
                        }

                        if (!character.GuildMember.HasRight(Game.Guild.GuildRightEnum.COLLECT_TAXCOLLECTOR))
                        {
                            character.GuildMember.SendHasNotEnoughRights();
                            return;
                        }

                        character.ExchangeTaxCollector((TaxCollectorEntity)distantEntity);
                        break;

                    case EntityTypeEnum.TYPE_MERCHANT:
                        character.ExchangeMerchant((MerchantEntity)distantEntity);
                        break;

                    case EntityTypeEnum.TYPE_CHARACTER:
                        if (exchangeType == ExchangeTypeEnum.EXCHANGE_PERSONAL_SHOP_EDIT && character.Id == distantEntity.Id)
                        {
                            character.ExchangePersonalShop();
                        }
                        else if (exchangeType == ExchangeTypeEnum.EXCHANGE_CRAFT_SECURE_ARTISAN || exchangeType == ExchangeTypeEnum.EXCHANGE_CRAFT_SECURE_CLIENT)
                        {
                            // 12 = el iniciador (character) invita siendo ARTESANO.
                            // 13 = el iniciador (character) pide siendo CLIENTE; el invitado es el artesano.
                            // En ambos casos la habilidad pertenece al ARTESANO.
                            var invited = (CharacterEntity)distantEntity;
                            var artisan = exchangeType == ExchangeTypeEnum.EXCHANGE_CRAFT_SECURE_ARTISAN ? character : invited;
                            var client = exchangeType == ExchangeTypeEnum.EXCHANGE_CRAFT_SECURE_ARTISAN ? invited : character;

                            var skill = artisan.CharacterJobs.GetSkill(craftSecureSkillId);
                            if (!(skill is Game.Job.Skill.CraftSkill || skill is Game.Job.Skill.MagicSkill))
                            {
                                Logger.Debug("ExchangeFrame::Request craft seguro sin habilidad válida: " + craftSecureSkillId + " artesano=" + artisan.Name);
                                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                                return;
                            }

                            character.RequestCraftSecure(invited, artisan, client, skill, exchangeTypeId);
                        }
                        else
                        {
                            character.ExchangePlayer((CharacterEntity)distantEntity);
                        }
                        break;

                    case EntityTypeEnum.TYPE_NPC:
                        NonPlayerCharacterEntity npc = (NonPlayerCharacterEntity)distantEntity;
                        switch (exchangeType)
                        {
                            case ExchangeTypeEnum.EXCHANGE_NPC:
                                character.ExchangeNpc(npc);
                                break;

                            case ExchangeTypeEnum.EXCHANGE_SHOP:
                                character.ExchangeShop(npc);
                                break;

                            case ExchangeTypeEnum.EXCHANGE_AUCTION_HOUSE_BUY:
                                character.ExchangeAuctionHouseBuy(npc);
                                break;

                            case ExchangeTypeEnum.EXCHANGE_AUCTION_HOUSE_SELL:
                                character.ExchangeAuctionHouseSell(npc);
                                break;
                        }
                        break;
                }
            });
        }

        private void ExchangeAccept(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.EXCHANGE))
                {
                    Logger.Debug("ExchangeFrame::Accept la entidad no tiene solicitud de intercambio: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                // Intercambio entre jugadores o craft seguro: ambos derivan de
                // AbstractGameExchangeAction y exponen DistantEntity (el invitado).
                var action = character.CurrentAction as AbstractGameExchangeAction;
                if (action == null || action.DistantEntity == null || !(action is GamePlayerExchangeAction || action is GameCraftSecureExchangeAction))
                {
                    Logger.Debug("ExchangeFrame::Accept la entidad no esta en un intercambio entre jugadores: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (character.Id != action.DistantEntity.Id)
                {
                    Logger.Debug("ExchangeFrame::Accept el jugador no puede aceptar un intercambio que el mismo solicito: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                action.Accept();
            });
        }

        /// <summary>
        /// Craft seguro: pago del cliente. EP&lt;zona&gt;&lt;G|O&gt;&lt;...&gt; (zona 1=siempre, 2=si éxito).
        /// </summary>
        private void ExchangePayMovement(CharacterEntity character, string message)
        {
            if (message.Length < 5 || (message[3] != 'G' && message[3] != 'O'))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var zone = message[2] - '0';
            var isKamas = message[3] == 'G';
            var payload = message.AsSpan(4);

            long kamas = 0;
            bool add = false;
            long guid = -1;
            int quantity = 1;

            if (isKamas)
            {
                if (!long.TryParse(payload, out kamas) || kamas < 0)
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }
            }
            else
            {
                if (payload.IsEmpty)
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                add = payload[0] == '+';
                var itemData = payload.Slice(1);
                Span<Range> parts = stackalloc Range[3];
                var partCount = itemData.Split(parts, '|');
                if (partCount < 1 || !long.TryParse(itemData[parts[0]], out guid))
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }
                if (partCount > 1 && !int.TryParse(itemData[parts[1]], out quantity))
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }
            }

            character.AddMessage(() =>
            {
                if (!(character.CurrentAction is AbstractGameExchangeAction action) || !(action.Exchange is ExchangeCraftSecure craft))
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                // Solo el cliente (no el artesano) aporta el pago.
                if (character.Id != craft.Client.Id)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (isKamas)
                    craft.MovePayKamas(zone, kamas);
                else if (add)
                    craft.AddPayItem(zone, guid, quantity);
                else
                    craft.RemovePayItem(zone, guid, quantity);
            });
        }

        private void ExchangeLeave(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.EXCHANGE))
                {
                    Logger.Debug("ExchangeFrame::Leave la entidad no esta en un intercambio: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                character.AbortAction(GameActionTypeEnum.EXCHANGE, character.Id);
            });
        }

        private void ExchangeValidate(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
                {
                    if (!character.HasGameAction(GameActionTypeEnum.EXCHANGE))
                    {
                        Logger.Debug("ExchangeFrame::Validate la entidad no esta en un intercambio: " + character.Name);
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    var action = character.CurrentAction as AbstractGameExchangeAction;
                    var exchange = action.Exchange as IValidableExchange;
                    if (exchange == null)
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    if (!exchange.Validate(character))
                    {
                        return;
                    }

                    character.StopAction(GameActionTypeEnum.EXCHANGE);
                });
        }

        private void ExchangeSell(CharacterEntity character, string message)
        {
            var data = message.AsSpan(2);
            Span<Range> parts = stackalloc Range[3];
            var partCount = data.Split(parts, '|');

            if (partCount != 2)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            long itemId = -1;
            if (!long.TryParse(data[parts[0]], out itemId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            int quantity = -1;
            if (!int.TryParse(data[parts[1]], out quantity))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.EXCHANGE))
                {
                    Logger.Debug("ExchangeFrame::Sell la entidad no esta en un intercambio: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                ((AbstractGameExchangeAction)character.CurrentAction).Exchange.SellItem(character, itemId, quantity);
            });
        }

        private void ExchangeBuy(CharacterEntity character, string message)
        {
            var data = message.AsSpan(2);
            Span<Range> parts = stackalloc Range[3];
            var partCount = data.Split(parts, '|');

            if (partCount != 2)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            int templateId = -1;
            if (!int.TryParse(data[parts[0]], out templateId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            int quantity = -1;
            if (!int.TryParse(data[parts[1]], out quantity))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (quantity <= 0)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.EXCHANGE))
                {
                    Logger.Debug("ExchangeFrame::Buy la entidad no esta en un intercambio: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                ((AbstractGameExchangeAction)character.CurrentAction).Exchange.BuyItem(character, templateId, quantity);
            });
        }

        private void ExchangeMoveGold(CharacterEntity character, string message)
        {
            long kamas = -1;
            if (!long.TryParse(message.AsSpan(3), out kamas))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (kamas < 0)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.EXCHANGE))
                {
                    Logger.Debug("ExchangeFrame::MoveGold la entidad no esta en un intercambio: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                ((AbstractGameExchangeAction)character.CurrentAction).Exchange.MoveKamas(character, kamas);
            });
        }

        private void ExchangeMoveObject(CharacterEntity character, string message)
        {
            var data = message.AsSpan(3);
            Span<Range> parts = stackalloc Range[4];
            var partCount = data.Split(parts, '|');

            if (partCount < 2 || data[parts[0]].IsEmpty)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var itemData = data[parts[0]];
            var add = itemData[0] == '+';
            long itemId = -1;
            if (!long.TryParse(itemData.Slice(1), out itemId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            int quantity = -1;
            if (!int.TryParse(data[parts[1]], out quantity))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (quantity < 0)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            long price = -1;
            if (partCount > 2)
            {
                if (!long.TryParse(data[parts[2]], out price))
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }
            }

            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.EXCHANGE))
                {
                    Logger.Debug("ExchangeFrame::MoveObject la entidad no esta en un intercambio: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                if (add)
                    ((AbstractGameExchangeAction)character.CurrentAction).Exchange.AddItem(character, itemId, quantity, price);
                else
                    ((AbstractGameExchangeAction)character.CurrentAction).Exchange.RemoveItem(character, itemId, quantity);
            });
        }

        private void ExchangeRetry(CharacterEntity character, string message)
        {
            var count = -1;
            if (!int.TryParse(message.AsSpan(3), out count))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.EXCHANGE))
                {
                    Logger.Debug("ExchangeFrame::MoveObject la entidad no esta en un intercambio: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var action = character.CurrentAction as AbstractGameExchangeAction;

                if (action.Exchange is not IRetryableExchange exchange)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                exchange.Retry(count);
            });
        }

        private void ExchangeCancelRetry(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
            {
                if (!character.HasGameAction(GameActionTypeEnum.EXCHANGE))
                {
                    Logger.Debug("ExchangeFrame::MoveObject la entidad no esta en un intercambio: " + character.Name);
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var action = character.CurrentAction as AbstractGameExchangeAction;
                IRetryableExchange exchange = action.Exchange as IRetryableExchange;
                if (exchange == null)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                exchange.CancelRetry();
            });
        }
    }
}
