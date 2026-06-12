using Game.Spell;
using System;
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

        public IReadOnlySet<EffectEnum> EstadisticasExoDuras { get; init; } = new HashSet<EffectEnum> { EffectEnum.AddAP, EffectEnum.AddMP, EffectEnum.AddPO };

        public double MultiplicadorProbabilidadExoDura { get; init; } = 0.4;

        public double FactorPenalizacionPesoRunaExo { get; init; } = 1.0;

        public IReadOnlyList<IReadOnlySet<EffectEnum>> GruposExoIncompatibles { get; init; } =
    new List<IReadOnlySet<EffectEnum>>
    {
                new HashSet<EffectEnum> { EffectEnum.AddAP, EffectEnum.AddMP },
    };

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

        public ConfiguracionForjamagia()
        {
            PesosUnitarios = PesosPorDefecto();
        }

        public double PesoUnitario(EffectEnum efecto)
        {
            return PesosUnitarios.TryGetValue(efecto, out var peso) ? peso : 0.0;
        }

        public bool EsForjable(EffectEnum efecto)
        {
            return PesosUnitarios.ContainsKey(efecto);
        }

        public double PesoRuna(RunaForjamagia runa)
        {
            return CalculoPesos.DesdeCenti(PesoRunaCenti(runa));
        }

        public int PesoUnitarioCenti(EffectEnum efecto)
        {
            return CalculoPesos.ACenti(PesoUnitario(efecto));
        }

        public int PesoRunaCenti(RunaForjamagia runa)
        {
            return CalculoPesos.Multiplicar(PesoUnitarioCenti(runa.Estadistica), runa.Potencia);
        }

        public int LimitePesoOvermaxCenti()
        {
            return CalculoPesos.ACenti(LimitePesoOvermax);
        }

        public int ValorMaximo(EffectEnum efecto)
        {
            var peso = PesoUnitarioCenti(efecto);
            return peso <= 0 ? 0 : LimitePesoOvermaxCenti() / peso;
        }

        public int ValorMaximoRecomendado(RunaForjamagia runa)
        {
            return runa.Potencia * MultiplicadorValorRecomendado;
        }

        private static Dictionary<EffectEnum, double> PesosPorDefecto()
        {
            return new Dictionary<EffectEnum, double>
            {
                { EffectEnum.AddStrength, 1.0 },
                { EffectEnum.AddIntelligence, 1.0 },
                { EffectEnum.AddAgility, 1.0 },
                { EffectEnum.AddChance, 1.0 },
                { EffectEnum.AddVitality, 0.25 },
                { EffectEnum.AddWisdom, 3.0 },
                { EffectEnum.AddAP, 100.0 },
                { EffectEnum.AddMP, 90.0 },
                { EffectEnum.AddPO, 51.0 },
                { EffectEnum.AddDamage, 20.0 },
                { EffectEnum.AddDamagePercent, 2.0 },
                { EffectEnum.AddDamageCritic, 30.0 },
                { EffectEnum.AddReflectDamage, 30.0 },
                { EffectEnum.AddHealCare, 20.0 },
                { EffectEnum.AddInvocationMax, 30.0 },
                { EffectEnum.AddPods, 0.1 },
                { EffectEnum.AddInitiative, 0.1 },
                { EffectEnum.AddProspection, 3.0 },
                { EffectEnum.AddDamagePiege, 15.0 },
                { EffectEnum.AddReduceDamageFire, 2.0 },
                { EffectEnum.AddReduceDamageAir, 2.0 },
                { EffectEnum.AddReduceDamageWater, 2.0 },
                { EffectEnum.AddReduceDamageEarth, 2.0 },
                { EffectEnum.AddReduceDamageNeutral, 2.0 },
                { EffectEnum.AddReduceDamagePercentFire, 6.0 },
                { EffectEnum.AddReduceDamagePercentAir, 6.0 },
                { EffectEnum.AddReduceDamagePercentWater, 6.0 },
                { EffectEnum.AddReduceDamagePercentEarth, 6.0 },
                { EffectEnum.AddReduceDamagePercentNeutral, 6.0 },
            };
        }
    }
}
