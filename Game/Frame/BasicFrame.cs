using Game.Action;
using Game.Command;
using Game.Database.Structure;
using Game.Entity;
using Game.Guild;
using Game.Manager;
using Game.Map;
using Game.Network;
using Game.Spell;
using Game.Stats;
using Protocolo.Framework.Network;
using System;
using System.Collections.Generic;

namespace Game.Frame
{
    public sealed class BasicFrame : AbstractNetworkFrame<BasicFrame, CharacterEntity, string>
    {
        private readonly Dictionary<int, EffectEnum> m_statById = new Dictionary<int, EffectEnum>()
        {
            {10, EffectEnum.AddStrength},
            {11, EffectEnum.AddVitality},
            {12, EffectEnum.AddWisdom},
            {13, EffectEnum.AddChance},
            {14, EffectEnum.AddAgility},
            {15, EffectEnum.AddIntelligence},
        };

        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message.Length < 2)
                return null;

            switch (message[0])
            {
                case 'c':
                    switch (message[1])
                    {
                        case 'C':
                            return ChatChannelEnable;
                    }
                    break;

                case 'g':
                    switch (message[1])
                    {
                        case 'P':
                            return GuildProfilUpdate;
                        case 'K':
                            return GuildKick;
                        case 'V':
                            return GuildCreationLeave;
                        case 'C':
                            return GuildCreationRequest;
                        case 'B':
                            return GuildBoostStats;
                        case 'H':
                            return GuildHireTaxcollector;
                        case 'F':
                            return GuildTaxCollectorRemove;
                        case 'f':
                            break;
                        case 'h':
                            break;
                        case 'b':
                            return GuildBoostSpell;
                        case 'J':
                            switch (message[2])
                            {
                                case 'R':
                                    return GuildJoinInvite;
                                case 'K':
                                    return GuildJoinAccept;
                                case 'E':
                                    return GuildJoinRefuse;
                            }
                            break;
                        case 'T':
                            switch (message[2])
                            {
                                case 'J':
                                    return GuildTaxCollectorJoin;
                                case 'V':
                                    return GuildTaxCollectorLeave;
                            }
                            break;
                        case 'I':
                            switch (message[2])
                            {
                                case 'M':
                                    return GuildMembersInformations;
                                case 'B':
                                    return GuildBoostInformations;
                                case 'G':
                                    return GuildGeneralInformations;
                                case 'F':
                                    break;
                                case 'H':
                                    break;
                                case 'T':
                                    if (message.Length > 3)
                                        return GuildTaxCollectorInterfaceLeave;
                                    else
                                        return GuildTaxCollectorsList;
                            }
                            break;
                    }
                    break;

                case 'P':
                    switch (message[1])
                    {
                        case 'I':
                            return PartyInvite;

                        case 'A':
                            return PartyAccept;

                        case 'R':
                            return PartyRefuse;

                        case 'W':
                            return PartyLocalize;

                        case 'V':
                            return PartyLeave;
                    }
                    break;
                case 'A':
                    switch (message[1])
                    {
                        case 'B':
                            return BoostStats;
                    }
                    break;

                case 'B':
                    switch (message[1])
                    {
                        case 'A':
                            return BasicCommand;

                        case 'D':
                            return BasicDate;

                        case 'T':
                            return BasicTime;

                        case 'M':
                            return BasicMessage;

                        case 'Y':
                            switch (message[2])
                            {
                                case 'A':
                                    return BasicAway;
                            }
                            break;

                        case 'a':
                            if (message.Length > 2 && message[2] == 'M')
                                return BasicAdminMapTeleport;
                            break;
                    }
                    break;

                case 'p':
                    if (message == "ping")
                        return BasicPong;
                    break;

                case 'q':
                    if (message == "qping")
                        return BasicQPong;
                    break;

                case 'r':
                    if (message.StartsWith("rpong"))
                        return BasicRPong;
                    break;

                case 'J':
                    if (message[1] == 'O')
                        return JobChangeOptions;
                    break;
            }

            return null;
        }

        private void JobChangeOptions(CharacterEntity character, string message)
        {
            var data = message.AsSpan(2);
            Span<Range> parts = stackalloc Range[4];
            var partCount = data.Split(parts, '|');

            if (partCount < 3
                || !int.TryParse(data[parts[0]], out var jobId)
                || !int.TryParse(data[parts[1]], out var optionParams)
                || !int.TryParse(data[parts[2]], out var minSlots))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() => character.CharacterJobs.ChangeOptions(jobId, optionParams, minSlots));
        }

        private void PartyLocalize(CharacterEntity character, string message)
        {

        }

        private void ChatChannelEnable(CharacterEntity character, string message)
        {
            var enabled = message[2] == '+';
            var channel = (ChatChannelEnum)message[3];
            character.SafeDispatch(WorldMessage.CHAT_CHANNEL(enabled, channel));
        }

        private void BasicAway(CharacterEntity character, string message)
        {
            character.AddMessage(character.SetAway);
        }

        private void BasicCommand(CharacterEntity character, string message)
        {
            if (!WorldCommandPermissions.CanUseStaffConsole(character))
            {
                Logger.Warn("BasicFrame::BasicCommand jugador sin permisos ha intentado usar la consola: " + character.Name + " -> " + message);
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var command = message.AsSpan(2).ToString();

            character.AddMessage(() =>
            {
                if (!WorldService.Instance.CommandManager.Execute(new WorldCommandContext(character, command)))
                    character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Comando no reconocido. Escribe help para ver la lista."));
                else
                    Logger.Info($"[COMANDO CONSOLA] nombre={character.Name} ip={character.Ip} comando={command}");
            });
        }

        private void GuildTaxCollectorRemove(CharacterEntity character, string message)
        {
            long taxCollectorId = -1;
            if (!long.TryParse(message.AsSpan(2), out taxCollectorId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
                {
                    if (!character.HasGameAction(GameActionTypeEnum.MAP))
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    TaxCollectorEntity taxCollector = character.Map.GetEntity(taxCollectorId) as TaxCollectorEntity;
                    if (taxCollector == null)
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    if (character.GuildMember == null)
                    {
                        character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    character.GuildMember.RemoveTaxCollector(taxCollector);
                });
        }

        private void GuildTaxCollectorInterfaceLeave(CharacterEntity character, string message)
        {
            if (character.GuildMember == null)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.GuildMember.TaxCollectorsInterfaceLeave();
        }

        public static void GuildTaxCollectorLeave(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
                {
                    if (character.GuildMember == null)
                    {
                        character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    if (!character.HasGameAction(GameActionTypeEnum.TAXCOLLECTOR_AGGRESSION))
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    character.StopAction(GameActionTypeEnum.TAXCOLLECTOR_AGGRESSION);
                });
        }

        private void GuildTaxCollectorJoin(CharacterEntity character, string message)
        {
            if (character.GuildMember == null)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (!long.TryParse(message.AsSpan(3), out long taxCollectorId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.GuildMember.TaxCollectorJoin(taxCollectorId);
        }

        private void GuildHireTaxcollector(CharacterEntity entity, string message)
        {
            entity.AddMessage(() =>
                {
                    if (!entity.HasGameAction(GameActionTypeEnum.MAP))
                    {
                        entity.SafeDispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_YOU_ARE_AWAY));
                        return;
                    }

                    if (entity.GuildMember == null)
                    {
                        entity.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    entity.GuildMember.HireTaxCollector();
                });
        }

        private void GuildBoostStats(CharacterEntity character, string message)
        {
            if (character.GuildMember == null)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.GuildMember.BoostGuildStats(message[2]);
        }

        private void GuildBoostSpell(CharacterEntity character, string message)
        {
            if (character.GuildMember == null)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (!int.TryParse(message.AsSpan(2), out int spellId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.GuildMember.BoostGuildSpell(spellId);
        }

        private void GuildTaxCollectorsList(CharacterEntity character, string message)
        {
            if (character.GuildMember == null)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.GuildMember.SendTaxCollectorsList();
            character.GuildMember.TaxCollectorsInterfaceJoin();
        }


        private void GuildBoostInformations(CharacterEntity character, string message)
        {
            if (character.GuildMember == null)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.GuildMember.SendBoostInformations();
        }

        private void GuildCreationRequest(CharacterEntity character, string message)
        {
            var guildData = message.AsSpan(2);
            Span<Range> guildParts = stackalloc Range[6];
            var guildPartCount = guildData.Split(guildParts, '|');
            if (guildPartCount < 5)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            int backId = -1, backColor = -1, symbolId = -1, symbolColor = -1;
            if (!int.TryParse(guildData[guildParts[0]], out backId) || !int.TryParse(guildData[guildParts[1]], out backColor) ||
                !int.TryParse(guildData[guildParts[2]], out symbolId) || !int.TryParse(guildData[guildParts[3]], out symbolColor))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var name = guildData[guildParts[4]].ToString();

            character.AddMessage(() =>
            {
                if (character.GuildMember != null)
                {
                    character.SafeDispatch(WorldMessage.GUILD_CREATION_ERROR_ALREADY_IN_GUILD());
                    return;
                }

                if (!character.HasGameAction(GameActionTypeEnum.GUILD_CREATE))
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                WorldService.Instance.AddMessage(() =>
                    {
                        if (GuildManager.Instance.Exists(name))
                        {
                            character.SafeDispatch(WorldMessage.GUILD_CREATION_ERROR_NAME_ALREADY_EXISTS());
                            return;
                        }

                        if (!GuildManager.Instance.Create(character, name, backId, backColor, symbolId, symbolColor))
                            return;

                        character.SafeDispatch(WorldMessage.GUILD_CREATION_SUCCESS());
                        character.GuildMember.SendGuildStats();
                        character.RefreshOnMap();

                        character.AddMessage(() => { character.StopAction(GameActionTypeEnum.GUILD_CREATE); });
                    });
            });
        }

        private void GuildCreationLeave(CharacterEntity character, string message)
        {
            character.AddMessage(() =>
                {
                    if (!character.HasGameAction(GameActionTypeEnum.GUILD_CREATE))
                    {
                        character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }
                    character.StopAction(GameActionTypeEnum.GUILD_CREATE);
                });
        }

        private void GuildKick(CharacterEntity character, string message)
        {
            if (character.GuildMember == null)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.GuildMember.MemberKick(message.AsSpan(2));
        }

        private void GuildProfilUpdate(CharacterEntity character, string message)
        {
            if (character.GuildMember == null)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var messageData = message.AsSpan(2);
            Span<Range> messageParts = stackalloc Range[5];
            var messagePartCount = messageData.Split(messageParts, '|');
            if (messagePartCount < 4)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            long profilId = -1;
            int rank = -1, xpSharePercent = -1, power = -1;
            if (!long.TryParse(messageData[messageParts[0]], out profilId) || !int.TryParse(messageData[messageParts[1]], out rank) ||
                !int.TryParse(messageData[messageParts[2]], out xpSharePercent) || !int.TryParse(messageData[messageParts[3]], out power))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.GuildMember.MemberProfilUpdate(profilId, rank, xpSharePercent, power);
        }

        public static void GuildJoinRefuse(CharacterEntity character, string message)
        {
            WorldService.Instance.AddMessage(() =>
                {

                    if (character.GuildInvitedPlayerId == -1 && character.GuildInviterPlayerId == -1)
                    {
                        character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    CharacterEntity distantCharacter = null;


                    if (character.GuildInvitedPlayerId != -1)
                        distantCharacter = EntityManager.Instance.GetCharacterById(character.GuildInvitedPlayerId);
                    else
                        distantCharacter = EntityManager.Instance.GetCharacterById(character.GuildInviterPlayerId);


                    if (distantCharacter != null)
                    {
                        if (character.Id == distantCharacter.GuildInvitedPlayerId)
                        {
                            character.SafeDispatch(WorldMessage.GUILD_JOIN_ERROR_REFUSED_LOCAL());
                            distantCharacter.SafeDispatch(WorldMessage.GUILD_JOIN_ERROR_REFUSED_DISTANT(character.Name));
                        }
                        else
                        {
                            character.SafeDispatch(WorldMessage.GUILD_JOIN_ERROR_REFUSED_LOCAL());
                            distantCharacter.SafeDispatch(WorldMessage.GUILD_JOIN_ERROR_REFUSED_LOCAL());
                        }
                        distantCharacter.GuildInvitedPlayerId = -1;
                        distantCharacter.GuildInviterPlayerId = -1;
                    }
                    character.GuildInvitedPlayerId = -1;
                    character.GuildInviterPlayerId = -1;
                });
        }

        private void GuildJoinAccept(CharacterEntity character, string message)
        {
            WorldService.Instance.AddMessage(() =>
                {

                    if (character.GuildInviterPlayerId == -1)
                    {
                        character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    var distantCharacter = EntityManager.Instance.GetCharacterById(character.GuildInviterPlayerId);

                    character.GuildInvitedPlayerId = -1;
                    character.GuildInviterPlayerId = -1;


                    if (distantCharacter == null)
                    {
                        character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    distantCharacter.GuildInvitedPlayerId = -1;
                    distantCharacter.GuildInviterPlayerId = -1;
                    distantCharacter.SafeDispatch(WorldMessage.GUILD_JOIN_ACCEPTED_DISTANT(character.Name));

                    distantCharacter.GuildMember.Guild.MemberJoin(character);

                    character.SafeDispatch(WorldMessage.GUILD_JOIN_ACCEPTED_LOCAL());
                    character.SafeDispatch(WorldMessage.GUILD_JOIN_CLOSE());
                });
        }

        private void GuildJoinInvite(CharacterEntity character, string message)
        {
            WorldService.Instance.AddMessage(() =>
                {

                    if (character.GuildMember == null)
                    {
                        character.SafeDispatch(WorldMessage.GUILD_JOIN_ERROR_UNKNOW());
                        return;
                    }

                    var distantCharacterName = message.AsSpan(3).ToString();


                    var distantCharacter = EntityManager.Instance.GetCharacterByName(distantCharacterName);
                    if (distantCharacter == null)
                    {
                        character.SafeDispatch(WorldMessage.GUILD_JOIN_ERROR_UNKNOW());
                        return;
                    }


                    if (distantCharacter.GuildMember != null)
                    {
                        character.SafeDispatch(WorldMessage.GUILD_JOIN_ERROR_ALREADY_IN_GUILD());
                        return;
                    }


                    if (character.GuildInvitedPlayerId != -1 ||
                        character.GuildInviterPlayerId != -1 ||
                        distantCharacter.GuildInvitedPlayerId != -1 ||
                        distantCharacter.GuildInviterPlayerId != -1)
                    {
                        character.SafeDispatch(WorldMessage.GUILD_JOIN_ERROR_OCCUPIED());
                        return;
                    }

                    if (!character.GuildMember.HasRight(GuildRightEnum.INVITE))
                    {
                        character.SafeDispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_GUILD_NOT_ENOUGH_RIGHTS));
                        return;
                    }

                    character.GuildInvitedPlayerId = distantCharacter.Id;
                    distantCharacter.GuildInviterPlayerId = character.Id;

                    character.SafeDispatch(WorldMessage.GUILD_JOIN_REQUEST_LOCAL(distantCharacterName));
                    distantCharacter.SafeDispatch(WorldMessage.GUILD_JOIN_REQUEST_DISTANT(character.Id, character.Name, character.GuildMember.Guild.Name));
                });
        }

        private void GuildGeneralInformations(CharacterEntity character, string message)
        {
            if (character.GuildMember == null)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.GuildMember.SendGeneralInformations();
        }

        private void GuildMembersInformations(CharacterEntity character, string message)
        {
            if (character.GuildMember == null)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.GuildMember.SendMembersInformations();
        }

        private void PartyInvite(CharacterEntity character, string message)
        {
            WorldService.Instance.AddMessage(() =>
                {
                    var distantCharacterName = message.AsSpan(2).ToString();


                    var distantCharacter = EntityManager.Instance.GetCharacterByName(distantCharacterName);
                    if (distantCharacter == null)
                    {
                        character.SafeDispatch(WorldMessage.PARTY_INVITE_ERROR_PLAYER_OFFLINE(distantCharacterName));
                        return;
                    }


                    if (distantCharacter.PartyId != -1 || distantCharacter.PartyInvitedPlayerId != -1 || distantCharacter.PartyInviterPlayerId != -1)
                    {
                        character.SafeDispatch(WorldMessage.PARTY_INVITE_ERROR_ALREADY_IN_PARTY());
                        return;
                    }


                    if (character.PartyInvitedPlayerId != -1 || character.PartyInviterPlayerId != -1)
                    {
                        character.SafeDispatch(WorldMessage.PARTY_INVITE_ERROR_ALREADY_IN_PARTY());
                        return;
                    }


                    var party = PartyManager.Instance.GetParty(character.PartyId);
                    if (party != null)
                    {
                        if (party.MemberCount > 7)
                        {
                            character.SafeDispatch(WorldMessage.PARTY_INVITE_ERROR_FULL());
                            return;
                        }
                    }

                    character.PartyInvitedPlayerId = distantCharacter.Id;
                    distantCharacter.PartyInviterPlayerId = character.Id;

                    message = WorldMessage.PARTY_INVITE_SUCCESS(character.Name, distantCharacterName);

                    character.SafeDispatch(message);
                    distantCharacter.SafeDispatch(message);
                });
        }

        public static void PartyRefuse(CharacterEntity character, string message)
        {
            WorldService.Instance.AddMessage(() =>
                {

                    if (character.PartyInvitedPlayerId == -1 && character.PartyInviterPlayerId == -1)
                    {
                        character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    CharacterEntity distantCharacter = null;


                    if (character.PartyInvitedPlayerId != -1)
                        distantCharacter = EntityManager.Instance.GetCharacterById(character.PartyInvitedPlayerId);
                    else
                        distantCharacter = EntityManager.Instance.GetCharacterById(character.PartyInviterPlayerId);


                    if (distantCharacter != null)
                    {
                        distantCharacter.PartyInvitedPlayerId = -1;
                        distantCharacter.PartyInviterPlayerId = -1;
                        distantCharacter.SafeDispatch(WorldMessage.PARTY_REFUSE());
                    }

                    character.PartyInvitedPlayerId = -1;
                    character.PartyInviterPlayerId = -1;
                    character.SafeDispatch(WorldMessage.PARTY_REFUSE());
                });
        }

        private void PartyAccept(CharacterEntity character, string message)
        {
            WorldService.Instance.AddMessage(() =>
            {

                if (character.PartyInviterPlayerId == -1)
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                var distantCharacter = EntityManager.Instance.GetCharacterById(character.PartyInviterPlayerId);

                character.PartyInvitedPlayerId = -1;
                character.PartyInviterPlayerId = -1;


                if (distantCharacter == null)
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                distantCharacter.PartyInvitedPlayerId = -1;
                distantCharacter.PartyInviterPlayerId = -1;

                distantCharacter.SafeDispatch(WorldMessage.PARTY_REFUSE());


                if (distantCharacter.PartyId != -1)
                {
                    var party = PartyManager.Instance.GetParty(distantCharacter.PartyId);
                    if (party == null)
                    {
                        character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    party.AddMember(character);
                    return;
                }


                PartyManager.Instance.CreateParty(distantCharacter, character);
            });
        }

        private void PartyLeave(CharacterEntity character, string message)
        {
            WorldService.Instance.AddMessage(() =>
            {

                if (character.PartyId == -1)
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }


                var party = PartyManager.Instance.GetParty(character.PartyId);
                if (party == null)
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }


                if (message == "PV")
                {
                    party.RemoveMember(character);
                    return;
                }

                long kickedPlayerId = -1;
                if (!long.TryParse(message.AsSpan(2), out kickedPlayerId))
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                party.KickMember(character, kickedPlayerId);
            });
        }

        private void BoostStats(CharacterEntity character, string message)
        {
            if (!int.TryParse(message.AsSpan(2), out int statId))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (!m_statById.TryGetValue(statId, out EffectEnum effect))
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            character.AddMessage(() =>
            {
                var actualValue = character.Statistics.GetEffect(effect).Base;
                var boostValue = statId == 11 && character.Breed == CharacterBreedEnum.BREED_SACRIEUR ? 2 : 1;
                var requiredPoint = GenericStats.GetRequiredStatsPoint(character.Breed, statId, actualValue);

                if (character.CaractPoint < requiredPoint)
                {
                    character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }

                character.CaractPoint -= requiredPoint;

                switch (effect)
                {
                    case EffectEnum.AddStrength:
                        character.DatabaseRecord.Strength += boostValue;
                        break;

                    case EffectEnum.AddVitality:
                        character.DatabaseRecord.Vitality += boostValue;
                        break;

                    case EffectEnum.AddWisdom:
                        character.DatabaseRecord.Wisdom += boostValue;
                        break;

                    case EffectEnum.AddIntelligence:
                        character.DatabaseRecord.Intelligence += boostValue;
                        break;

                    case EffectEnum.AddAgility:
                        character.DatabaseRecord.Agility += boostValue;
                        break;

                    case EffectEnum.AddChance:
                        character.DatabaseRecord.Chance += boostValue;
                        break;
                }

                character.Statistics.AddBase(effect, boostValue);
                character.Dispatch(WorldMessage.ACCOUNT_STATS(character));
            });
        }

        private void BasicPong(CharacterEntity character, string message)
        {
            character.SafeDispatch(WorldMessage.BASIC_PONG());
        }

        private void BasicQPong(CharacterEntity character, string message)
        {
            character.SafeDispatch(WorldMessage.BASIC_QPONG());
        }

        private void BasicRPong(CharacterEntity character, string message)
        {
            if (!long.TryParse(message.AsSpan(5), out long sentAt))
                return;
            long rtt = Environment.TickCount64 - sentAt;
            if (rtt < 0 || rtt > 10000)
                return;
            character.RttMs = (character.RttMs * 3 + rtt) / 4;
        }

        private void BasicDate(CharacterEntity character, string message)
        {
            character.SafeDispatch(WorldMessage.BASIC_DATE());
        }

        private void BasicTime(CharacterEntity character, string message)
        {
            character.SafeDispatch(WorldMessage.BASIC_TIME());
        }

        private void BasicMessage(CharacterEntity character, string message)
        {
            var messageData = message.AsSpan(2);
            Span<Range> messageParts = stackalloc Range[4];
            var messagePartCount = messageData.Split(messageParts, '|');
            if (messagePartCount < 2)
            {
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            var channel = messageData[messageParts[0]].ToString();
            var messageContent = messageData[messageParts[1]].ToString();

            if (channel.Length == 1)
            {
                if (messagePartCount > 2)
                    messageContent = messageContent + "|" + messageData[messageParts[2]].ToString();
                if (!Enum.IsDefined(typeof(ChatChannelEnum), (int)channel[0]))
                {
                    character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                    return;
                }
                character.AddMessage(() => character.DispatchChatMessage((ChatChannelEnum)channel[0], messageContent));
            }
            else
            {
                WorldService.Instance.AddMessage(() =>
                    {
                        var remoteEntity = EntityManager.Instance.GetCharacterByName(channel);
                        if (remoteEntity == null)
                        {
                            character.SafeDispatch(WorldMessage.CHAT_MESSAGE_ERROR_PLAYER_OFFLINE());
                            return;
                        }

                        character.AddMessage(() =>
                        {
                            if (character.DispatchChatMessage(ChatChannelEnum.CHANNEL_PRIVATE_SEND, messageContent, remoteEntity))
                            {
                                remoteEntity.AddMessage(() => remoteEntity.DispatchChatMessage(ChatChannelEnum.CHANNEL_PRIVATE_RECEIVE, messageContent, character));
                            }
                        });
                    });
            }
        }

        private void BasicAdminMapTeleport(CharacterEntity character, string message)
        {
            if (!WorldCommandPermissions.HasRole(character, StaffRole.GameMaster))
            {
                Logger.Warn("BasicFrame::BasicAdminMapTeleport jugador sin permisos ha intentado usar teletransporte de mapa: " + character.Name + " -> " + message);
                character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }


            var parts = message.AsSpan(3);
            var separatorIndex = parts.IndexOf(',');
            var yData = separatorIndex < 0 ? ReadOnlySpan<char>.Empty : parts.Slice(separatorIndex + 1);
            var nextSeparatorIndex = yData.IndexOf(',');
            if (nextSeparatorIndex >= 0)
                yData = yData.Slice(0, nextSeparatorIndex);

            if (separatorIndex < 0 || !int.TryParse(parts.Slice(0, separatorIndex), out int x) || !int.TryParse(yData, out int y))
                return;

            int superAreaId = character.Map.SubArea.Area.SuperAreaId;

            character.AddMessage(() =>
            {
                MapInstance map = MapManager.Instance.GetByCoordinates(x, y, superAreaId);

                if (map == null)
                    return;

                if (character.HasGameAction(GameActionTypeEnum.FIGHT))
                    return;

                int celda = map.RandomFreeCell();
                if (celda == -1) return;

                character.CloseCurrentInteraction();
                character.Teleport(map.Id, celda);
            });
        }
    }
}



