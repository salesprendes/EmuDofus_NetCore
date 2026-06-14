using Protocolo.Framework.Generic;
using Game.Database.Structure;
using Game.ActionEffect;
using Game.Entity;
using Game.Spell;
using Game.Network;
using System.Collections.Generic;

namespace Game.Manager
{
    public sealed class ActionEffectManager : Singleton<ActionEffectManager>
    {
        private readonly Dictionary<EffectEnum, IActionEffect> m_effectById;
        private readonly Dictionary<ItemTypeEnum, List<IActionEffect>> m_effectByType;

        public ActionEffectManager()
        {
            m_effectById = new Dictionary<EffectEnum, IActionEffect>
            {
                {EffectEnum.BDD_SALIR_DIALOGO, DialogLeaveEffect.Instance},
                {EffectEnum.BDD_RESPUESTA_DIALOGO, DialogReplyEffect.Instance},
                {EffectEnum.BDD_ABRIR_BANCO, OpenBankEffect.Instance},
                {EffectEnum.BDD_SUMAR_ESTADISTICA, AddStatsEffect.Instance},
                {EffectEnum.BDD_SUMAR_OBJETO, AddItemEffect.Instance},
                {EffectEnum.BDD_TELETRANSPORTAR, TeleportEffect.Instance},
                {EffectEnum.BDD_REINICIAR_STATS, ResetStatsEffect.Instance},
                {EffectEnum.BDD_REINICIAR_HECHIZOS, ResetSpellEffect.Instance},
                {EffectEnum.BDD_APRENDER_OFICIO, AddJobEffect.Instance},
                {EffectEnum.BDD_QUITAR_OBJETO, RemoveItemEffect.Instance},
                {EffectEnum.BDD_CREAR_GREMIO, GuildCreationEffect.Instance},
                {EffectEnum.BDD_INICIAR_COMBATE, StartFightEffect.Instance},
                {EffectEnum.COMBATE_INICIAR, StartFightEffect.Instance},
                {EffectEnum.OFICIO_APRENDER, AddJobEffect.Instance},
                {EffectEnum.PRISMA_COLOCAR, PlacePrismEffect.Instance},
                {EffectEnum.ALINEAMIENTO_CAMBIAR, ChangeAlignmentEffect.Instance},
                {EffectEnum.MOVIMIENTO_TELETRANSPORTAR_ZAAP_GUARDADO, RecallEffect.Instance},
                {EffectEnum.STAT_MAS_VIDA, AddLifeEffect.Instance},
                {EffectEnum.KAMAS_MAS, AddKamasEffect.Instance},
                {EffectEnum.BOOST_MAS, AddBoostEffect.Instance},
                {EffectEnum.STAT_MAS_ENERGIA, AddEnergyEffect.Instance},
                {EffectEnum.EXPERIENCIA_MAS, AddExperienceEffect.Instance},
                {EffectEnum.HECHIZO_APRENDER, AddSpellEffect.Instance},
                {EffectEnum.HECHIZO_MAS_PUNTOS, AddSpellpointEffect.Instance},
                {EffectEnum.CARACTERISTICA_MAS_VITALIDAD, AddStatsEffect.Instance},
                {EffectEnum.CARACTERISTICA_MAS_SABIDURIA, AddStatsEffect.Instance},
                {EffectEnum.CARACTERISTICA_MAS_FUERZA, AddStatsEffect.Instance},
                {EffectEnum.CARACTERISTICA_MAS_INTELIGENCIA, AddStatsEffect.Instance},
                {EffectEnum.CARACTERISTICA_MAS_AGILIDAD, AddStatsEffect.Instance},
                {EffectEnum.CARACTERISTICA_MAS_SUERTE, AddStatsEffect.Instance},
                {EffectEnum.STAT_MAS_VITALIDAD, AddStatsEffect.Instance},
                {EffectEnum.STAT_MAS_SABIDURIA, AddStatsEffect.Instance},
                {EffectEnum.STAT_MAS_FUERZA, AddStatsEffect.Instance},
                {EffectEnum.STAT_MAS_INTELIGENCIA, AddStatsEffect.Instance},
                {EffectEnum.STAT_MAS_AGILIDAD, AddStatsEffect.Instance},
                {EffectEnum.STAT_MAS_SUERTE, AddStatsEffect.Instance}
            };
            m_effectByType = new Dictionary<ItemTypeEnum, List<IActionEffect>> { { ItemTypeEnum.TYPE_FEE_ARTIFICE, new List<IActionEffect>() { } } };
        }

        public void ApplyEffects(CharacterEntity character, long itemId, long targetId = -1, int targetCell = -1, Dictionary<string, string> parameters = null)
        {
            var item = character.Inventory.GetItem(itemId);
            if (item == null)
                return;

            if (!item.Template.Usable && !item.Template.Buff && !item.Template.Targetable && targetId != -1 && targetCell != -1)
            {
                Logger.Info($"ActionEffectManager::Apply objeto no usable, sin bonificacion ni objetivo objeto={item.Template.Name} personaje={character.Name}");
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
                return;
            }

            if (!item.SatisfyConditions(character))
            {
                character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_CONDITIONS_UNSATISFIED));
                return;
            }

            // Los fuegos artificiales (tipo 74) solo pueden lanzarse a cielo abierto.
            if (item.Template.Type == (int)ItemTypeEnum.TYPE_FEE_ARTIFICE && !character.Map.Outdoor)
            {
                character.Dispatch(WorldMessage.INFORMATION_MESSAGE(InformationTypeEnum.ERROR, InformationEnum.ERROR_CONDITIONS_UNSATISFIED));
                return;
            }

            var used = false;
            if (item.StringEffects != string.Empty)
            {
                foreach (var effect in item.Statistics.Effects.Values)
                {
                    if (m_effectById.ContainsKey(effect.EffectType))
                    {
                        used = m_effectById[effect.EffectType].ProcessItem(character, item, effect, targetId, targetCell) || used;
                    }
                }
            }
            else
            {
                if (m_effectByType.ContainsKey((ItemTypeEnum)item.Template.Type))
                {
                    foreach (var effect in m_effectByType[(ItemTypeEnum)item.Template.Type])
                    {
                        used = effect.ProcessItem(character, item, null, targetId, targetCell) || used;
                    }
                }
            }

            if (used)
                character.Inventory.RemoveItem(itemId);
            else
                character.Dispatch(WorldMessage.BASIC_NO_OPERATION());
        }

        public void ApplyEffect(CharacterEntity character, EffectEnum effect, Dictionary<string, string> parameters)
        {
            if (m_effectById.ContainsKey(effect))
                m_effectById[effect].Process(character, parameters);
        }
    }
}
