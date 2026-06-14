using Protocolo.Framework.Generic;
using Game.Database.Structure;
using Game.Spell;
using System;
using System.Collections.Generic;

namespace Game.Job.Forjamagia
{
    public sealed class GestorRunasForjamagia : Singleton<GestorRunasForjamagia>
    {
        private static readonly Dictionary<int, RunaForjamagia> TablaRunas = new Dictionary<int, RunaForjamagia>
        {

            { 1519, new RunaForjamagia(EffectEnum.STAT_MAS_FUERZA, 1, RangoRuna.Simple) },
            { 1521, new RunaForjamagia(EffectEnum.STAT_MAS_SABIDURIA, 1, RangoRuna.Simple) },
            { 1522, new RunaForjamagia(EffectEnum.STAT_MAS_INTELIGENCIA, 1, RangoRuna.Simple) },
            { 1523, new RunaForjamagia(EffectEnum.STAT_MAS_VITALIDAD, 3, RangoRuna.Simple) },
            { 1524, new RunaForjamagia(EffectEnum.STAT_MAS_AGILIDAD, 1, RangoRuna.Simple) },
            { 1525, new RunaForjamagia(EffectEnum.STAT_MAS_SUERTE, 1, RangoRuna.Simple) },
            { 1545, new RunaForjamagia(EffectEnum.STAT_MAS_FUERZA, 3, RangoRuna.Pa) },
            { 1546, new RunaForjamagia(EffectEnum.STAT_MAS_SABIDURIA, 3, RangoRuna.Pa) },
            { 1547, new RunaForjamagia(EffectEnum.STAT_MAS_INTELIGENCIA, 3, RangoRuna.Pa) },
            { 1548, new RunaForjamagia(EffectEnum.STAT_MAS_VITALIDAD, 10, RangoRuna.Pa) },
            { 1549, new RunaForjamagia(EffectEnum.STAT_MAS_AGILIDAD, 3, RangoRuna.Pa) },
            { 1550, new RunaForjamagia(EffectEnum.STAT_MAS_SUERTE, 3, RangoRuna.Pa) },
            { 1551, new RunaForjamagia(EffectEnum.STAT_MAS_FUERZA, 10, RangoRuna.Ra) },
            { 1552, new RunaForjamagia(EffectEnum.STAT_MAS_SABIDURIA, 10, RangoRuna.Ra) },
            { 1553, new RunaForjamagia(EffectEnum.STAT_MAS_INTELIGENCIA, 10, RangoRuna.Ra) },
            { 1554, new RunaForjamagia(EffectEnum.STAT_MAS_VITALIDAD, 30, RangoRuna.Ra) },
            { 1555, new RunaForjamagia(EffectEnum.STAT_MAS_AGILIDAD, 10, RangoRuna.Ra) },
            { 1556, new RunaForjamagia(EffectEnum.STAT_MAS_SUERTE, 10, RangoRuna.Ra) },


            { 1557, new RunaForjamagia(EffectEnum.STAT_MAS_PA, 1, RangoRuna.Ga) },
            { 1558, new RunaForjamagia(EffectEnum.STAT_MAS_PM, 1, RangoRuna.Ga) },


            { 7433, new RunaForjamagia(EffectEnum.STAT_MAS_DANO_CRITICO, 1) },
            { 7434, new RunaForjamagia(EffectEnum.STAT_MAS_CURAS, 1) },
            { 7435, new RunaForjamagia(EffectEnum.STAT_MAS_DANO, 1) },
            { 7436, new RunaForjamagia(EffectEnum.STAT_MAS_DANO_PORCENTAJE, 1) },
            { 7437, new RunaForjamagia(EffectEnum.STAT_MAS_DANO_DEVUELTO, 1) },
            { 7438, new RunaForjamagia(EffectEnum.STAT_MAS_ALCANCE, 1) },
            { 7442, new RunaForjamagia(EffectEnum.STAT_MAS_INVOCACIONES_MAX, 1) },
            { 7443, new RunaForjamagia(EffectEnum.STAT_MAS_PODS, 10) },
            { 7444, new RunaForjamagia(EffectEnum.STAT_MAS_PODS, 30) },
            { 7445, new RunaForjamagia(EffectEnum.STAT_MAS_PODS, 100) },
            { 7446, new RunaForjamagia(EffectEnum.STAT_MAS_DANO_TRAMPA, 1) },
            { 7448, new RunaForjamagia(EffectEnum.STAT_MAS_INICIATIVA, 10) },
            { 7449, new RunaForjamagia(EffectEnum.STAT_MAS_INICIATIVA, 30) },
            { 7450, new RunaForjamagia(EffectEnum.STAT_MAS_INICIATIVA, 100) },
            { 7451, new RunaForjamagia(EffectEnum.STAT_MAS_PROSPECCION, 1) },


            { 7452, new RunaForjamagia(EffectEnum.STAT_MAS_RESISTENCIA_FUEGO, 1) },
            { 7453, new RunaForjamagia(EffectEnum.STAT_MAS_RESISTENCIA_AIRE, 1) },
            { 7454, new RunaForjamagia(EffectEnum.STAT_MAS_RESISTENCIA_AGUA, 1) },
            { 7455, new RunaForjamagia(EffectEnum.STAT_MAS_RESISTENCIA_TIERRA, 1) },
            { 7456, new RunaForjamagia(EffectEnum.STAT_MAS_RESISTENCIA_NEUTRAL, 1) },


            { 7457, new RunaForjamagia(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO, 1) },
            { 7458, new RunaForjamagia(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE, 1) },
            { 7459, new RunaForjamagia(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA, 1) },
            { 7460, new RunaForjamagia(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL, 1) },
            { 7560, new RunaForjamagia(EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA, 1) },


            { 10613, new RunaForjamagia(EffectEnum.STAT_MAS_DANO_TRAMPA, 3, RangoRuna.Pa) },
            { 10618, new RunaForjamagia(EffectEnum.STAT_MAS_DANO_PORCENTAJE, 3, RangoRuna.Pa) },
            { 10619, new RunaForjamagia(EffectEnum.STAT_MAS_DANO_PORCENTAJE, 10, RangoRuna.Ra) },
            { 10662, new RunaForjamagia(EffectEnum.STAT_MAS_PROSPECCION, 3, RangoRuna.Pa) },
        };

        private readonly Dictionary<int, RunaForjamagia> m_sobrescrituras = new Dictionary<int, RunaForjamagia>();

        public void Registrar(int idPlantillaRuna, EffectEnum estadistica, int potencia, RangoRuna rango = RangoRuna.Desconocido)
        {
            m_sobrescrituras[idPlantillaRuna] = new RunaForjamagia(estadistica, potencia, rango);
        }

        public bool EsRuna(ItemDAO item)
        {
            return item?.Template != null && (ItemTypeEnum)item.Template.Type == ItemTypeEnum.TYPE_RUNE_FORGEMAGIE;
        }

        public RunaForjamagia Resolver(ItemDAO runa)
        {
            if (runa?.Template == null)
                return default;

            if (m_sobrescrituras.TryGetValue(runa.TemplateId, out var sobrescrita))
                return sobrescrita;

            if (TablaRunas.TryGetValue(runa.TemplateId, out var conocida))
                return conocida;


            foreach (var effect in runa.Template.RandomEffects)
            {
                if (!ConfiguracionForjamagia.PorDefecto.EsForjable(effect.Type))
                    continue;

                var potencia = Math.Max(effect.Minimum, effect.Maximum);
                if (potencia <= 0)
                    potencia = 1;

                return new RunaForjamagia(effect.Type, potencia);
            }

            return default;
        }
    }
}
