using Game.Entity;
using Game.Fight.Effect;
using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using Game.Map;
using Game.Spell;
using Protocolo.Framework.Generic.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Bosses.Mechanics
{
    public sealed class KralamarMechanic : IBossMechanic
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(KralamarMechanic));

        static KralamarMechanic()
        {
            FighterStateManager.RegisterCodeManagedState((int)FighterStateEnum.STATE_KRALAMAR_DESIRE_KILL);
            FighterStateManager.RegisterCodeManagedState((int)FighterStateEnum.STATE_KRALAMAR_DESIRE_PARALYZE);
            FighterStateManager.RegisterCodeManagedState((int)FighterStateEnum.STATE_KRALAMAR_DESIRE_CURSE);
            FighterStateManager.RegisterCodeManagedState((int)FighterStateEnum.STATE_KRALAMAR_DESIRE_POISON);
        }

        private const int KralamarTemplateId = 423;

        private const int SpellKraken = 1103;
        private const int SpellSkupehagua = 1105;
        private const int SpellVulnerabilidad = 1106;
        private const int SpellInvocPrimario = 1107;
        private const int SpellInvocSecundario = 1108;
        private const int SpellInvocTerciario = 1109;
        private const int SpellInvocCuaternario = 1110;
        private const int SpellTurba = 1279;

        private const int TentaculoPrimario = 424;
        private const int TentaculoCuaternario = 1090;
        private const int TentaculoTerciario = 1091;
        private const int TentaculoSecundario = 1092;

        private static readonly HashSet<int> RequiredTentacleTemplateIds = new HashSet<int> { TentaculoCuaternario, TentaculoTerciario, TentaculoSecundario, TentaculoPrimario };

        private static readonly TentacleStep[] SummonOrder =
        {
            new TentacleStep(KralamarElement.Water, SpellInvocCuaternario, TentaculoCuaternario, "Cuaternario", FighterStateEnum.STATE_KRALAMAR_DESIRE_PARALYZE),
            new TentacleStep(KralamarElement.Fire,  SpellInvocTerciario,   TentaculoTerciario,   "Terciario",   FighterStateEnum.STATE_KRALAMAR_DESIRE_CURSE),
            new TentacleStep(KralamarElement.Earth, SpellInvocSecundario,  TentaculoSecundario,  "Secundario",  FighterStateEnum.STATE_KRALAMAR_DESIRE_POISON),
            new TentacleStep(KralamarElement.Air,   SpellInvocPrimario,    TentaculoPrimario,    "Primario",    FighterStateEnum.STATE_KRALAMAR_DESIRE_KILL)
        };

        private readonly object m_sync = new object ();
        private int m_expectedStep;
        private TentacleStep m_pendingTentacle;
        private bool m_punishmentPending;
        private int m_lastActivatingSpellId = -1;

        public void OnKralamarTurnStart(AbstractFighter kralamar)
        {
            lock (m_sync)
            {
                ConfirmPendingTentacle(kralamar);
            }
        }

        public void OnDamageReceived(AbstractFighter kralamar, CastInfos castInfos, int damageBeforeResistance)
        {
            if (!IsKralamar(kralamar) || castInfos == null || damageBeforeResistance <= 0)
            {
                Logger.Warn($"[Kralamar] OnDamageReceived ignorado: esKralamar={IsKralamar(kralamar)} infoLanzamiento={(castInfos != null)} danio={damageBeforeResistance} tipoEfecto={castInfos?.EffectType}");
                return;
            }

            if (!TryGetElement(castInfos.EffectType, out KralamarElement element))
            {
                Logger.Warn($"[Kralamar] Efecto {castInfos.EffectType} (id={(int)castInfos.EffectType}) no reconocido como elemental.");
                return;
            }

            Logger.Warn($"[Kralamar] Golpe elemental recibido: {element} paso={m_expectedStep} hechizoId={castInfos.SpellId} danio={damageBeforeResistance}");

            lock (m_sync)
            {
                ConfirmPendingTentacle(kralamar);

                if (m_pendingTentacle != null)
                {
                    if (castInfos.SpellId == m_lastActivatingSpellId)
                        return;

                    if (element == m_pendingTentacle.Element)
                        return;

                    Logger.Warn($"[Kralamar] Secuencia rota: tentaculo pendiente={m_pendingTentacle.Name} elemento nuevo={element}");
                    BreakSequence(kralamar, element);
                    return;
                }

                var expected = SummonOrder[m_expectedStep];
                if (element != expected.Element)
                {
                    Logger.Warn($"[Kralamar] Elemento incorrecto: esperado={expected.Element} recibido={element}");
                    BreakSequence(kralamar, element);
                    return;
                }

                m_pendingTentacle = expected;
                m_lastActivatingSpellId = castInfos.SpellId;
                m_punishmentPending = false;
                m_expectedStep = (m_expectedStep + 1) % SummonOrder.Length;
                kralamar.StateManager?.ForceAddState(expected.DesireState);
                Logger.Warn($"[Kralamar] Tentaculo pendiente activado: {expected.Name} (hechizo={expected.SpellId} estado={(int)expected.DesireState})");
            }
        }

        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            if (context?.Fighter == null)
                yield break;

            var pendingTentacle = GetPendingTentacle(context.Fighter);
            var punishmentPending = ConsumePunishment();

            if (punishmentPending)
            {
                foreach (var punishment in EvaluatePunishment(context))
                    yield return punishment;
            }

            if (pendingTentacle != null)
            {
                foreach (var summon in EvaluatePendingSummon(context, pendingTentacle))
                    yield return summon;

                yield break;
            }

            if (punishmentPending)
                yield break;

            if (CanCastVulnerability(context))
            {
                var vulnerability = FindSpell(context, SpellVulnerabilidad);
                if (vulnerability != null
                    && vulnerability.APCost <= context.CurrentAP
                    && SpellEvaluator.CanCastFromCurrentCell(context, vulnerability, context.CurrentCellId))
                {
                    yield return new AIDecision
                    {
                        Type = AIDecisionType.Buff,
                        Priority = AIDecisionPriority.Critical,
                        Score = 900,
                        SpellId = SpellVulnerabilidad,
                        TargetId = context.Fighter.Id,
                        CellId = (short)context.CurrentCellId,
                        Reason = "Kralamar ventana de vulnerabilidad"
                    };
                }
            }

            foreach (var decision in EvaluateKraken(context, AIDecisionPriority.High, 240, "Kraken - vitality upkeep"))
                yield return decision;

            foreach (var decision in EvaluateSkupehagua(context))
                yield return decision;

            foreach (var decision in EvaluateTurba(context))
                yield return decision;
        }

        private IEnumerable<AIDecision> EvaluatePendingSummon(AIContext context, TentacleStep pendingTentacle)
        {
            if (HasLivingTentacle(context.Fighter, pendingTentacle.TemplateId))
            {
                Logger.Warn($"[Kralamar] Tentaculo {pendingTentacle.Name} (id={pendingTentacle.TemplateId}) ya esta vivo en el equipo. El grupo del monstruo incluye los tentaculos desde el inicio?");
                ClearPendingTentacle(context.Fighter);
                yield break;
            }

            var spell = FindSpell(context, pendingTentacle.SpellId);
            if (spell == null)
            {
                Logger.Warn($"[Kralamar] Hechizo de invocacion {pendingTentacle.SpellId} ({pendingTentacle.Name}) no encontrado en el grimorio del Kralamar.");
                yield break;
            }

            if (spell.APCost > context.CurrentAP)
            {
                Logger.Warn($"[Kralamar] Sin PA para invocar {pendingTentacle.Name}: necesita {spell.APCost} tiene {context.CurrentAP}");
                yield break;
            }

            var cell = new MovementEvaluator().GetBestSummonCell(context, spell);
            if (!cell.HasValue)
            {
                Logger.Warn($"[Kralamar] GetBestSummonCell no encontro celda valida para {pendingTentacle.Name} (hechizoId={pendingTentacle.SpellId} poMin={spell.MinPO} poMax={spell.MaxPO} enLinea={spell.InLine} lineaVision={spell.LOS} celdaVacia={spell.EmptyCell})");
                yield break;
            }

            yield return new AIDecision
            {
                Type = AIDecisionType.Summon,
                Priority = AIDecisionPriority.Critical,
                Score = 1000,
                SpellId = pendingTentacle.SpellId,
                CellId = (short)cell.Value,
                Reason = "Kralamar invoca tentaculo " + pendingTentacle.Name
            };
        }

        private IEnumerable<AIDecision> EvaluatePunishment(AIContext context)
        {
            foreach (var kraken in EvaluateKraken(context, AIDecisionPriority.Critical, 800, "Kraken - broken tentacle sequence"))
                yield return kraken;

            foreach (var turba in EvaluateTurba(context, AIDecisionPriority.High, 500))
                yield return turba;
        }

        private IEnumerable<AIDecision> EvaluateKraken(AIContext context, AIDecisionPriority priority, int score, string reason)
        {
            var spell = FindSpell(context, SpellKraken);
            if (spell == null || spell.APCost > context.CurrentAP)
                yield break;

            if (!SpellEvaluator.CanCastFromCurrentCell(context, spell, context.CurrentCellId))
                yield break;

            yield return new AIDecision
            {
                Type = AIDecisionType.CastSpell,
                Priority = priority,
                Score = score,
                SpellId = SpellKraken,
                CellId = (short)context.CurrentCellId,
                Reason = reason
            };
        }

        private IEnumerable<AIDecision> EvaluateSkupehagua(AIContext context)
        {
            var spell = FindSpell(context, SpellSkupehagua);
            if (spell == null || spell.APCost > context.CurrentAP)
                yield break;

            var target = TargetEvaluator.GetMostDangerousEnemy(context) ?? TargetEvaluator.GetNearestEnemy(context);

            if (target?.Cell == null || target.IsFighterDead)
                yield break;

            if (!SpellEvaluator.CanCastFromCurrentCell(context, spell, target.Cell.Id))
                yield break;

            yield return new AIDecision
            {
                Type = AIDecisionType.Debuff,
                Priority = AIDecisionPriority.High,
                Score = 220 + TargetEvaluator.ScoreLowHp(target) + TargetEvaluator.ScorePriorityTarget(target) / 3,
                SpellId = SpellSkupehagua,
                TargetId = target.Id,
                CellId = (short)target.Cell.Id,
                Reason = "Skupehagua - danio de agua y presion de PM"
            };
        }

        private IEnumerable<AIDecision> EvaluateTurba(AIContext context, AIDecisionPriority priority = AIDecisionPriority.Normal, int score = 120)
        {
            var spell = FindSpell(context, SpellTurba);
            if (spell == null || spell.APCost > context.CurrentAP)
                yield break;

            if (!context.Enemies.Any(e => e != null && !e.IsFighterDead))
                yield break;

            if (!SpellEvaluator.CanCastFromCurrentCell(context, spell, context.CurrentCellId))
                yield break;

            yield return new AIDecision
            {
                Type = AIDecisionType.Buff,
                Priority = priority,
                Score = score,
                SpellId = SpellTurba,
                TargetId = context.Fighter.Id,
                CellId = (short)context.CurrentCellId,
                Reason = "Turba Aplastante"
            };
        }

        private TentacleStep GetPendingTentacle(AbstractFighter kralamar)
        {
            lock (m_sync)
            {
                ConfirmPendingTentacle(kralamar);
                return m_pendingTentacle;
            }
        }

        private bool ConsumePunishment()
        {
            lock (m_sync)
            {
                if (!m_punishmentPending)
                    return false;

                m_punishmentPending = false;
                return true;
            }
        }

        private void ClearPendingTentacle(AbstractFighter kralamar = null)
        {
            lock (m_sync)
            {
                if (m_pendingTentacle != null)
                    kralamar?.StateManager?.ForceRemoveState(m_pendingTentacle.DesireState);
                m_pendingTentacle = null;
                m_lastActivatingSpellId = -1;
            }
        }

        private void ConfirmPendingTentacle(AbstractFighter kralamar)
        {
            if (m_pendingTentacle != null && HasLivingTentacle(kralamar, m_pendingTentacle.TemplateId))
            {
                kralamar.StateManager?.ForceRemoveState(m_pendingTentacle.DesireState);
                m_pendingTentacle = null;
                m_lastActivatingSpellId = -1;
            }
        }

        private void BreakSequence(AbstractFighter kralamar = null, KralamarElement triggerElement = KralamarElement.None)
        {
            if (m_pendingTentacle != null)
                kralamar?.StateManager?.ForceRemoveState(m_pendingTentacle.DesireState);

            m_pendingTentacle = null;
            m_lastActivatingSpellId = -1;
            m_punishmentPending = true;



            var stepIdx = triggerElement != KralamarElement.None ? FindStepForElement(triggerElement) : -1;

            if (stepIdx < 0 || HasLivingTentacle(kralamar, SummonOrder[stepIdx].TemplateId))
                stepIdx = FindNextAvailableStep(kralamar, stepIdx >= 0 ? (stepIdx + 1) % SummonOrder.Length : 0);

            if (stepIdx >= 0)
            {
                var step = SummonOrder[stepIdx];
                m_pendingTentacle = step;
                m_expectedStep = (stepIdx + 1) % SummonOrder.Length;
                kralamar?.StateManager?.ForceAddState(step.DesireState);
                Logger.Warn($"[Kralamar] Secuencia rota: tentaculo asignado={step.Name} (elemento={triggerElement})");
            }
            else
            {
                m_expectedStep = 0;
                Logger.Warn("[Kralamar] Secuencia rota: todos los tentaculos ya estan vivos, solo castigo.");
            }
        }

        private int FindStepForElement(KralamarElement element)
        {
            for (int i = 0; i < SummonOrder.Length; i++)
                if (SummonOrder[i].Element == element)
                    return i;
            return -1;
        }

        private int FindNextAvailableStep(AbstractFighter kralamar, int startFrom)
        {
            for (int i = 0; i < SummonOrder.Length; i++)
            {
                var idx = (startFrom + i) % SummonOrder.Length;
                if (!HasLivingTentacle(kralamar, SummonOrder[idx].TemplateId))
                    return idx;
            }
            return -1;
        }

        private static bool CanCastVulnerability(AIContext context)
        {
            if (!HasAllRequiredTentacles(context))
                return false;

            return context.Enemies.Any(f => f?.StateManager?.HasState(FighterStateEnum.STATE_KRALAMAR_QUATERNARY_INK) == true);
        }

        private static bool HasAllRequiredTentacles(AIContext context)
        {
            return RequiredTentacleTemplateIds.All(templateId => context.Allies.Any(f => GetMonsterId(f) == templateId && !f.IsFighterDead));
        }

        private static bool TryGetElement(EffectEnum effectType, out KralamarElement element)
        {
            switch (effectType)
            {
                case EffectEnum.DamageWater:
                case EffectEnum.StealWater:
                case EffectEnum.DamageLifeWater:
                    element = KralamarElement.Water;
                    return true;

                case EffectEnum.DamageFire:
                case EffectEnum.StealFire:
                case EffectEnum.DamageLifeFire:
                    element = KralamarElement.Fire;
                    return true;

                case EffectEnum.DamageEarth:
                case EffectEnum.StealEarth:
                case EffectEnum.DamageLifeEarth:
                    element = KralamarElement.Earth;
                    return true;

                case EffectEnum.DamageAir:
                case EffectEnum.StealAir:
                case EffectEnum.DamageLifeAir:
                    element = KralamarElement.Air;
                    return true;

                default:
                    element = KralamarElement.None;
                    return false;
            }
        }

        private static bool HasLivingTentacle(AbstractFighter kralamar, int templateId)
        {
            return kralamar?.Team?.AliveFighters != null && kralamar.Team.AliveFighters.Any(f => GetMonsterId(f) == templateId && !f.IsFighterDead);
        }

        private static bool IsKralamar(AbstractFighter fighter)
        {
            return GetMonsterId(fighter) == KralamarTemplateId;
        }

        private static int GetMonsterId(AbstractFighter fighter)
        {
            return (fighter as MonsterEntity)?.Grade?.MonsterId ?? 0;
        }

        private static SpellLevel FindSpell(AIContext context, int spellId)
        {
            return context.SpellBook.AllSpells.FirstOrDefault(s => s?.SpellId == spellId);
        }

        private enum KralamarElement
        {
            None,
            Water,
            Fire,
            Earth,
            Air
        }

        private sealed class TentacleStep
        {
            public KralamarElement Element { get; }
            public int SpellId { get; }
            public int TemplateId { get; }
            public string Name { get; }
            public FighterStateEnum DesireState { get; }

            public TentacleStep(KralamarElement element, int spellId, int templateId, string name, FighterStateEnum desireState)
            {
                Element = element;
                SpellId = spellId;
                TemplateId = templateId;
                Name = name;
                DesireState = desireState;
            }
        }
    }
}
