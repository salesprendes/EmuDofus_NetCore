using Game.Database.Structure;
using Game.Spell;
using System;
using System.Collections.Generic;

namespace Game.Job.Forjamagia
{
    public sealed class AdaptadorObjetoForjable : IObjetoForjable
    {
        private readonly ItemDAO m_item;

        public AdaptadorObjetoForjable(ItemDAO item)
        {
            m_item = item;
        }

        public ItemDAO Objeto => m_item;

        public int Nivel => m_item.Template.Level;

        public (int Min, int Max)? ObtenerRangoPlantilla(EffectEnum efecto)
        {
            foreach (var entry in m_item.Template.RandomEffects)
            {
                if (entry.Type == efecto)
                    return (Math.Min(entry.Minimum, entry.Maximum), Math.Max(entry.Minimum, entry.Maximum));
            }

            return null;
        }

        public int ObtenerValor(EffectEnum efecto)
        {
            if (!m_item.Statistics.HasEffect(efecto))
                return 0;

            var entry = m_item.Statistics.GetEffect(efecto);


            return entry.Value1 != 0 || entry.Value3 == 0 ? entry.Value1 : entry.Value3;
        }

        public void EstablecerValor(EffectEnum efecto, int valor)
        {
            var stats = m_item.Statistics;

            if (valor <= 0)
            {
                stats.RemoveEffect(efecto);
                return;
            }

            if (stats.HasEffect(efecto))
            {
                var entry = stats.GetEffect(efecto);
                entry.Value1 = valor;
                entry.Value2 = 0;
                entry.Value3 = 0;
                entry.Args = "0";
            }
            else
            {
                stats.AddEffect(efecto, valor);
            }
        }

        public IEnumerable<EffectEnum> EfectosActuales
        {
            get => new List<EffectEnum>(m_item.Statistics.Effects.Keys);
        }

        public IEnumerable<EffectEnum> EfectosPlantilla
        {
            get
            {
                var effects = new List<EffectEnum>();
                foreach (var entry in m_item.Template.RandomEffects)
                {
                    if (!effects.Contains(entry.Type))
                        effects.Add(entry.Type);
                }
                return effects;
            }
        }

        public double Pozo
        {
            get => m_item.ForgemagiePuits;
            set => m_item.ForgemagiePuits = value;
        }

        public int PesoCentiPozo
        {
            get => CalculoPesos.ACenti(m_item.ForgemagiePuits);
            set => m_item.ForgemagiePuits = CalculoPesos.DesdeCenti(value);
        }
    }
}
