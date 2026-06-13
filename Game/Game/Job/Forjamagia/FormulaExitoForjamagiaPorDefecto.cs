using System;

namespace Game.Job.Forjamagia
{
    public interface IFormulaExitoForjamagia
    {
        ResultadoForjamagia Tirar(ContextoForjamagia contexto);
    }

    public sealed class FormulaExitoForjamagiaPorDefecto : IFormulaExitoForjamagia
    {

        private readonly Func<int, int, int> m_rng;

        public FormulaExitoForjamagiaPorDefecto(Func<int, int, int> rng = null)
        {
            m_rng = rng ?? Util.Next;
        }

        public ResultadoForjamagia Tirar(ContextoForjamagia contexto)
        {
            var probabilidades = Estimar(contexto);

            if (Tirar100() >= probabilidades.ProbabilidadEntrada)
                return ResultadoForjamagia.FalloCritico;

            return Tirar100() < probabilidades.ProbabilidadTiradaCritica ? ResultadoForjamagia.ExitoCritico : ResultadoForjamagia.ExitoNeutro;
        }

        public ProbabilidadesForjamagia Estimar(ContextoForjamagia contexto)
        {
            var configuracion = contexto.Configuracion;

            var valorMaximo = configuracion.ValorMaximo(contexto.Runa.Estadistica);
            if (valorMaximo > 0 && contexto.ValorNuevo > valorMaximo)
                return new ProbabilidadesForjamagia(0, 0);


            double probabilidadEntrada;
            if (contexto.EsExo)
            {
                probabilidadEntrada = configuracion.ProbabilidadBaseEntradaExo;
                if (configuracion.EstadisticasExoDuras.Contains(contexto.Runa.Estadistica))
                    probabilidadEntrada *= configuracion.MultiplicadorProbabilidadExoDura;
                probabilidadEntrada -= contexto.PesoRuna * configuracion.FactorPenalizacionPesoRunaExo;
            }
            else if (contexto.EsOver)
            {
                var nivelObjeto = Math.Max(1, contexto.Objeto.Nivel);
                var puntosOver = contexto.ValorNuevo - contexto.MaximoPlantilla;
                var pesoOver = CalculoPesos.DesdeCenti(CalculoPesos.Multiplicar(configuracion.PesoUnitarioCenti(contexto.Runa.Estadistica), puntosOver));
                var tolerancia = Math.Max(1.0, nivelObjeto * configuracion.ToleranciaOvermaxPorNivel);
                probabilidadEntrada = configuracion.ProbabilidadBaseEntradaOvermax - (pesoOver / tolerancia) * configuracion.PenalizacionOvermaxPorUnidad;
            }
            else
            {
                probabilidadEntrada = configuracion.ProbabilidadEntradaEnRango;
            }

            probabilidadEntrada += contexto.NivelMago / 4.0;
            if (contexto.PesoCentiPozo > 0)
                probabilidadEntrada += Math.Min(10.0, contexto.Pozo);

            var penalizacionRecomendada = PenalizacionValorRecomendado(contexto);
            var penalizacionSaturacion = PenalizacionSaturacion(contexto);
            probabilidadEntrada -= penalizacionRecomendada;
            probabilidadEntrada -= penalizacionSaturacion;

            probabilidadEntrada = Limitar(probabilidadEntrada, 2, 98);

            double probabilidadCritica = configuracion.ProbabilidadBaseCritica + contexto.NivelMago / 5.0;
            if (contexto.EstaEnJet)
                probabilidadCritica += 25;
            if (contexto.EsOver || contexto.EsExo)
                probabilidadCritica -= 10;
            if (contexto.Pozo > 0)
                probabilidadCritica += Math.Min(15.0, contexto.Pozo / 2.0);
            if (penalizacionRecomendada > 0)
                probabilidadCritica -= Math.Min(20.0, penalizacionRecomendada / 3.0);
            if (penalizacionSaturacion > 0)
                probabilidadCritica -= Math.Min(25.0, penalizacionSaturacion / 2.0);

            probabilidadCritica = Limitar(probabilidadCritica, 1, 95);

            return new ProbabilidadesForjamagia(probabilidadEntrada, probabilidadCritica);
        }

        private double Tirar100()
        {
            return m_rng(0, 100);
        }

        private static double Limitar(double valor, double minimo, double maximo)
        {
            return valor < minimo ? minimo : (valor > maximo ? maximo : valor);
        }

        private static double PenalizacionValorRecomendado(ContextoForjamagia contexto)
        {
            var configuracion = contexto.Configuracion;
            if (!configuracion.AplicarPenalizacionValorRecomendado)
                return 0;

            var recomendado = configuracion.ValorMaximoRecomendado(contexto.Runa);
            if (recomendado <= 0 || contexto.ValorNuevo <= recomendado)
                return 0;

            var ratioExcedido = (contexto.ValorNuevo - recomendado) / (double)recomendado;
            return Math.Min(configuracion.PenalizacionMaximaValorRecomendado, ratioExcedido * configuracion.PenalizacionValorRecomendadoPorMultiploExcedido);
        }

        private static double PenalizacionSaturacion(ContextoForjamagia contexto)
        {
            var configuracion = contexto.Configuracion;
            var pesoCentiOver = Math.Max(contexto.PesoCentiOverObjeto, contexto.PesoCentiOverObjetoNuevo);
            if (pesoCentiOver <= 0)
                return 0;

            var nivelObjeto = Math.Max(1, contexto.Objeto.Nivel);
            var tolerancia = Math.Max(1.0, nivelObjeto * configuracion.ToleranciaSaturacionPorNivel);
            var penalizacion = (CalculoPesos.DesdeCenti(pesoCentiOver) / tolerancia) * configuracion.PenalizacionSaturacionPorTolerancia;
            return Math.Min(configuracion.PenalizacionMaximaSaturacion, penalizacion);
        }
    }
}
