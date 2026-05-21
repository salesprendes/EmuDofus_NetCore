using Game.Database.Repository;
using Game.Database.Structure;
using Game.Entity;
using Game.Job;
using Game.Map;
using Game.Manager;
using Game.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Interactive.Type
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class Waypoint : InteractiveObject
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cellId"></param>
        public Waypoint(MapInstance map, int cellId)
            : base(map, cellId)
        {
            WaypointManager.Instance.AddWaypoint(Map.Id, this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="skill"></param>
        public override void UseWithSkill(CharacterEntity character, JobSkill skill)
        {
            if (character.AddWaypoint(Map.Id))
            {
                character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_WAYPOINT_REGISTERED));
            }

            switch(skill.Id)
            { 
                case SkillIdEnum.SKILL_SAUVEGARDER:
                    Save(character);
                    break;

                case SkillIdEnum.SKILL_UTILISER_ZAAP:
                    Use(character);
                    break;
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        public void Save(CharacterEntity character)
        {
            character.SavedMapId = Map.Id;
            character.SavedCellId = character.CellId;
            character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.INFO, InformationEnum.INFO_WAYPOINT_SAVED));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        public void Use(CharacterEntity character)
        {            
            character.WaypointStart(this);
        }
    }
}


