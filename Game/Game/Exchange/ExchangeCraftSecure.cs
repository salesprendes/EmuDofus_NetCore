using Game.Database.Structure;
using Game.Entity;
using Game.Job;
using Game.Job.Forjamagia;
using Game.Job.Skill;
using Game.Network;
using Game.Spell;
using System.Collections.Generic;
using System.Linq;

namespace Game.Exchange
{
    /// <summary>
    /// Craft seguro para otro jugador (artesano público). Dos roles:
    ///  - Artesano (m_local): aporta el oficio. Recibe el tipo 12.
    ///  - Cliente (m_distant): recibe el objeto. Recibe el tipo 13.
    ///
    /// Soporta dos modos según la habilidad:
    ///  - Craft normal (<see cref="CraftSkill"/>): se fabrica un objeto para el cliente con los
    ///    ingredientes aportados por ambos.
    ///  - Forjamagia pública (<see cref="MagicSkill"/>): el artesano maguea el objeto que aporta
    ///    el cliente, con runas/pociones puestas por cualquiera.
    ///
    /// Pagos del cliente al artesano en dos zonas (1 = siempre, 2 = solo si éxito).
    ///
    /// El craft normal se modela sobre StarLoco (craftPublicMode). El FM público NO existe en
    /// StarLoco (allí está comentado), así que es diseño propio reutilizando el motor de forja.
    /// </summary>
    public sealed class ExchangeCraftSecure : AbstractEntityExchange
    {
        private const int SIGNING_ITEM_TEMPLATE = 7508;
        private const int ZONE_ALWAYS = 1;
        private const int ZONE_ON_SUCCESS = 2;

        public CharacterEntity Artisan { get; }
        public CharacterEntity Client { get; }
        public JobSkill Skill { get; }
        public int JobId { get; }
        public int MaxCase { get; }

        private bool IsMaging => Skill is MagicSkill;

        private readonly ServicioForjamagia m_forge;
        private readonly Dictionary<long, int> m_payItems = new Dictionary<long, int>();
        private readonly Dictionary<long, int> m_payItemsOnSuccess = new Dictionary<long, int>();
        private long m_payKamas;
        private long m_payKamasOnSuccess;

        public ExchangeCraftSecure(CharacterEntity artisan, CharacterEntity client, JobSkill skill)
            : base(ExchangeTypeEnum.EXCHANGE_CRAFT_SECURE_ARTISAN, artisan, client)
        {
            Artisan = artisan;
            Client = client;
            Skill = skill;
            JobId = artisan.CharacterJobs.GetJobId(skill.Id);
            m_forge = new ServicioForjamagia();
            // En forjamagia el cliente usa 3 casillas (como StarLoco); el craft, las del nivel.
            MaxCase = skill is MagicSkill ? 3 : artisan.CharacterJobs.GetCraftMaxCase(JobId);
        }

        protected override string SerializeAs_ExchangeCreate()
        {
            return MaxCase + ";" + (int)Skill.Id;
        }

        /// <summary>Cada jugador recibe el ECK con SU tipo (artesano 12, cliente 13).</summary>
        public override void Create()
        {
            var args = SerializeAs_ExchangeCreate();
            Artisan.Dispatch(WorldMessage.EXCHANGE_CREATE(ExchangeTypeEnum.EXCHANGE_CRAFT_SECURE_ARTISAN, args));
            Client.Dispatch(WorldMessage.EXCHANGE_CREATE(ExchangeTypeEnum.EXCHANGE_CRAFT_SECURE_CLIENT, args));
        }

        // --- Pago (solo lo aporta el cliente) ---

        public void MovePayKamas(int zone, long kamas)
        {
            if (kamas < 0)
                return;
            if (kamas > Client.Inventory.Kamas)
                kamas = Client.Inventory.Kamas;

            UnValidateAll();

            if (zone == ZONE_ON_SUCCESS)
                m_payKamasOnSuccess = kamas;
            else
                m_payKamas = kamas;

            base.Dispatch(WorldMessage.EXCHANGE_PAY_KAMAS(zone, kamas));
        }

        public void AddPayItem(int zone, long guid, int quantity)
        {
            var item = Client.Inventory.GetItem(guid);
            if (item == null || item.Slot != ItemSlotEnum.SLOT_INVENTORY || quantity < 1)
                return;

            var store = zone == ZONE_ON_SUCCESS ? m_payItemsOnSuccess : m_payItems;
            var already = store.TryGetValue(guid, out var existing) ? existing : 0;
            var realQuantity = item.Quantity - already;
            if (quantity > realQuantity)
                quantity = realQuantity;
            if (quantity <= 0)
                return;

            UnValidateAll();

            store[guid] = already + quantity;

            // El cliente ve su pago sin stats; el artesano lo ve con detalle (template|stats).
            Client.Dispatch(WorldMessage.EXCHANGE_PAY_ITEM(zone, OperatorEnum.OPERATOR_ADD, guid + "|" + store[guid]));
            Artisan.Dispatch(WorldMessage.EXCHANGE_PAY_ITEM(zone, OperatorEnum.OPERATOR_ADD, guid + "|" + store[guid] + "|" + item.TemplateId + "|" + item.StringEffects));
        }

        public void RemovePayItem(int zone, long guid, int quantity)
        {
            var store = zone == ZONE_ON_SUCCESS ? m_payItemsOnSuccess : m_payItems;
            if (!store.TryGetValue(guid, out var current) || quantity < 1)
                return;

            UnValidateAll();

            if (quantity >= current)
                store.Remove(guid);
            else
                store[guid] = current - quantity;

            var exists = store.ContainsKey(guid);
            base.Dispatch(WorldMessage.EXCHANGE_PAY_ITEM(zone, OperatorEnum.OPERATOR_REMOVE, guid.ToString()));
            if (exists)
            {
                Client.Dispatch(WorldMessage.EXCHANGE_PAY_ITEM(zone, OperatorEnum.OPERATOR_ADD, guid + "|" + store[guid]));
                var item = Client.Inventory.GetItem(guid);
                if (item != null)
                    Artisan.Dispatch(WorldMessage.EXCHANGE_PAY_ITEM(zone, OperatorEnum.OPERATOR_ADD, guid + "|" + store[guid] + "|" + item.TemplateId + "|" + item.StringEffects));
            }
        }

        // --- Validación + ejecución ---

        public override bool Validate(AbstractEntity entity)
        {
            m_validated[entity.Id] = !m_validated[entity.Id];
            base.Dispatch(WorldMessage.EXCHANGE_VALIDATE(entity.Id, m_validated[entity.Id]));

            if (!m_validated.Values.All(value => value))
                return false;

            Artisan.CachedBuffer = true;
            Client.CachedBuffer = true;

            if (IsMaging)
                ExecuteMagicCraft();
            else
                ExecuteNormalCraft();

            Artisan.CachedBuffer = false;
            Client.CachedBuffer = false;

            // El intercambio sigue abierto; se desvalida a ambos.
            UnValidateAll();
            return false;
        }

        // --- Craft normal público ---

        private void ExecuteNormalCraft()
        {
            var craftSkill = (CraftSkill)Skill;

            // Combinar los ingredientes de ambos (la firma 7508 no cuenta como ingrediente).
            var templateQuantity = new Dictionary<int, long>();
            var signed = false;
            foreach (var (item, _) in EnumerateIngredients())
            {
                if (item.TemplateId == SIGNING_ITEM_TEMPLATE)
                {
                    signed = true;
                    continue;
                }
                if (!templateQuantity.ContainsKey(item.TemplateId))
                    templateQuantity[item.TemplateId] = 0;
                templateQuantity[item.TemplateId] += 1;
            }

            var craftItem = templateQuantity.Count > 0
                ? craftSkill.Craftables.Find(entry => entry.MatchCraft(templateQuantity))
                : null;

            if (craftItem == null)
            {
                Artisan.Dispatch(WorldMessage.CRAFT_NO_RESULT());
                Client.Dispatch(WorldMessage.CRAFT_NO_RESULT());
                ClearExchange();
                return;
            }

            ConsumeAllIngredients();

            var chance = Artisan.CharacterJobs.GetCraftSuccessPercent(JobId, templateQuantity.Count);
            var success = Util.Next(0, 100) < chance;

            if (success)
            {
                var item = craftItem.Create(Client.Id, (int)Client.Type);
                if (signed)
                {
                    item.Statistics.AddEffect(EffectEnum.OBJETO_FABRICADO_POR, 0, 0, 0, Artisan.Name);
                    item.SaveStats();
                }
                Client.Inventory.AddItem(item, merge: !signed);

                // Resultado en la zona cooperativa + mensajes "fabricado para/por".
                var coopArgs = item.Id + "|1|" + item.TemplateId + "|" + item.StringEffects;
                Artisan.Dispatch(WorldMessage.EXCHANGE_COOP_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, coopArgs));
                Client.Dispatch(WorldMessage.EXCHANGE_COOP_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, coopArgs));
                Artisan.Dispatch(WorldMessage.CRAFT_SECURE_RESULT(item.TemplateId, 'T', Client.Name, item.StringEffects));
                Client.Dispatch(WorldMessage.CRAFT_SECURE_RESULT(item.TemplateId, 'B', Artisan.Name, item.StringEffects));
                Artisan.Map.Dispatch(WorldMessage.CRAFT_INTERACTIVE_SUCCESS(Artisan.Id, item.TemplateId));
            }
            else
            {
                Artisan.Dispatch(WorldMessage.CRAFT_TEMPLATE_FAILED(craftItem.Id));
                Client.Dispatch(WorldMessage.CRAFT_TEMPLATE_FAILED(craftItem.Id));
                Artisan.Map.Dispatch(WorldMessage.CRAFT_INTERACTIVE_FAILED(Artisan.Id, craftItem.Id));
            }

            Artisan.CharacterJobs.AddExperience(JobId, Artisan.CharacterJobs.GetCraftExperience(JobId, templateQuantity.Count));

            ApplyPayments(success);
            ClearExchange();
        }

        // --- Forjamagia pública (diseño propio, sin referencia en StarLoco) ---

        private void ExecuteMagicCraft()
        {
            var magicSkill = (MagicSkill)Skill;

            ItemDAO target = null, rune = null, potion = null, signature = null;
            CharacterEntity targetOwner = null, runeOwner = null, potionOwner = null, signatureOwner = null;

            foreach (var (item, owner) in EnumerateIngredients())
            {
                if (signature == null && item.TemplateId == SIGNING_ITEM_TEMPLATE)
                {
                    signature = item;
                    signatureOwner = owner;
                }
                else if (rune == null && GestorRunasForjamagia.Instance.EsRuna(item))
                {
                    rune = item;
                    runeOwner = owner;
                }
                else if (potion == null && GestorPocionesForjamagia.Instance.EsPocion(item))
                {
                    potion = item;
                    potionOwner = owner;
                }
                else if (target == null && magicSkill.CanEnhance(item.Template) && item.Template.Forgemageable)
                {
                    target = item;
                    targetOwner = owner;
                }
            }

            if (target == null || (rune == null && potion == null))
            {
                Artisan.Dispatch(WorldMessage.CRAFT_NO_RESULT());
                Client.Dispatch(WorldMessage.CRAFT_NO_RESULT());
                ClearExchange();
                return;
            }

            var jobLevel = Artisan.CharacterJobs.GetJobLevel(JobId);
            var changed = false;
            var success = false;

            if (rune != null)
            {
                var forgeRune = GestorRunasForjamagia.Instance.Resolver(rune);
                var forgeItem = new AdaptadorObjetoForjable(target);
                if (forgeRune.EsValida && m_forge.PuedeAplicarse(forgeItem, forgeRune, out _))
                {
                    var result = m_forge.AplicarRuna(forgeItem, forgeRune, jobLevel);
                    changed = result.ObjetoModificado;
                    success = result.Resultado == ResultadoForjamagia.ExitoCritico;
                    runeOwner.Inventory.RemoveItem(rune.Id, 1);
                    var experience = m_forge.ObtenerExperiencia(forgeItem, forgeRune, jobLevel, result.Resultado);
                    if (experience > 0)
                        Artisan.CharacterJobs.AddExperience(JobId, experience);
                }
            }
            else
            {
                var element = GestorPocionesForjamagia.Instance.Resolver(potion);
                if (element != null)
                {
                    changed = GestorPocionesForjamagia.Instance.Aplicar(target, element.Value);
                    success = changed;
                    potionOwner.Inventory.RemoveItem(potion.Id, 1);
                }
            }

            if (changed)
            {
                // Firma "Modificado por <artesano>".
                if (signature != null)
                {
                    target.Statistics.RemoveEffect(EffectEnum.OBJETO_MODIFICADO_POR);
                    target.Statistics.AddEffect(EffectEnum.OBJETO_MODIFICADO_POR, 0, 0, 0, Artisan.Name);
                    signatureOwner.Inventory.RemoveItem(signature.Id, 1);
                }

                target.SaveStats();
                targetOwner.Dispatch(WorldMessage.OBJECT_UPDATE(target));

                var coopArgs = target.Id + "|1|" + target.TemplateId + "|" + target.StringEffects;
                Artisan.Dispatch(WorldMessage.EXCHANGE_COOP_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, coopArgs));
                Client.Dispatch(WorldMessage.EXCHANGE_COOP_MOVEMENT(ExchangeMoveEnum.MOVE_OBJECT, OperatorEnum.OPERATOR_ADD, coopArgs));
            }

            var info = success ? InformationEnum.INFO_MAGIC_NOT_PERFECT : InformationEnum.INFO_MAGIC_FAILED;
            if (rune != null && success)
            {
                Artisan.Dispatch(WorldMessage.CRAFT_TEMPLATE_CREATED(target.TemplateId));
                Client.Dispatch(WorldMessage.CRAFT_TEMPLATE_CREATED(target.TemplateId));
            }
            else
            {
                Artisan.Dispatch(WorldMessage.IM_INFO_MESSAGE(info));
                Client.Dispatch(WorldMessage.IM_INFO_MESSAGE(info));
            }

            ApplyPayments(success);
            ClearExchange();
        }

        // --- Ingredientes / pagos / limpieza ---

        private IEnumerable<(ItemDAO Item, CharacterEntity Owner)> EnumerateIngredients()
        {
            foreach (var owner in new[] { Artisan, Client })
            {
                foreach (var entry in m_exchangedItems[owner.Id].ToList())
                {
                    var item = owner.Inventory.GetItem(entry.Key);
                    if (item == null)
                        continue;
                    for (var i = 0; i < entry.Value; i++)
                        yield return (item, owner);
                }
            }
        }

        private void ConsumeAllIngredients()
        {
            foreach (var owner in new[] { Artisan, Client })
            {
                foreach (var entry in m_exchangedItems[owner.Id].ToList())
                    owner.Inventory.RemoveItem(entry.Key, entry.Value);
            }
        }

        private void ApplyPayments(bool success)
        {
            var kamas = m_payKamas + (success ? m_payKamasOnSuccess : 0);
            if (kamas > 0 && Client.Inventory.Kamas >= kamas)
            {
                Client.Inventory.SubKamas(kamas);
                Artisan.Inventory.AddKamas(kamas);
            }

            GivePayItems(m_payItems);
            if (success)
                GivePayItems(m_payItemsOnSuccess);
        }

        private void GivePayItems(Dictionary<long, int> store)
        {
            foreach (var entry in store)
            {
                var moved = Client.Inventory.RemoveItem(entry.Key, entry.Value);
                if (moved != null)
                    Artisan.Inventory.AddItem(moved);
            }
        }

        private void ClearExchange()
        {
            m_exchangedItems[Artisan.Id].Clear();
            m_exchangedItems[Client.Id].Clear();
            m_exchangedKamas[Artisan.Id] = 0;
            m_exchangedKamas[Client.Id] = 0;
            m_payItems.Clear();
            m_payItemsOnSuccess.Clear();
            m_payKamas = 0;
            m_payKamasOnSuccess = 0;
        }
    }
}
