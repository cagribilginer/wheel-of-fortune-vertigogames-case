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
    /// Deliberately builds a <em>static</em> scene: no DOTween (not yet imported), no presenter wiring, no
    /// clickable behaviour. Buttons exist and raise their C# events, but nothing subscribes yet — that is
    /// Day 4's job, once <c>GameInstaller</c> composes the state machine with these views.
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

            (BombPopupView bombView, CollectPopupView collectView, GiveUpConfirmPopupView giveUpView) =
                BuildPopupLayer(canvasRoot, bankEntryPrefab);
            BuildVfxLayer(canvasRoot);

            BuildGameInstaller(canvasRoot, headerView, wheelView, zoneMapView, bankView, actionBarView,
                bombView, collectView, giveUpView, tilePrefab, bankEntryPrefab);

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
            GiveUpConfirmPopupView giveUpPopup, ZoneMapTileView tilePrefab, BankEntryView bankEntryPrefab)
        {
            var installer = new GameObject("GameInstaller").AddComponent<GameInstaller>();
            Sprite bombIcon = EditorSpriteUtility.FindSprite("ui_card_icon_death");
            Sprite zoneBg = EditorSpriteUtility.FindSprite("ui_card_panel_zone_bg");
            Sprite zoneCurrent = EditorSpriteUtility.FindSprite("ui_card_panel_zone_current");
            Sprite zoneSuper = EditorSpriteUtility.FindSprite("ui_card_panel_zone_super");

            installer.Configure(header, wheel, zoneMap, bank, actionBar, bombPopup, collectPopup, giveUpPopup,
                tilePrefab, bankEntryPrefab, canvasRoot, bombIcon, zoneBg, zoneCurrent, zoneSuper);
        }

        // ------------------------------------------------------------------ pooled prefabs

        private static ZoneMapTileView BuildZoneMapTilePrefab()
        {
            var root = new GameObject("ui_item_zonemap_tile", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(76f, 104f);
            root.AddComponent<LayoutElement>().preferredWidth = 76f;
            root.GetComponent<LayoutElement>().preferredHeight = 104f;

            Image bg = AddImage(NewNode("ui_image_zonemap_tile_bg_value", rt), "ui_card_panel_zone_bg");
            bg.type = Image.Type.Sliced;
            bg.maskable = true;
            Stretch((RectTransform)bg.transform, 0, 0, 0, 0);

            Image frame = AddImage(NewNode("ui_image_zonemap_tile_frame", rt), "ui_card_frame_4px_zone");
            frame.type = Image.Type.Sliced;
            frame.maskable = true;
            Stretch((RectTransform)frame.transform, 0, 0, 0, 0);

            TextMeshProUGUI number = AddText(NewNode("ui_text_zonemap_tile_number_value", rt), "7", 28f);
            number.maskable = true; // pooled into ui_content_zonemap, which sits inside a RectMask2D
            FixedCentered((RectTransform)number.transform, Vector2.zero, new Vector2(70, 40));

            Image badge = AddImage(NewNode("ui_image_zonemap_tile_badge_value", rt), null);
            badge.preserveAspect = true;
            badge.maskable = true;
            badge.enabled = false;
            FixedTop((RectTransform)badge.transform, new Vector2(32, 32), 6f);

            var view = root.AddComponent<ZoneMapTileView>();
            view.RebindReferences();
            return SaveAsPrefab(root, view);
        }

        private static WheelSlotView BuildWheelSlotPrefab()
        {
            var root = new GameObject("ui_item_wheel_slot", typeof(RectTransform));
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(140f, 140f);

            Image icon = AddImage(NewNode("ui_image_slot_icon_value", rt), null);
            icon.preserveAspect = true;
            FixedCentered((RectTransform)icon.transform, new Vector2(0, 14), new Vector2(84, 84));

            TextMeshProUGUI amount = AddText(NewNode("ui_text_slot_amount_value", rt), "x25", 22f);
            FixedCentered((RectTransform)amount.transform, new Vector2(0, -42), new Vector2(120, 32));

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
            FixedCentered((RectTransform)amount.transform, new Vector2(0, -54), new Vector2(120, 32));

            var view = root.AddComponent<BankEntryView>();
            view.RebindReferences();
            return SaveAsPrefab(root, view);
        }

        private static T SaveAsPrefab<T>(GameObject root, T view) where T : Component
        {
            EnsureFolder(PrefabFolder);
            string path = $"{PrefabFolder}/{root.name}.prefab";

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            return saved.GetComponent<T>();
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

            TextMeshProUGUI zone = AddText(NewNode("ui_text_header_zone_value", header), "ZONE 7", 44f);
            zone.alignment = TextAlignmentOptions.MidlineLeft;
            LeftMiddle((RectTransform)zone.transform, 40f, new Vector2(400, 80));

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

            Image bg = AddImage(NewNode("ui_image_zonemap_bg", zoneMap), "ui_card_panel_zone_bg");
            bg.type = Image.Type.Sliced;
            Stretch((RectTransform)bg.transform, 0, 0, 0, 0);

            Image frame = AddImage(NewNode("ui_image_zonemap_frame", zoneMap), "ui_card_zone_map_frame");
            frame.type = Image.Type.Sliced;
            Stretch((RectTransform)frame.transform, 0, 0, 0, 0);

            // Fixed width, centred, vertically stretched: the backdrop bar behind it spans the full strip,
            // but the scrollable window itself is capped so ~9 tiles are visible at once instead of the
            // ~20+ a full-width viewport would show, which made "centre the current zone" imperceptible.
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

            var view = zoneMap.gameObject.AddComponent<ZoneMapView>();
            view.RebindReferences();
            return view;
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
            indicator.anchoredPosition = Vector2.zero;

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

            var view = wheelPanel.gameObject.AddComponent<WheelView>();
            view.RebindReferences();
            return view;
        }

        // ------------------------------------------------------------------ bank + actions

        private static BankView BuildBank(RectTransform sidePanel, BankEntryView bankEntryPrefab)
        {
            RectTransform bank = NewNode("ui_panel_bank", sidePanel);
            Stretch(bank, 0, 150f, 0, 0);

            Image bg = AddImage(NewNode("ui_image_bank_bg", bank), "ui_card_frame_12px_neutral");
            bg.type = Image.Type.Sliced;
            // The source art is neutral (pure white), meant to be tinted per context — left untinted it
            // reads as a blown-out white panel rather than as part of the dark theme.
            bg.color = new Color(0.06f, 0.07f, 0.1f, 0.85f);
            Stretch((RectTransform)bg.transform, 0, 0, 0, 0);

            TextMeshProUGUI title = AddText(NewNode("ui_text_bank_title", bank), "COLLECTED", 26f);
            title.alignment = TextAlignmentOptions.TopLeft;
            TopStripText((RectTransform)title.transform, 24f, new Vector2(300, 40));

            TextMeshProUGUI empty = AddText(NewNode("ui_text_bank_empty_value", bank), "Spin to earn rewards", 24f);
            empty.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)empty.transform, 20, 20, 20, 60);

            RectTransform scrollRect = NewNode("ui_scroll_bank", bank);
            Stretch(scrollRect, 12, 12, 12, 60);
            var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

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
            grid.childAlignment = TextAnchor.UpperLeft;

            var contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.content = content;

            var view = bank.gameObject.AddComponent<BankView>();
            view.RebindReferences();
            return view;
        }

        private static ActionBarView BuildActions(RectTransform sidePanel)
        {
            RectTransform actions = NewNode("ui_panel_actions", sidePanel);
            BottomStrip(actions, 130f);

            BuildActionButton(actions, "ui_button_action_collect", "ui_transform_action_collect_anim",
                "ui_text_action_collect_value", "COLLECT", "UI_button_orange_standard",
                pivotX: 1f, anchoredX: -20f);

            BuildActionButton(actions, "ui_button_action_giveup", "ui_transform_action_giveup_anim",
                "ui_text_action_giveup_value", "GIVE UP", "UI_button_grey_standard",
                pivotX: 0f, anchoredX: 20f);

            var view = actions.gameObject.AddComponent<ActionBarView>();
            view.RebindReferences();
            return view;
        }

        private static void BuildActionButton(
            RectTransform parent, string buttonName, string animName, string textName,
            string label, string spriteName, float pivotX, float anchoredX)
        {
            RectTransform buttonRect = NewNode(buttonName, parent);
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(pivotX, 0.5f);
            buttonRect.sizeDelta = new Vector2(300f, 90f);
            buttonRect.anchoredPosition = new Vector2(anchoredX, 0f);

            Image image = buttonRect.gameObject.AddComponent<Image>();
            image.sprite = EditorSpriteUtility.FindSprite(spriteName);
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            image.maskable = false;
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            RectTransform anim = NewNode(animName, buttonRect);
            Stretch(anim, 0, 0, 0, 0);

            TextMeshProUGUI text = AddText(NewNode(textName, anim), label, 30f);
            text.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)text.transform, 0, 0, 0, 0);
        }

        // ------------------------------------------------------------------ popups

        private static (BombPopupView, CollectPopupView, GiveUpConfirmPopupView) BuildPopupLayer(
            RectTransform canvasRoot, BankEntryView bankEntryPrefab)
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

            return (bombView, collectView, giveUpView);
        }

        private static BombPopupView BuildBombPopup(RectTransform layer)
        {
            RectTransform root = NewNode("ui_popup_bomb", layer);
            Stretch(root, 0, 0, 0, 0);
            root.gameObject.AddComponent<CanvasGroup>();

            Image backdrop = AddImage(NewNode("ui_image_popup_bomb_backdrop", root), null);
            backdrop.color = new Color(0f, 0f, 0f, 0.82f);
            backdrop.raycastTarget = true;
            Stretch((RectTransform)backdrop.transform, 0, 0, 0, 0);

            RectTransform anim = NewNode("ui_transform_popup_bomb_anim", root);
            FixedCentered(anim, Vector2.zero, new Vector2(900f, 560f));

            Image frame = AddImage(NewNode("ui_image_popup_bomb_frame", anim), "ui_card_frame_12px_neutral");
            frame.type = Image.Type.Sliced;
            Stretch((RectTransform)frame.transform, 0, 0, 0, 0);

            Image icon = AddImage(NewNode("ui_image_popup_bomb_icon", anim), "ui_card_icon_death");
            icon.preserveAspect = true;
            FixedCentered((RectTransform)icon.transform, new Vector2(0, 140), new Vector2(140, 140));

            TextMeshProUGUI title = AddText(NewNode("ui_text_popup_bomb_title", anim), "EVERYTHING LOST", 40f);
            title.alignment = TextAlignmentOptions.Center;
            FixedCentered((RectTransform)title.transform, new Vector2(0, 20), new Vector2(700, 60));

            TextMeshProUGUI zone = AddText(NewNode("ui_text_popup_bomb_zone_value", anim), "You reached Zone 17", 28f);
            zone.alignment = TextAlignmentOptions.Center;
            FixedCentered((RectTransform)zone.transform, new Vector2(0, -30), new Vector2(700, 40));

            BuildPopupButton(anim, "ui_button_popup_bomb_continue", "ui_transform_popup_bomb_continue_anim",
                "UI_button_orange_standard", pivotX: 1f, anchoredX: -20f, anchoredY: -180f,
                animOut: out RectTransform continueAnim);
            Image continueIcon = AddImage(NewNode("ui_image_popup_bomb_continue_icon", continueAnim), "UI_icon_gold");
            continueIcon.preserveAspect = true;
            LeftMiddle((RectTransform)continueIcon.transform, 30f, new Vector2(36, 36));
            TextMeshProUGUI continueText = AddText(NewNode("ui_text_popup_bomb_continue_value", continueAnim), "220", 28f);
            continueText.alignment = TextAlignmentOptions.MidlineRight;
            RightMiddle((RectTransform)continueText.transform, -30f, new Vector2(160, 40));

            BuildPopupButton(anim, "ui_button_popup_bomb_restart", "ui_transform_popup_bomb_restart_anim",
                "UI_button_grey_standard", pivotX: 0f, anchoredX: 20f, anchoredY: -180f,
                animOut: out RectTransform restartAnim);
            TextMeshProUGUI restartText = AddText(NewNode("ui_text_popup_bomb_restart_value", restartAnim), "TRY AGAIN", 28f);
            restartText.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)restartText.transform, 0, 0, 0, 0);

            var bombView = root.gameObject.AddComponent<BombPopupView>();
            bombView.RebindReferences();
            root.gameObject.SetActive(false);
            return bombView;
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
            grid.childAlignment = TextAnchor.UpperLeft;

            var contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.content = content;

            // Anchored to the lower-right of the 720-tall card, inside the 100px margin the reward list
            // leaves below itself (Stretch(scrollRect, 480, 100, 40, 140)) rather than to the card center.
            BuildPopupButton(anim, "ui_button_popup_collect_confirm", "ui_transform_popup_collect_confirm_anim",
                "UI_button_orange_standard", pivotX: 0.5f, anchoredX: 0f, anchoredY: 50f,
                animOut: out RectTransform confirmAnim, fixedAnchorMode: true, anchorPoint: new Vector2(0.75f, 0f));
            TextMeshProUGUI confirmText = AddText(NewNode("ui_text_popup_collect_confirm_value", confirmAnim), "AWESOME", 30f);
            confirmText.alignment = TextAlignmentOptions.Center;
            Stretch((RectTransform)confirmText.transform, 0, 0, 0, 0);

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

        /// <summary>Shared skeleton for a popup action button: sliced image, Button, and an _anim child.</summary>
        private static void BuildPopupButton(
            RectTransform parent, string buttonName, string animName, string spriteName,
            float pivotX, float anchoredX, float anchoredY, out RectTransform animOut,
            bool fixedAnchorMode = false, Vector2 anchorPoint = default)
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
            var button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            animOut = NewNode(animName, buttonRect);
            Stretch(animOut, 0, 0, 0, 0);
        }

        // ------------------------------------------------------------------ vfx layer

        private static void BuildVfxLayer(RectTransform canvasRoot)
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
