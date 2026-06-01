# TaxCollector AI

## Detection

`AIProfileResolver` resolves every `TaxCollectorEntity` to `AIProfile.TaxCollector`.
This is an internal AI profile only. It is not a tax collector id, spell id, template id, or variant.

## Brain

`TaxCollectorBrain` is the only tax collector brain.
It inherits from `AIBrain`, builds the normal `AIContext`, uses `AITurnBudget`, and falls back to `EndTurn` when the fight state is incomplete or no useful action exists.

## Fight Model

In `TaxCollectorFight`, the tax collector joins the defender team.
The AI uses `AIContext.Allies` for the tax collector and living defenders, and `AIContext.Enemies` for attackers.

## Spell Classification

The AI uses `AISpellBook`.
Existing categories are reused for damage, heal, buff, debuff, AP/MP removal, push/pull, movement, summon, trap, glyph, and vulnerability.
The tax collector integration adds lightweight categories for range removal, defensive spells, and unbewitch spells using real `EffectEnum` values.

## Scoring

`TaxCollectorEvaluator` wraps the generic evaluators:

- `AttackEvaluator`
- `HealEvaluator`
- `BuffEvaluator`
- `DebuffEvaluator`
- `MovementEvaluator`
- `TargetEvaluator`
- `RiskEvaluator`
- `SpellEvaluator`

It adjusts scores by combat context:

- finish an attacker if a current-cell spell can kill it
- heal or protect the collector at low health
- control nearby dangerous attackers
- heal or protect wounded defenders
- use useful debuffs, buffs, and damage spells
- move only when `CanBeMoved()` is true and movement reduces risk or enables a useful cast

## Defense Mode

`TaxCollectorDefenseMode` is calculated cheaply from the current turn:

- `CannotAct`: invalid context, no AP, or no spells
- `LowHealth`: collector HP is at or below 30 percent
- `Surrounded`: multiple enemies are adjacent or very close
- `NoDefenders`: no living defender remains besides the collector
- `UnderPressure`: at least one nearby threat exists
- `Normal`: no special pressure detected

## Logs

When `WorldConfig.LOG_DEBUG` is enabled, decisions are logged as:

`[AI][TaxCollector] Fighter={id} Mode={mode} Decision={type} Priority={priority} Score={score} Spell={spellId} Target={targetId} Reason={reason}`

## Limitations

- No database lookup is done during the turn.
- No spell ids, state ids, or tax collector variants are hardcoded.
- The AI does not simulate future turns.
- The current action chain precomputes the turn, so post-movement spell decisions remain low priority to let movement execute first.

## TODO

- Add unit tests if an AI test project is introduced.
- Consider exposing a reusable action validation helper so evaluators can avoid duplicating current-cell spell checks.
