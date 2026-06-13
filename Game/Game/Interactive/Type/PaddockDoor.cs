using Game.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Entity;
using Game.Job;
using Game.Database.Structure;
using Game.Mount;
using Game.Manager;
using Game.Network;

namespace Game.Interactive.Type
{
    public sealed class PaddockDoor : InteractiveObject
    {
        private Paddock m_paddock;

        public PaddockDoor(MapInstance map, int cellId)
    : base(map, cellId)
        {
            m_paddock = map.Paddock;
            if (m_paddock == null)
                Logger.Info($"No hay cercado asociado al mapa {map.Id}");
        }

        public override void UseWithSkill(CharacterEntity character, JobSkill skill)
        {
            if (m_paddock == null)
            {
                base.UseWithSkill(character, skill);
            }
            else
            {
                switch (skill.Id)
                {
                    case SkillIdEnum.SKILL_ACCEDER:
                        Access(character);
                        break;

                    case SkillIdEnum.SKILL_ACHETER_ENCLOS:
                        Buy(character);
                        break;
                }
            }
        }

        public void Access(CharacterEntity character)
        {
            if (m_paddock.Public)
            {
                character.ExchangePaddock(m_paddock);
            }
            else if (!m_paddock.OnSale)
            {

            }
            else
            {

                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
            }
        }

        public void Buy(CharacterEntity character)
        {
            if (m_paddock.OnSale)
            {

            }
            else
            {

                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                Logger.Info($"PaddockDoor::Buy() se ha intentado comprar un cercado publico o ya comprado: {character.Name}");
            }
        }
    }
}


