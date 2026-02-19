# Project: Another (deck-building, line-based effects)

## Project Overview
- Cards have a fixed count. We grow card "lines" (contexts) instead of adding new cards.
- Each line is executed top-to-bottom; buffs can amplify debuffs and vice versa.
- Enhancement (AnotherTextSystem): add a random positive line + random negative line to a card.
- Effects are data-driven. JSON defines effects, code in `Assets/_project/script/card/` executes them.

## Your Role (Gemini CLI)
- Primary task: add or edit effect definitions in JSON only. 
- Understand the existing effects from the JSON file and add effects to make the game deeper and more interesting.
List concrete effect ideas categorized by complexity. I need effects that are not just static numbers but interact with the "Line" system.

## Where to Edit
- Effects JSON: `Assets/_project/data/json/effect/effects.json`
- Effect control code (do not edit unless asked): `Assets/_project/script/card/`

## Effect JSON Schema
Each entry in `effects`:
- `id` (string, unique)
- `pool` (string, e.g. `enhancement_plus`, `enhancement_minus`)
- `lineID` (string)
- `displayName` (string)
- `displayPhrase` (string)
- `displayValueText` (string)
- `description` (string)
- `type` (string: `Effect`, `Buff`, `Debuff`, `Curse`)
- `valueScore` (int: positive for good, negative for bad)
- `paramsInt` (list of `{ key, value }`)
- `paramsStr` (list of `{ key, value }`)

## Supported Behaviors (No Code Changes Needed)
- **Attack**: `type = Effect` and `paramsInt` contains `baseValue` (or `value`).
  - Optional `paramsStr` `dmgType`: `normal`, `fire`, `ice`, `lightning`, `poison`, `dark`.
- **Buff**: `type = Buff`, `paramsStr.targetStat = atkBuff`, `paramsInt.value = +/-`.
- **Debuff**: `type = Debuff`, `paramsStr.targetStat = EnemyDefenseDown`, `paramsInt.value = +/-`.
- **Curse (self damage)**: `type = Curse`, `lineID = self_dmg`, `paramsInt.value`, optional `dmgType`.

## Naming & Consistency
- PLEASE follow existing effect.json consistency.
- Keep `id` unique and stable. Use lowercase with underscores.
- `displayName` should look good on UI; `displayPhrase` + `displayValueText` should read like a short compound label.
- Keep UTF-8 (no BOM). No comments in JSON.

## Pending Implementation (Requires Code Changes in CardEffectExecutor.cs)


