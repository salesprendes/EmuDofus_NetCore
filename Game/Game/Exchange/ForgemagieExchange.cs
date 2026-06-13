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
    /// <summary>
    /// Intercambio de forjamagia: el jugador coloca un objeto equipable + un agente de
    /// transformación (runa o poción) + opcionalmente una firma (item 7508). Cada validación
    /// (clic en "fusionar") aplica UNA transformación, como en el juego real.
    ///
    /// Protocolo (aprendido del cliente 1.29 y de StarLoco):
    ///  - consumir el agente: OQ/OR automáticos del inventario.
    ///  - si el objeto cambia: se reemplaza por un CLON con guid nuevo (OR viejo + OAKO clon)
    ///    y se refresca la ventana de FM con EmKO+ (con el mismo guid el cliente DUPLICA).
    ///  - resultado: SC -> EcK;tpl + IO+ | SN -> Im0194 + IO- | EC -> Im0117 + IO-. Cualquier
    ///    Ec vacía la cuadrícula del cliente -> se repuebla con EMKO+ (objeto + restos).
    ///  - repetición: EMR n -> un intento por tick con EA/Ea.
    /// </summary>
    public sealed class ForgemagieExchange : AbstractExchange, IValidableExchange, IRetryableExchange
    {
        private const int LOOP_OK = 1;
        private const int LOOP_INTERUPT = 2;
        private const int LOOP_ERROR = 3;

        // Runa de firma "firmado por": al fusionar con ella, el objeto queda "Modificado por X".
        private const int SIGNING_ITEM_TEMPLATE = 7508;

        public CharacterEntity Character
        {
            get;
            private set;
        }

        public int JobId
        {
            get;
            private set;
        }

        public MagicSkill Skill
        {
            get;
            private set;
        }

        public int MaxCase
        {
            get;
            private set;
        }

        private readonly ServicioForjamagia m_forge;
        private Dictionary<long, int> m_caseItems;
        private CraftPlan m_plan;
        private int m_loopCount;
        private UpdatableTimer m_loopTimer;

        public ForgemagieExchange(CharacterEntity character, CraftPlan plan, JobSkill skill, ExchangeTypeEnum type = ExchangeTypeEnum.EXCHANGE_CRAFTPLAN) : base(type)
        {
            m_forge = new ServicioForjamagia();
            m_caseItems = new Dictionary<long, int>();
            m_plan = plan;
            Character = character;
            Skill = (MagicSkill)skill;
            JobId = character.CharacterJobs.GetJobId(skill.Id);
            MaxCase = character.CharacterJobs.GetCraftMaxCase(JobId);
        }

        protected override string SerializeAs_ExchangeCreate()
        {
            return MaxCase + ";" + (int)Skill.Id;
        }

        public override void Leave(bool success = false)
        {
            CancelRetry();

            m_plan.StopCraft();
            base.Leave(success);
        }

        public override int AddItem(AbstractEntity entity, long guid, int quantity, long price = -1)
        {
            var item = Character.Inventory.GetItem(guid);
            if (item == null || item.Slot != ItemSlotEnum.SLOT_INVENTORY)
                return 0;

            if (quantity > item.Quantity)
                quantity = item.Quantity;

            var already = m_caseItems.TryGetValue(guid, out var existing) ? existing : 0;
            if (already > 0)
            {
                var realQuantity = item.Quantity - already;
                if (quantity > realQuantity)
                    quantity = realQuantity;
            }

            if (quantity <= 0)
                return 0;

            m_caseItems[guid] = already + quantity;

            Character.Dispatch(WorldMessage.EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, item.Id.ToString() + '|' + m_caseItems[guid]));

            return quantity;
        }

        public override int RemoveItem(AbstractEntity entity, long guid, int quantity)
        {
            if (!m_caseItems.TryGetValue(guid, out var current))
                return 0;

            if (quantity >= current)
            {
                quantity = current;
                m_caseItems.Remove(guid);
            }
            else
            {
                m_caseItems[guid] = current - quantity;
            }

            var item = Character.Inventory.GetItem(guid);
            var idStr = (item != null ? item.Id : guid).ToString();

            Character.Dispatch(WorldMessage.EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_REMOVE, idStr));
            if (m_caseItems.ContainsKey(guid))
                Character.Dispatch(WorldMessage.EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, idStr + '|' + m_caseItems[guid]));

            return quantity;
        }

        /// <summary>
        /// Un intento de forja: aplica UN agente (runa o poción) sobre el objeto colocado.
        /// </summary>
        public bool Validate(AbstractEntity entity)
        {
            ItemDAO target = null, rune = null, potion = null, signature = null;

            foreach (var entry in m_caseItems)
            {
                var item = Character.Inventory.GetItem(entry.Key);
                if (item == null)
                    continue;

                if (signature == null && item.TemplateId == SIGNING_ITEM_TEMPLATE)
                    signature = item;
                else if (rune == null && GestorRunasForjamagia.Instance.EsRuna(item))
                    rune = item;
                else if (potion == null && GestorPocionesForjamagia.Instance.EsPocion(item))
                    potion = item;
                else if (target == null && Skill.CanEnhance(item.Template))
                    target = item;
            }

            Character.CachedBuffer = true;
            try
            {
                // Sin objeto forjable: estado inválido — vaciar casillas (EcEI).
                if (target == null || !target.Template.Forgemageable)
                {
                    Logger.Debug("Forgemagie[" + Character.Name + "] sin objeto forjable en las casillas");
                    AbortAttempt();
                    return false;
                }

                // Sin agente (runa/poción): se acabaron los recursos. Interrumpir SIN vaciar.
                if (rune == null && potion == null)
                {
                    Logger.Debug("Forgemagie[" + Character.Name + "] sin runa ni poción en las casillas");
                    InterruptAttempt();
                    return false;
                }

                return rune != null
                    ? ApplyRuneAttempt(target, rune, signature)
                    : ApplyPotionAttempt(target, potion, signature);
            }
            finally
            {
                Character.CachedBuffer = false;
            }
        }

        /// <summary>Intento con runa: SC/SN/EC vía el motor.</summary>
        private bool ApplyRuneAttempt(ItemDAO target, ItemDAO rune, ItemDAO signature)
        {
            var forgeRune = GestorRunasForjamagia.Instance.Resolver(rune);
            if (!forgeRune.EsValida)
            {
                Logger.Debug("Forgemagie[" + Character.Name + "] runa sin resolver: template " + rune.TemplateId);
                InterruptAttempt();
                return false;
            }

            // Validar sobre el objeto tal cual (solo lectura; una unidad del stack tiene las
            // mismas stats que el conjunto).
            if (!m_forge.PuedeAplicarse(new AdaptadorObjetoForjable(target), forgeRune, out var refuseReason))
            {
                Logger.Debug("Forgemagie[" + Character.Name + "] runa " + rune.TemplateId + " rechazada: " + refuseReason);
                InterruptAttempt();
                return false;
            }

            // Si el objeto está en un stack, maguear UNA unidad sin tocar las demás.
            target = EnsureSingle(target);

            ConsumeOne(rune.Id, out var runeRemaining);

            var jobLevel = Character.CharacterJobs.GetJobLevel(JobId);
            var forgeItem = new AdaptadorObjetoForjable(target);
            var result = m_forge.AplicarRuna(forgeItem, forgeRune, jobLevel);

            var displayItem = FinalizeObject(target, signature, result.ObjetoModificado);

            switch (result.Resultado)
            {
                case ResultadoForjamagia.ExitoCritico:
                    EmitSuccess(displayItem);
                    break;
                case ResultadoForjamagia.ExitoNeutro:
                    EmitFailure(displayItem, InformationEnum.INFO_MAGIC_NOT_PERFECT);
                    break;
                case ResultadoForjamagia.FalloCritico:
                    EmitFailure(displayItem, InformationEnum.INFO_MAGIC_FAILED);
                    break;
            }

            RepopulateGrid(displayItem, rune.Id, runeRemaining);

            var experience = m_forge.ObtenerExperiencia(forgeItem, forgeRune, jobLevel, result.Resultado);
            if (experience > 0)
                Character.CharacterJobs.AddExperience(JobId, experience);

            return false;
        }

        /// <summary>Intento con poción: cambia el elemento de las líneas de daño del arma.</summary>
        private bool ApplyPotionAttempt(ItemDAO target, ItemDAO potion, ItemDAO signature)
        {
            var element = GestorPocionesForjamagia.Instance.Resolver(potion);
            if (element == null)
            {
                Logger.Debug("Forgemagie[" + Character.Name + "] poción sin resolver: template " + potion.TemplateId);
                InterruptAttempt();
                return false;
            }

            // Si el objeto está en un stack, maguear UNA unidad sin tocar las demás.
            target = EnsureSingle(target);

            ConsumeOne(potion.Id, out var potionRemaining);

            var changed = GestorPocionesForjamagia.Instance.Aplicar(target, element.Value);

            var displayItem = FinalizeObject(target, signature, changed);

            if (changed)
                EmitSuccess(displayItem);
            else
                EmitFailure(displayItem, InformationEnum.INFO_MAGIC_FAILED);

            RepopulateGrid(displayItem, potion.Id, potionRemaining);

            return false;
        }

        /// <summary>
        /// Si el objeto está en un stack (cantidad &gt; 1), separa UNA unidad: reduce el stack
        /// (OQ al cliente) y añade una instancia individual al inventario, para maguearla sin
        /// afectar a las demás. Devuelve la unidad individual (o el mismo objeto si ya era único).
        /// </summary>
        private ItemDAO EnsureSingle(ItemDAO target)
        {
            if (target.Quantity <= 1)
                return target;

            var single = Character.Inventory.RemoveItem(target.Id, 1);
            Character.Inventory.AddItem(single, merge: false);

            if (m_caseItems.Remove(target.Id))
                m_caseItems[single.Id] = 1;

            return single;
        }

        /// <summary>
        /// Aplica la firma (si procede) y, si el objeto cambió, lo reemplaza por un clon con
        /// guid nuevo (evita la duplicación visual del EmKO+). Devuelve el objeto a mostrar.
        /// </summary>
        private ItemDAO FinalizeObject(ItemDAO target, ItemDAO signature, bool itemChanged)
        {
            if (!itemChanged)
                return target;

            // Firma de forjamagia: "Modificado por <nombre>".
            if (signature != null)
            {
                target.Statistics.RemoveEffect(EffectEnum.ModifiedBy);
                target.Statistics.AddEffect(EffectEnum.ModifiedBy, 0, 0, 0, Character.Name);
                ConsumeOne(signature.Id, out _);
            }

            target.SaveStats();

            // Clonar ANTES de quitar el original (RemoveItem marca OwnerId = -1). target ya es
            // una unidad individual (EnsureSingle), así que se quita 1.
            var forged = target.Clone(1);
            forged.ForgemagiePuits = target.ForgemagiePuits;

            Character.Inventory.RemoveItem(target.Id, 1);
            Character.Inventory.AddItem(forged, merge: false);

            m_caseItems.Remove(target.Id);
            m_caseItems[forged.Id] = 1;

            // Ventana de forjamagia: resultado con las stats nuevas.
            Character.Dispatch(WorldMessage.EXCHANGE_DISTANT_MOVEMENT(
                ExchangeMoveEnum.MOVE_OBJECT,
                OperatorEnum.OPERATOR_ADD,
                forged.Id + "|1|" + forged.TemplateId + "|" + forged.StringEffects));

            return forged;
        }

        private void EmitSuccess(ItemDAO displayItem)
        {
            Character.Dispatch(WorldMessage.CRAFT_TEMPLATE_CREATED(displayItem.TemplateId));
            Character.Map.Dispatch(WorldMessage.CRAFT_INTERACTIVE_SUCCESS(Character.Id, displayItem.TemplateId));
        }

        private void EmitFailure(ItemDAO displayItem, InformationEnum info)
        {
            Character.Dispatch(WorldMessage.CRAFT_TEMPLATE_FAILED(displayItem.TemplateId));
            Character.Dispatch(WorldMessage.IM_INFO_MESSAGE(info));
            Character.Map.Dispatch(WorldMessage.CRAFT_INTERACTIVE_FAILED(Character.Id, displayItem.TemplateId));
        }

        /// <summary>
        /// Repuebla la cuadrícula (el paquete Ec del resultado la vació en el cliente):
        /// objeto + agente restante, con el guid del clon si el objeto cambió.
        /// </summary>
        private void RepopulateGrid(ItemDAO displayItem, long agentGuid, int agentRemaining)
        {
            Character.Dispatch(WorldMessage.EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, displayItem.Id + "|1"));
            if (agentRemaining > 0)
                Character.Dispatch(WorldMessage.EXCHANGE_LOCAL_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, agentGuid + "|" + agentRemaining));
        }

        /// <summary>Consume una unidad del item dado del inventario y de las casillas.</summary>
        private void ConsumeOne(long guid, out int remainingInCase)
        {
            remainingInCase = (m_caseItems.TryGetValue(guid, out var current) ? current : 0) - 1;
            Character.Inventory.RemoveItem(guid, 1);

            if (remainingInCase > 0)
                m_caseItems[guid] = remainingInCase;
            else
                m_caseItems.Remove(guid);
        }

        /// <summary>
        /// Repetición automática (EMR n): un intento por tick, como el craft en bucle.
        /// </summary>
        public void Retry(int count)
        {
            if (m_loopTimer != null)
                return;

            m_loopCount = count;
            m_loopTimer = base.AddTimer(1100, Loop);
        }

        public void CancelRetry()
        {
            if (m_loopTimer == null)
                return;

            EndLoop(LOOP_INTERUPT);
        }

        private void Loop()
        {
            Character.CachedBuffer = true;

            Character.Dispatch(WorldMessage.CRAFT_LOOP_COUNT(m_loopCount - 1));

            Validate(null);

            m_loopCount--;

            if (m_loopCount <= 0 && m_loopTimer != null)
                EndLoop(LOOP_OK);

            Character.CachedBuffer = false;
        }

        private void EndLoop(int reason)
        {
            Character.Dispatch(WorldMessage.CRAFT_LOOP_END(reason));

            base.RemoveTimer(m_loopTimer);
            m_loopTimer = null;
        }

        /// <summary>
        /// Sin agente aplicable (agotado, desconocido o límite de forja): se interrumpe la
        /// fusión SIN vaciar las casillas — el objeto sigue en el panel para añadir más runas.
        /// El cliente muestra "ya no te quedan suficientes recursos..." (Ea3) y, en modo
        /// forgemagus, ese paquete NO vacía la cuadrícula.
        /// </summary>
        private void InterruptAttempt()
        {
            if (m_loopTimer != null)
                EndLoop(LOOP_ERROR);
            else
                Character.Dispatch(WorldMessage.CRAFT_LOOP_END(LOOP_ERROR));
        }

        /// <summary>
        /// Estado inválido (sin objeto forjable): avisa al cliente y corta el bucle. El EcEI
        /// vacía la cuadrícula local en modo forgemagus, así que el servidor la vacía también.
        /// </summary>
        private void AbortAttempt()
        {
            if (m_loopTimer != null)
                EndLoop(LOOP_ERROR);

            m_caseItems.Clear();

            Character.Dispatch(WorldMessage.CRAFT_NO_RESULT());
            Character.Map.Dispatch(WorldMessage.CRAFT_INTERACTIVE_NOTHING(Character.Id));
        }
    }
}
