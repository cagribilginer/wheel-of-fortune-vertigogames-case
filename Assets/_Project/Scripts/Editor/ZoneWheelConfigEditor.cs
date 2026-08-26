using UnityEditor;
using UnityEngine;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Data.Configs;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// Draws the authored wheel as the ring it actually is, with each slice's icon in its real slot.
    /// <para>
    /// The list of eight entries already satisfies "slice content changeable from the editor", but a flat
    /// list makes a reviewer reconstruct the wheel in their head. Showing the ring makes a mis-authored
    /// wheel — two bombs, a missing reward, a slot in the wrong place — visible at a glance instead of on
    /// a play-through.
    /// </para>
    /// </summary>
    [CustomEditor(typeof(ZoneWheelConfig))]
    public sealed class ZoneWheelConfigEditor : UnityEditor.Editor
    {
        private const float PreviewSize = 260f;
        private const float SlotRadiusFactor = 0.34f;
        private const float SlotSize = 54f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var config = (ZoneWheelConfig)target;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Wheel Preview", EditorStyles.boldLabel);

            Rect area = GUILayoutUtility.GetRect(PreviewSize, PreviewSize);
            DrawRing(area, config);

            DrawSummary(config);
        }

        private static void DrawRing(Rect area, ZoneWheelConfig config)
        {
            Vector2 centre = area.center;
            float radius = Mathf.Min(area.width, area.height) * SlotRadiusFactor;

            Sprite baseSprite = config.Theme != null ? config.Theme.BaseSprite : null;
            if (baseSprite != null && baseSprite.texture != null)
            {
                float plate = Mathf.Min(area.width, area.height);
                var plateRect = new Rect(centre.x - plate / 2f, centre.y - plate / 2f, plate, plate);
                GUI.DrawTexture(plateRect, baseSprite.texture, ScaleMode.ScaleToFit);
            }
            else
            {
                EditorGUI.DrawRect(area, new Color(0f, 0f, 0f, 0.15f));
            }

            int count = config.Slices.Count;
            if (count == 0)
            {
                EditorGUI.LabelField(area, "No slices authored", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                // Slot 0 sits at the top, under the indicator, and slots run clockwise — matching the
                // polar layout the runtime uses so the preview cannot disagree with the built wheel.
                float angle = i * Mathf.PI * 2f / count;
                var slotCentre = new Vector2(
                    centre.x + Mathf.Sin(angle) * radius,
                    centre.y - Mathf.Cos(angle) * radius);

                var slotRect = new Rect(
                    slotCentre.x - SlotSize / 2f, slotCentre.y - SlotSize / 2f, SlotSize, SlotSize);

                DrawSlot(slotRect, config.Slices[i], i);
            }
        }

        private static void DrawSlot(Rect rect, WheelSliceEntry entry, int index)
        {
            if (entry == null)
            {
                EditorGUI.DrawRect(rect, new Color(1f, 0f, 0f, 0.35f));
                EditorGUI.LabelField(rect, $"{index}\nnull", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            if (entry.IsBomb)
            {
                EditorGUI.DrawRect(rect, new Color(0.7f, 0.1f, 0.1f, 0.75f));
                EditorGUI.LabelField(rect, "BOMB", EditorStyles.whiteMiniLabel);
                return;
            }

            if (entry.Reward == null)
            {
                EditorGUI.DrawRect(rect, new Color(1f, 0.6f, 0f, 0.5f));
                EditorGUI.LabelField(rect, $"{index}\nempty", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.35f));

            Sprite icon = entry.Reward.Icon;
            if (icon != null && icon.texture != null)
                GUI.DrawTexture(rect, icon.texture, ScaleMode.ScaleToFit);

            var label = new Rect(rect.x, rect.yMax - 14f, rect.width, 14f);
            EditorGUI.LabelField(label, $"x{entry.ResolveBaseAmount()}", EditorStyles.whiteMiniLabel);
        }

        private static void DrawSummary(ZoneWheelConfig config)
        {
            int bombs = 0;
            int weight = 0;

            for (int i = 0; i < config.Slices.Count; i++)
            {
                WheelSliceEntry entry = config.Slices[i];
                if (entry == null) continue;

                weight += entry.Weight;
                if (entry.IsBomb) bombs++;
            }

            EditorGUILayout.LabelField(
                $"{config.Slices.Count} slices · {bombs} bomb(s) · total weight {weight}",
                EditorStyles.miniLabel);

            if (config.Slices.Count != WheelModel.StandardSliceCount)
            {
                EditorGUILayout.HelpBox(
                    $"The artwork has {WheelModel.StandardSliceCount} slots but this wheel authors " +
                    $"{config.Slices.Count}.", MessageType.Error);
            }

            if (bombs > 0 && weight > 0)
            {
                int bombWeight = 0;
                for (int i = 0; i < config.Slices.Count; i++)
                {
                    WheelSliceEntry entry = config.Slices[i];
                    if (entry != null && entry.IsBomb) bombWeight += entry.Weight;
                }

                EditorGUILayout.HelpBox(
                    $"Bomb chance at these weights: {bombWeight / (float)weight:P1}", MessageType.Info);
            }
        }
    }
}
