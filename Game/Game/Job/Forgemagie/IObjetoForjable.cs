using Game.Spell;
using System.Collections.Generic;

namespace Game.Job.Forjamagia
{
    public interface IObjetoForjable
    {
        int Nivel { get; }

        (int Min, int Max)? ObtenerRangoPlantilla(EffectEnum efecto);

        int ObtenerValor(EffectEnum efecto);

        void EstablecerValor(EffectEnum efecto, int valor);

        IEnumerable<EffectEnum> EfectosActuales { get; }

        IEnumerable<EffectEnum> EfectosPlantilla { get; }

        double Pozo { get; set; }

        int PesoCentiPozo { get; set; }
    }
}
