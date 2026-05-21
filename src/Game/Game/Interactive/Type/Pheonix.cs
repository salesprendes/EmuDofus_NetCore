using Game.Entity;
using Game.Job;
using Game.Map;
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
    public sealed class Pheonix : InteractiveObject
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="map"></param>
        /// <param name="cellId"></param>
        public Pheonix(MapInstance map, int cellId) 
            : base(map, cellId)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <param name="skill"></param>
        public override void UseWithSkill(CharacterEntity character, JobSkill skill)
        {
            if (character == null)
                return;

            if (skill != null && skill.Id != SkillIdEnum.SKILL_USE_PHOENIX)
                return;

            ReleasePlayer(character);
        }

        /// <summary>
        /// 
        /// </summary>
        private void ReleasePlayer(CharacterEntity character)
        {
            if (!character.IsGhost)
                return;

            character.Reborn();
        }
    }
}


