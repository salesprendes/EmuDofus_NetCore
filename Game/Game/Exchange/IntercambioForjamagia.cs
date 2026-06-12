using Game.Database.Structure;
using Game.Entity;
using Game.Interactive.Type;
using Game.Job;
using Game.Job.Forjamagia;
using Game.Job.Skill;
using Game.Network;
using Protocolo.Framework.Generic;
using System;
using System.Collections.Generic;

namespace Game.Exchange
{
    public sealed class IntercambioForjamagia : AbstractExchange, IValidableExchange, IRetryableExchange
    {
        private const int BUCLE_OK = 1;
        private const int BUCLE_INTERRUMPIDO = 2;
        private const int BUCLE_ERROR = 3;

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
        private Dictionary<long, int> m_itemsCasillas;
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

        public bool Validate(AbstractEntity entity)
        {
            ItemDAO objetivo = null;
            ItemDAO runa = null;

            foreach (var entry in m_itemsCasillas)
            {
                var item = Personaje.Inventory.GetItem(entry.Key);
                if (item == null)
                    continue;

                if (runa == null && GestorRunasForjamagia.Instance.EsRuna(item))
                    runa = item;
                else if (objetivo == null && Habilidad.CanEnhance(item.Template))
                    objetivo = item;
            }

            Personaje.CachedBuffer = true;
            try
            {
                if (objetivo == null || !objetivo.Template.Forgemageable)
                {
                    Logger.Debug("Forjamagia[" + Personaje.Name + "] sin objeto forjable en las casillas (objetivo=" + (objetivo == null ? "null" : objetivo.TemplateId + " forjable=" + objetivo.Template.Forgemageable) + ")");
                    AbortarIntento();
                    return false;
                }

                if (runa == null)
                {
                    Logger.Debug("Forjamagia[" + Personaje.Name + "] sin runas en las casillas");
                    InterrumpirIntento();
                    return false;
                }

                var runaForjamagia = GestorRunasForjamagia.Instance.Resolver(runa);
                var objetoForjable = new AdaptadorObjetoForjable(objetivo);
                if (!runaForjamagia.EsValida)
                {
                    Logger.Debug("Forjamagia[" + Personaje.Name + "] runa sin resolver: template " + runa.TemplateId + " (añadirla a GestorRunasForjamagia o exponer su efecto en plantilla)");
                    InterrumpirIntento();
                    return false;
                }

                if (!m_servicioForjamagia.PuedeAplicarse(objetoForjable, runaForjamagia, out var motivoRechazo))
                {
                    Logger.Debug("Forjamagia[" + Personaje.Name + "] runa " + runa.TemplateId + " rechazada: " + motivoRechazo);
                    InterrumpirIntento();
                    return false;
                }

                var idRuna = runa.Id;
                var cantidadRestanteEnCasilla = m_itemsCasillas[idRuna] - 1;
                Personaje.Inventory.RemoveItem(idRuna, 1);

                if (cantidadRestanteEnCasilla > 0)
                    m_itemsCasillas[idRuna] = cantidadRestanteEnCasilla;
                else
                    m_itemsCasillas.Remove(idRuna);

                var nivelOficio = Personaje.CharacterJobs.GetJobLevel(IdOficio);
                var resultado = m_servicioForjamagia.AplicarRuna(objetoForjable, runaForjamagia, nivelOficio);

                var objetoMostrado = objetivo;
                if (resultado.ObjetoModificado)
                {
                    objetivo.SaveStats();
                    var objetoForjado = objetivo.Clone(1);
                    objetoForjado.ForgemagiePuits = objetivo.ForgemagiePuits;

                    Personaje.Inventory.RemoveItem(objetivo.Id, objetivo.Quantity);
                    Personaje.Inventory.AddItem(objetoForjado, merge: false);

                    m_itemsCasillas.Remove(objetivo.Id);
                    m_itemsCasillas[objetoForjado.Id] = 1;

                    objetoMostrado = objetoForjado;

                    Personaje.Dispatch(WorldMessage.EXCHANGE_DISTANT_MOVEMENT(
                        ExchangeMoveEnum.MOVE_OBJECT,
                        OperatorEnum.OPERATOR_ADD,
                        objetoForjado.Id + "|1|" + objetoForjado.TemplateId + "|" + objetoForjado.StringEffects));
                }

                switch (resultado.Resultado)
                {
                    case ResultadoForjamagia.ExitoCritico:
                        Personaje.Dispatch(WorldMessage.CRAFT_TEMPLATE_CREATED(objetoMostrado.TemplateId));
                        Personaje.Map.Dispatch(WorldMessage.CRAFT_INTERACTIVE_SUCCESS(Personaje.Id, objetoMostrado.TemplateId));
                        break;

                    case ResultadoForjamagia.ExitoNeutro:
                        Personaje.Dispatch(WorldMessage.CRAFT_TEMPLATE_FAILED(objetoMostrado.TemplateId));
                        Personaje.Dispatch(WorldMessage.IM_INFO_MESSAGE(InformationEnum.INFO_MAGIC_NOT_PERFECT));
                        Personaje.Map.Dispatch(WorldMessage.CRAFT_INTERACTIVE_FAILED(Personaje.Id, objetoMostrado.TemplateId));
                        break;

                    case ResultadoForjamagia.FalloCritico:
                        Personaje.Dispatch(WorldMessage.CRAFT_TEMPLATE_FAILED(objetoMostrado.TemplateId));
                        Personaje.Dispatch(WorldMessage.IM_INFO_MESSAGE(InformationEnum.INFO_MAGIC_FAILED));
                        Personaje.Map.Dispatch(WorldMessage.CRAFT_INTERACTIVE_FAILED(Personaje.Id, objetoMostrado.TemplateId));
                        break;
                }

                Personaje.Dispatch(WorldMessage.EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, objetoMostrado.Id + "|1"));
                if (cantidadRestanteEnCasilla > 0)
                    Personaje.Dispatch(WorldMessage.EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, idRuna + "|" + cantidadRestanteEnCasilla));

                var experiencia = m_servicioForjamagia.ObtenerExperiencia(objetoForjable, runaForjamagia, nivelOficio, resultado.Resultado);
                if (experiencia > 0)
                    Personaje.CharacterJobs.AddExperience(IdOficio, experiencia);

                return false;
            }
            finally
            {
                Personaje.CachedBuffer = false;
            }
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
