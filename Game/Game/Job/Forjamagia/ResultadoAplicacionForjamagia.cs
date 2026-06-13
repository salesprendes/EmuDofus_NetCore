using Game.Spell;
using System;
using System.Collections.Generic;

namespace Game.Job.Forjamagia
{
    public sealed class ResultadoAplicacionForjamagia
    {
        public ResultadoForjamagia Resultado { get; init; }
        public RunaForjamagia RunaAplicada { get; init; }
        public EffectEnum? EfectoGanado { get; init; }
        public int CantidadGanada { get; init; }
        public IReadOnlyList<PerdidaEfecto> EfectosPerdidos { get; init; } = Array.Empty<PerdidaEfecto>();
        public ProbabilidadesForjamagia Probabilidades { get; init; } = ProbabilidadesForjamagia.Imposible;
        public int PesoCentiReliquatAntes { get; init; }
        public int PesoCentiReliquatDespues { get; init; }
        public double ReliquatAntes => CalculoPesos.DesdeCenti(PesoCentiReliquatAntes);
        public double ReliquatDespues => CalculoPesos.DesdeCenti(PesoCentiReliquatDespues);
        public string MensajeLog { get; init; } = string.Empty;
        public bool ObjetoModificado => EfectoGanado != null || EfectosPerdidos.Count > 0;
    }
}
