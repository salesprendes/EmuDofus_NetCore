using Protocolo.Framework.Generic;
using Game.Database.Structure;
using Game.Spell;
using Game.Stats;
using System.Collections.Generic;

namespace Game.Job.Forjamagia
{
    /// <summary>
    /// Elemento de daño de un arma (offset dentro de las líneas Steal/Damage del cliente).
    /// </summary>
    public enum ElementoArma
    {
        Agua = 0,
        Tierra = 1,
        Aire = 2,
        Fuego = 3,
        Neutral = 4
    }

    /// <summary>
    /// Pociones de forjamagia para armas (objetos de tipo TYPE_FM_POTION = 26): cambian el
    /// elemento de las líneas de daño/robo del arma (p.ej. Poción de Foudroiement → aire).
    ///
    /// StarLoco dejó este caso sin implementar; aquí se modela como un cambio determinista del
    /// elemento de todas las líneas Steal (91-95) y Damage (96-100) del arma. La tabla
    /// idPlantilla → elemento es configurable (<see cref="Registrar"/>); si la poción no está
    /// listada, se intenta deducir el elemento del primer efecto de daño de su plantilla.
    /// </summary>
    public sealed class GestorPocionesForjamagia : Singleton<GestorPocionesForjamagia>
    {
        // Bases de las líneas de arma en el orden Agua, Tierra, Aire, Fuego, Neutral.
        private const int BaseRobo = (int)EffectEnum.StealWater;    // 91
        private const int BaseDanio = (int)EffectEnum.DamageWater;  // 96

        /// <summary>Pociones conocidas: idPlantilla → elemento destino (vacío por defecto, sembrar aquí).</summary>
        private readonly Dictionary<int, ElementoArma> m_pociones = new Dictionary<int, ElementoArma>();

        /// <summary>Registra/redefine el elemento de una poción concreta por su idPlantilla.</summary>
        public void Registrar(int idPlantillaPocion, ElementoArma elemento)
        {
            m_pociones[idPlantillaPocion] = elemento;
        }

        /// <summary>¿El objeto es una poción de forjamagia de arma?</summary>
        public bool EsPocion(ItemDAO item)
        {
            return item?.Template != null
                && (ItemTypeEnum)item.Template.Type == ItemTypeEnum.TYPE_FM_POTION;
        }

        /// <summary>
        /// Resuelve el elemento destino de una poción: tabla → plantilla.
        /// Devuelve <c>null</c> si no se puede determinar.
        /// </summary>
        public ElementoArma? Resolver(ItemDAO pocion)
        {
            if (pocion?.Template == null)
                return null;

            if (m_pociones.TryGetValue(pocion.TemplateId, out var elemento))
                return elemento;

            // Fallback: leer el primer efecto de daño/robo de la plantilla de la poción.
            foreach (var efecto in pocion.Template.RandomEffects)
            {
                var resuelto = ElementoDe(efecto.Type);
                if (resuelto != null)
                    return resuelto;
            }

            return null;
        }

        /// <summary>
        /// Aplica la poción: cambia el elemento de todas las líneas Steal/Damage del arma al
        /// elemento dado. Devuelve <c>true</c> si el arma cambió.
        /// </summary>
        public bool Aplicar(ItemDAO arma, ElementoArma elemento)
        {
            var stats = arma.Statistics;

            // Capturar las líneas a transformar antes de mutar el diccionario.
            var lineas = new List<(EffectEnum Efecto, GenericEffect Valor)>();
            foreach (var entrada in stats.Effects)
            {
                if (EsLineaDanioArma(entrada.Key))
                    lineas.Add((entrada.Key, entrada.Value));
            }

            var cambiado = false;
            foreach (var (efecto, valor) in lineas)
            {
                var destinoId = Reasignar(efecto, elemento);
                if (destinoId == efecto)
                    continue;

                // Mover el valor a la línea del nuevo elemento (fusionando si ya existe).
                stats.RemoveEffect(efecto);
                var destino = stats.GetEffect(destinoId);
                destino.Value1 += valor.Value1;
                destino.Value2 += valor.Value2;
                destino.Value3 += valor.Value3;
                cambiado = true;
            }

            if (cambiado)
                stats.StatisticsChanged();

            return cambiado;
        }

        private static bool EsLineaDanioArma(EffectEnum efecto)
        {
            var id = (int)efecto;
            return (id >= BaseRobo && id <= BaseRobo + 4)
                || (id >= BaseDanio && id <= BaseDanio + 4);
        }

        private static EffectEnum Reasignar(EffectEnum efecto, ElementoArma elemento)
        {
            var id = (int)efecto;
            if (id >= BaseRobo && id <= BaseRobo + 4)
                return (EffectEnum)(BaseRobo + (int)elemento);
            if (id >= BaseDanio && id <= BaseDanio + 4)
                return (EffectEnum)(BaseDanio + (int)elemento);
            return efecto;
        }

        private static ElementoArma? ElementoDe(EffectEnum efecto)
        {
            var id = (int)efecto;
            if (id >= BaseRobo && id <= BaseRobo + 4)
                return (ElementoArma)(id - BaseRobo);
            if (id >= BaseDanio && id <= BaseDanio + 4)
                return (ElementoArma)(id - BaseDanio);
            return null;
        }
    }
}
