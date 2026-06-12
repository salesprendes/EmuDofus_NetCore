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
    public sealed class CraftPlan : InteractiveObject
    {
        public const int FRAME_STOP_CRAFT = 1;

        public const int FRAME_CRAFTING = 2;

        private int m_craftersCount;

        public CraftPlan(MapInstance map, int cellId)
    : base(map, cellId)
        {
        }

        public override void UseWithSkill(CharacterEntity character, JobSkill skill)
        {
            switch (skill.Id)
            {
                case SkillIdEnum.SKILL_SCIER:
                case SkillIdEnum.SKILL_COUDRE_UN_CHAPEAU:
                case SkillIdEnum.SKILL_COUDRE_UNE_CAPE:
                case SkillIdEnum.SKILL_CONFECTIONNER_UNE_CEINTURE:
                case SkillIdEnum.SKILL_CONFECTIONNER_DES_BOTTES:
                case SkillIdEnum.SKILL_CREER_UN_ANNEAU:
                case SkillIdEnum.SKILL_CREER_UNE_AMULETTE:
                case SkillIdEnum.SKILL_FORGER_UN_BOUCLIER:
                case SkillIdEnum.SKILL_FORGER_UNE_DAGUE:
                case SkillIdEnum.SKILL_FORGER_UNE_HACHE:
                case SkillIdEnum.SKILL_SCULPTER_UN_ARC:
                case SkillIdEnum.SKILL_SCULPTER_UNE_BAGUETTE:
                case SkillIdEnum.SKILL_SCULPTER_UN_BATON:
                case SkillIdEnum.SKILL_FORGER_UN_MARTEAU:
                case SkillIdEnum.SKILL_FORGER_UNE_FAUX:
                case SkillIdEnum.SKILL_FORGER_UNE_PIOCHE:
                case SkillIdEnum.SKILL_FORGER_UNE_EPEE:
                case SkillIdEnum.SKILL_COUDRE_UN_SAC:
                case SkillIdEnum.SKILL_FORGER_UNE_PELLE:
                case SkillIdEnum.SKILL_CUIRE_DU_PAIN:
                case SkillIdEnum.SKILL_EGRENER:
                case SkillIdEnum.SKILL_MOUDRE:
                case SkillIdEnum.SKILL_FONDRE:
                case SkillIdEnum.SKILL_POLIR_UNE_PIERRE:
                case SkillIdEnum.SKILL_PREPARER_UN_POISSON:
                case SkillIdEnum.SKILL_PREPARER_UNE_POTION:
                case SkillIdEnum.SKILL_PREPARER_UNE_VIANDE:
                case SkillIdEnum.SKILL_CONFECTIONNER_UNE_CLEF:
                case SkillIdEnum.SKILL_BRICOLER:
                case SkillIdEnum.SKILL_PREPARER:
                case SkillIdEnum.SKILL_VIDER_POISSON:

                case SkillIdEnum.SKILL_REFORGER_UNE_EPEE:
                case SkillIdEnum.SKILL_REFORGER_UNE_DAGUE:
                case SkillIdEnum.SKILL_REFORGER_UN_MARTEAU:
                case SkillIdEnum.SKILL_REFORGER_UNE_PELLE:
                case SkillIdEnum.SKILL_REFORGER_UNE_HACHE:
                case SkillIdEnum.SKILL_RESCULPTER_UN_ARC:
                case SkillIdEnum.SKILL_RESCULPTER_UNE_BAGUETTE:
                case SkillIdEnum.SKILL_RESCULPTER_UN_BATON:
                case SkillIdEnum.SKILL_AMELIORER_DES_BOTTES:
                case SkillIdEnum.SKILL_AMELIORER_UNE_CEINTURE:
                case SkillIdEnum.SKILL_AMELIORER_UN_ANNEAU:
                case SkillIdEnum.SKILL_AMELIORER_UNE_AMULETTE:
                case SkillIdEnum.SKILL_AMELIORER_UN_CHAPEAU:
                case SkillIdEnum.SKILL_AMELIORER_UNE_CAPE:
                case SkillIdEnum.SKILL_AMELIORER_UN_SAC:
                    Craft(character, skill);
                    break;
            }
        }

        private void Craft(CharacterEntity character, JobSkill skill)
        {
            character.CraftStart(this, skill);

            m_craftersCount++;

            UpdateFrame(FRAME_CRAFTING, FRAME_CRAFTING, true);
        }

        public void StopCraft()
        {
            m_craftersCount--;

            if (m_craftersCount == 0)
                UpdateFrame(FRAME_STOP_CRAFT, FRAME_NORMAL, true);
        }
    }
}


