using Protocolo.Framework.Network;
using Game.Database.Repository;
using Game.Database.Structure;
using Game.Entity;
using Game.Manager;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Frame
{
    public sealed class SocialFrame : AbstractNetworkFrame<SocialFrame, CharacterEntity, string>
    {
        public override Action<CharacterEntity, string> GetHandler(string message)
        {
            if (message.Length < 2)
                return null;

            switch (message[0])
            {
                case 'F':
                    switch (message[1])
                    {
                        case 'L':
                            return FriendsList;

                        case 'A':
                            return AddFriend;

                        case 'D':
                            return RemoveFriend;

                        case 'O':
                            return FriendNotifyChange;
                    }
                    break;

                case 'i':
                    switch (message[1])
                    {
                        case 'L':
                            return EnnemiesList;

                        case 'A':
                            return AddEnnemy;

                        case 'D':
                            return RemoveEnnemy;
                    }
                    break;
            }

            return null;
        }

        private void EnnemiesList(CharacterEntity character, string message)
        {
            character.SafeDispatch(WorldMessage.ENNEMIES_LIST(character.Account.Pseudo, character.Ennemies));
        }

        private void RemoveEnnemy(CharacterEntity character, string message)
        {
            WorldService.Instance.AddMessage(() =>
                {
                    var pseudo = message.AsSpan(3);
                    SocialRelationDAO relation = null;
                    foreach (var ennemy in character.Ennemies)
                    {
                        if (!pseudo.SequenceEqual(ennemy.Pseudo))
                            continue;
                        relation = ennemy;
                        break;
                    }
                    if (relation == null)
                    {
                        character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    SocialRelationRepository.Instance.Removed(relation);

                    character.SafeDispatch(WorldMessage.ENNEMY_DELETE_SUCCESS());
                });
        }

        private void AddEnnemy(CharacterEntity character, string message)
        {
            WorldService.Instance.AddMessage(() =>
            {
                var name = message.AsSpan(2);
                CharacterEntity target = null;
                if (name[0] == '%')
                {
                    var nameText = name.Slice(1).ToString();
                    target = EntityManager.Instance.GetCharacterByName(nameText);
                    if (target == null)
                        target = EntityManager.Instance.GetCharacterByNickname(nameText);
                }
                else
                    target = EntityManager.Instance.GetCharacterByName(name.ToString());

                if (target == null || target.Account == null)
                {
                    character.SafeDispatch(WorldMessage.ENNEMY_ADD_ERROR_UNKNOW_PLAYER());
                    return;
                }

                if (target.Id == character.Id)
                {
                    character.SafeDispatch(WorldMessage.ENNEMY_ADD_ERROR_YOURSELF());
                    return;
                }

                if (character.HasFriend(target.Pseudo))
                {
                    character.SafeDispatch(WorldMessage.FRIEND_ADD_ERROR_ALREADY());
                    return;
                }

                if (character.HasEnnemy(target.Pseudo))
                {
                    character.SafeDispatch(WorldMessage.ENNEMY_ADD_ERROR_ALREADY());
                    return;
                }

                if (character.Ennemies.Count() >= 100)
                {
                    character.SafeDispatch(WorldMessage.ENNEMY_ADD_ERROR_FULL());
                    return;
                }

                var relation = SocialRelationRepository.Instance.Create(character.AccountId, target.Account.Pseudo, (int)SocialRelationTypeEnum.TYPE_ENNEMY);

                character.SafeDispatch(WorldMessage.ENNEMY_ADD(character.Account.Pseudo, relation));
            });
        }

        private void FriendNotifyChange(CharacterEntity character, string message)
        {
            WorldService.Instance.AddMessage(() => character.NotifyOnFriendConnection = message[2] == '+');
        }

        private void FriendsList(CharacterEntity character, string message)
        {
            WorldService.Instance.AddMessage(() => character.SafeDispatch(WorldMessage.FRIENDS_LIST(character.Account.Pseudo, character.Friends)));
        }

        private void RemoveFriend(CharacterEntity character, string message)
        {
            WorldService.Instance.AddMessage(() =>
                {
                    var pseudo = message.AsSpan(3);
                    SocialRelationDAO relation = null;
                    foreach (var friend in character.Friends)
                    {
                        if (!pseudo.SequenceEqual(friend.Pseudo))
                            continue;
                        relation = friend;
                        break;
                    }
                    if (relation == null)
                    {
                        character.SafeDispatch(WorldMessage.BASIC_NO_OPERATION());
                        return;
                    }

                    SocialRelationRepository.Instance.Removed(relation);

                    character.SafeDispatch(WorldMessage.FRIEND_DELETE_SUCCESS());
                });
        }

        private void AddFriend(CharacterEntity character, string message)
        {
            WorldService.Instance.AddMessage(() =>
                {
                    var name = message.AsSpan(2);
                    CharacterEntity target = null;
                    if (name[0] == '%')
                    {
                        var nameText = name.Slice(1).ToString();
                        target = EntityManager.Instance.GetCharacterByName(nameText);
                        if (target == null)
                            target = EntityManager.Instance.GetCharacterByNickname(nameText);
                    }
                    else
                        target = EntityManager.Instance.GetCharacterByName(name.ToString());

                    if (target == null || target.Account == null)
                    {
                        character.SafeDispatch(WorldMessage.FRIEND_ADD_ERROR_UNKNOW_PLAYER());
                        return;
                    }

                    if (target.Id == character.Id)
                    {
                        character.SafeDispatch(WorldMessage.FRIEND_ADD_ERROR_YOURSELF());
                        return;
                    }

                    if (character.HasFriend(target.Pseudo))
                    {
                        character.SafeDispatch(WorldMessage.FRIEND_ADD_ERROR_ALREADY());
                        return;
                    }

                    if (character.HasEnnemy(target.Pseudo))
                    {
                        character.SafeDispatch(WorldMessage.ENNEMY_ADD_ERROR_ALREADY());
                        return;
                    }

                    if (character.Friends.Count() >= 100)
                    {
                        character.SafeDispatch(WorldMessage.FRIEND_ADD_ERROR_FULL());
                        return;
                    }

                    var relation = SocialRelationRepository.Instance.Create(character.AccountId, target.Account.Pseudo, (int)SocialRelationTypeEnum.TYPE_FRIEND);

                    character.SafeDispatch(WorldMessage.FRIEND_ADD(character.Account.Pseudo, relation));
                });
        }
    }
}


