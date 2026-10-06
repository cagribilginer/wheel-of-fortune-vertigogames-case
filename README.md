# Wheel of Fortune — Zone Progression Roulette

A landscape mobile prize-wheel game built as a technical evaluation piece. The player spins a
themed wheel to bank rewards, pushing deeper through numbered **zones** where the stakes rise and a
bomb slice can wipe the run — unless they cash out first, or pay to revive.

- **Engine:** Unity **2021.3.30f1** (LTS), Built-in Render Pipeline
- **Target:** Android, landscape, notch/dynamic-island aware
- **Third-party runtime:** DOTween (animation), TextMesh Pro (text)
- **Repository note:** the `Vertigo Games Game Developer Demo` brief PDF is intentionally git-ignored.
  `Assets/_Project/Audio` holds only the clips the game uses, picked from a larger audio pack; the
  full pack is not in the repo.

---

## 1. Project Overview

Each run starts on **Zone 1** and moves one zone at a time:

| Concept | Rule |
| --- | --- |
| **Spin** | Weighted random slice on the current zone's wheel. Grants a reward, or detonates a bomb. |
| **Bank** | Rewards accumulate per-run, stacked by id. This is what a bomb takes. |
| **Zone type** | `Normal` carries exactly one bomb; `Safe` (every 5th zone) and `Super` (every 30th) are bomb-free and pay more. `ZoneWheelFactory` enforces both counts when it builds a wheel, and a bomb needs a weight of at least 1. |
| **Cash out** | Leave only on a `Safe` or `Super` zone, while the wheel is idle and the bank is non-empty. Every banked currency (gold, cash, …) converts to the persistent wallet — the only way a balance ever grows. |
| **Bomb** | The whole bank is lost and the run ends — but the lost haul is snapshotted first (see §4). |
| **Revive** | Gold revive: uncapped, price doubles per use in a run. Ad revive: one free per run. Either restores the snapshotted haul. |

---

## 2. Architecture

### Layered assemblies

```
Vertigo.Wheel.Core      pure C#, no UnityEngine UI — rules, state machine, run model
Vertigo.Wheel.Data      ScriptableObject configs + save/audio services (references Core)
Vertigo.Wheel.UI        MonoBehaviour Views + UI helpers (references Core and Data)
Vertigo.Wheel.Gameplay  Presenters + the composition root (references all of the above)
Vertigo.Wheel.Editor    config + hygiene validators, build pipeline, sprite import rules
Vertigo.Wheel.Tests.EditMode / .PlayMode
```

The dependency arrows only ever point inward. `Core` compiles and is fully testable without a
scene, a camera, or the UI assembly.

### MVP with a hard Core/Presentation seam

- **Model** — `RunModel`, `RewardBank`, `Wallet`, `ZoneClassifier`, `ContinueService`,
  `CashOutPolicy`. All pure logic.
- **View** — `WheelViewMono`, `BankViewMono`, `BombPopupViewMono`, `CollectPopupViewMono`, … Each derives from
  `UIViewBaseMono` and wires its own child references **by GameObject name** (`Bind(ref field,
  "ui_node_name")`) — no dragging references in the Inspector, no `FindObjectOfType`.
- **Presenter** — `ScreenPresentation` composed of per-region presenters (`WheelPresenter`,
  `BankPresenter`, `PopupPresenter`, `AudioPresenter`, …). Presenters own all
  Unity/DOTween/animation concerns.

The seam between Core and Presentation is **`IWheelPresentation`**, a composition of five role
interfaces (`IZonePresentation`, `IInputPresentation`, `ISpinPresentation`, `IGameOverPresentation`,
`ICashOutPresentation`); each state depends only on the role it uses. None contain Unity types.
Animating calls take an `Action onComplete` callback, so the state machine only
advances when the presentation reports it is done. This is what makes the whole game loop testable
headlessly (see §5).

### Finite state machine

`GameStateMachine` drains a queue of state changes and exposes a fixed input surface
(`RequestSpin`, `RequestExit`, `Confirm`, `Cancel`, `RequestContinue`, `RequestAdContinue`,
`RequestRestart`). `IGameState` is only `Enter`/`Exit`; each input is a one-method interface
(`ISpinInputHandler`, `IExitInputHandler`, …) that a state implements for exactly the inputs it accepts.
The machine forwards an input only to a state that declares the matching handler and ignores it
otherwise, so a state's declaration lists what it reacts to and double-tapping spin mid-spin cannot
queue a second spin.

```
BootState → ZoneSetupState → IdleState ⇄ SpinningState → ResolvingState → RewardGrantedState
                                │                              │
                                │                              └→ BombHitState → GameOverState
                                └→ CashOutState (confirm/cancel)
```

Every run starts in `BootState`: the first launch, a give-up on the game-over screen and a confirmed
cash-out all re-enter it, and it resets the run before handing over to zone setup.

`IdleState` is the only state that declares the spin and exit handlers, and it asks `RunModel` /
`CashOutPolicy` whether leaving is legal rather than deciding for itself — so the button
enabled-state and the guard clause are the same rule read twice and can never drift. Whether the
wheel is idle is the state machine's own rule; there is no second "phase" copy of it in the run model.

### Dependency injection — a composition root, not a container

There is **no DI framework** (no Zenject / VContainer). `GameInstallerMono` is the single composition
root: in `Awake()` it loads the configs through Addressables, constructs every Core service and
Presenter with explicit `new`, wires them into one `GameStateMachine`, and starts the flow. Its
View fields are scene references serialized in `Main.unity`. Constructor injection everywhere
means the dependencies of any class are visible in its signature and trivially faked in a test.

---

## 3. Data-Driven Design

Everything tunable is a ScriptableObject under `Assets/_Project/Configs`, edited in the Inspector
and checked by `Tools ▸ Vertigo ▸ Validate Game Configs`:

| Asset | Drives |
| --- | --- |
| `ZoneProgressionConfig` | Safe/Super intervals, one wheel per special zone type, per-band overrides |
| `ZoneWheelConfig` / `WheelSpinConfig` | Slice layout, weights, bomb placement per zone type |
| `WheelThemeConfig` (Bronze / Silver / Golden) | Wheel sprite set, accent + glow colours, tick SFX, and the zone-strip number colour / weight for zones that use the theme |
| `RewardCategoryDefinition` | What a kind of reward may do: stackable, per-drop ceiling, is-wallet-currency. A new category is a new asset, not a code change |
| `RewardCatalog` + `RewardDefinition` | The only bridge from a Core `RewardId` to a sprite; also names the gold (revive) currency. Every reward whose category is flagged as a wallet currency is a wallet currency: cash-out banks it, the popups list it, Reset Save clears it |
| Scaling strategy (`LinearScalingSO`) | How a reward's base amount grows with zone depth (unit-tested) |
| `ContinueConfig` | Revive base cost, cost-per-zone, ad-revive cap |
| `AudioLibrary` | Named SFX slots (`_buttonClick`, `_rewardChime`, `_bombExplosion`, …) |

### Adding a wallet currency

A currency is data, not code. Create a `RewardDefinition` whose category is a wallet currency
(`Category_Currency`, plus an icon and a balance colour for the popups), add it to the `RewardCatalog`
list, and place it on a wheel slice.
Cash-out banks it, the wallet persists it, `Reset Save` clears it and both popups list it in catalog
order. The gold currency stays a named role only because revives are paid in it.

`ZoneClassifier` reads the intervals and classifies any zone by pure arithmetic
(`zone % superInterval`, then `zone % safeInterval`), so zone 1 is an ordinary bronze zone with a bomb.
The Super interval is tested first, which is why zone 30 is Super, not Safe.

---

## 4. Safety & State Management

**The bank is a per-run, never-persisted structure. The wallet is persistent. A bomb must never
touch the wallet** — otherwise it could lock the player out of the very revive meant to answer it.

### Deferred detonation / `LostHaul` snapshot

```
RunModel.Detonate():
    _lostHaul = new List<BankEntry>(Bank.Entries)   // snapshot BEFORE clearing
    Bank.Clear()
    Phase = GameOver

RunModel.ApplyGoldRevive() / ApplyAdRevive():
    revivesUsed++
    RestoreLostHaul()        // pours _lostHaul back into the bank, then nulls it
    Phase = Idle
```

`_lostHaul` is non-null only while a bomb is waiting on a revive-or-restart decision. A restart or
a fresh run discards it; `ResetRun()` zeroes both revive counters. The bank's own invariants
(stack-by-id, first-acquisition order) are untouched — a revive is an ordinary
sequence of `Bank.Add` calls, not a special path.

### Deferred cash-out commit

`CashOutState.Enter` shows the summary and commits **nothing**. `OnCancelled` drops the player
straight back onto the wheel with the haul intact. Only `OnConfirmed` runs `RunModel.CashOut()`
(the one place banked currencies enter the wallet), plays the claim celebration, and — once it
finishes — boots a fresh run. The summary lists `RunModel.WalletGains`, the same rows `CashOut`
credits, so weapons, chests and points (which belong to the run and end with it) are never shown as if
they were kept.

### `CashOutPolicy`

A pure static function: _"leave when the zone is Safe or Super and the bank has something."_ (That the
wheel is idle is the state machine's rule: only `IdleState` handles the exit input.) The EXIT button's interactable state is a reflection of this, never a re-implementation.

---

## 5. Testing Suite

```
Assets/_Project/Tests/EditMode    218 tests          pure logic + full flow
Assets/_Project/Tests/PlayMode      1 test            composition-root smoke test
```

### EditMode — logic and the whole loop, headless

- **Unit** — `ZoneClassifierTests`, `CashOutPolicyTests`, `ContinueServiceTests`,
  `RewardBankTests`, `RewardScalingTests`, `WeightedSliceResolverTests`, `WheelModelTests`,
  `RunModelTests`, `ZoneWheelFactoryTests`.
- **Flow** — `GameStateMachineTests` and `FullRunTests` drive the **real** `GameStateMachine`
  end-to-end.

### The `InstantPresentation` test double

`GameStateMachineTests` runs the shipping state machine against `InstantPresentation`, an
`IWheelPresentation` whose animating calls invoke their `onComplete` **synchronously**. The state
machine advances immediately, so a 60-zone run resolves in microseconds with zero timing hangs and
no DOTween. Test doubles also cover the seams below Core:

| Double | Replaces |
| --- | --- |
| `InstantPresentation` | `ScreenPresentation` (the whole Unity presentation layer) |
| `BlockingPresentation` | overrides only `PlaySpin` to hold the machine mid-spin |
| `FixedSliceResolver` | the weighted RNG — lands a scripted slice |
| `SystemRandomProvider` | `UnityRandomProvider` — seedable, so a run reproduces exactly |
| `StubBlueprintProvider` | authored wheel configs — deterministic 7-reward + 1-bomb layouts |
| `InMemorySaveService` | `PlayerPrefsSaveService` |

### PlayMode — one test on purpose

`BootstrapTests.Scene_Loads_AndReachesIdle_WithinTwoSeconds` loads `Main.unity`, lets
`GameInstallerMono` wire everything for real, and asserts the flow reaches `IdleState`. Everything
else is proven faster in EditMode; this proves the composition root itself.

---

## 6. Juice & Polish

- **DOTween pipelines** — wheel spin easing, `DOPunchScale` on buttons and the collect card, popup open/close
  scale+fade (`PopupViewBaseMono`), reward icons flying from the wheel slot into their bank cell
  (`BankPresenter.FlyIn`: a new cell appears exactly when its icon lands, every icon has its own ghost), the red bomb-alert vignette yoyo.
- **Dynamic counters** — the collect popup's currency rows count up smoothly (`CurrencyRowViewMono.CountTo`,
  `DOVirtual.Int`, `Ease.OutCubic`) instead of snapping; the claim flow animates each one to the
  wallet's new post-claim total.
- **Milestone UI** — Safe / Super badges on the zone bar open a preview modal showing that band's
  wheel tier and reward slots.
- **Responsive layout** — `SafeAreaFitterMono` on the gameplay and popup layers keeps content clear of
  the landscape notch / dynamic island (backdrops bleed back out to keep the dim full-screen);
  `GridEdgePadding` keeps the bank grid's left/right margins mirrored while it still fills
  left-to-right, at any aspect ratio under the `Expand` canvas scaler.
- **Audio** — `AudioPresenter` (null-safe) fires library SFX on click, reward, bomb, popup, claim
  and defeat; per-theme wheel tick.
- **In-editor debug bar** — collapsible cheat panel (jump to zone, force the defeat screen, +gold,
  +40 random items) compiled only into `UNITY_EDITOR` / development builds.

---

## 7. Build & Verification Guide

All tooling lives under **`Tools ▸ Vertigo`** in the editor, and every entry has a static method
for `-batchmode -executeMethod`.

### Run the tests

```bash
UNITY=/Applications/Unity/Hub/Editor/2021.3.30f1/Unity.app/Contents/MacOS/Unity

"$UNITY" -runTests -batchmode -nographics -projectPath . \
  -testPlatform EditMode -testResults TestResults-EditMode.xml -logFile -

"$UNITY" -runTests -batchmode -nographics -projectPath . \
  -testPlatform PlayMode -testResults TestResults-PlayMode.xml -logFile -
```

Or in-editor: **Window ▸ General ▸ Test Runner**.

### Validate before committing

```
Tools ▸ Vertigo ▸ Validate UI Hygiene     raycast targets, the Maskable/RectMask2D trap, 9-slice sprites
Tools ▸ Vertigo ▸ Validate Game Configs    config asset integrity
Tools ▸ Vertigo ▸ Reset Save               clear the persistent wallet (every currency)
```

### Build the APK

```
Tools ▸ Vertigo ▸ Build Android APK
# batch: -executeMethod Vertigo.Wheel.Editor.BuildPipelineRunner.BuildAndroid -quit
```

`BuildPipelineRunner` passes `Main.unity` to `BuildPlayer` explicitly, so a batch build does not depend on
the Build Settings list. The Addressables
content (the six `Settings/` configs) is built as part of the player build — the project pins
"Build Addressables on Player Build", so the result doesn't depend on a machine-local preference.

---

## 8. Repository Layout

```
Assets/
  _Project/
    Scenes/Main.unity
    Scripts/
      Core/        Run/ States/ States/Flow/ Zones/ Rewards/ Spin/
      Data/        Configs/ Services/
      UI/          Views/ Views/Popups/
      Gameplay/    GameInstallerMono.cs  Presenters/
      Editor/      BuildPipelineRunner, GameConfigValidator, UIHygieneValidator, ResetSaveMenuItem,
                   WheelSpriteImportPostprocessor, ZoneWheelConfigEditor
    Tests/         EditMode/ (+ Doubles/)  PlayMode/
    Configs/       ScriptableObject instances (Settings/ Themes/ Rewards/ …), loaded via Addressables
    Art/Sprites/
```
