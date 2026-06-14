using Game.Database.Structure;
using Game.Entity;
using Game.Interactive.Type;
using Game.Job;
using Game.Job.Forjamagia;
using Game.Job.Skill;
using Game.Network;
using Game.Spell;
using Protocolo.Framework.Generic;
using System.Collections.Generic;

namespace Game.Exchange
{
    public sealed class IntercambioForjamagia : AbstractExchange, IValidableExchange, IRetryableExchange
    {
        private const int BUCLE_OK = 1;
        private const int BUCLE_INTERRUMPIDO = 2;
        private const int BUCLE_ERROR = 3;
        private const int PLANTILLA_FIRMA = 7508;

        public CharacterEntity Personaje
        {
            get;
            private set;
        }

        public int IdOficio
        {
            get;
            private set;
        }

        public MagicSkill Habilidad
        {
            get;
            private set;
        }

        public int CasillaMaxima
        {
            get;
            private set;
        }

        private readonly ServicioForjamagia m_servicioForjamagia;
        private readonly Dictionary<long, int> m_itemsCasillas;
        private CraftPlan m_plan;
        private int m_repeticionesPendientes;
        private UpdatableTimer m_temporizadorRepeticion;

        public IntercambioForjamagia(CharacterEntity character, CraftPlan plan, JobSkill skill, ExchangeTypeEnum type = ExchangeTypeEnum.EXCHANGE_CRAFTPLAN) : base(type)
        {
            m_servicioForjamagia = new ServicioForjamagia();
            m_itemsCasillas = new Dictionary<long, int>();
            m_plan = plan;
            Personaje = character;
            Habilidad = (MagicSkill)skill;
            IdOficio = character.CharacterJobs.GetJobId(skill.Id);
            CasillaMaxima = character.CharacterJobs.GetCraftMaxCase(IdOficio);
        }

        protected override string SerializeAs_ExchangeCreate()
        {
            return CasillaMaxima + ";" + (int)Habilidad.Id;
        }

        public override void Leave(bool success = false)
        {
            CancelRetry();

            m_plan.StopCraft();
            base.Leave(success);
        }

        public override int AddItem(AbstractEntity entity, long guid, int quantity, long price = -1)
        {
            var item = Personaje.Inventory.GetItem(guid);
            if (item == null || item.Slot != ItemSlotEnum.SLOT_INVENTORY)
                return 0;

            if (quantity > item.Quantity)
                quantity = item.Quantity;

            var already = m_itemsCasillas.TryGetValue(guid, out var existing) ? existing : 0;
            if (already > 0)
            {
                var realQuantity = item.Quantity - already;
                if (quantity > realQuantity)
                    quantity = realQuantity;
            }

            if (quantity <= 0)
                return 0;

            m_itemsCasillas[guid] = already + quantity;

            Personaje.Dispatch(WorldMessage.EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, item.Id.ToString() + '|' + m_itemsCasillas[guid]));

            return quantity;
        }

        public override int RemoveItem(AbstractEntity entity, long guid, int quantity)
        {
            if (!m_itemsCasillas.TryGetValue(guid, out var current))
                return 0;

            if (quantity >= current)
            {
                quantity = current;
                m_itemsCasillas.Remove(guid);
            }
            else
            {
                m_itemsCasillas[guid] = current - quantity;
            }

            var item = Personaje.Inventory.GetItem(guid);
            var idObjeto = (item != null ? item.Id : guid).ToString();

            Personaje.Dispatch(WorldMessage.EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_REMOVE, idObjeto));
            if (m_itemsCasillas.ContainsKey(guid))
                Personaje.Dispatch(WorldMessage.EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, idObjeto + '|' + m_itemsCasillas[guid]));

            return quantity;
        }

        /// <summary>
        /// Un intento de forja: aplica UN agente (runa o poción) sobre el objeto colocado.
        /// </summary>
        public bool Validate(AbstractEntity entity)
        {
            ItemDAO objetivo = null, runa = null, pocion = null, firma = null;

            foreach (var entry in m_itemsCasillas)
            {
                var item = Personaje.Inventory.GetItem(entry.Key);
                if (item == null)
                    continue;

                if (firma == null && item.TemplateId == PLANTILLA_FIRMA)
                    firma = item;
                else if (runa == null && GestorRunasForjamagia.Instance.EsRuna(item))
                    runa = item;
                else if (pocion == null && GestorPocionesForjamagia.Instance.EsPocion(item))
                    pocion = item;
                else if (objetivo == null && Habilidad.CanEnhance(item.Template))
                    objetivo = item;
            }

            Personaje.CachedBuffer = true;

            try
            {
                if (objetivo == null || !objetivo.Template.Forgemageable)
                {
                    Logger.Debug($"Forjamagia[{Personaje.Name}] sin objeto forjable en las casillas (objetivo={(objetivo == null ? "null" : $"{objetivo.TemplateId} forjable={objetivo.Template.Forgemageable}")})");
                    AbortarIntento();
                    return false;
                }

                if (runa == null && pocion == null)
                {
                    Logger.Debug($"Forjamagia[{Personaje.Name}] sin runa ni poción en las casillas");
                    InterrumpirIntento();
                    return false;
                }

                return runa != null ? IntentarConRuna(objetivo, runa, firma) : IntentarConPocion(objetivo, pocion, firma);
            }
            finally
            {
                Personaje.CachedBuffer = false;
            }
        }

        private bool IntentarConRuna(ItemDAO objetivo, ItemDAO runa, ItemDAO firma)
        {
            var runaForjamagia = GestorRunasForjamagia.Instance.Resolver(runa);
            if (!runaForjamagia.EsValida)
            {
                Logger.Debug($"Forjamagia[{Personaje.Name}] runa sin resolver: template {runa.TemplateId} (añadirla a GestorRunasForjamagia o exponer su efecto en plantilla)");
                InterrumpirIntento();
                return false;
            }

            if (!m_servicioForjamagia.PuedeAplicarse(new AdaptadorObjetoForjable(objetivo), runaForjamagia, out var motivoRechazo))
            {
                Logger.Debug($"Forjamagia[{Personaje.Name}] runa {runa.TemplateId} rechazada: {motivoRechazo}");
                InterrumpirIntento();
                return false;
            }

            ConsumirUno(runa.Id, out var runaRestante);

            var forjado = objetivo.Clone(1);
            forjado.ForjamagiaPozo = objetivo.ForjamagiaPozo;

            var nivelOficio = Personaje.CharacterJobs.GetJobLevel(IdOficio);
            var objetoForjable = new AdaptadorObjetoForjable(forjado);
            var resultado = m_servicioForjamagia.AplicarRuna(objetoForjable, runaForjamagia, nivelOficio);

            var objetoMostrado = FinalizarObjeto(objetivo, forjado, firma, resultado.ObjetoModificado);

            switch (resultado.Resultado)
            {
                case ResultadoForjamagia.ExitoCritico:
                    EmitirExito(objetoMostrado);
                break;

                case ResultadoForjamagia.ExitoNeutro:
                    EmitirFallo(objetoMostrado, InformationEnum.INFO_MAGIC_NOT_PERFECT);
                break;

                case ResultadoForjamagia.FalloCritico:
                    EmitirFallo(objetoMostrado, InformationEnum.INFO_MAGIC_FAILED);
                break;
            }

            RepoblarCasillas(objetoMostrado, runa.Id, runaRestante);

            var experiencia = m_servicioForjamagia.ObtenerExperiencia(objetoForjable, runaForjamagia, nivelOficio, resultado.Resultado);
            if (experiencia > 0)
                Personaje.CharacterJobs.AddExperience(IdOficio, experiencia);

            return false;
        }

        private bool IntentarConPocion(ItemDAO objetivo, ItemDAO pocion, ItemDAO firma)
        {
            var elemento = GestorPocionesForjamagia.Instance.Resolver(pocion);
            if (elemento == null)
            {
                Logger.Debug($"Forjamagia[{Personaje.Name}] poción sin resolver: template {pocion.TemplateId}");
                InterrumpirIntento();
                return false;
            }

            ConsumirUno(pocion.Id, out var pocionRestante);

            ItemDAO forjado = objetivo.Clone(1);
            forjado.ForjamagiaPozo = objetivo.ForjamagiaPozo;

            var cambiado = GestorPocionesForjamagia.Instance.Aplicar(forjado, elemento.Value);

            var objetoMostrado = FinalizarObjeto(objetivo, forjado, firma, cambiado);

            if (cambiado)
                EmitirExito(objetoMostrado);
            else
                EmitirFallo(objetoMostrado, InformationEnum.INFO_MAGIC_FAILED);

            RepoblarCasillas(objetoMostrado, pocion.Id, pocionRestante);

            return false;
        }

        private ItemDAO FinalizarObjeto(ItemDAO objetivo, ItemDAO forjado, ItemDAO firma, bool objetoModificado)
        {
            if (!objetoModificado)
            {
                forjado.OwnerId = -1;
                return objetivo;
            }

            if (firma != null)
            {
                forjado.Statistics.RemoveEffect(EffectEnum.OBJETO_MODIFICADO_POR);
                forjado.Statistics.AddEffect(EffectEnum.OBJETO_MODIFICADO_POR, 0, 0, 0, Personaje.Name);
                ConsumirUno(firma.Id, out _);
            }

            forjado.SaveStats();

            var cantidadAntes = objetivo.Quantity;
            Personaje.Inventory.RemoveItem(objetivo.Id, 1);
            var stackTras = Personaje.Inventory.GetItem(objetivo.Id);
            Personaje.Inventory.AddItem(forjado, merge: false);

            m_itemsCasillas.Remove(objetivo.Id);
            m_itemsCasillas[forjado.Id] = 1;

            Personaje.Dispatch(WorldMessage.EXCHANGE_DISTANT_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, forjado.Id + "|" + forjado.Quantity + "|" + forjado.TemplateId + "|" + forjado.StringEffects));
            return forjado;
        }

        private void EmitirExito(ItemDAO objetoMostrado)
        {
            Personaje.Dispatch(WorldMessage.CRAFT_TEMPLATE_CREATED(objetoMostrado.TemplateId));
            Personaje.Map.Dispatch(WorldMessage.CRAFT_INTERACTIVE_SUCCESS(Personaje.Id, objetoMostrado.TemplateId));
        }

        private void EmitirFallo(ItemDAO objetoMostrado, InformationEnum info)
        {
            Personaje.Dispatch(WorldMessage.CRAFT_TEMPLATE_FAILED(objetoMostrado.TemplateId));
            Personaje.Dispatch(WorldMessage.IM_INFO_MESSAGE(info));
            Personaje.Map.Dispatch(WorldMessage.CRAFT_INTERACTIVE_FAILED(Personaje.Id, objetoMostrado.TemplateId));
        }

        private void RepoblarCasillas(ItemDAO objetoMostrado, long guidAgente, int agenteRestante)
        {
            Personaje.Dispatch(WorldMessage.EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, objetoMostrado.Id + "|1"));
            if (agenteRestante > 0)
                Personaje.Dispatch(WorldMessage.EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, guidAgente + "|" + agenteRestante));
        }

        private void ConsumirUno(long guid, out int restanteEnCasilla)
        {
            restanteEnCasilla = (m_itemsCasillas.TryGetValue(guid, out var current) ? current : 0) - 1;
            Personaje.Inventory.RemoveItem(guid, 1);

            if (restanteEnCasilla > 0)
                m_itemsCasillas[guid] = restanteEnCasilla;
            else
                m_itemsCasillas.Remove(guid);
        }

        public void Retry(int count)
        {
            if (m_temporizadorRepeticion != null)
                return;

            m_repeticionesPendientes = count;
            m_temporizadorRepeticion = base.AddTimer(1100, Loop);
        }

        public void CancelRetry()
        {
            if (m_temporizadorRepeticion == null)
                return;

            FinalizarBucle(BUCLE_INTERRUMPIDO);
        }

        private void Loop()
        {
            Personaje.CachedBuffer = true;

            Personaje.Dispatch(WorldMessage.CRAFT_LOOP_COUNT(m_repeticionesPendientes - 1));

            Validate(null);

            m_repeticionesPendientes--;

            if (m_repeticionesPendientes <= 0 && m_temporizadorRepeticion != null)
                FinalizarBucle(BUCLE_OK);

            Personaje.CachedBuffer = false;
        }

        private void FinalizarBucle(int motivo)
        {
            Personaje.Dispatch(WorldMessage.CRAFT_LOOP_END(motivo));

            base.RemoveTimer(m_temporizadorRepeticion);
            m_temporizadorRepeticion = null;
        }

        private void InterrumpirIntento()
        {
            if (m_temporizadorRepeticion != null)
                FinalizarBucle(BUCLE_ERROR);
            else
                Personaje.Dispatch(WorldMessage.CRAFT_LOOP_END(BUCLE_ERROR));
        }
        
        private void AbortarIntento()
        {
            if (m_temporizadorRepeticion != null)
                FinalizarBucle(BUCLE_ERROR);

            m_itemsCasillas.Clear();

            Personaje.Dispatch(WorldMessage.CRAFT_NO_RESULT());
            Personaje.Map.Dispatch(WorldMessage.CRAFT_INTERACTIVE_NOTHING(Personaje.Id));
        }
    }
}
