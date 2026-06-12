using System;
using System.Globalization;

namespace Game.Job.Forjamagia
{
    public sealed class ProbabilidadesForjamagia
    {
        public static ProbabilidadesForjamagia Imposible { get; } = new ProbabilidadesForjamagia(0, 0);

        public ProbabilidadesForjamagia(double probabilidadEntrada, double probabilidadTiradaCritica)
        {
            ProbabilidadEntrada = Limitar(probabilidadEntrada);
            ProbabilidadTiradaCritica = Limitar(probabilidadTiradaCritica);
            ProbabilidadExitoCritico = ProbabilidadEntrada * ProbabilidadTiradaCritica / 100.0;
            ProbabilidadExitoNeutro = Math.Max(0, ProbabilidadEntrada - ProbabilidadExitoCritico);
            ProbabilidadFalloCritico = Math.Max(0, 100.0 - ProbabilidadEntrada);
        }

        public double ProbabilidadEntrada { get; }

        public double ProbabilidadTiradaCritica { get; }

        public double ProbabilidadExitoCritico { get; }

        public double ProbabilidadExitoNeutro { get; }

        public double ProbabilidadFalloCritico { get; }

        public override string ToString()
        {
            return "SC " + Formatear(ProbabilidadExitoCritico) + "% | SN " + Formatear(ProbabilidadExitoNeutro) + "% | EC " + Formatear(ProbabilidadFalloCritico) + "%";
        }

        private static string Formatear(double valor)
        {
            return valor.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static double Limitar(double valor)
        {
            return valor < 0 ? 0 : valor > 100 ? 100 : valor;
        }
    }
}
