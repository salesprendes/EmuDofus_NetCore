using Game.Spell;
using System.Collections.Generic;

namespace Game.Job.Forjamagia
{
    public sealed class ConfiguracionForjamagia
    {
        public static ConfiguracionForjamagia PorDefecto { get; } = new ConfiguracionForjamagia();
        public IReadOnlyDictionary<EffectEnum, double> PesosUnitarios { get; init; }
        public bool PermitirOvermax { get; init; } = true;
        public bool PermitirExo { get; init; } = true;
        public double LimitePesoOvermax { get; init; } = 101.0;
        public int MultiplicadorValorRecomendado { get; init; } = 20;
        public bool AplicarPenalizacionValorRecomendado { get; init; } = true;
        public double PenalizacionValorRecomendadoPorMultiploExcedido { get; init; } = 35.0;
        public double PenalizacionMaximaValorRecomendado { get; init; } = 60.0;
        public IReadOnlySet<EffectEnum> EstadisticasBloqueadas { get; init; } = new HashSet<EffectEnum>();
        public IReadOnlySet<EffectEnum> EstadisticasExoDuras { get; init; } = new HashSet<EffectEnum> { EffectEnum.STAT_MAS_PA, EffectEnum.STAT_MAS_PM, EffectEnum.STAT_MAS_ALCANCE };
        public double MultiplicadorProbabilidadExoDura { get; init; } = 0.4;
        public double FactorPenalizacionPesoRunaExo { get; init; } = 1.0;
        public int ProbabilidadEntradaEnRango { get; init; } = 90;
        public double ProbabilidadBaseEntradaOvermax { get; init; } = 80.0;
        public double PenalizacionOvermaxPorUnidad { get; init; } = 35.0;
        public double ToleranciaOvermaxPorNivel { get; init; } = 0.5;
        public double ToleranciaSaturacionPorNivel { get; init; } = 1.0;
        public double PenalizacionSaturacionPorTolerancia { get; init; } = 20.0;
        public double PenalizacionMaximaSaturacion { get; init; } = 70.0;
        public double ProbabilidadBaseEntradaExo { get; init; } = 35.0;
        public int ProbabilidadBaseCritica { get; init; } = 15;
        public double MultiplicadorExperienciaExitoCritico { get; init; } = 1.0;
        public double MultiplicadorExperienciaExitoNeutro { get; init; } = 1.0;
        public double MultiplicadorExperienciaFalloCritico { get; init; } = 0.0;
        public long ExperienciaMinima { get; init; } = 1;
        public ConfiguracionForjamagia() => PesosUnitarios = PesosPorDefecto();
        public double PesoUnitario(EffectEnum efecto) => PesosUnitarios.TryGetValue(efecto, out var peso) ? peso : 0.0;
        public bool EsForjable(EffectEnum efecto) => PesosUnitarios.ContainsKey(efecto);
        public double PesoRuna(RunaForjamagia runa) => CalculoPesos.DesdeCenti(PesoRunaCenti(runa));
        public int PesoUnitarioCenti(EffectEnum efecto) => CalculoPesos.ACenti(PesoUnitario(efecto));
        public int PesoRunaCenti(RunaForjamagia runa) => CalculoPesos.Multiplicar(PesoUnitarioCenti(runa.Estadistica), runa.Potencia);
        public int LimitePesoOvermaxCenti() => CalculoPesos.ACenti(LimitePesoOvermax);
        public int ValorMaximoRecomendado(RunaForjamagia runa) => runa.Potencia * MultiplicadorValorRecomendado;
        public int ValorMaximo(EffectEnum efecto) => PesoUnitarioCenti(efecto) is var peso && peso > 0 ? LimitePesoOvermaxCenti() / peso : 0;
        public double UmbralPesoExoExcluyente { get; init; } = 50.0;
        public bool EsExoExcluyente(EffectEnum efecto) => PesoUnitario(efecto) >= UmbralPesoExoExcluyente;

        private static Dictionary<EffectEnum, double> PesosPorDefecto()
        {
            return new Dictionary<EffectEnum, double>
            {
                { EffectEnum.STAT_MAS_FUERZA, 1.0 },
                { EffectEnum.STAT_MAS_INTELIGENCIA, 1.0 },
                { EffectEnum.STAT_MAS_AGILIDAD, 1.0 },
                { EffectEnum.STAT_MAS_SUERTE, 1.0 },
                { EffectEnum.STAT_MAS_VITALIDAD, 0.25 },
                { EffectEnum.STAT_MAS_SABIDURIA, 3.0 },
                { EffectEnum.STAT_MAS_PA, 100.0 },
                { EffectEnum.STAT_MAS_PM, 90.0 },
                { EffectEnum.STAT_MAS_ALCANCE, 51.0 },
                { EffectEnum.STAT_MAS_DANO, 20.0 },
                { EffectEnum.STAT_MAS_DANO_PORCENTAJE, 2.0 },
                { EffectEnum.STAT_MAS_DANO_CRITICO, 30.0 },
                { EffectEnum.STAT_MAS_DANO_DEVUELTO, 30.0 },
                { EffectEnum.STAT_MAS_CURAS, 20.0 },
                { EffectEnum.STAT_MAS_INVOCACIONES_MAX, 30.0 },
                { EffectEnum.STAT_MAS_PODS, 0.1 },
                { EffectEnum.STAT_MAS_INICIATIVA, 0.1 },
                { EffectEnum.STAT_MAS_PROSPECCION, 3.0 },
                { EffectEnum.STAT_MAS_DANO_TRAMPA, 15.0 },
                { EffectEnum.STAT_MAS_RESISTENCIA_FUEGO, 2.0 },
                { EffectEnum.STAT_MAS_RESISTENCIA_AIRE, 2.0 },
                { EffectEnum.STAT_MAS_RESISTENCIA_AGUA, 2.0 },
                { EffectEnum.STAT_MAS_RESISTENCIA_TIERRA, 2.0 },
                { EffectEnum.STAT_MAS_RESISTENCIA_NEUTRAL, 2.0 },
                { EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_FUEGO, 6.0 },
                { EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AIRE, 6.0 },
                { EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_AGUA, 6.0 },
                { EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_TIERRA, 6.0 },
                { EffectEnum.STAT_MAS_RESISTENCIA_PORCENTAJE_NEUTRAL, 6.0 },
            };
        }
    }
}
