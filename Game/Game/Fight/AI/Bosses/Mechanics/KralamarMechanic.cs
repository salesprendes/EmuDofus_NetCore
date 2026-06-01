using Game.Entity;
using Game.Fight;
using Game.Fight.AI.Core;
using Game.Fight.AI.Evaluation;
using Game.Spell;
using System.Collections.Generic;
using System.Linq;

namespace Game.Fight.AI.Bosses.Mechanics
{
    /// <summary>
    /// Mecánica del Kralamoure Géant (template 423).
    ///
    /// Resumen del combate (confirmado en DB game_emudofus.sql):
    ///
    ///   • El Kralamar tiene 4 "humores" elementales representados por los estados 35-38:
    ///       Estado 35 = Humor Fuego  → invoca Tentáculo Primario   (template 424, hechizo 1107)
    ///       Estado 36 = Humor Agua   → invoca Tentáculo Cuaternario (template 1090, hechizo 1110)
    ///       Estado 37 = Humor Aire   → invoca Tentáculo Terciario   (template 1091, hechizo 1109)
    ///       Estado 38 = Humor Tierra → invoca Tentáculo Secundario  (template 1092, hechizo 1108)
    ///
    ///   • El hechizo "Kraken" (1103) cicla los humores (elimina todos y aplica el siguiente)
    ///     y se lanza sobre la celda propia (MinPO=MaxPO=0). También cura 500 HP y hace 200 de daño.
    ///
    ///   • El hechizo "Skupehagua Paralizante" (1105) causa daño de agua + drena PM al objetivo.
    ///
    ///   • El hechizo "Vulnerabilidad de la Turbera" (1106) se activa cuando el estado 34 está
    ///     presente (condición gestionada por el motor de combate, normalmente al caer todos los
    ///     tentáculos). Aplica vulnerabilidad masiva a todos los elementos en AoE.
    ///
    ///   • El hechizo "Turba Aplastante" (1279) se lanza sobre la propia celda y aplica los
    ///     estados 6 (raíz) y 7 (gravedad) al Kralamar, bloqueando teletransportación enemiga.
    ///
    ///   Cómo matar al Kralamar Gigante:
    ///     1. El Kralamar lanza Kraken → cambia de humor elemental.
    ///     2. Según el humor activo, invoca un tipo de tentáculo.
    ///     3. Los jugadores deben matar los tentáculos; al caer todos el motor aplica el estado 34.
    ///     4. Con estado 34, el Kralamar puede lanzar Vulnerabilidad → el grupo lo puede dañar
    ///        masivamente aprovechando las resistencias reducidas.
    ///     5. Repetir hasta derrotarlo.
    /// </summary>
    public sealed class KralamarMechanic : IBossMechanic
    {
        // ─── Template IDs confirmados en DB (tabla monstruos_template) ─────────────
        private static readonly HashSet<int> TentacleTemplateIds = new HashSet<int>
        {
            424,   // Tentacule Primaire
            425,   // Tentacule Kralamoure (nombre alternativo en tabla drops)
            1090,  // Tentáculo Cuaternario (invocado por hechizo 1110)
            1091,  // Tentáculo Terciario   (invocado por hechizo 1109)
            1092,  // Tentáculo Secundario  (invocado por hechizo 1108)
        };

        // ─── IDs de hechizo confirmados en DB (tabla monstruos_hechizos monster=423) ─
        private const int SpellKraken          = 1103; // Kraken           — auto-cast (PO 0-0)
        private const int SpellSkupehagua      = 1105; // Skupehagua       — ataque de agua a distancia (PO 4-22)
        private const int SpellVulnerabilidad  = 1106; // Vulnerabilidad   — auto-cast; requiere estado 34
        private const int SpellInvocPrimario   = 1107; // Invocar Tent. Primario   — requiere estado 35
        private const int SpellInvocSecundario = 1108; // Invocar Tent. Secundario — requiere estado 38
        private const int SpellInvocTerciario  = 1109; // Invocar Tent. Terciario  — requiere estado 37
        private const int SpellInvocCuaternario= 1110; // Invocar Tent. Cuaternario— requiere estado 36
        private const int SpellTurba           = 1279; // Turba Aplastante — auto-cast (PO 0-0)

        // ─── Umbrales de fase ────────────────────────────────────────────────────────
        private const double HpThresholdEnrage = 0.40; // < 40% HP → modo enrage

        // ─────────────────────────────────────────────────────────────────────────────

        public IEnumerable<AIDecision> Evaluate(AIContext context)
        {
            if (context?.Fighter == null)
                yield break;

            int livingTentacles = CountLivingTentacles(context);
            double hpRatio = context.Fighter.MaxLife > 0
                ? (double)context.Fighter.Life / context.Fighter.MaxLife
                : 1.0;

            bool enragePhase = hpRatio <= HpThresholdEnrage || livingTentacles == 0;

            // ══════════════════════════════════════════════════════════════════════
            // 1. INVOCACIÓN DE TENTÁCULOS
            //    Cada hechizo de invocación (1107-1110) requiere uno de los estados
            //    de humor elemental (35-38).  El motor rechaza automáticamente los que
            //    no cumplen la condición → proponemos los cuatro y el motor elige.
            // ══════════════════════════════════════════════════════════════════════
            if (!enragePhase || livingTentacles < 2)
            {
                var summonPriority = livingTentacles == 0
                    ? AIDecisionPriority.Critical
                    : AIDecisionPriority.High;

                var summonSpellIds = new[]
                {
                    SpellInvocPrimario,
                    SpellInvocSecundario,
                    SpellInvocTerciario,
                    SpellInvocCuaternario
                };

                var movement = new MovementEvaluator();
                foreach (var spellId in summonSpellIds)
                {
                    var spell = FindSpell(context, spellId);
                    if (spell == null || spell.APCost > context.CurrentAP)
                        continue;

                    // CanCastFromCurrentCell verifica el estado requerido via SpellManager
                    var cell = movement.GetBestSummonCell(context, spell);
                    if (!cell.HasValue)
                        continue;

                    yield return new AIDecision
                    {
                        Type     = AIDecisionType.Summon,
                        Priority = summonPriority,
                        Score    = 300 + (4 - livingTentacles) * 60,
                        SpellId  = spellId,
                        CellId   = (short)cell.Value,
                        Reason   = "Kralamar invoca tentáculo (humor-gate resuelto por motor)"
                    };
                }
            }

            // ══════════════════════════════════════════════════════════════════════
            // 2. KRAKEN (auto-cast)
            //    Cicla los humores elementales; también cura +500 HP y hace 200 dmg.
            //    Prioridad Alta cuando hay tentáculos (prepara la siguiente invocación).
            //    Prioridad Normal en fase de enrage (ya no necesita ciclar).
            // ══════════════════════════════════════════════════════════════════════
            var krakenSpell = FindSpell(context, SpellKraken);
            if (krakenSpell != null && krakenSpell.APCost <= context.CurrentAP)
            {
                // PO 0-0 → lanza sobre la propia celda
                if (SpellEvaluator.CanCastFromCurrentCell(context, krakenSpell, context.CurrentCellId))
                {
                    yield return new AIDecision
                    {
                        Type     = AIDecisionType.CastSpell,
                        Priority = livingTentacles > 0
                                    ? AIDecisionPriority.High
                                    : AIDecisionPriority.Normal,
                        Score    = enragePhase ? 160 : 240,
                        SpellId  = SpellKraken,
                        CellId   = (short)context.CurrentCellId,
                        Reason   = "Kraken — cicla humor elemental"
                    };
                }
            }

            // ══════════════════════════════════════════════════════════════════════
            // 3. VULNERABILIDAD DE LA TURBERA (auto-cast, requiere estado 34)
            //    Estado 34 es aplicado por el motor cuando caen todos los tentáculos.
            //    CanCastFromCurrentCell delega en SpellManager que comprueba el estado;
            //    si el estado 34 no está activo, el hechizo simplemente no se lanzará.
            // ══════════════════════════════════════════════════════════════════════
            var vulnSpell = FindSpell(context, SpellVulnerabilidad);
            if (vulnSpell != null && vulnSpell.APCost <= context.CurrentAP)
            {
                if (SpellEvaluator.CanCastFromCurrentCell(context, vulnSpell, context.CurrentCellId))
                {
                    yield return new AIDecision
                    {
                        Type     = AIDecisionType.Buff,
                        Priority = AIDecisionPriority.Critical,
                        Score    = 600,
                        SpellId  = SpellVulnerabilidad,
                        CellId   = (short)context.CurrentCellId,
                        Reason   = "Vulnerabilidad — debilita resistencias de todos los enemigos"
                    };
                }
            }

            // ══════════════════════════════════════════════════════════════════════
            // 4. SKUPEHAGUA PARALIZANTE (ataque a distancia, PO 4-22)
            //    Daño de agua + drenaje de PM → debilita movilidad enemiga.
            //    Target prioritario: el más peligroso, luego el más cercano.
            // ══════════════════════════════════════════════════════════════════════
            var skupSpell = FindSpell(context, SpellSkupehagua);
            if (skupSpell != null && skupSpell.APCost <= context.CurrentAP)
            {
                var target = TargetEvaluator.GetMostDangerousEnemy(context)
                          ?? TargetEvaluator.GetNearestEnemy(context);

                if (target?.Cell != null && !target.IsFighterDead
                    && SpellEvaluator.CanCastFromCurrentCell(context, skupSpell, target.Cell.Id))
                {
                    int skupScore = 180
                        + TargetEvaluator.ScoreLowHp(target)
                        + TargetEvaluator.ScorePriorityTarget(target) / 3;

                    yield return new AIDecision
                    {
                        Type     = AIDecisionType.Debuff,
                        Priority = AIDecisionPriority.High,
                        Score    = skupScore,
                        SpellId  = SpellSkupehagua,
                        TargetId = target.Id,
                        CellId   = (short)target.Cell.Id,
                        Reason   = "Skupehagua — daño agua + drenaje de PM"
                    };
                }

                // Si no puede alcanzar el objetivo óptimo, prueba con cualquier enemigo
                if (!context.Enemies.Any(e => e?.Cell != null && !e.IsFighterDead
                    && SpellEvaluator.CanCastFromCurrentCell(context, skupSpell, e.Cell.Id)))
                {
                    // Intenta acercarse para alcanzar rango (delegamos al MovementEvaluator del Brain)
                }
            }

            // ══════════════════════════════════════════════════════════════════════
            // 5. TURBA APLASTANTE (auto-cast)
            //    Aplica estados de raíz/gravedad sobre el Kralamar y bloquea
            //    hechizos de teletransporte en un área (descripción DB).
            //    Solo útil cuando hay enemigos vivos; coste bajo en PA.
            // ══════════════════════════════════════════════════════════════════════
            var turbaSpell = FindSpell(context, SpellTurba);
            if (turbaSpell != null && turbaSpell.APCost <= context.CurrentAP
                && context.Enemies.Any(e => e != null && !e.IsFighterDead))
            {
                if (SpellEvaluator.CanCastFromCurrentCell(context, turbaSpell, context.CurrentCellId))
                {
                    yield return new AIDecision
                    {
                        Type     = AIDecisionType.Buff,
                        Priority = AIDecisionPriority.Normal,
                        Score    = 100,
                        SpellId  = SpellTurba,
                        CellId   = (short)context.CurrentCellId,
                        Reason   = "Turba Aplastante — bloquea teletransporte enemigo"
                    };
                }
            }
        }

        private static int CountLivingTentacles(AIContext context) => context.Allies.Count(a => a != null && !a.IsFighterDead && TentacleTemplateIds.Contains(GetMonsterId(a)));
        private static int GetMonsterId(AbstractFighter fighter) => (fighter as MonsterEntity)?.Grade?.MonsterId ?? 0;
        private static SpellLevel FindSpell(AIContext context, int spellId) => context.SpellBook.AllSpells.FirstOrDefault(s => s?.SpellId == spellId);
    }
}
