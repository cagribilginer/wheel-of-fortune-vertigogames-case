# Wheel of Fortune — Zone Progression Roulette

A landscape mobile prize-wheel game built as a technical evaluation piece. The player spins a
themed wheel to bank rewards, pushing deeper through numbered **zones** where the stakes rise and a
bomb slice can wipe the run — unless they cash out first, or pay to revive.

- **Engine:** Unity **2021.3.30f1** (LTS), Built-in Render Pipeline
- **Target:** Android, landscape, notch/dynamic-island aware
- **Third-party runtime:** DOTween (animation), TextMesh Pro (text)
- **License note:** the `Vertigo Games Game Developer Demo` brief PDF and the audio pack license
  are intentionally git-ignored.

---

## 1. Project Overview

Each run starts on **Zone 1** (always safe) and moves one zone at a time:

| Concept | Rule |
| --- | --- |
| **Spin** | Weighted random slice on the current zone's wheel. Grants a reward, or detonates a bomb. |
| **Bank** | Rewards accumulate per-run, stacked by id. This is what a bomb takes. |
| **Zone type** | `Normal` carries a bomb; `Safe` (zone 1, then every _N_) and `Super` (every _M_) are bomb-free and pay more. |
| **Cash out** | Leave at any idle zone with a non-empty bank. Banked gold converts to the persistent wallet — the only way the wallet ever grows. |
| **Bomb** | The whole bank is lost and the run ends — but the lost haul is snapshotted first (see §4). |
| **Revive** | Gold revive: uncapped, price doubles per use in a run. Ad revive: one free per run. Either restores the snapshotted haul. |

---

## 2. Architecture

### Layered assemblies

```
Vertigo.Wheel.Core      pure C#, no UnityEngine UI — rules, state machine, run model
Vertigo.Wheel.Data      ScriptableObject configs + save/audio services (references Core)
Vertigo.Wheel.UI        MonoBehaviour Views + UI helpers (references Core)
Vertigo.Wheel.Gameplay  Presenters + the composition root (references all of the above)
Vertigo.Wheel.Editor    scene builder, config generator, hygiene validator, build pipeline
Vertigo.Wheel.Tests.EditMode / .PlayMode
```

The dependency arrows only ever point inward. `Core` compiles and is fully testable without a
scene, a camera, or the UI assembly.

### MVP with a hard Core/Presentation seam

- **Model** — `RunModel`, `RewardBank`, `GoldWallet`, `ZoneClassifier`, `ContinueService`,
  `CashOutPolicy`. All pure logic.
- **View** — `HeaderView`, `WheelView`, `BankView`, `BombPopupView`, … Each derives from
  `UIViewBase` and wires its own child references **by GameObject name** (`Bind(ref field,
  "ui_node_name")`) — no dragging references in the Inspector, no `FindObjectOfType`.
- **Presenter** — `ScreenPresentation` composed of per-region presenters (`WheelPresenter`,
  `BankPresenter`, `PopupPresenter`, `HeaderPresenter`, `AudioPresenter`, …). Presenters own all
  Unity/DOTween/animation concerns.

The seam between Core and Presentation is the **`IWheelPresentation`** interface. It contains no
Unity types. Animating calls take an `Action onComplete` callback, so the state machine only
advances when the presentation reports it is done. This is what makes the whole game loop testable
headlessly (see §5).

### Finite state machine

`GameStateMachine` drains a queue of state changes and forwards a fixed input surface
(`RequestSpin`, `RequestLeave`, `RequestGiveUp`, `Confirm`, `Cancel`, `RequestContinue`,
`RequestAdContinue`, `RequestRestart`) to the current state, which ignores what it does not accept.

```
BootState → ZoneSetupState → IdleState ⇄ SpinningState → ResolvingState → RewardGrantedState
                                │                              │
                                │                              └→ BombHitState → GameOverState
                                ├→ CashOutState (confirm/cancel)
                                └→ GiveUpConfirmState (modelled, not wired to a button)
```

`IdleState` is the only state that accepts player input, and it asks `RunModel` /
`CashOutPolicy` whether each action is legal rather than deciding for itself — so the button
enabled-states and the guard clauses are the same rule read twice and can never drift.

### Dependency injection — a composition root, not a container

There is **no DI framework** (no Zenject / VContainer). `GameInstaller` is the single composition
root: in `Awake()` it loads the Resources-backed configs, constructs every Core service and
Presenter with explicit `new`, wires them into one `GameStateMachine`, and starts the flow. Its
serialized View fields are populated once by `MainSceneBuilder` at scene-build time — the same
"never wire by hand" rule the Views follow. Constructor injection everywhere means the
dependencies of any class are visible in its signature and trivially faked in a test.

---

## 3. Data-Driven Design

Everything tunable is a ScriptableObject under `Assets/Resources/Configs`, generated and validated
by editor tooling (`Tools ▸ Vertigo ▸ Generate Game Configs` / `Validate Game Configs`):

| Asset | Drives |
| --- | --- |
| `ZoneProgressionConfig` | Safe/Super intervals, per-band wheel tier and overrides |
| `ZoneWheelConfig` / `WheelSpinConfig` | Slice layout, weights, bomb placement per zone type |
| `WheelThemeConfig` (Bronze / Silver / Golden) | Wheel sprite set, accent + glow colours, tick SFX |
| `RewardCatalog` + `RewardDefinition` | The only bridge from a Core `RewardId` to a sprite / display name / cash-out worth |
| Scaling strategies (`LinearScalingSO`, `CurveScalingSO`, `StepScalingSO`) | How a reward's base amount grows with zone depth (swappable, each unit-tested) |
| `ContinueConfig` | Revive base cost, cost-per-zone, ad-revive cap |
| `AudioLibrary` | Named SFX slots (`_buttonClick`, `_rewardChime`, `_bombExplosion`, …) |

`ZoneClassifier` reads the intervals and classifies any zone by pure arithmetic
(`zone % superInterval`, `zone % safeInterval`), with zone 1 special-cased to `Safe` so a run can
never end on the first spin.

`AudioAutoWirer` (editor) decodes each clip in a dropped-in SFX pack, reduces it to a handful of
acoustic features (length, crest factor, attack, low-frequency ratio, …) and scores every clip
against a target profile per `AudioLibrary` slot — so a new pack wires itself with no Inspector
work.

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
(stack-by-id, first-acquisition order, `Changed` event) are untouched — a revive is an ordinary
sequence of `Bank.Add` calls, not a special path.

### Deferred cash-out commit

`CashOutState.Enter` shows the summary and commits **nothing**. `OnCancelled` drops the player
straight back onto the wheel with the haul intact. Only `OnConfirmed` runs `RunModel.CashOut()`
(the one place banked gold enters the wallet), plays the claim celebration, and — once it
finishes — resets the run.

### `CashOutPolicy`

A pure static function: _"leave when the wheel is idle and the bank has something."_ The EXIT
button's interactable state is a reflection of this, never a re-implementation.

---

## 5. Testing Suite

```
Assets/_Project/Tests/EditMode    ~168 tests, ~0.2s   pure logic + full flow
Assets/_Project/Tests/PlayMode      1 test,   ~1.1s   composition-root smoke test
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
| `ScriptedRandomProvider` | `UnityRandomProvider` |
| `StubBlueprintProvider` | authored wheel configs — deterministic 7-reward + 1-bomb layouts |
| `InMemorySaveService` | `PlayerPrefsSaveService` |

### PlayMode — one test on purpose

`BootstrapTests.Scene_Loads_AndReachesIdle_WithinTwoSeconds` loads `Main.unity`, lets
`GameInstaller` wire everything for real, and asserts the flow reaches `IdleState`. Everything
else is proven faster in EditMode; this proves the composition root itself.

---

## 6. Juice & Polish

- **DOTween pipelines** — wheel spin easing, chest `DOPunchScale` on claim, popup open/close
  scale+fade (`PopupViewBase`), reward tiles flying from the wheel slot into their bank cell
  (`BankPresenter.FlyIn`), the red bomb-alert vignette yoyo.
- **Dynamic counters** — `HeaderView` counts the gold value up smoothly (`DOVirtual.Int`,
  `Ease.OutCubic`) instead of snapping; the claim flow animates the wallet to its new total.
- **Milestone UI** — Safe / Super badges on the zone bar open a preview modal showing that band's
  wheel tier and reward slots.
- **Responsive layout** — `SafeAreaFitter` on the gameplay and popup layers keeps content clear of
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

### Rebuild the scene

`Main.unity` is **generated**, not hand-edited — this keeps the layout reproducible.

```
Tools ▸ Vertigo ▸ Build Main Scene UI
# batch: -executeMethod Vertigo.Wheel.Editor.MainSceneBuilder.Build
```

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
Tools ▸ Vertigo ▸ Generate Game Configs    regenerate the ScriptableObject assets
```

### Build the APK

```
Tools ▸ Vertigo ▸ Build Android APK
# batch: -executeMethod Vertigo.Wheel.Editor.BuildPipelineRunner.BuildAndroid -quit
```

`Main.unity` is passed to `BuildPlayer` directly rather than sitting in Build Settings.

---

## 8. Repository Layout

```
Assets/
  _Project/
    Scenes/Main.unity            generated by MainSceneBuilder
    Scripts/
      Core/        Run/ States/ States/Flow/ Zones/ Rewards/ Spin/
      Data/        Configs/ Services/
      UI/          Views/ Views/Popups/
      Gameplay/    GameInstaller.cs  Presenters/
      Editor/      MainSceneBuilder, GameConfigGenerator, UIHygieneValidator,
                   AudioAutoWirer, BuildPipelineRunner, EditorSpriteUtility
    Tests/         EditMode/ (+ Doubles/)  PlayMode/
    Art/Sprites/
  Resources/Configs/             ScriptableObject instances (Settings/ Themes/ Rewards/ …)
```
