# Another UI/Audio Migration Plan (uGUI -> UI Toolkit + FMOD)

## Goal
- Keep current game loop and effect logic.
- Rebuild UI with modern runtime UI.
- Add scalable, event-driven SFX.
- Avoid big-bang rewrite.

## Current Baseline
- Core loop is centralized in `Assets/_project/script/core/BattleManager.cs`.
- UI is mostly imperative `Show()/Hide()` calls from `BattleManager`.
- Existing uGUI screens:
  - `Assets/_project/script/UI/TitleScreenUI.cs`
  - `Assets/_project/script/UI/CardSelectionUI.cs`
  - `Assets/_project/script/UI/PostBattleEnhancementUI.cs`
  - `Assets/_project/script/UI/LayerRewardUI.cs`
  - `Assets/_project/script/UI/ShopUI.cs`
  - `Assets/_project/script/UI/RunResultUI.cs`
  - `Assets/_project/script/UI/DamagePopupManager.cs`

## Target Architecture
- `BattleManager` remains gameplay authority.
- Add a presentation boundary:
  - `BattleUIBridge` (new): receives state/events from `BattleManager`.
  - `UI Toolkit Presenter` (new): updates VisualElements.
  - `Legacy UI Adapter` (temporary): keeps current uGUI alive during migration.
- Audio boundary:
  - `BattleAudioBridge` (new): one-shot/event calls.
  - FMOD event IDs in ScriptableObject or constants file.

## Screen Migration Order (Low Risk)
1. Title + About
2. Result
3. Battle HUD (HP, shield, turn, score)
4. Card selection
5. Enhancement/Reward/Shop overlays
6. Line calibration/remove
7. Damage popup/line display polish

## Why This Order
- Title/Result are isolated and easy to verify.
- Battle HUD gives immediate visual upgrade with low logic risk.
- Selection/Reward/Shop are list-heavy and benefit most from UI Toolkit.

## Phase Plan

### Phase 0: Stabilize Contracts
- Freeze existing UI behavior.
- Define DTOs used by UI only:
  - `BattleHudViewData`
  - `CardListViewData`
  - `RewardViewData`
  - `ShopViewData`
  - `ResultViewData`

### Phase 1: Add Bridge (No Visual Change Yet)
- Create `BattleUIBridge` with methods called by `BattleManager`:
  - `ShowTitle()`, `ShowBattleHud()`, `ShowEnhancement(...)`, `ShowReward(...)`, `ShowShop(...)`, `ShowResult(...)`
- Internally route to legacy uGUI first.
- Keep old fields in `BattleManager` until each screen is replaced.

### Phase 2: UI Toolkit Base
- Add `UIDocument` root scene object.
- Create `UXML/USS` structure:
  - `Assets/_project/ui/uxml/MainRoot.uxml`
  - `Assets/_project/ui/uss/theme.uss`
  - `Assets/_project/ui/uss/layout.uss`
- Build panel stack:
  - `TitlePanel`
  - `BattlePanel`
  - `OverlayPanel` (Enhancement/Reward/Shop/Calibration)
  - `ResultPanel`

### Phase 3: Replace Screens One by One
- For each screen:
  1. Implement Toolkit presenter
  2. Wire via `BattleUIBridge`
  3. Remove legacy reference from `BattleManager`
  4. Verify keyboard + mouse behavior

### Phase 4: Remove Legacy uGUI Dependencies
- Remove old `Show/Hide` branching in `BattleManager`.
- Keep only bridge calls.
- Delete unused uGUI prefabs/components.

## FMOD Integration Plan

## Event Set (minimum)
- `ui/click`
- `ui/open_panel`
- `ui/confirm`
- `battle/line_execute`
- `battle/damage_hit`
- `battle/heal`
- `battle/shield`
- `battle/turn_player`
- `battle/turn_enemy`
- `battle/victory`
- `battle/defeat`
- `reward/claim`
- `shop/buy`

## Trigger Source
- Trigger from gameplay events, not button code only.
- Suggested hook points:
  - `BattleManager` turn start/end
  - `CardEffectExecutor` resolved effects
  - Reward/Shop purchase handlers

## Mix Design
- Buses:
  - `Master`
  - `UI`
  - `Battle`
  - `Ambience`
- Add sidechain ducking: `UI` briefly ducks `Battle` for strong feedback sounds.

## UI Toolkit Technical Notes
- Use `ListView` for:
  - Card hand
  - Enhancement options
  - Reward items
  - Shop items
- Use pooled item binders for performance.
- Use class toggles for selection state (`.is-selected`, `.is-hovered`).
- Keep line color logic centralized in one mapper (valueScore -> style class).

## Input/Navigation
- Keep Input System.
- Unify navigation:
  - Arrow/WASD move
  - Enter/Space confirm
  - Mouse hover updates preview
- EventSystem + panel focus must be validated at each screen switch.

## Immediate First Sprint (Recommended)
1. Implement `BattleUIBridge` and route Title/Result through it.
2. Create Toolkit `TitlePanel` and `ResultPanel`.
3. Add FMOD `ui/click`, `battle/victory`, `battle/defeat`.
4. Leave battle overlay screens on legacy UI for now.

## Done Criteria
- No direct `Show/Hide` calls from `BattleManager` to concrete UI screen classes.
- Title/Result fully on UI Toolkit.
- FMOD events play for key game transitions.
- Existing run loop behavior unchanged.

