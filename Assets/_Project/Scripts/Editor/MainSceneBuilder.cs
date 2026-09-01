using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vertigo.Wheel.Gameplay;
using Vertigo.Wheel.UI.Views;
using Vertigo.Wheel.UI.Views.Popups;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// Builds the entire landscape UI hierarchy from the architecture plan's section 4.3 into a fresh
    /// <c>Main.unity</c>, with real sprites, anchors and pivots, and every runtime-changing node ending
    /// <c>_value</c>.
    /// <para>
    /// Building this by hand in the editor is exactly the kind of work that silently drifts from the spec
    /// one dragged anchor at a time. Generating it makes the layout reproducible: re-running after an art
    /// or spec change rebuilds the tree from the same source of truth instead of accumulating manual edits.
    /// </para>
    /// <para>
    /// The saved scene itself is still static: the wheel's 8 slots sit stacked at the rotor's centre with no
    /// icons, and the zone map strip is empty. <c>GameInstaller</c> is built and wired into it (see
    /// <see cref="BuildGameInstaller"/>), but as a plain <c>MonoBehaviour</c> with no
    /// <c>[ExecuteAlways]</c>, its <c>Awake()</c> — which lays out the slots, starts the state machine, and
    /// populates everything — only runs once Unity enters Play Mode. An empty-looking wheel or zone map in
    /// the Scene view or right after a fresh build is expected; it is not evidence of a bug.
    /// </para>
    /// </summary>
    public static class MainSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";

        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);

        // 9 tiles (76px) + 8 gaps (12px) + layout padding (20px), plus a little breathing room so a
        // fractional 10th tile peeks in at each edge — reads as "scrolling", not "cut off".
        private const float ZoneMapViewportWidth = 860f;

        [MenuItem("Tools/Vertigo/Build Main Scene UI")]
        public static void Build()
        {
            ZoneMapTileView tilePrefab = BuildZoneMapTilePrefab();
            WheelSlotView slotPrefab = BuildWheelSlotPrefab();
            BankEntryView bankEntryPrefab = BuildBankEntryPrefab();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildMainCamera();
            BuildEventSystem();

            RectTransform canvasRoot = BuildCanvasRoot();
            BuildBackground(canvasRoot);

            RectTransform safeArea = NewNode("ui_panel_safearea", canvasRoot);
            Stretch(safeArea, 0, 0, 0, 0);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            HeaderView headerView = BuildHeader(safeArea);
            ZoneMapView zoneMapView = BuildZoneMap(safeArea, tilePrefab);

            RectTransform playArea = NewNode("ui_panel_play", safeArea);
            Stretch(playArea, 0f, 0f, 0f, 220f);

            WheelView wheelView = BuildWheel(playArea, slotPrefab);
            RectTransform sidePanel = NewNode("ui_panel_side", playArea);
            Stretch(sidePanel, 820f, 20f, 40f, 20f);
            BankView bankView = BuildBank(sidePanel, bankEntryPrefab);
            ActionBarView actionBarView = BuildActions(sidePanel);

            (BombPopupView bombView, CollectPopupView collectView, GiveUpConfirmPopupView giveUpView,
                MilestonePreviewPopupView milestoneView) = BuildPopupLayer(canvasRoot, bankEntryPrefab);
            VfxView vfxView = BuildVfxLayer(canvasRoot);
            DebugOverlayView debugView = BuildDebugOverlay(canvasRoot);

            BuildGameInstaller(canvasRoot, headerView, wheelView, zoneMapView, bankView, actionBarView,
                bombView, collectView, giveUpView, milestoneView, vfxView, debugView, tilePrefab, bankEntryPrefab);

            EnsureFolder(Path.GetDirectoryName(ScenePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Vertigo] Built {ScenePath} with GameInstaller wired and the flow live. " +
                       "Run Tools/Vertigo/Validate UI Hygiene next, then check the three landscape Game view presets.");
        }

        // ------------------------------------------------------------------ composition root

        private static void BuildGameInstaller(
            RectTransform canvasRoot, HeaderView header, WheelView wheel, ZoneMapView zoneMap, BankView bank,
            ActionBarView actionBar, BombPopupView bombPopup, CollectPopupView collectPopup,
            GiveUpConfirmPopupView giveUpPopup, MilestonePreviewPopupView milestonePopup, VfxView vfx,
            DebugOverlayView debugOverlay, ZoneMapTileView tilePrefab, BankEntryView bankEntryPrefab)
        {
            var installer = new GameObject("GameInstaller").AddComponent<GameInstaller>();
            Sprite bombIcon = EditorSpriteUtility.FindSprite("ui_card_icon_death");

            installer.Configure(header, wheel, zoneMap, bank, actionBar, bombPopup, collectPopup, giveUpPopup,
                milestonePopup, vfx, debugOverlay, tilePrefab, bankEntryPrefab, canvasRoot, bombIcon);
        }

        // ------------------------------------------------------------------ pooled prefabs

        private static ZoneMapTileView BuildZoneMapTilePrefab()
        {
            var root = new GameObject("ui_item_zonemap_tile", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(60f, 76f);
            var layoutElement = root.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 60f;
            layoutElement.preferredHeight = 76f;

            // The reference strip has no per-tile card: tiles are bare numbers on the shared dark bar. The
            // only decoration a tile ever carries is this raised white marker, shown on exactly one tile —
            // the current zone. The sprite is 9-sliced horizontally only (border 4/0/4/0), so its top notch
            // keeps its shape at any width; the rect height is pinned to the art's native 64px for the same
            // reason. The presenter toggles Image.enabled, never the GameObject, so a pooled tile that was
            // once "current" cleanly goes back to plain.
            Image marker = AddImage(NewNode("ui_image_zonemap_tile_marker_value", rt), "ui_card_panel_zone_current_white");
            marker.type = Image.Type.Sliced;
            marker.maskable = true;
            marker.enabled = false;
            FixedCentered((RectTransform)marker.transform, new Vector2(0f, 2f), new Vector2(56f, 64f));

            TextMeshProUGUI number = AddText(NewNode("ui_text_zonemap_tile_number_value", rt), "7", 30f);
            number.maskable = true; // pooled into ui_content_zonemap, which sits inside a RectMask2D
            FixedCentered((RectTransform)number.transform, Vector2.zero, new Vector2(56, 48));

            var view = root.AddComponent<ZoneMapTileView>();
            view.RebindReferences();
            return SaveAsPrefab(root, view);
        }

        private static WheelSlotView BuildWheelSlotPrefab()
        {
            var root = new GameObject("ui_item_wheel_slot", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            // Anchored dead-centre so WheelPresenter.LayoutSlots' anchoredPosition places the slot's centre
            // exactly on the polar ring. 90x90 with a 55x55 icon sits strictly inside one bronze cylinder
            // hole: it clears the outer rim and the neighbouring numbers, where the old 140x140 slot's
            // rectangular grey-backed reward icons bled over the rim. preserveAspect stops the non-square
            // icons distorting inside that box.
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(90f, 90f);

            Image icon = AddImage(NewNode("ui_image_slot_icon_value", rt), null);
            icon.preserveAspect = true;
            FixedCentered((RectTransform)icon.transform, Vector2.zero, new Vector2(55f, 55f));

            TextMeshProUGUI amount = AddText(NewNode("ui_text_slot_amount_value", rt), "x25", 17f);
            FixedCentered((RectTransform)amount.transform, new Vector2(0f, -30f), new Vector2(84f, 24f));

            var view = root.AddComponent<WheelSlotView>();
            view.RebindReferences();
            return SaveAsPrefab(root, view);
        }

        private static BankEntryView BuildBankEntryPrefab()
        {
            var root = new GameObject("ui_item_bank_entry", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(140f, 160f);

            Image frame = AddImage(NewNode("ui_image_bank_entry_frame", rt), "ui_card_frame_4px_zone");
            frame.type = Image.Type.Sliced;
            frame.maskable = true;
            Stretch((RectTransform)frame.transform, 0, 0, 0, 0);

            Image icon = AddImage(NewNode("ui_image_bank_entry_icon_value", rt), null);
            icon.preserveAspect = true;
            icon.maskable = true;
            FixedCentered((RectTransform)icon.transform, new Vector2(0, 14), new Vector2(88, 88));

            TextMeshProUGUI amount = AddText(NewNode("ui_text_bank_entry_amount_value", rt), "x42", 24f);
            amount.maskable = true; // pooled into a masked scroll view (bank grid or collect popup list)
            // TMP's default overflow mode renders past the text box instead of clipping to it — invisible at
            // "x42" but a stacked reward on an endless, ever-scaling economy can reach 4+ digits, and that
            // would spill past the card's edge. Auto-sizing shrinks the font to fit instead.
            amount.enableAutoSizing = true;
            amount.fontSizeMin = 14f;
            amount.fontSizeMax = 24f;
            amount.overflowMode = TextOverflowModes.Truncate;
            FixedCentered((RectTransform)amount.transform, new Vector2(0, -54), new Vector2(120, 32));

            var view = root.AddComponent<BankEntryView>();
            view.RebindReferences();
            return SaveAsPrefab(root, view);
        }

        private static T SaveAsPrefab<T>(GameObject root, T view) where T : Component
        {
            EnsureFolder(PrefabFolder);
            string path = $"{PrefabFolder}/{root.name}.prefab";

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            // The reference SaveAsPrefabAsset hands back doesn't survive the NewScene(...) teardown a few
            // lines later in Build() — it reads as valid here but comes back destroyed by the time BuildWheel
            // tries to instantiate from it. Forcing a full import + a fresh AssetDatabase load gives every
            // caller a genuinely disk-backed reference instead.
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<T>();
        }

        // ------------------------------------------------------------------ canvas root

        private static RectTransform BuildCanvasRoot()
        {
            var go = new GameObject("ui_canvas_main", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = 100f;

            return (RectTransform)go.transform;
        }

        private static void BuildEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        /// <summary>
        /// The Screen Space - Overlay canvas renders without any camera, but Unity still needs one tagged
        /// MainCamera in the scene — otherwise the Game view shows "No cameras rendering" and, on device,
        /// nothing clears the frame before the UI draws.
        /// </summary>
        private static void BuildMainCamera()
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";

            var camera = go.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
            camera.cullingMask = ~0;
        }

        private static void BuildBackground(RectTransform canvasRoot)
        {
            Image bg = AddImage(NewNode("ui_image_background", canvasRoot), null);
            bg.color = new Color(0.08f, 0.08f, 0.10f, 1f);
            Stretch((RectTransform)bg.transform, 0, 0, 0, 0);
        }

        // ------------------------------------------------------------------ header

        private static HeaderView BuildHeader(RectTransform safeArea)
        {
            RectTransform header = NewNode("ui_panel_header", safeArea);
            TopStrip(header, 100f, 0f);

            Image bg = AddImage(NewNode("ui_image_header_bg", header), "ui_card_panel_zone_bg");
            bg.type = Image.Type.Sliced;
            Stretch((RectTransform)bg.transform, 0, 0, 0, 0);

            Image goldIcon = AddImage(NewNode("ui_image_header_gold_icon", header), "UI_icon_gold");
            goldIcon.preserveAspect = true;
            // -180 left only ~112px of clearance before the icon's box collides with a right-aligned value
            // — comfortable for "1,250" but not for a run's gold climbing into five or six digits.
            RightMiddle((RectTransform)goldIcon.transform, -220f, new Vector2(56, 56));

            TextMeshProUGUI gold = AddText(NewNode("ui_text_header_gold_value", header), "1,250", 40f);
            gold.alignment = TextAlignmentOptions.MidlineRight;
            RightMiddle((RectTransform)gold.transform, -40f, new Vector2(300, 80));

            var headerView = header.gameObject.AddComponent<HeaderView>();
            headerView.RebindReferences();
            return headerView;
        }

        // ------------------------------------------------------------------ zone map

        private static ZoneMapView BuildZoneMap(RectTransform safeArea, ZoneMapTileView tilePrefab)
        {
            RectTransform zoneMap = NewNode("ui_panel_zonemap", safeArea);
            TopStrip(zoneMap, 120f, 100f);

            // One solid, rounded, dark container strip — no gradient backdrop bars, no outer corner
            // brackets. Fixed width, centred, vertically inset a little inside the 120px panel so the raised
            // current-zone marker still reads as a badge sitting on the bar rather than filling it. The bar
            // is matched to the scroll window's width (not the full panel) so it hugs the actual track and
            // never reaches into the milestone badges: at the 1920 reference width (CanvasScaler Expand only
            // grows it) there is a clear ~320px gap between the centred bar and the badges.
            RectTransform barRect = NewNode("ui_image_zonemap_bg", zoneMap);
            barRect.anchorMin = new Vector2(0.5f, 0f);
            barRect.anchorMax = new Vector2(0.5f, 1f);
            barRect.pivot = new Vector2(0.5f, 0.5f);
            barRect.sizeDelta = new Vector2(ZoneMapViewportWidth, -16f);
            barRect.anchoredPosition = Vector2.zero;
            Image bar = barRect.gameObject.AddComponent<Image>();
            // Opaque panel sprite for the fill (ui_card_frame_12px_neutral has a transparent centre and only
            // ever draws its edge — see the bank panel note), tinted almost to black.
            bar.sprite = EditorSpriteUtility.FindSprite("ui_card_panel_zone_bg");
            bar.type = Image.Type.Sliced;
            bar.color = new Color(0.07f, 0.075f, 0.09f, 1f);
            bar.raycastTarget = false;
            bar.maskable = false;

            Image barStroke = AddImage(NewNode("ui_image_zonemap_stroke", zoneMap), "ui_card_frame_12px_neutral");
            barStroke.type = Image.Type.Sliced;
            barStroke.color = new Color(0.34f, 0.37f, 0.43f, 0.85f);
            RectTransform barStrokeRect = (RectTransform)barStroke.transform;
            barStrokeRect.anchorMin = new Vector2(0.5f, 0f);
            barStrokeRect.anchorMax = new Vector2(0.5f, 1f);
            barStrokeRect.pivot = new Vector2(0.5f, 0.5f);
            barStrokeRect.sizeDelta = new Vector2(ZoneMapViewportWidth, -16f);
            barStrokeRect.anchoredPosition = Vector2.zero;

            RectTransform scrollRect = NewNode("ui_scroll_zonemap", zoneMap);
            scrollRect.anchorMin = new Vector2(0.5f, 0f);
            scrollRect.anchorMax = new Vector2(0.5f, 1f);
            scrollRect.pivot = new Vector2(0.5f, 0.5f);
            scrollRect.sizeDelta = new Vector2(ZoneMapViewportWidth, 0f);
            scrollRect.anchoredPosition = Vector2.zero;
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;

            // No Image on the viewport, deliberately: the strip is presenter-driven and never dragged, so
            // there is nothing here that should ever be a raycast target.
            RectTransform viewport = NewNode("ui_viewport_zonemap", scrollRect);
            Stretch(viewport, 0, 0, 0, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            RectTransform content = NewNode("ui_content_zonemap", viewport);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.padding = new RectOffset(10, 10, 8, 8);

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.content = content;

            // Near-transparent dark fill with a thin coloured stroke — the reference badges read as a sleek
            // outline, not a filled bevelled pill.
            BuildMilestoneBadge(zoneMap, "ui_card_zonemap_milestone_super", "ui_text_zonemap_milestone_super_value",
                "UI_icon_chest_gold_nolight", "SUPER ZONE 30",
                new Color(0.06f, 0.05f, 0.03f, 0.62f), new Color(0.92f, 0.74f, 0.32f, 0.9f),
                new Vector2(-14f, 30f));

            BuildMilestoneBadge(zoneMap, "ui_card_zonemap_milestone_safe", "ui_text_zonemap_milestone_safe_value",
                "UI_icon_chest_silver_nolight", "SAFE ZONE 10",
                new Color(0.04f, 0.07f, 0.04f, 0.62f), new Color(0.46f, 0.86f, 0.50f, 0.9f),
                new Vector2(-14f, -30f));

            var view = zoneMap.gameObject.AddComponent<ZoneMapView>();
            view.RebindReferences();
            return view;
        }

        // Every SUPER/SAFE milestone card shares these exactly so the two badges are typographically
        // identical — same box, same fixed font size (auto-sizing was letting the longer "SUPER ZONE 60"
        // shrink relative to "SAFE ZONE 10"), same padding, same icon slot. Compact, with a thin 4px
        // stroke rather than the chunky bevelled 12px frame.
        private static readonly Vector2 MilestoneCardSize = new Vector2(224f, 50f);
        private const float MilestoneFontSize = 16f;
        private const float MilestoneIconSize = 30f;

        /// <summary>
        /// One top-right milestone card ("SAFE ZONE 10", "SUPER ZONE 30"): an opaque, high-contrast pill
        /// with a bright stroke and its chest icon on the right. The number in <paramref name="placeholder"/>
        /// is cosmetic — <c>ZoneMapPresenter.ShowZone</c> rewrites both cards on every zone change with the
        /// next safe/super zone strictly ahead of the player, so the targets count up as the run does.
        /// </summary>
        private static void BuildMilestoneBadge(
            RectTransform parent, string cardName, string textName, string iconSpriteName, string placeholder,
            Color fillColor, Color strokeColor, Vector2 anchoredPosition)
        {
            RectTransform card = NewNode(cardName, parent);
            card.anchorMin = new Vector2(1f, 0.5f);
            card.anchorMax = new Vector2(1f, 0.5f);
            card.pivot = new Vector2(1f, 0.5f);
            card.sizeDelta = MilestoneCardSize;
            card.anchoredPosition = anchoredPosition;

            // Opaque panel sprite for the near-transparent dark fill; ui_card_frame_4px_zone on top is a
            // thin outline (4px 9-slice), for a sleek 1-2px-reading border instead of the old bevel.
            Image bg = AddImage(NewNode(cardName + "_bg", card), "ui_card_panel_zone_bg");
            bg.type = Image.Type.Sliced;
            bg.color = fillColor;
            bg.raycastTarget = true; // the badge is tappable — it opens the milestone preview
            Stretch((RectTransform)bg.transform, 0, 0, 0, 0);

            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;

            Image stroke = AddImage(NewNode(cardName + "_stroke", card), "ui_card_frame_4px_zone");
            stroke.type = Image.Type.Sliced;
            stroke.color = strokeColor;
            Stretch((RectTransform)stroke.transform, 0, 0, 0, 0);

            Image icon = AddImage(NewNode(cardName + "_icon", card), iconSpriteName);
            icon.preserveAspect = true;
            RightMiddle((RectTransform)icon.transform, -8f, new Vector2(MilestoneIconSize, MilestoneIconSize));

            TextMeshProUGUI text = AddText(NewNode(textName, card), placeholder, MilestoneFontSize);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = new Color(strokeColor.r, strokeColor.g, strokeColor.b, 1f);
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            Stretch((RectTransform)text.transform, 14f, 2f, 14f + MilestoneIconSize + 8f, 2f);
        }

        // ------------------------------------------------------------------ wheel

        private static WheelView BuildWheel(RectTransform playArea, WheelSlotView slotPrefab)
        {
            RectTransform wheelPanel = NewNode("ui_panel_wheel", playArea);
            wheelPanel.anchorMin = new Vector2(0f, 0.5f);
            wheelPanel.anchorMax = new Vector2(0f, 0.5f);
            wheelPanel.pivot = new Vector2(0.5f, 0.5f);
            wheelPanel.sizeDelta = new Vector2(720f, 720f);
            wheelPanel.anchoredPosition = new Vector2(400f, 0f);

            Image glow = AddImage(NewNode("ui_image_wheel_glow", wheelPanel), "star_glow_alpha");
            glow.preserveAspect = true;
            Stretch((RectTransform)glow.transform, 0, 0, 0, 0);

            RectTransform rotor = NewNode("ui_transform_wheel_rotor", wheelPanel);
            Stretch(rotor, 0, 0, 0, 0);
            var fitter = rotor.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;

            Image baseImage = AddImage(NewNode("ui_image_wheel_base_value", rotor), "ui_spin_bronze_base");
            baseImage.preserveAspect = true;
            Stretch((RectTransform)baseImage.transform, 0, 0, 0, 0);

            RectTransform slotsGroup = NewNode("ui_group_wheel_slots", rotor);
            FixedCentered(slotsGroup, Vector2.zero, Vector2.zero);

            for (int i = 0; i < 8; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab.gameObject);
                instance.transform.SetParent(slotsGroup, false);
                instance.name = $"ui_item_wheel_slot_{i}";
            }

            RectTransform indicator = NewNode("ui_transform_wheel_indicator", wheelPanel);
            indicator.anchorMin = new Vector2(0.5f, 1f);
            indicator.anchorMax = new Vector2(0.5f, 1f);
            indicator.pivot = new Vector2(0.5f, 0f);
            indicator.sizeDelta = new Vector2(120f, 120f);
            // The top slot's outer edge sits at R+70 = 297 from the wheel's centre (R=227, slot half-size
            // 70); pulled down from the wheelPanel's own top edge (360) so the indicator's tip lands right
            // on it instead of pointing at 63px of empty space above the wheel.
            indicator.anchoredPosition = new Vector2(0f, -60f);

            Image indicatorImage = AddImage(NewNode("ui_image_wheel_indicator_value", indicator), "ui_spin_bronze_indicator");
            indicatorImage.preserveAspect = true;
            Stretch((RectTransform)indicatorImage.transform, 0, 0, 0, 0);

            RectTransform spinButtonRect = NewNode("ui_button_wheel_spin", wheelPanel);
            FixedCentered(spinButtonRect, Vector2.zero, new Vector2(134f, 134f));
            Image spinImage = spinButtonRect.gameObject.AddComponent<Image>();
            spinImage.sprite = EditorSpriteUtility.FindSprite("ui_spin_generic_button");
            spinImage.preserveAspect = true;
            spinImage.raycastTarget = true;
            spinImage.maskable = false;
            var spinButton = spinButtonRect.gameObject.AddComponent<Button>();
            spinButton.targetGraphic = spinImage;

            RectTransform spinAnim = NewNode("ui_transform_wheel_spin_anim", spinButtonRect);
            Stretch(spinAnim, 0, 0, 0, 0);

            spinButtonRect.gameObject.AddComponent<UIButtonPunch>();

            var view = wheelPanel.gameObject.AddComponent<WheelView>();
            view.RebindReferences();
            return view;
        }

        // ------------------------------------------------------------------ bank + actions

        private static BankView BuildBank(RectTransform sidePanel, BankEntryView bankEntryPrefab)
        {
            RectTransform bank = NewNode("ui_panel_bank", sidePanel);
            Stretch(bank, 0, 74f, 0, 0);

            // Fill: ui_card_panel_zone_bg is an opaque 9-slice (the same one the zone bar and milestone
            // badges use cleanly), tinted near-black. Rim: ui_card_frame_4px_zone is a 4px outline
            // (transparent centre) so it only ever draws a crisp 1-2px edge — no blown-up bevel like the old
            // 12px frame that read as "stretched / blurry".
            Image bg = AddImage(NewNode("ui_image_bank_bg", bank), "ui_card_panel_zone_bg");
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.10f, 0.11f, 0.13f, 0.97f);
            Stretch((RectTransform)bg.transform, 0, 0, 0, 0);

            Image frame = AddImage(NewNode("ui_image_bank_frame", bank), "ui_card_frame_4px_zone");
            frame.type = Image.Type.Sliced;
            frame.color = new Color(0.34f, 0.37f, 0.43f, 0.55f);
            Stretch((RectTransform)frame.transform, 0, 0, 0, 0);

            TextMeshProUGUI empty = AddText(NewNode("ui_text_bank_empty_value", bank), "Spin to earn rewards", 24f);
            empty.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)empty.transform, 20, 20, 20, 20);

            RectTransform scrollRect = NewNode("ui_scroll_bank", bank);
            Stretch(scrollRect, 12, 12, 12, 14);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            RectTransform viewport = NewNode("ui_viewport_bank", scrollRect);
            Stretch(viewport, 0, 0, 0, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0, 0, 0, 0f);
            viewportImage.raycastTarget = true; // draggable ScrollRect viewport: RaycastTarget stays ON
            viewportImage.maskable = false; // its own RectMask2D is not an ancestor mask for itself
            scroll.viewport = viewport;

            RectTransform content = NewNode("ui_content_bank", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(140f, 160f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            // UpperCenter, not UpperLeft: the column count is decided at layout time from the panel's real
            // width, so any leftover space is split evenly to both sides instead of all piling up on the
            // right. Left/right padding is equal, so the margins are mirrored by construction.
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(16, 16, 12, 12);

            var contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.content = content;

            var view = bank.gameObject.AddComponent<BankView>();
            view.RebindReferences();
            return view;
        }

        /// <summary>
        /// The single EXIT action, in a strip below the bank grid. It always opens the cash-out summary
        /// now (<c>IdleState.OnExitRequested</c>); the view only raises the click and mirrors legality.
        /// </summary>
        private static ActionBarView BuildActions(RectTransform sidePanel)
        {
            RectTransform actions = NewNode("ui_panel_actions", sidePanel);
            BottomStrip(actions, 64f);

            // Centred in a strip that shares the bank panel's own left/right bounds.
            RectTransform buttonRect = NewNode("ui_button_action_exit", actions);
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.sizeDelta = new Vector2(180f, 52f);
            buttonRect.anchoredPosition = Vector2.zero;

            Image image = buttonRect.gameObject.AddComponent<Image>();
            image.sprite = EditorSpriteUtility.FindSprite("UI_button_grey_standard");
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            image.maskable = false;
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            RectTransform anim = NewNode("ui_transform_action_exit_anim", buttonRect);
            Stretch(anim, 0, 0, 0, 0);

            buttonRect.gameObject.AddComponent<UIButtonPunch>();

            TextMeshProUGUI text = AddText(NewNode("ui_text_action_exit_value", anim), "EXIT", 26f);
            text.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)text.transform, 0, 0, 0, 0);

            var view = actions.gameObject.AddComponent<ActionBarView>();
            view.RebindReferences();
            return view;
        }

        // ------------------------------------------------------------------ popups

        private static (BombPopupView, CollectPopupView, GiveUpConfirmPopupView, MilestonePreviewPopupView)
            BuildPopupLayer(RectTransform canvasRoot, BankEntryView bankEntryPrefab)
        {
            RectTransform layer = NewNode("ui_panel_popup_layer", canvasRoot);
            Stretch(layer, 0, 0, 0, 0);
            var canvas = layer.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10;
            layer.gameObject.AddComponent<GraphicRaycaster>();

            BombPopupView bombView = BuildBombPopup(layer);
            CollectPopupView collectView = BuildCollectPopup(layer, bankEntryPrefab);
            GiveUpConfirmPopupView giveUpView = BuildGiveUpConfirmPopup(layer);
            MilestonePreviewPopupView milestoneView = BuildMilestonePreviewPopup(layer);

            return (bombView, collectView, giveUpView, milestoneView);
        }

        private static BombPopupView BuildBombPopup(RectTransform layer)
        {
            RectTransform root = NewNode("ui_popup_bomb", layer);
            Stretch(root, 0, 0, 0, 0);
            root.gameObject.AddComponent<CanvasGroup>();

            // Dark but not opaque so the retained bank panel shows through faintly. The exact alpha is
            // driven by BombPopupView (0.86); this is just the resting tint.
            Image backdrop = AddImage(NewNode("ui_image_popup_bomb_backdrop", root), null);
            backdrop.color = new Color(0.02f, 0f, 0f, 0.86f);
            backdrop.raycastTarget = true;
            Stretch((RectTransform)backdrop.transform, 0, 0, 0, 0);

            // Breathing red alert wash over the backdrop. star_glow_alpha is a soft radial, oversized past
            // the canvas so its falloff never shows an edge; the view yoyos its alpha 0.5..0.9.
            Image vignette = AddImage(NewNode("ui_image_popup_bomb_vignette", root), "star_glow_alpha");
            vignette.color = new Color(0.80f, 0.05f, 0.05f, 0.5f);
            vignette.raycastTarget = false;
            Stretch((RectTransform)vignette.transform, -220, -220, -220, -220);

            // No modal card: content sits straight on the vignette. This full-bleed transparent node is only
            // here so PopupViewBase can scale the centred content in on open without moving the corner HUD or
            // the button row (both siblings under root).
            RectTransform anim = NewNode("ui_transform_popup_bomb_anim", root);
            Stretch(anim, 0, 0, 0, 0);

            Image titleBg = AddImage(NewNode("ui_image_popup_bomb_title_bg", anim), "ui_card_panel_zone_bg");
            titleBg.type = Image.Type.Sliced;
            titleBg.color = new Color(0f, 0f, 0f, 0.34f);
            FixedCentered((RectTransform)titleBg.transform, new Vector2(0, 320), new Vector2(1180, 78));

            TextMeshProUGUI title = AddText(NewNode("ui_text_popup_bomb_title", anim),
                "OH NO, A BOMB EXPLODED RIGHT IN YOUR HANDS!", 34f);
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;
            title.enableWordWrapping = false;
            FixedCentered((RectTransform)title.transform, new Vector2(0, 320), new Vector2(1160, 60));

            TextMeshProUGUI subtitle = AddText(NewNode("ui_text_popup_bomb_subtitle", anim),
                "Revive yourself to keep your rewards.", 24f);
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.color = new Color(0.86f, 0.87f, 0.90f, 1f);
            FixedCentered((RectTransform)subtitle.transform, new Vector2(0, 262), new Vector2(900, 36));

            TextMeshProUGUI zone = AddText(NewNode("ui_text_popup_bomb_zone_value", anim), "You reached Zone 17", 20f);
            zone.alignment = TextAlignmentOptions.Center;
            zone.color = new Color(0.62f, 0.63f, 0.68f, 1f);
            FixedCentered((RectTransform)zone.transform, new Vector2(0, 222), new Vector2(900, 30));

            // Big red skull, dead centre, behind the lost-haul strip.
            Image skull = AddImage(NewNode("ui_image_popup_bomb_icon", anim), "ui_card_icon_death");
            skull.preserveAspect = true;
            skull.color = new Color(0.78f, 0.13f, 0.13f, 0.92f);
            FixedCentered((RectTransform)skull.transform, new Vector2(0, 40), new Vector2(220, 220));

            // The lost haul: pooled BankEntryView tiles in one horizontal row, centred below the skull.
            // A deep run can bank more tiles than fit across the screen, so the row is a real horizontal
            // ScrollRect (RectMask2D clip + HorizontalLayoutGroup) — the player can swipe through the full
            // haul instead of it clipping at the edges.
            RectTransform listFrame = NewNode("ui_scroll_popup_bomb_list", anim);
            FixedCentered(listFrame, new Vector2(0f, -155f), new Vector2(1160f, 176f));
            listFrame.gameObject.AddComponent<RectMask2D>();
            var listScroll = listFrame.gameObject.AddComponent<ScrollRect>();
            listScroll.horizontal = true;
            listScroll.vertical = false;
            listScroll.movementType = ScrollRect.MovementType.Elastic;
            listScroll.scrollSensitivity = 24f;

            // Transparent fill so the ScrollRect (which is its own viewport here) has a raycast target and
            // actually receives swipe/drag events across the strip.
            Image listRaycast = listFrame.gameObject.AddComponent<Image>();
            listRaycast.color = new Color(0f, 0f, 0f, 0f);
            listRaycast.raycastTarget = true;
            listRaycast.maskable = false; // its own RectMask2D is not an ancestor mask for itself

            RectTransform content = NewNode("ui_content_popup_bomb_list", listFrame);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var listLayout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            listLayout.spacing = 14f;
            listLayout.childAlignment = TextAnchor.MiddleCenter;
            listLayout.childForceExpandWidth = false;
            listLayout.childForceExpandHeight = false;
            listLayout.childControlWidth = false;
            listLayout.childControlHeight = false;
            listLayout.padding = new RectOffset(16, 16, 8, 8);

            var listFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            listFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            listFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            listScroll.viewport = listFrame;
            listScroll.content = content;

            TextMeshProUGUI empty = AddText(NewNode("ui_text_popup_bomb_empty_value", anim), "Nothing was banked yet", 22f);
            empty.alignment = TextAlignmentOptions.Center;
            empty.color = new Color(0.7f, 0.72f, 0.77f, 1f);
            FixedCentered((RectTransform)empty.transform, new Vector2(0, -140), new Vector2(700, 36));

            BuildBombCurrencyHud(root);
            BuildBombButtons(root);

            var bombView = root.gameObject.AddComponent<BombPopupView>();
            bombView.RebindReferences();
            root.gameObject.SetActive(false);
            return bombView;
        }

        /// <summary>
        /// Top-right HUD for the bomb screen: cash value, gold value, and a "+" welded tight to the gold
        /// amount. One HorizontalLayoutGroup at 6px spacing keeps the group cohesive and right-aligned; a
        /// ContentSizeFitter lets the row hug its content so it stays pinned to the corner as the numbers
        /// change.
        /// </summary>
        private static void BuildBombCurrencyHud(RectTransform root)
        {
            RectTransform row = NewNode("ui_row_popup_bomb_currency", root);
            row.anchorMin = new Vector2(1f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(1f, 1f);
            row.anchoredPosition = new Vector2(-28f, -20f);

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = row.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Image cashIcon = AddImage(NewNode("ui_image_popup_bomb_cash_icon", row), "UI_icon_cash");
            cashIcon.preserveAspect = true;
            AddLayoutSize((RectTransform)cashIcon.transform, 38f, 38f);

            TextMeshProUGUI cash = AddText(NewNode("ui_text_popup_bomb_cash_value", row), "0", 30f);
            cash.alignment = TextAlignmentOptions.Midline;
            cash.color = new Color(0.55f, 0.92f, 0.55f, 1f);
            cash.fontStyle = FontStyles.Bold;
            cash.enableWordWrapping = false;

            Image goldIcon = AddImage(NewNode("ui_image_popup_bomb_gold_icon", row), "UI_icon_gold");
            goldIcon.preserveAspect = true;
            AddLayoutSize((RectTransform)goldIcon.transform, 38f, 38f);

            TextMeshProUGUI gold = AddText(NewNode("ui_text_popup_bomb_gold_value", row), "0", 30f);
            gold.alignment = TextAlignmentOptions.Midline;
            gold.color = new Color(1f, 0.83f, 0.35f, 1f);
            gold.fontStyle = FontStyles.Bold;
            gold.enableWordWrapping = false;

            // Store-style "+" welded right up against the gold amount — cosmetic only (no purchase flow in
            // the demo), so a plain Image, not a Button that would look interactive and do nothing.
            Image plus = AddImage(NewNode("ui_image_popup_bomb_plus", row), "ui_card_panel_zone_bg");
            plus.type = Image.Type.Sliced;
            plus.color = new Color(1f, 0.80f, 0.15f, 1f);
            AddLayoutSize((RectTransform)plus.transform, 38f, 38f);
            TextMeshProUGUI plusGlyph = AddText(NewNode("ui_text_popup_bomb_plus_glyph", (RectTransform)plus.transform), "+", 32f);
            plusGlyph.alignment = TextAlignmentOptions.Center;
            plusGlyph.color = new Color(0.1f, 0.08f, 0f, 1f);
            plusGlyph.fontStyle = FontStyles.Bold;
            Stretch((RectTransform)plusGlyph.transform, 0, 2, 0, 0);
        }

        /// <summary>Pins a fixed preferred size on a node so a layout group lays it out at that size.</summary>
        private static void AddLayoutSize(RectTransform rt, float width, float height)
        {
            var element = rt.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.minWidth = width;
            element.minHeight = height;
        }

        private const string VideoIconPath = "Assets/_Project/Art/Sprites/Icons/UI/ui_icon_video.png";

        /// <summary>
        /// Generates a crisp "watch video" glyph once into the art folder: a white rounded card with a play
        /// triangle knocked out of it, so on the blue ad-revive button the triangle reads as the button
        /// colour showing through. The demo art pack ships no video/play sprite, and the previous
        /// text-glyph-in-a-box stand-in never read cleanly.
        /// </summary>
        private static Sprite EnsureVideoIconSprite()
        {
            Sprite existing = EditorSpriteUtility.FindSprite("ui_icon_video");
            if (existing != null) return existing;

            EnsureFolder(Path.GetDirectoryName(VideoIconPath).Replace('\\', '/'));

            const int size = 144;
            const int ss = 4; // supersamples per axis, for anti-aliased edges when scaled down to 40px

            float inset = size * 0.06f;
            float radius = size * 0.20f;
            float minX = inset, minY = inset, maxX = size - inset, maxY = size - inset;
            var t0 = new Vector2(size * 0.40f, size * 0.28f);
            var t1 = new Vector2(size * 0.40f, size * 0.72f);
            var t2 = new Vector2(size * 0.70f, size * 0.50f);

            var pixels = new Color[size * size];
            for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                int covered = 0;
                for (int sy = 0; sy < ss; sy++)
                for (int sx = 0; sx < ss; sx++)
                {
                    var p = new Vector2(px + (sx + 0.5f) / ss, py + (sy + 0.5f) / ss);
                    if (InRoundedRect(p, minX, minY, maxX, maxY, radius) && !InTriangle(p, t0, t1, t2))
                        covered++;
                }
                pixels[py * size + px] = new Color(1f, 1f, 1f, covered / (float)(ss * ss));
            }

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(VideoIconPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(VideoIconPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(VideoIconPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();

            return EditorSpriteUtility.FindSprite("ui_icon_video");
        }

        private static bool InRoundedRect(Vector2 p, float minX, float minY, float maxX, float maxY, float r)
        {
            if (p.x < minX || p.x > maxX || p.y < minY || p.y > maxY) return false;
            float dx = Mathf.Max(0f, Mathf.Max(minX + r - p.x, p.x - (maxX - r)));
            float dy = Mathf.Max(0f, Mathf.Max(minY + r - p.y, p.y - (maxY - r)));
            return dx * dx + dy * dy <= r * r;
        }

        private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y);
            float d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y);
            float d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
            bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(hasNeg && hasPos);
        }

        /// <summary>
        /// The three defeat-screen buttons, siblings of the scaled content so they hold their row. All are
        /// always present; BombPopupView only toggles <c>interactable</c> on a revive that isn't available.
        /// </summary>
        private static void BuildBombButtons(RectTransform root)
        {
            const float y = -350f;

            EnsureVideoIconSprite();

            // GIVE UP — sleek grey, skull on the left (no trash-can art ships; the skull reads as "give up").
            BuildPopupButton(root, "ui_button_popup_bomb_giveup", "ui_transform_popup_bomb_giveup_anim",
                "UI_button_grey_standard", pivotX: 0.5f, anchoredX: -330f, anchoredY: y,
                animOut: out RectTransform giveUpAnim);
            Image giveUpIcon = AddImage(NewNode("ui_image_popup_bomb_giveup_icon", giveUpAnim), "ui_card_icon_death");
            giveUpIcon.preserveAspect = true;
            giveUpIcon.color = new Color(0.85f, 0.86f, 0.9f, 1f);
            LeftMiddle((RectTransform)giveUpIcon.transform, 26f, new Vector2(30, 30));
            TextMeshProUGUI giveUpText = AddText(NewNode("ui_text_popup_bomb_giveup_value", giveUpAnim), "GIVE UP", 25f);
            giveUpText.alignment = TextAlignmentOptions.Center;
            giveUpText.fontStyle = FontStyles.Bold;
            Stretch((RectTransform)giveUpText.transform, 44f, 0f, 8f, 0f);

            // REVIVE (Gold) — vibrant green with a glow behind it; coin + cost on top, "REVIVE" below. The
            // glow goes down first so the button, added next, renders over it.
            Image continueGlow = AddImage(NewNode("ui_image_popup_bomb_continue_glow", root), "star_glow_alpha");
            continueGlow.color = new Color(0.35f, 1f, 0.40f, 0.5f);
            RectTransform glowRt = (RectTransform)continueGlow.transform;
            glowRt.anchorMin = new Vector2(0.5f, 0.5f);
            glowRt.anchorMax = new Vector2(0.5f, 0.5f);
            glowRt.pivot = new Vector2(0.5f, 0.5f);
            glowRt.sizeDelta = new Vector2(440f, 210f);
            glowRt.anchoredPosition = new Vector2(0f, y);

            BuildPopupButton(root, "ui_button_popup_bomb_continue", "ui_transform_popup_bomb_continue_anim",
                "UI_button_grey_standard", pivotX: 0.5f, anchoredX: 0f, anchoredY: y,
                animOut: out RectTransform continueAnim, tint: new Color(0.28f, 0.72f, 0.33f, 1f));

            // Top line: gold icon + cost, laid out side by side and centred (no overlap).
            RectTransform costRow = NewNode("ui_row_popup_bomb_continue_cost", continueAnim);
            FixedCentered(costRow, new Vector2(0f, 16f), new Vector2(200f, 34f));
            var costLayout = costRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            costLayout.spacing = 6f;
            costLayout.childAlignment = TextAnchor.MiddleCenter;
            costLayout.childControlWidth = true;
            costLayout.childControlHeight = true;
            costLayout.childForceExpandWidth = false;
            costLayout.childForceExpandHeight = false;

            Image continueIcon = AddImage(NewNode("ui_image_popup_bomb_continue_icon", costRow), "UI_icon_gold");
            continueIcon.preserveAspect = true;
            AddLayoutSize((RectTransform)continueIcon.transform, 26f, 26f);
            TextMeshProUGUI continueText = AddText(NewNode("ui_text_popup_bomb_continue_value", costRow), "50", 24f);
            continueText.alignment = TextAlignmentOptions.Midline;
            continueText.fontStyle = FontStyles.Bold;
            continueText.enableWordWrapping = false;

            // Bottom line: the action label.
            TextMeshProUGUI continueLabel = AddText(NewNode("ui_text_popup_bomb_continue_label", continueAnim), "REVIVE", 22f);
            continueLabel.alignment = TextAlignmentOptions.Center;
            continueLabel.fontStyle = FontStyles.Bold;
            FixedCentered((RectTransform)continueLabel.transform, new Vector2(0f, -18f), new Vector2(280, 32));

            // REVIVE (Ad) — vibrant blue; "REVIVE" and a high-contrast white play badge, centred as one row.
            BuildPopupButton(root, "ui_button_popup_bomb_advert", "ui_transform_popup_bomb_advert_anim",
                "UI_button_grey_standard", pivotX: 0.5f, anchoredX: 330f, anchoredY: y,
                animOut: out RectTransform advertAnim, tint: new Color(0.20f, 0.46f, 0.86f, 1f));

            RectTransform advertRow = NewNode("ui_row_popup_bomb_advert", advertAnim);
            FixedCentered(advertRow, Vector2.zero, new Vector2(240f, 44f));
            var advertLayout = advertRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            advertLayout.spacing = 9f;
            advertLayout.childAlignment = TextAnchor.MiddleRight;
            advertLayout.childControlWidth = true;
            advertLayout.childControlHeight = true;
            advertLayout.childForceExpandWidth = false;
            advertLayout.childForceExpandHeight = false;
            // Hug the text+icon cluster so it stays centred on the button rather than pinned to a corner.
            var advertFitter = advertRow.gameObject.AddComponent<ContentSizeFitter>();
            advertFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            advertFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            TextMeshProUGUI advertText = AddText(NewNode("ui_text_popup_bomb_advert_value", advertRow), "REVIVE", 24f);
            advertText.alignment = TextAlignmentOptions.Midline;
            advertText.fontStyle = FontStyles.Bold;
            advertText.enableWordWrapping = false;

            // Stark-white "watch video" glyph, generated once into the art folder (no such sprite ships with
            // the demo pack), sized 32x32 to sit level with the 24pt label and placed to the right of it.
            Image advertIcon = AddImage(NewNode("ui_image_popup_bomb_advert_icon", advertRow), "ui_icon_video");
            advertIcon.color = Color.white;
            advertIcon.preserveAspect = true;
            AddLayoutSize((RectTransform)advertIcon.transform, 32f, 32f);
        }

        private static CollectPopupView BuildCollectPopup(RectTransform layer, BankEntryView bankEntryPrefab)
        {
            RectTransform root = NewNode("ui_popup_collect", layer);
            Stretch(root, 0, 0, 0, 0);

            Image backdrop = AddImage(NewNode("ui_image_popup_collect_backdrop", root), null);
            backdrop.color = new Color(0f, 0f, 0f, 0.82f);
            backdrop.raycastTarget = true;
            Stretch((RectTransform)backdrop.transform, 0, 0, 0, 0);

            RectTransform anim = NewNode("ui_transform_popup_collect_anim", root);
            FixedCentered(anim, Vector2.zero, new Vector2(1280f, 720f));

            Image frame = AddImage(NewNode("ui_image_popup_collect_frame", anim), "ui_card_frame_gardient");
            frame.type = Image.Type.Sliced;
            Stretch((RectTransform)frame.transform, 0, 0, 0, 0);

            Image chest = AddImage(NewNode("ui_image_popup_collect_chest_value", anim), "UI_icon_chest_standart_nolight");
            chest.preserveAspect = true;
            LeftMiddle((RectTransform)chest.transform, 60f, new Vector2(320, 320));

            Image shine = AddImage(NewNode("ui_image_popup_collect_shine", anim), "star_glow_alpha");
            shine.preserveAspect = true;
            LeftMiddle((RectTransform)shine.transform, 60f, new Vector2(400, 400));
            shine.transform.SetSiblingIndex(chest.transform.GetSiblingIndex());

            TextMeshProUGUI title = AddText(NewNode("ui_text_popup_collect_title", anim), "REWARDS COLLECTED", 38f);
            title.alignment = TextAlignmentOptions.Top;
            TopStripText((RectTransform)title.transform, 30f, new Vector2(1200, 50));

            TextMeshProUGUI zone = AddText(NewNode("ui_text_popup_collect_zone_value", anim), "Cleared 25 zones", 26f);
            zone.alignment = TextAlignmentOptions.Top;
            TopStripText((RectTransform)zone.transform, 84f, new Vector2(1200, 40));

            // Hidden until CLAIM & LEAVE: the "added to inventory" recap that fades in during the claim
            // celebration. CollectPopupView.Show puts it back down for the next summary. It sits centred
            // over the reward list (not down by the buttons, where it used to collide with CLAIM & LEAVE)
            // on its own dark plate, drawn last so it reads on top of the icons behind it.
            RectTransform bannerPanel = NewNode("ui_panel_popup_collect_banner", anim);
            FixedCentered(bannerPanel, new Vector2(0f, 10f), new Vector2(820f, 104f));
            Image bannerBg = AddImage(NewNode("ui_image_popup_collect_banner_bg", bannerPanel), "ui_card_panel_zone_bg");
            bannerBg.type = Image.Type.Sliced;
            bannerBg.color = new Color(0f, 0f, 0f, 0.9f);
            Stretch((RectTransform)bannerBg.transform, 0, 0, 0, 0);

            TextMeshProUGUI banner = AddText(NewNode("ui_text_popup_collect_banner_value", bannerPanel),
                "Total Rewards Added to Inventory", 28f);
            banner.alignment = TextAlignmentOptions.Center;
            banner.fontStyle = FontStyles.Bold;
            banner.color = new Color(1f, 0.88f, 0.45f, 1f);
            Stretch((RectTransform)banner.transform, 24, 0, 24, 0);

            bannerPanel.gameObject.SetActive(false);

            // Corner X: a genuine Button (unlike the decorative bits elsewhere) so the player can bail back
            // to the wheel with the haul untouched.
            RectTransform cancel = NewNode("ui_button_popup_collect_cancel", anim);
            cancel.anchorMin = new Vector2(1f, 1f);
            cancel.anchorMax = new Vector2(1f, 1f);
            cancel.pivot = new Vector2(1f, 1f);
            cancel.sizeDelta = new Vector2(56f, 56f);
            cancel.anchoredPosition = new Vector2(-18f, -18f);
            Image cancelImage = cancel.gameObject.AddComponent<Image>();
            cancelImage.sprite = EditorSpriteUtility.FindSprite("UI_button_grey_standard");
            cancelImage.type = Image.Type.Sliced;
            cancelImage.raycastTarget = true;
            cancelImage.maskable = false;
            var cancelButton = cancel.gameObject.AddComponent<Button>();
            cancelButton.targetGraphic = cancelImage;
            cancel.gameObject.AddComponent<UIButtonPunch>();
            TextMeshProUGUI cancelGlyph = AddText(NewNode("ui_text_popup_collect_cancel_glyph", cancel), "X", 28f);
            cancelGlyph.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)cancelGlyph.transform, 0, 0, 0, 2);

            RectTransform scrollRect = NewNode("ui_scroll_popup_collect_list", anim);
            Stretch(scrollRect, 480f, 100f, 40f, 140f);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            RectTransform viewport = NewNode("ui_viewport_popup_collect_list", scrollRect);
            Stretch(viewport, 0, 0, 0, 0);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0, 0, 0, 0f);
            viewportImage.raycastTarget = true;
            viewportImage.maskable = false; // its own RectMask2D is not an ancestor mask for itself
            scroll.viewport = viewport;

            RectTransform content = NewNode("ui_content_popup_collect_list", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(140f, 160f);
            grid.spacing = new Vector2(12f, 12f);
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            // Same as the gameplay bank: centre the column block with equal left/right padding so the
            // collected-items grid has mirrored margins rather than a wide gap on the right.
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.padding = new RectOffset(16, 16, 12, 12);

            var contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.content = content;

            // Anchored to the lower-right of the 720-tall card, inside the 100px margin the reward list
            // leaves below itself (Stretch(scrollRect, 480, 100, 40, 140)) rather than to the card center.
            BuildPopupButton(anim, "ui_button_popup_collect_confirm", "ui_transform_popup_collect_confirm_anim",
                "UI_button_orange_standard", pivotX: 0.5f, anchoredX: 0f, anchoredY: 50f,
                animOut: out RectTransform confirmAnim, fixedAnchorMode: true, anchorPoint: new Vector2(0.75f, 0f));
            TextMeshProUGUI confirmText = AddText(NewNode("ui_text_popup_collect_confirm_value", confirmAnim), "CLAIM & LEAVE", 30f);
            confirmText.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)confirmText.transform, 0, 0, 0, 0);

            // Drawn last so the celebration recap sits above the reward list and the action buttons.
            bannerPanel.SetAsLastSibling();

            var collectView = root.gameObject.AddComponent<CollectPopupView>();
            collectView.RebindReferences();
            root.gameObject.SetActive(false);
            return collectView;
        }

        private static GiveUpConfirmPopupView BuildGiveUpConfirmPopup(RectTransform layer)
        {
            RectTransform root = NewNode("ui_popup_confirm_giveup", layer);
            Stretch(root, 0, 0, 0, 0);

            Image backdrop = AddImage(NewNode("ui_image_popup_confirm_giveup_backdrop", root), null);
            backdrop.color = new Color(0f, 0f, 0f, 0.82f);
            backdrop.raycastTarget = true;
            Stretch((RectTransform)backdrop.transform, 0, 0, 0, 0);

            RectTransform anim = NewNode("ui_transform_popup_confirm_giveup_anim", root);
            FixedCentered(anim, Vector2.zero, new Vector2(820f, 420f));

            Image frame = AddImage(NewNode("ui_image_popup_confirm_giveup_frame", anim), "ui_card_frame_12px_neutral");
            frame.type = Image.Type.Sliced;
            Stretch((RectTransform)frame.transform, 0, 0, 0, 0);

            TextMeshProUGUI title = AddText(NewNode("ui_text_popup_confirm_giveup_title", anim), "Give up this run?", 34f);
            title.alignment = TextAlignmentOptions.Center;
            FixedCentered((RectTransform)title.transform, new Vector2(0, 110), new Vector2(700, 50));

            TextMeshProUGUI body = AddText(NewNode("ui_text_popup_confirm_giveup_body_value", anim), "You will lose 6 rewards.", 26f);
            body.alignment = TextAlignmentOptions.Center;
            FixedCentered((RectTransform)body.transform, new Vector2(0, 40), new Vector2(700, 40));

            BuildPopupButton(anim, "ui_button_popup_confirm_giveup_yes", "ui_transform_popup_confirm_giveup_yes_anim",
                "UI_button_orange_standard", pivotX: 1f, anchoredX: -20f, anchoredY: -130f,
                animOut: out RectTransform yesAnim);
            TextMeshProUGUI yesText = AddText(NewNode("ui_text_popup_confirm_giveup_yes_value", yesAnim), "GIVE UP", 26f);
            yesText.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)yesText.transform, 0, 0, 0, 0);

            BuildPopupButton(anim, "ui_button_popup_confirm_giveup_no", "ui_transform_popup_confirm_giveup_no_anim",
                "UI_button_grey_standard", pivotX: 0f, anchoredX: 20f, anchoredY: -130f,
                animOut: out RectTransform noAnim);
            TextMeshProUGUI noText = AddText(NewNode("ui_text_popup_confirm_giveup_no_value", noAnim), "CANCEL", 26f);
            noText.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)noText.transform, 0, 0, 0, 0);

            var giveUpView = root.gameObject.AddComponent<GiveUpConfirmPopupView>();
            giveUpView.RebindReferences();
            root.gameObject.SetActive(false);
            return giveUpView;
        }

        /// <summary>
        /// The milestone teaser: a dark overlay with a row of preview cards and a one-line description of
        /// what a Safe / Super zone is worth. Tapping the backdrop or the X closes it. The two card rows
        /// are pre-built and <see cref="MilestonePreviewPopupView"/> toggles which one shows.
        /// </summary>
        private static MilestonePreviewPopupView BuildMilestonePreviewPopup(RectTransform layer)
        {
            RectTransform root = NewNode("ui_popup_milestone", layer);
            Stretch(root, 0, 0, 0, 0);

            Image backdrop = AddImage(NewNode("ui_image_popup_milestone_backdrop", root), null);
            backdrop.color = new Color(0.02f, 0.03f, 0.02f, 0.88f);
            backdrop.raycastTarget = true;
            Stretch((RectTransform)backdrop.transform, 0, 0, 0, 0);
            var backdropButton = backdrop.gameObject.AddComponent<Button>();
            backdropButton.transition = Selectable.Transition.None;
            backdropButton.targetGraphic = backdrop;

            RectTransform anim = NewNode("ui_transform_popup_milestone_anim", root);
            Stretch(anim, 0, 0, 0, 0);

            TextMeshProUGUI title = AddText(NewNode("ui_text_popup_milestone_title_value", anim), "SAFE ZONE", 40f);
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;
            FixedCentered((RectTransform)title.transform, new Vector2(0f, 305f), new Vector2(900f, 56f));

            // Safe zones ride the Silver wheel, super zones the Golden one — show the actual spin art plus
            // a few reward-chest preview slots, not a stand-in card game.
            BuildMilestonePreviewRow(anim, "ui_row_popup_milestone_safe", "ui_spin_silver_base",
                new[] { "UI_icon_chest_silver_nolight", "UI_icon_gold", "UI_icon_chest_big_nolight" },
                new Color(0.42f, 0.92f, 0.46f, 1f), active: true);
            BuildMilestonePreviewRow(anim, "ui_row_popup_milestone_super", "ui_spin_golden_base",
                new[] { "UI_icon_chest_gold_nolight", "UI_icon_chest_super_nolight", "UI_icon_gold" },
                new Color(0.96f, 0.78f, 0.36f, 1f), active: false);

            TextMeshProUGUI desc = AddText(NewNode("ui_text_popup_milestone_desc_value", anim),
                "Win special rewards in bomb-free Safe Zones!", 28f);
            desc.alignment = TextAlignmentOptions.Center;
            desc.enableWordWrapping = false;
            FixedCentered((RectTransform)desc.transform, new Vector2(0f, -300f), new Vector2(1100f, 44f));

            RectTransform close = NewNode("ui_button_popup_milestone_close", anim);
            close.anchorMin = new Vector2(1f, 1f);
            close.anchorMax = new Vector2(1f, 1f);
            close.pivot = new Vector2(1f, 1f);
            close.sizeDelta = new Vector2(56f, 56f);
            close.anchoredPosition = new Vector2(-28f, -24f);
            Image closeImage = close.gameObject.AddComponent<Image>();
            closeImage.sprite = EditorSpriteUtility.FindSprite("UI_button_grey_standard");
            closeImage.type = Image.Type.Sliced;
            closeImage.raycastTarget = true;
            closeImage.maskable = false;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = closeImage;
            close.gameObject.AddComponent<UIButtonPunch>();
            TextMeshProUGUI closeGlyph = AddText(NewNode("ui_text_popup_milestone_close_glyph", close), "X", 28f);
            closeGlyph.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)closeGlyph.transform, 0, 0, 0, 2);

            var view = root.gameObject.AddComponent<MilestonePreviewPopupView>();
            view.RebindReferences();
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// One milestone preview: the real spin wheel for that tier under a soft accent glow, with a strip
        /// of reward-chest preview slots below it. <see cref="MilestonePreviewPopupView"/> shows exactly one
        /// of the two rows.
        /// </summary>
        private static void BuildMilestonePreviewRow(
            RectTransform parent, string rowName, string wheelSprite, string[] previewIcons, Color accent, bool active)
        {
            RectTransform row = NewNode(rowName, parent);
            FixedCentered(row, Vector2.zero, new Vector2(1120f, 760f));

            // Vertical rhythm: title (~y 305) -> wheel (~y 95) -> reward slots (~y -165) -> description
            // (~y -300), leaving ~35px of clear space between each block.
            Image glow = AddImage(NewNode(rowName + "_glow", row), "star_glow_alpha");
            glow.color = new Color(accent.r, accent.g, accent.b, 0.32f);
            FixedCentered((RectTransform)glow.transform, new Vector2(0f, 95f), new Vector2(540f, 540f));

            Image wheel = AddImage(NewNode(rowName + "_wheel", row), wheelSprite);
            wheel.preserveAspect = true;
            FixedCentered((RectTransform)wheel.transform, new Vector2(0f, 95f), new Vector2(300f, 300f));

            RectTransform slots = NewNode(rowName + "_slots", row);
            FixedCentered(slots, new Vector2(0f, -165f), new Vector2(720f, 150f));
            var slotLayout = slots.gameObject.AddComponent<HorizontalLayoutGroup>();
            slotLayout.spacing = 40f;
            slotLayout.padding = new RectOffset(12, 12, 0, 0);
            slotLayout.childAlignment = TextAnchor.MiddleCenter;
            slotLayout.childControlWidth = true;
            slotLayout.childControlHeight = true;
            slotLayout.childForceExpandWidth = false;
            slotLayout.childForceExpandHeight = false;

            for (int i = 0; i < previewIcons.Length; i++)
            {
                RectTransform slot = NewNode(rowName + "_slot" + i, slots);
                AddLayoutSize(slot, 140f, 140f);

                Image slotBg = AddImage(NewNode(slot.name + "_bg", slot), "ui_card_panel_zone_bg");
                slotBg.type = Image.Type.Sliced;
                slotBg.color = new Color(0.05f, 0.06f, 0.06f, 1f);
                Stretch((RectTransform)slotBg.transform, 0, 0, 0, 0);

                Image slotStroke = AddImage(NewNode(slot.name + "_stroke", slot), "ui_card_frame_4px_zone");
                slotStroke.type = Image.Type.Sliced;
                slotStroke.color = accent;
                Stretch((RectTransform)slotStroke.transform, 0, 0, 0, 0);

                Image icon = AddImage(NewNode(slot.name + "_icon", slot), previewIcons[i]);
                icon.preserveAspect = true;
                FixedCentered((RectTransform)icon.transform, Vector2.zero, new Vector2(96f, 96f));
            }

            row.gameObject.SetActive(active);
        }

        /// <summary>Shared skeleton for a popup action button: sliced image, Button, and an _anim child.</summary>
        private static void BuildPopupButton(
            RectTransform parent, string buttonName, string animName, string spriteName,
            float pivotX, float anchoredX, float anchoredY, out RectTransform animOut,
            bool fixedAnchorMode = false, Vector2 anchorPoint = default, Color tint = default)
        {
            RectTransform buttonRect = NewNode(buttonName, parent);
            buttonRect.anchorMin = fixedAnchorMode ? anchorPoint : new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = buttonRect.anchorMin;
            buttonRect.pivot = new Vector2(pivotX, 0.5f);
            buttonRect.sizeDelta = new Vector2(300f, 90f);
            buttonRect.anchoredPosition = new Vector2(anchoredX, anchoredY);

            Image image = buttonRect.gameObject.AddComponent<Image>();
            image.sprite = EditorSpriteUtility.FindSprite(spriteName);
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            image.maskable = false;
            // No green button art ships, so the ad-revive button borrows the grey sprite under a green tint.
            if (tint.a > 0f) image.color = tint;
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            animOut = NewNode(animName, buttonRect);
            Stretch(animOut, 0, 0, 0, 0);

            buttonRect.gameObject.AddComponent<UIButtonPunch>();
        }

        // ------------------------------------------------------------------ vfx layer

        private static VfxView BuildVfxLayer(RectTransform canvasRoot)
        {
            RectTransform layer = NewNode("ui_panel_vfx_layer", canvasRoot);
            Stretch(layer, 0, 0, 0, 0);
            var canvas = layer.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 20;
            // Deliberately no GraphicRaycaster: this layer is never interactive.

            RectTransform shake = NewNode("ui_transform_vfx_screenshake", layer);
            Stretch(shake, 0, 0, 0, 0);

            Image flash = AddImage(NewNode("ui_image_vfx_flash", shake), "star_flash_alpha");
            flash.color = new Color(1f, 0.2f, 0.2f, 0f);
            Stretch((RectTransform)flash.transform, 0, 0, 0, 0);

            Image burst = AddImage(NewNode("ui_image_vfx_reward_burst", shake), "star_glow_alpha");
            burst.color = new Color(1f, 1f, 1f, 0f);
            Stretch((RectTransform)burst.transform, 0, 0, 0, 0);

            var view = layer.gameObject.AddComponent<VfxView>();
            view.RebindReferences();
            return view;
        }

        // ------------------------------------------------------------------ debug overlay

        /// <summary>
        /// The cheat bar, bottom-left on its own top-most canvas. Built into every scene but
        /// <see cref="DebugOverlayView"/> switches itself off outside the editor / development builds.
        /// </summary>
        private static DebugOverlayView BuildDebugOverlay(RectTransform canvasRoot)
        {
            RectTransform root = NewNode("ui_panel_debug", canvasRoot);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.zero;
            root.pivot = Vector2.zero;
            root.sizeDelta = new Vector2(230f, 48f);
            root.anchoredPosition = new Vector2(16f, 16f);

            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 30;
            root.gameObject.AddComponent<GraphicRaycaster>();

            // The toggle fills the root; the body stacks upward from the root's top edge.
            BuildDebugButton(root, "ui_button_debug_toggle", "DEBUG", stretchFill: true, topOffset: 0f);

            RectTransform body = NewNode("ui_panel_debug_body", root);
            body.anchorMin = new Vector2(0f, 1f);
            body.anchorMax = new Vector2(1f, 1f);
            body.pivot = new Vector2(0.5f, 0f);
            body.sizeDelta = new Vector2(0f, 292f);
            body.anchoredPosition = new Vector2(0f, 8f);

            Image bodyBg = AddImage(NewNode("ui_image_debug_body_bg", body), "ui_card_panel_zone_bg");
            bodyBg.type = Image.Type.Sliced;
            bodyBg.color = new Color(0f, 0f, 0f, 0.82f);
            Stretch((RectTransform)bodyBg.transform, 0, 0, 0, 0);

            BuildDebugButton(body, "ui_button_debug_zone5", "Jump to Zone 5", stretchFill: false, topOffset: -8f);
            BuildDebugButton(body, "ui_button_debug_zone30", "Jump to Zone 30", stretchFill: false, topOffset: -64f);
            BuildDebugButton(body, "ui_button_debug_bomb", "Trigger Bomb Defeat", stretchFill: false, topOffset: -120f);
            BuildDebugButton(body, "ui_button_debug_gold", "+1000 Gold", stretchFill: false, topOffset: -176f);
            BuildDebugButton(body, "ui_button_debug_items", "+40 Random Items", stretchFill: false, topOffset: -232f);

            var view = root.gameObject.AddComponent<DebugOverlayView>();
            view.RebindReferences();
            return view;
        }

        private static Button BuildDebugButton(
            RectTransform parent, string name, string label, bool stretchFill, float topOffset)
        {
            RectTransform rt = NewNode(name, parent);
            if (stretchFill)
            {
                Stretch(rt, 0, 0, 0, 0);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(-16f, 48f);
                rt.anchoredPosition = new Vector2(0f, topOffset);
            }

            Image image = rt.gameObject.AddComponent<Image>();
            image.sprite = EditorSpriteUtility.FindSprite("UI_button_grey_standard");
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            image.maskable = false;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TextMeshProUGUI text = AddText(NewNode(name + "_label", rt), label, 18f);
            text.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)text.transform, 6, 0, 6, 0);

            return button;
        }

        // ------------------------------------------------------------------ node/anchor helpers

        private static RectTransform NewNode(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        /// <summary>Stretches on both axes with a per-edge inset. Pivot is irrelevant under a full stretch.</summary>
        private static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Full width, fixed height, welded to the top edge with the given offset below it.</summary>
        private static void TopStrip(RectTransform rt, float height, float topOffset)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = new Vector2(0f, -topOffset);
        }

        /// <summary>Full width, fixed height, welded to the bottom edge.</summary>
        private static void BottomStrip(RectTransform rt, float height)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(0f, height);
            rt.anchoredPosition = Vector2.zero;
        }

        /// <summary>A fixed-size node centered on its parent, offset by <paramref name="offset"/>.</summary>
        private static void FixedCentered(RectTransform rt, Vector2 offset, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
        }

        /// <summary>A fixed-size node welded to the parent's top-center edge.</summary>
        private static void FixedTop(RectTransform rt, Vector2 size, float topOffset)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = new Vector2(0f, -topOffset);
        }

        /// <summary>A fixed-size node vertically centered against the parent's left edge.</summary>
        private static void LeftMiddle(RectTransform rt, float leftOffset, Vector2 size)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = new Vector2(leftOffset, 0f);
        }

        /// <summary>A fixed-size node vertically centered against the parent's right edge.</summary>
        private static void RightMiddle(RectTransform rt, float rightOffset, Vector2 size)
        {
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = new Vector2(rightOffset, 0f);
        }

        /// <summary>A fixed-size, full-width-ish text row welded under the parent's top edge.</summary>
        private static void TopStripText(RectTransform rt, float topOffset, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = new Vector2(0f, -topOffset);
        }

        private static Image AddImage(RectTransform node, string spriteName)
        {
            Image image = node.gameObject.AddComponent<Image>();
            image.sprite = string.IsNullOrEmpty(spriteName) ? null : EditorSpriteUtility.FindSprite(spriteName);
            image.raycastTarget = false;
            image.maskable = false;
            return image;
        }


        private static TextMeshProUGUI AddText(RectTransform node, string text, float fontSize)
        {
            TextMeshProUGUI tmp = node.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.maskable = false; // Unity defaults this true; only nodes inside a RectMask2D opt back in.
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path)) return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
