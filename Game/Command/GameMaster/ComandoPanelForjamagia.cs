using Game.Action;
using Game.Entity;
using Game.Interactive.Type;
using Game.Job;
using Game.Job.Skill;
using Game.Manager;
using Game.Network;
using System;
using System.Collections.Generic;

namespace Game.Command
{
    public sealed class ComandoPanelForjamagia : WorldStaffCommand
    {
        private static readonly SkillIdEnum[] HabilidadesMagicas =
        {
            SkillIdEnum.SKILL_REFORGER_UNE_EPEE,
            SkillIdEnum.SKILL_REFORGER_UNE_DAGUE,
            SkillIdEnum.SKILL_REFORGER_UN_MARTEAU,
            SkillIdEnum.SKILL_REFORGER_UNE_PELLE,
            SkillIdEnum.SKILL_REFORGER_UNE_HACHE,
            SkillIdEnum.SKILL_RESCULPTER_UN_ARC,
            SkillIdEnum.SKILL_RESCULPTER_UNE_BAGUETTE,
            SkillIdEnum.SKILL_RESCULPTER_UN_BATON,
            SkillIdEnum.SKILL_AMELIORER_DES_BOTTES,
            SkillIdEnum.SKILL_AMELIORER_UNE_CEINTURE,
            SkillIdEnum.SKILL_AMELIORER_UN_ANNEAU,
            SkillIdEnum.SKILL_AMELIORER_UNE_AMULETTE,
            SkillIdEnum.SKILL_AMELIORER_UN_CHAPEAU,
            SkillIdEnum.SKILL_AMELIORER_UNE_CAPE,
            SkillIdEnum.SKILL_AMELIORER_UN_SAC,
        };

        private static readonly Dictionary<JobIdEnum, SkillIdEnum> HabilidadPredeterminadaPorOficio = new Dictionary<JobIdEnum, SkillIdEnum>
        {
            { JobIdEnum.JOB_FORGEMAGE_EPEES, SkillIdEnum.SKILL_REFORGER_UNE_EPEE },
            { JobIdEnum.JOB_FORGEMAGE_DE_DAGUES, SkillIdEnum.SKILL_REFORGER_UNE_DAGUE },
            { JobIdEnum.JOB_FORGEMAGE_DE_MARTEAUX, SkillIdEnum.SKILL_REFORGER_UN_MARTEAU },
            { JobIdEnum.JOB_FORGEMAGE_DE_PELLES, SkillIdEnum.SKILL_REFORGER_UNE_PELLE },
            { JobIdEnum.JOB_FORGEMAGE_DE_HACHES, SkillIdEnum.SKILL_REFORGER_UNE_HACHE },
            { JobIdEnum.JOB_SCULPTEMAGE_ARCS, SkillIdEnum.SKILL_RESCULPTER_UN_ARC },
            { JobIdEnum.JOB_SCULPTEMAGE_DE_BAGUETTES, SkillIdEnum.SKILL_RESCULPTER_UNE_BAGUETTE },
            { JobIdEnum.JOB_SCULPTEMAGE_DE_BATONS, SkillIdEnum.SKILL_RESCULPTER_UN_BATON },
            { JobIdEnum.JOB_CORDOMAGE, SkillIdEnum.SKILL_AMELIORER_DES_BOTTES },
            { JobIdEnum.JOB_JOAILLOMAGE, SkillIdEnum.SKILL_AMELIORER_UN_ANNEAU },
            { JobIdEnum.JOB_COSTUMAGE, SkillIdEnum.SKILL_AMELIORER_UN_CHAPEAU },
        };

        private static readonly Dictionary<string, SkillIdEnum> AliasHabilidades = new Dictionary<string, SkillIdEnum>(StringComparer.OrdinalIgnoreCase)
        {
            { "epee", SkillIdEnum.SKILL_REFORGER_UNE_EPEE },
            { "epees", SkillIdEnum.SKILL_REFORGER_UNE_EPEE },
            { "espada", SkillIdEnum.SKILL_REFORGER_UNE_EPEE },
            { "dagas", SkillIdEnum.SKILL_REFORGER_UNE_DAGUE },
            { "dague", SkillIdEnum.SKILL_REFORGER_UNE_DAGUE },
            { "daga", SkillIdEnum.SKILL_REFORGER_UNE_DAGUE },
            { "marteau", SkillIdEnum.SKILL_REFORGER_UN_MARTEAU },
            { "martillo", SkillIdEnum.SKILL_REFORGER_UN_MARTEAU },
            { "pelle", SkillIdEnum.SKILL_REFORGER_UNE_PELLE },
            { "pala", SkillIdEnum.SKILL_REFORGER_UNE_PELLE },
            { "hache", SkillIdEnum.SKILL_REFORGER_UNE_HACHE },
            { "hacha", SkillIdEnum.SKILL_REFORGER_UNE_HACHE },
            { "arc", SkillIdEnum.SKILL_RESCULPTER_UN_ARC },
            { "arco", SkillIdEnum.SKILL_RESCULPTER_UN_ARC },
            { "baguette", SkillIdEnum.SKILL_RESCULPTER_UNE_BAGUETTE },
            { "varita", SkillIdEnum.SKILL_RESCULPTER_UNE_BAGUETTE },
            { "baton", SkillIdEnum.SKILL_RESCULPTER_UN_BATON },
            { "baston", SkillIdEnum.SKILL_RESCULPTER_UN_BATON },
            { "bottes", SkillIdEnum.SKILL_AMELIORER_DES_BOTTES },
            { "botas", SkillIdEnum.SKILL_AMELIORER_DES_BOTTES },
            { "ceinture", SkillIdEnum.SKILL_AMELIORER_UNE_CEINTURE },
            { "cinturon", SkillIdEnum.SKILL_AMELIORER_UNE_CEINTURE },
            { "anneau", SkillIdEnum.SKILL_AMELIORER_UN_ANNEAU },
            { "anillo", SkillIdEnum.SKILL_AMELIORER_UN_ANNEAU },
            { "amulette", SkillIdEnum.SKILL_AMELIORER_UNE_AMULETTE },
            { "amuleto", SkillIdEnum.SKILL_AMELIORER_UNE_AMULETTE },
            { "amu", SkillIdEnum.SKILL_AMELIORER_UNE_AMULETTE },
            { "chapeau", SkillIdEnum.SKILL_AMELIORER_UN_CHAPEAU },
            { "coiffe", SkillIdEnum.SKILL_AMELIORER_UN_CHAPEAU },
            { "sombrero", SkillIdEnum.SKILL_AMELIORER_UN_CHAPEAU },
            { "cape", SkillIdEnum.SKILL_AMELIORER_UNE_CAPE },
            { "capa", SkillIdEnum.SKILL_AMELIORER_UNE_CAPE },
            { "sac", SkillIdEnum.SKILL_AMELIORER_UN_SAC },
            { "saco", SkillIdEnum.SKILL_AMELIORER_UN_SAC },
        };

        private readonly string[] _aliases = { "forjamagia", "fm" };

        public override string[] Aliases => _aliases;
        public override string Description => "Abre el panel de forjamagia. Uso: fm [%skill|oficio%] [%nombreJugador%]";
        protected override StaffRole RequiredRole => StaffRole.GameMaster;

        protected override void Process(WorldCommandContext context)
        {
            var primerArgumento = context.TextCommandArgument.NextWord();
            SkillIdEnum? habilidadSolicitada = null;
            string nombreObjetivo = null;

            if (!string.IsNullOrEmpty(primerArgumento))
            {
                if (IntentarResolverHabilidadMagica(primerArgumento, out var idHabilidad))
                {
                    habilidadSolicitada = idHabilidad;
                    nombreObjetivo = context.TextCommandArgument.NextWord();
                }
                else
                {
                    nombreObjetivo = primerArgumento;
                }
            }

            var objetivo = string.IsNullOrEmpty(nombreObjetivo) ? context.Character : EntityManager.Instance.GetCharacterByName(nombreObjetivo);

            if (objetivo == null)
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Personaje '" + nombreObjetivo + "' no encontrado o no esta conectado."));
                return;
            }

            if (objetivo.HasGameAction(GameActionTypeEnum.FIGHT))
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("No puedes abrir forjamagia mientras el personaje esta en combate."));
                return;
            }

            var habilidad = habilidadSolicitada.HasValue ? objetivo.CharacterJobs.GetSkill((int)habilidadSolicitada.Value) : ObtenerPrimeraHabilidadMagica(objetivo);

            if (habilidad is not MagicSkill)
            {
                context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("El personaje no tiene ese oficio de forjamagia disponible."));
                return;
            }

            objetivo.CloseCurrentInteraction();

            var plan = new CraftPlan(objetivo.Map, objetivo.CellId);
            plan.UseWithSkill(objetivo, habilidad);

            context.Character.Dispatch(WorldMessage.BASIC_CONSOLE_MESSAGE("Panel de forjamagia abierto para " + objetivo.Name + " (" + habilidad.Id + ")."));
        }

        private static JobSkill ObtenerPrimeraHabilidadMagica(CharacterEntity objetivo)
        {
            foreach (var skillId in HabilidadesMagicas)
            {
                var habilidad = objetivo.CharacterJobs.GetSkill((int)skillId);
                if (habilidad is MagicSkill)
                    return habilidad;
            }

            return null;
        }

        private static bool IntentarResolverHabilidadMagica(string valor, out SkillIdEnum idHabilidad)
        {
            idHabilidad = default;
            if (string.IsNullOrWhiteSpace(valor))
                return false;

            if (AliasHabilidades.TryGetValue(valor, out idHabilidad))
                return true;

            if (int.TryParse(valor, out var numerico))
            {
                if (EsHabilidadMagica((SkillIdEnum)numerico))
                {
                    idHabilidad = (SkillIdEnum)numerico;
                    return true;
                }

                if (HabilidadPredeterminadaPorOficio.TryGetValue((JobIdEnum)numerico, out idHabilidad))
                    return true;

                return false;
            }

            if (Enum.TryParse(valor, true, out SkillIdEnum habilidadParseada) && EsHabilidadMagica(habilidadParseada))
            {
                idHabilidad = habilidadParseada;
                return true;
            }

            var nombreOficio = valor.StartsWith("JOB_", StringComparison.OrdinalIgnoreCase) ? valor : "JOB_" + valor;

            if (Enum.TryParse(nombreOficio, true, out JobIdEnum oficioParseado)
                && HabilidadPredeterminadaPorOficio.TryGetValue(oficioParseado, out idHabilidad))
            {
                return true;
            }

            return false;
        }

        private static bool EsHabilidadMagica(SkillIdEnum idHabilidad)
        {
            foreach (var habilidadMagica in HabilidadesMagicas)
            {
                if (habilidadMagica == idHabilidad)
                    return true;
            }

            return false;
        }
    }
}
