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
    /// <summary>
    /// 
    /// </summary>
    public sealed class PaddockDoor : InteractiveObject
    {
        /// <summary>
        /// 
        /// </summary>
        private Paddock m_paddock;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="map"></param>
        /// <param name="cellId"></param>
        public PaddockDoor(MapInstance map, int cellId)
            :base(map, cellId)
        {
            m_paddock = map.Paddock;
            if (m_paddock == null)
                Logger.Info("No hay cercado asociado al mapa " + map.Id);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="skill"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        public void Access(CharacterEntity character)
        {
            if (m_paddock.Public)
            {
                character.ExchangePaddock(m_paddock);
            }
            else if (!m_paddock.OnSale)
            {
                // TODO : if in the same guild and has enough rights
            }
            else
            {
                // Intento de entrar en un cercado en venta.
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        public void Buy(CharacterEntity character)
        {
            if (m_paddock.OnSale)
            {

            }
            else
            {
                // Intento invalido de comprar un cercado publico o que ya tiene dueño.
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                Logger.Info("PaddockDoor::Buy() se ha intentado comprar un cercado publico o ya comprado: " + character.Name);
            }
        }
    }
}


