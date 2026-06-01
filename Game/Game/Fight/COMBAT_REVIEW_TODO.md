# Combat review TODO

This note records combat findings from the Dofus 1.29 client comparison and the
server-side AI/movement pass.

## Fixed in this pass

- `AIFighter.CanBeMoved()` now returns true by default so monster, summon and
  dopeul brains can generate movement again.
- `ConquestPrismEntity.CanBeMoved()` explicitly returns false so static prisms
  do not inherit the generic AI movement behavior.
- `MoveToCellAIAction` prepares paths from the fighter current cell at execution
  time and revalidates the prepared path before queueing movement.
- Spell range evaluation now follows the 1.29 client behavior where `MaxPO = 0`
  is not extended by range bonuses.
- `CanLaunchSpell`, `TryLaunchSpell`, `Move` and public pathfinding helpers have
  extra null/invalid-cell guards to fail safely instead of ending the fight loop.
- PvM win-case repop is now delayed: `MonsterFight.FightEnd` destroys the defeated
  group immediately and calls `MapInstance.ScheduleRepop`, which spawns a fresh
  random group after a randomized `MONSTER_REPOP_DELAY_MIN..MAX` delay (5–10 min by
  default, configurable). The lose-case still re-pops the same group instantly.
  The repop timer revalidates monster data, fight cells and the group cap before
  spawning, so it cancels cleanly if the map state changed during the wait.

## Needs gameplay validation before changing

- Tackle currently returns the random failure roll and uses it as the AP loss
  percent. This may not match the intended 1.29 lock formula, but changing it
  should be tested against real combat examples first.
- Server LOS uses a simpler grid/Bresenham check than the client `checkView`,
  which accounts for cell height and sprites. Any parity change here can affect
  many spells and should be tested on several maps.
- Weapon range always receives `AddPO` in `CanUseWeapon`. Spell range has been
  aligned with the client, but weapon behavior should be confirmed separately
  before changing it.
