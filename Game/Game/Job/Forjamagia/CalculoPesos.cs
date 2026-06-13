using System;
using System.Globalization;

namespace Game.Job.Forjamagia
{
    public static class CalculoPesos
    {
        public const int Escala = 100;
        
        public static int ACenti(double peso) => (int)Math.Round(peso * Escala, MidpointRounding.AwayFromZero);
        public static double DesdeCenti(int pesoCenti) => pesoCenti / (double)Escala;
        public static int Multiplicar(int pesoUnitarioCenti, int puntos) => pesoUnitarioCenti * Math.Max(0, puntos);
        public static string Formatear(int pesoCenti) => DesdeCenti(pesoCenti).ToString("0.##", CultureInfo.InvariantCulture);
    }
}
