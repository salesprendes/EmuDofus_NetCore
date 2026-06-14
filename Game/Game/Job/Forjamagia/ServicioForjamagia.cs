using Game.Spell;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Job.Forjamagia
{
    public sealed class ServicioForjamagia
    {
        private readonly ConfiguracionForjamagia m_configuracion;
        private readonly IFormulaExitoForjamagia m_formula;
        private readonly Func<ContextoForjamagia, ProbabilidadesForjamagia> m_estimarProbabilidades;

        public ServicioForjamagia(ConfiguracionForjamagia configuracion = null, IFormulaExitoForjamagia formula = null, Func<ContextoForjamagia, ProbabilidadesForjamagia> estimarProbabilidades = null)
        {
            m_configuracion = configuracion ?? ConfiguracionForjamagia.PorDefecto;
            var formulaPorDefecto = new FormulaExitoForjamagiaPorDefecto();
            m_formula = formula ?? formulaPorDefecto;
            m_estimarProbabilidades = estimarProbabilidades ?? formulaPorDefecto.Estimar;
        }

        public ConfiguracionForjamagia Configuracion => m_configuracion;

        public bool PuedeAplicarse(IObjetoForjable objeto, RunaForjamagia runa, out string motivo)
        {
            motivo = null;

            if (objeto == null)
            {
                motivo = "objeto inválido";
                return false;
            }

            if (!runa.EsValida || !m_configuracion.EsForjable(runa.Estadistica))
            {
                motivo = "runa inválida";
                return false;
            }

            var rango = objeto.ObtenerRangoPlantilla(runa.Estadistica);
            var esExo = rango == null;
            if (esExo && !m_configuracion.PermitirExo)
            {
                motivo = "exomagia no permitida";
                return false;
            }

            if (esExo && TieneExoIncompatible(objeto, runa.Estadistica))
            {
                motivo = "exo incompatible";
                return false;
            }

            var valorNuevo = objeto.ObtenerValor(runa.Estadistica) + runa.Potencia;
            var maximoPlantilla = rango?.Max ?? 0;
            var esOver = esExo || valorNuevo > maximoPlantilla;
            if (esOver && !m_configuracion.PermitirOvermax)
            {
                motivo = "sobremáximo no permitido";
                return false;
            }

            return true;
        }

        public ResultadoAplicacionForjamagia AplicarRuna(IObjetoForjable objeto, RunaForjamagia runa, int nivelMago)
        {
            if (!PuedeAplicarse(objeto, runa, out var motivo))
                throw new InvalidOperationException($"No se puede aplicar la runa: {motivo}");

            var contexto = CrearContexto(objeto, runa, nivelMago);
            var probabilidades = m_estimarProbabilidades(contexto);
            var resultado = m_formula.Tirar(contexto);
            var pesoCentiReliquatAntes = objeto.PesoCentiPozo;

            EffectEnum? efectoGanado = null;
            var cantidadGanada = 0;
            IReadOnlyList<PerdidaEfecto> efectosPerdidos = Array.Empty<PerdidaEfecto>();

            switch (resultado)
            {
                case ResultadoForjamagia.ExitoCritico:
                    objeto.EstablecerValor(runa.Estadistica, contexto.ValorNuevo);
                    efectoGanado = runa.Estadistica;
                    cantidadGanada = runa.Potencia;
                    break;

                case ResultadoForjamagia.ExitoNeutro:
                    efectosPerdidos = ConsumirPeso(objeto, contexto.PesoRunaCenti, runa, out var pesoCentiConsumido);
                    if (pesoCentiConsumido > 0 || contexto.PesoRunaCenti <= 0)
                    {
                        objeto.EstablecerValor(runa.Estadistica, objeto.ObtenerValor(runa.Estadistica) + runa.Potencia);
                        efectoGanado = runa.Estadistica;
                        cantidadGanada = runa.Potencia;
                    }
                    break;

                case ResultadoForjamagia.FalloCritico:
                    efectosPerdidos = ConsumirPeso(objeto, contexto.PesoRunaCenti, runa, out _);
                    break;
            }

            return new ResultadoAplicacionForjamagia
            {
                Resultado = resultado,
                RunaAplicada = runa,
                EfectoGanado = efectoGanado,
                CantidadGanada = cantidadGanada,
                EfectosPerdidos = efectosPerdidos,
                Probabilidades = probabilidades,
                PesoCentiReliquatAntes = pesoCentiReliquatAntes,
                PesoCentiReliquatDespues = objeto.PesoCentiPozo,
                MensajeLog = ConstruirLog(resultado, runa, cantidadGanada, efectosPerdidos, probabilidades, contexto.PesoRunaCenti, pesoCentiReliquatAntes, objeto.PesoCentiPozo),
            };
        }

        public ProbabilidadesForjamagia EstimarProbabilidades(IObjetoForjable objeto, RunaForjamagia runa, int nivelMago)
        {
            if (!PuedeAplicarse(objeto, runa, out var motivo))
                throw new InvalidOperationException($"No se puede estimar la runa: {motivo}");

            return m_estimarProbabilidades(CrearContexto(objeto, runa, nivelMago));
        }

        public long ObtenerExperiencia(IObjetoForjable objeto, RunaForjamagia runa, int nivelMago, ResultadoForjamagia resultado)
        {
            if (objeto == null || !runa.EsValida || nivelMago >= 100)
                return 0;

            var multiplicador = ObtenerMultiplicadorExperiencia(resultado);
            if (multiplicador <= 0)
                return 0;

            var poids = (int)Math.Floor(m_configuracion.PesoRuna(runa));
            var experienciaBase = TablaExperienciaFm(Math.Max(1, objeto.Nivel), poids);
            var experiencia = (long)Math.Round(experienciaBase * multiplicador, MidpointRounding.AwayFromZero);

            return Math.Max(m_configuracion.ExperienciaMinima, experiencia);
        }

        private static int TablaExperienciaFm(int nivelObjeto, int poids)
        {
            if (nivelObjeto <= 1)
                return poids <= 10 ? 10 : poids <= 50 ? 25 : 50;
            if (nivelObjeto <= 25)
                return poids <= 10 ? 10 : 50;
            if (nivelObjeto <= 50)
                return poids <= 1 ? 10 : poids <= 10 ? 25 : poids <= 50 ? 50 : 100;
            if (nivelObjeto <= 75)
                return poids <= 3 ? 25 : poids <= 10 ? 50 : poids <= 50 ? 100 : 250;
            if (nivelObjeto <= 100)
                return poids <= 3 ? 50 : poids <= 10 ? 100 : poids <= 50 ? 250 : 500;
            if (nivelObjeto <= 125)
                return poids <= 3 ? 100 : poids <= 10 ? 250 : poids <= 50 ? 500 : 1000;
            if (nivelObjeto <= 150)
                return poids <= 10 ? 250 : 1000;
            if (nivelObjeto <= 175)
                return poids <= 1 ? 250 : poids <= 10 ? 500 : 1000;
            return poids <= 1 ? 500 : 1000;
        }

        private IReadOnlyList<PerdidaEfecto> ConsumirPeso(IObjetoForjable objeto, int pesoCentiRequerido, RunaForjamagia runa, out int pesoCentiConsumido)
        {
            pesoCentiConsumido = 0;

            if (pesoCentiRequerido <= 0)
                return Array.Empty<PerdidaEfecto>();

            var desdePozo = Math.Min(objeto.PesoCentiPozo, pesoCentiRequerido);
            objeto.PesoCentiPozo -= desdePozo;
            pesoCentiConsumido = desdePozo;

            var restante = pesoCentiRequerido - desdePozo;
            if (restante <= 0)
                return Array.Empty<PerdidaEfecto>();

            var perdidas = SeleccionarPerdidas(objeto, restante, runa);

            var pesoCentiPerdido = 0;
            foreach (var perdida in perdidas)
                pesoCentiPerdido += perdida.PesoPerdidoCenti;

            pesoCentiConsumido += Math.Min(pesoCentiPerdido, restante);

            var residuo = pesoCentiPerdido - restante;
            if (residuo > 0)
                objeto.PesoCentiPozo += residuo;

            return perdidas;
        }

        private IReadOnlyList<PerdidaEfecto> SeleccionarPerdidas(IObjetoForjable objeto, int pesoCentiRequerido, RunaForjamagia runa)
        {
            var perdidas = new List<PerdidaEfecto>();
            if (pesoCentiRequerido <= 0)
                return perdidas;

            var acumulado = 0;

            foreach (var nivel in ObtenerNivelesCandidatos(objeto, runa))
            {
                if (acumulado >= pesoCentiRequerido)
                    break;

                Mezclar(nivel);

                foreach (var efecto in nivel)
                {
                    if (acumulado >= pesoCentiRequerido)
                        break;

                    var pesoUnitarioCenti = m_configuracion.PesoUnitarioCenti(efecto);
                    if (pesoUnitarioCenti <= 0)
                        continue;

                    var actual = objeto.ObtenerValor(efecto);
                    var minimo = objeto.ObtenerRangoPlantilla(efecto)?.Min ?? 0;
                    var disponible = actual - minimo;
                    if (disponible <= 0)
                        continue;

                    var faltante = pesoCentiRequerido - acumulado;
                    var puntos = Math.Min(disponible, (faltante + pesoUnitarioCenti - 1) / pesoUnitarioCenti);
                    if (puntos <= 0)
                        continue;

                    var pesoCentiPerdido = CalculoPesos.Multiplicar(pesoUnitarioCenti, puntos);
                    objeto.EstablecerValor(efecto, actual - puntos);
                    perdidas.Add(new PerdidaEfecto(efecto, puntos, pesoCentiPerdido));
                    acumulado += pesoCentiPerdido;
                }
            }

            return perdidas;
        }

        private List<List<EffectEnum>> ObtenerNivelesCandidatos(IObjetoForjable objeto, RunaForjamagia runa)
        {
            var over = new List<EffectEnum>();
            var exo = new List<EffectEnum>();
            var natural = new List<EffectEnum>();
            var fallbackMismaEstadistica = new List<EffectEnum>();

            foreach (var efecto in objeto.EfectosActuales)
            {
                if (m_configuracion.EstadisticasBloqueadas.Contains(efecto) || !m_configuracion.EsForjable(efecto))
                    continue;

                var rango = objeto.ObtenerRangoPlantilla(efecto);
                var minimo = rango?.Min ?? 0;
                var valor = objeto.ObtenerValor(efecto);
                if (valor <= minimo)
                    continue;

                if (efecto == runa.Estadistica)
                {
                    fallbackMismaEstadistica.Add(efecto);
                    continue;
                }

                if (rango == null)
                    exo.Add(efecto);
                else if (valor > rango.Value.Max)
                    over.Add(efecto);
                else
                    natural.Add(efecto);
            }

            return new List<List<EffectEnum>> { over, exo, natural, fallbackMismaEstadistica, };
        }

        private static void Mezclar(List<EffectEnum> efectos)
        {
            for (var i = efectos.Count - 1; i > 0; i--)
            {
                int j = Util.Next(0, i + 1);
                (efectos[j], efectos[i]) = (efectos[i], efectos[j]);
            }
        }

        private ContextoForjamagia CrearContexto(IObjetoForjable objeto, RunaForjamagia runa, int nivelMago)
        {
            var rango = objeto.ObtenerRangoPlantilla(runa.Estadistica);
            var esExo = rango == null;
            var valorActual = objeto.ObtenerValor(runa.Estadistica);
            var valorNuevo = valorActual + runa.Potencia;
            var maximoPlantilla = rango?.Max ?? 0;
            var estaEnJet = !esExo && valorNuevo <= maximoPlantilla;
            var esOver = esExo || valorNuevo > maximoPlantilla;
            var pesoRunaCenti = m_configuracion.PesoRunaCenti(runa);
            var pesoCentiObjetoPlantilla = ObtenerPesoCentiObjetoPlantilla(objeto);
            var pesoCentiObjetoActual = ObtenerPesoCentiObjetoActual(objeto);
            var pesoCentiOverObjeto = Math.Max(0, pesoCentiObjetoActual - pesoCentiObjetoPlantilla);
            var pesoCentiOverObjetoNuevo = Math.Max(0, pesoCentiObjetoActual + pesoRunaCenti - pesoCentiObjetoPlantilla);

            return new ContextoForjamagia
            {
                Objeto = objeto,
                Runa = runa,
                NivelMago = nivelMago,
                Configuracion = m_configuracion,
                PesoRunaCenti = pesoRunaCenti,
                ValorActual = valorActual,
                ValorNuevo = valorNuevo,
                MaximoPlantilla = maximoPlantilla,
                EsExo = esExo,
                EsOver = esOver,
                EstaEnJet = estaEnJet,
                DistanciaAlMaximo = esExo ? 0 : Math.Max(0, maximoPlantilla - valorActual),
                PesoCentiPozo = objeto.PesoCentiPozo,
                PesoCentiObjetoPlantilla = pesoCentiObjetoPlantilla,
                PesoCentiObjetoActual = pesoCentiObjetoActual,
                PesoCentiOverObjeto = pesoCentiOverObjeto,
                PesoCentiOverObjetoNuevo = pesoCentiOverObjetoNuevo,
            };
        }

        private int ObtenerPesoCentiObjetoPlantilla(IObjetoForjable objeto)
        {
            var total = 0;
            foreach (var efecto in objeto.EfectosPlantilla)
            {
                if (!m_configuracion.EsForjable(efecto))
                    continue;

                var rango = objeto.ObtenerRangoPlantilla(efecto);
                if (rango == null)
                    continue;

                total += CalculoPesos.Multiplicar(m_configuracion.PesoUnitarioCenti(efecto), Math.Max(0, rango.Value.Max));
            }
            return total;
        }

        private int ObtenerPesoCentiObjetoActual(IObjetoForjable objeto)
        {
            var total = 0;
            foreach (var efecto in objeto.EfectosActuales)
            {
                if (!m_configuracion.EsForjable(efecto))
                    continue;

                total += CalculoPesos.Multiplicar(m_configuracion.PesoUnitarioCenti(efecto), Math.Max(0, objeto.ObtenerValor(efecto)));
            }
            return total;
        }

        private bool TieneExoIncompatible(IObjetoForjable objeto, EffectEnum estadistica)
        {
            if (!m_configuracion.EsExoExcluyente(estadistica))
                return false;

            foreach (var efecto in m_configuracion.PesosUnitarios.Keys)
            {
                if (efecto != estadistica && m_configuracion.EsExoExcluyente(efecto) && objeto.ObtenerRangoPlantilla(efecto) == null && objeto.ObtenerValor(efecto) > 0)
                    return true;
            }

            return false;
        }

        private double ObtenerMultiplicadorExperiencia(ResultadoForjamagia resultado)
        {
            return resultado switch
            {
                ResultadoForjamagia.ExitoCritico => m_configuracion.MultiplicadorExperienciaExitoCritico,
                ResultadoForjamagia.ExitoNeutro => m_configuracion.MultiplicadorExperienciaExitoNeutro,
                ResultadoForjamagia.FalloCritico => m_configuracion.MultiplicadorExperienciaFalloCritico,
                _ => 0,
            };
        }

        private static string ConstruirLog(ResultadoForjamagia resultado, RunaForjamagia runa, int cantidadGanada, IReadOnlyList<PerdidaEfecto> perdidas, ProbabilidadesForjamagia probabilidades, int pesoRunaCenti, int pozoAntes, int pozoDespues)
        {
            var builder = new StringBuilder();
            switch (resultado)
            {
                case ResultadoForjamagia.ExitoCritico:
                    builder.Append("SC: +").Append(cantidadGanada).Append(' ').Append(runa.Estadistica);
                break;

                case ResultadoForjamagia.ExitoNeutro:
                    builder.Append("SN: +").Append(cantidadGanada).Append(' ').Append(runa.Estadistica);
                break;

                case ResultadoForjamagia.FalloCritico:
                    builder.Append("EC: runa perdida (").Append(runa.Estadistica).Append(')');
                break;
            }

            foreach (var perdida in perdidas)
                builder.Append(" | -").Append(perdida.PuntosPerdidos).Append(' ').Append(perdida.Efecto);

            builder.Append(" | poids ").Append(CalculoPesos.Formatear(pesoRunaCenti)).Append(" | ").Append(probabilidades).Append(" | puits ").Append(CalculoPesos.Formatear(pozoAntes)).Append("->").Append(CalculoPesos.Formatear(pozoDespues));
            return builder.ToString();
        }
    }
}
