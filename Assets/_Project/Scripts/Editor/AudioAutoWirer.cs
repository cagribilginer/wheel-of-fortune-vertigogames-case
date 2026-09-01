using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Vertigo.Wheel.Data.Configs;

namespace Vertigo.Wheel.Editor
{
    /// <summary>
    /// Wires imported SFX onto <see cref="AudioLibrary"/> and the three <see cref="WheelThemeConfig"/>
    /// assets with no Inspector work.
    /// <para>
    /// The free packs people drop in here (Dustyroom's <c>DM-CGS-##</c>, Kenney, freesound dumps) have
    /// opaque file names, so matching on the name alone does not work. Instead every candidate clip is
    /// decoded from disk and reduced to a handful of acoustic features — length, loudness, crest factor,
    /// attack, low-frequency ratio, zero-crossing rate, sustain — and each library slot scores every clip
    /// against a target profile. A short sharp transient with almost no low end scores as a tick; a long,
    /// bass-heavy, sustained bed scores as the defeat drone; and so on. Names still help when they carry a
    /// real keyword ("menu-click", "explosion"), just as a bonus on top of the acoustic score.
    /// </para>
    /// <para>
    /// Runs three ways: the <c>Tools/Vertigo/Audio</c> menu items, and automatically (gap-fill only) the
    /// first time audio is imported while any slot is still empty — so a freshly imported pack is wired
    /// without anyone opening an Inspector.
    /// </para>
    /// </summary>
    public static class AudioAutoWirer
    {
        private const string LibraryProp_ButtonClick = "_buttonClick";
        private const string LibraryProp_PopupOpen = "_popupOpen";
        private const string LibraryProp_PopupClose = "_popupClose";
        private const string LibraryProp_RewardChime = "_rewardChime";
        private const string LibraryProp_BombExplosion = "_bombExplosion";
        private const string LibraryProp_DefeatAmbience = "_defeatAmbience";
        private const string ThemeProp_Tick = "_tick";

        // Anything at or below this score is treated as "no real match" and the slot is left as it is
        // rather than forced onto a clip that does not fit.
        private const float MinScore = 0.12f;

        // Folders whose audio is decoration for the toolchain, never game SFX.
        private static readonly string[] ExcludedPathFragments =
        {
            "/TextMesh Pro/", "/Editor/", "/Editor Default Resources/",
        };

        [MenuItem("Tools/Vertigo/Audio/Auto Wire Audio")]
        public static void WireFromMenu() => Run(apply: true, force: true, silentWhenIdle: false);

        [MenuItem("Tools/Vertigo/Audio/Auto Wire Audio (Preview)")]
        public static void PreviewFromMenu() => Run(apply: false, force: true, silentWhenIdle: false);

        /// <summary>
        /// Gap-fill pass for the import hook: only touches slots that are currently empty, and does nothing
        /// (and says nothing) when every slot is already wired.
        /// </summary>
        internal static void WireGapsAfterImport() => Run(apply: true, force: false, silentWhenIdle: true);

        private static void Run(bool apply, bool force, bool silentWhenIdle)
        {
            SerializedObject library = LoadLibrary();
            if (library == null)
            {
                if (!silentWhenIdle)
                    Debug.LogWarning("[Vertigo] AudioAutoWirer: no AudioLibrary asset found; nothing to wire.");
                return;
            }

            List<SerializedObject> themes = LoadThemes();

            var slots = new List<Slot>
            {
                new Slot("Wheel Tick", ScoreTick),
                new Slot("Button Click", ScoreButtonClick),
                new Slot("Reward Chime", ScoreRewardChime),
                new Slot("Bomb Explosion", ScoreBombExplosion),
                new Slot("Popup Open", ScorePopupOpen),
                new Slot("Popup Close", ScorePopupClose),
                new Slot("Defeat Ambience", ScoreDefeatAmbience),
            };

            // In gap-fill mode, a slot that already holds a clip is off the table entirely.
            if (!force)
            {
                slots.RemoveAll(s => CurrentClipFor(s.Label, library, themes) != null);
                if (slots.Count == 0)
                {
                    if (!silentWhenIdle)
                        Debug.Log("[Vertigo] AudioAutoWirer: every audio slot is already wired.");
                    return;
                }
            }

            List<ClipFeatures> clips = LoadAndAnalyseClips();
            if (clips.Count == 0)
            {
                if (!silentWhenIdle)
                    Debug.LogWarning(
                        "[Vertigo] AudioAutoWirer: found no importable AudioClips to match against. " +
                        "Import an SFX pack under Assets/ and try again.");
                return;
            }

            Dictionary<Slot, ClipFeatures> plan = BuildPlan(slots, clips);

            var report = new StringBuilder();
            report.AppendLine(apply
                ? "[Vertigo] AudioAutoWirer — wiring:"
                : "[Vertigo] AudioAutoWirer — preview (no changes written):");

            int changed = 0;
            foreach (Slot slot in slots)
            {
                if (!plan.TryGetValue(slot, out ClipFeatures pick))
                {
                    report.AppendLine($"  {slot.Label,-16} -> (no clip scored above {MinScore:0.00}; left unchanged)");
                    continue;
                }

                report.AppendLine($"  {slot.Label,-16} -> {pick.Name}   (score {slot.ScoreOf(pick):0.00}, {pick.Summary()})");

                if (!apply) continue;

                if (slot.Label == "Wheel Tick")
                {
                    foreach (SerializedObject theme in themes)
                        changed += SetClip(theme, ThemeProp_Tick, pick.Clip) ? 1 : 0;
                }
                else
                {
                    changed += SetClip(library, LibraryPropFor(slot.Label), pick.Clip) ? 1 : 0;
                }
            }

            if (apply && changed > 0)
            {
                library.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(library.targetObject);
                foreach (SerializedObject theme in themes)
                {
                    theme.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(theme.targetObject);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            report.AppendLine(apply
                ? $"  {changed} ScriptableObject field(s) updated and saved."
                : "  Run \"Tools/Vertigo/Audio/Auto Wire Audio\" to apply.");

            Debug.Log(report.ToString());
        }

        // ------------------------------------------------------------------ planning

        /// <summary>
        /// Greedy global assignment: take the strongest (slot, clip) pair still available, lock it in, and
        /// repeat. One clip never fills two slots, so the reward chime and the defeat drone can't collapse
        /// onto the same file just because both like "long and tonal".
        /// </summary>
        private static Dictionary<Slot, ClipFeatures> BuildPlan(List<Slot> slots, List<ClipFeatures> clips)
        {
            var pairs = new List<(Slot slot, ClipFeatures clip, float score)>();
            foreach (Slot slot in slots)
                foreach (ClipFeatures clip in clips)
                {
                    float score = slot.ScoreOf(clip);
                    if (score > MinScore) pairs.Add((slot, clip, score));
                }

            pairs.Sort((a, b) => b.score.CompareTo(a.score));

            var plan = new Dictionary<Slot, ClipFeatures>();
            var usedClips = new HashSet<string>();

            foreach ((Slot slot, ClipFeatures clip, float _) in pairs)
            {
                if (plan.ContainsKey(slot) || usedClips.Contains(clip.Path)) continue;
                plan[slot] = clip;
                usedClips.Add(clip.Path);
            }

            return plan;
        }

        // ------------------------------------------------------------------ slot profiles
        //
        // Every scorer returns roughly 0..1. Acoustic terms are weighted to sum to ~1; a matching name
        // keyword adds a flat bonus on top so a well-named clip beats an equally-fitting unnamed one.

        private static float ScoreTick(ClipFeatures c) =>
            0.45f * Band(c.Length, 0.02f, 0.14f, 0.12f) +
            0.25f * AtLeast(c.Crest, 3.5f, 3f) +
            0.15f * AtMost(c.LowRatio, 0.35f, 0.3f) +
            0.15f * AtMost(c.AttackSeconds, 0.02f, 0.03f) +
            Keyword(c, 0.35f, "tick", "click", "tap", "blip", "select");

        private static float ScoreButtonClick(ClipFeatures c) =>
            0.40f * Band(c.Length, 0.05f, 0.40f, 0.2f) +
            0.20f * AtLeast(c.Crest, 3f, 3f) +
            0.15f * AtMost(c.LowRatio, 0.5f, 0.3f) +
            0.15f * Band(c.ZeroCrossingRate, 900f, 6000f, 3500f) +
            0.10f * AtMost(c.SustainRatio, 0.55f, 0.3f) +
            Keyword(c, 0.5f, "button", "click", "ui", "menu", "tap", "select", "press", "confirm");

        private static float ScoreRewardChime(ClipFeatures c) =>
            0.30f * Band(c.Length, 0.35f, 2.2f, 0.6f) +
            0.30f * AtMost(c.ZeroCrossingRate, 2600f, 2200f) +
            0.20f * AtMost(c.LowRatio, 0.42f, 0.3f) +
            0.20f * AtMost(c.SustainRatio, 0.72f, 0.3f) +
            Keyword(c, 0.5f, "reward", "win", "coin", "collect", "chime", "success", "pickup", "prize", "star", "bonus", "positive");

        private static float ScoreBombExplosion(ClipFeatures c) =>
            0.38f * AtLeast(c.LowRatio, 0.45f, 0.3f) +
            0.20f * Band(c.Length, 0.4f, 2.5f, 0.7f) +
            0.16f * AtLeast(c.Rms, 0.12f, 0.15f) +
            0.16f * AtLeast(c.ZeroCrossingRate, 1400f, 2000f) +
            0.10f * AtLeast(c.Length, 0.5f, 0.4f) +
            Keyword(c, 0.5f, "explos", "bomb", "blast", "boom", "hit", "impact", "fail", "damage", "hurt");

        private static float ScorePopupOpen(ClipFeatures c) =>
            0.8f * Whoosh(c) + 0.2f * Rise(c) +
            Keyword(c, 0.4f, "open", "swoosh", "whoosh", "swipe", "transition", "appear", "reveal", "slide");

        private static float ScorePopupClose(ClipFeatures c) =>
            0.8f * Whoosh(c) + 0.2f * (1f - Rise(c)) +
            Keyword(c, 0.4f, "close", "swoosh", "whoosh", "swipe", "transition", "hide", "dismiss");

        private static float ScoreDefeatAmbience(ClipFeatures c) =>
            0.35f * AtLeast(c.Length, 1.5f, 1.0f) +
            0.30f * AtLeast(c.SustainRatio, 0.55f, 0.3f) +
            0.20f * AtLeast(c.LowRatio, 0.35f, 0.3f) +
            0.15f * AtMost(c.Crest, 4f, 3f) +
            Keyword(c, 0.5f, "drone", "ambien", "tension", "defeat", "lose", "lost", "gameover", "dark", "negative", "ominous", "sad");

        private static float Whoosh(ClipFeatures c) =>
            0.35f * AtLeast(c.ZeroCrossingRate, 2000f, 2500f) +
            0.25f * AtMost(c.Crest, 4.5f, 3f) +
            0.25f * Band(c.Length, 0.1f, 0.7f, 0.3f) +
            0.15f * Band(c.LowRatio, 0.12f, 0.6f, 0.3f);

        // 1 when the second half is brighter than the first (rising sweep), 0 when it is darker.
        private static float Rise(ClipFeatures c)
        {
            float denom = c.ZcrFirstHalf + c.ZcrSecondHalf + 1f;
            return Mathf.Clamp01(0.5f + 0.5f * ((c.ZcrSecondHalf - c.ZcrFirstHalf) / denom) * 4f);
        }

        // ------------------------------------------------------------------ scoring helpers

        private static float Band(float x, float lo, float hi, float soft)
        {
            if (x >= lo && x <= hi) return 1f;
            float d = x < lo ? lo - x : x - hi;
            return Mathf.Clamp01(1f - d / Mathf.Max(soft, 1e-4f));
        }

        private static float AtMost(float x, float threshold, float soft) =>
            x <= threshold ? 1f : Mathf.Clamp01(1f - (x - threshold) / Mathf.Max(soft, 1e-4f));

        private static float AtLeast(float x, float threshold, float soft) =>
            x >= threshold ? 1f : Mathf.Clamp01(1f - (threshold - x) / Mathf.Max(soft, 1e-4f));

        private static float Keyword(ClipFeatures c, float bonus, params string[] needles)
        {
            foreach (string n in needles)
                if (c.Haystack.Contains(n)) return bonus;
            return 0f;
        }

        // ------------------------------------------------------------------ asset access

        private static SerializedObject LoadLibrary()
        {
            var asset = FindSingle<AudioLibrary>("Assets/Resources/Configs/Settings/AudioLibrary.asset");
            return asset == null ? null : new SerializedObject(asset);
        }

        private static List<SerializedObject> LoadThemes()
        {
            var themes = new List<SerializedObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:WheelThemeConfig"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<WheelThemeConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) themes.Add(new SerializedObject(asset));
            }
            return themes;
        }

        private static T FindSingle<T>(string preferredPath) where T : ScriptableObject
        {
            var atPath = AssetDatabase.LoadAssetAtPath<T>(preferredPath);
            if (atPath != null) return atPath;

            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length == 0
                ? null
                : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static string LibraryPropFor(string slotLabel)
        {
            switch (slotLabel)
            {
                case "Button Click": return LibraryProp_ButtonClick;
                case "Reward Chime": return LibraryProp_RewardChime;
                case "Bomb Explosion": return LibraryProp_BombExplosion;
                case "Popup Open": return LibraryProp_PopupOpen;
                case "Popup Close": return LibraryProp_PopupClose;
                case "Defeat Ambience": return LibraryProp_DefeatAmbience;
                default: throw new ArgumentOutOfRangeException(nameof(slotLabel), slotLabel, "Not a library slot.");
            }
        }

        private static AudioClip CurrentClipFor(string slotLabel, SerializedObject library, List<SerializedObject> themes)
        {
            if (slotLabel == "Wheel Tick")
                return themes.Count == 0 ? null : themes[0].FindProperty(ThemeProp_Tick).objectReferenceValue as AudioClip;

            return library.FindProperty(LibraryPropFor(slotLabel)).objectReferenceValue as AudioClip;
        }

        private static bool SetClip(SerializedObject target, string prop, AudioClip clip)
        {
            SerializedProperty p = target.FindProperty(prop);
            if (p == null || p.objectReferenceValue == clip) return false;
            p.objectReferenceValue = clip;
            return true;
        }

        // ------------------------------------------------------------------ clip loading + analysis

        private static List<ClipFeatures> LoadAndAnalyseClips()
        {
            var features = new List<ClipFeatures>();

            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith("Packages/")) continue;
                if (ExcludedPathFragments.Any(path.Contains)) continue;

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;

                float[] mono;
                int sampleRate;
                if (!TryReadWavMono(path, out mono, out sampleRate))
                {
                    if (!TryReadClipMono(clip, out mono, out sampleRate)) continue;
                }

                if (mono.Length < 64) continue;
                features.Add(ClipFeatures.Analyse(clip, path, mono, sampleRate));
            }

            return features;
        }

        /// <summary>Decodes a canonical PCM .wav straight off disk — no dependence on import settings.</summary>
        private static bool TryReadWavMono(string path, out float[] mono, out int sampleRate)
        {
            mono = null;
            sampleRate = 0;

            if (!path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) return false;

            byte[] bytes;
            try { bytes = File.ReadAllBytes(path); }
            catch { return false; }

            if (bytes.Length < 44) return false;
            if (bytes[0] != 'R' || bytes[1] != 'I' || bytes[2] != 'F' || bytes[3] != 'F') return false;
            if (bytes[8] != 'W' || bytes[9] != 'A' || bytes[10] != 'V' || bytes[11] != 'E') return false;

            int channels = 0, bitsPerSample = 0, format = 0;
            int dataOffset = -1, dataLength = 0;

            int p = 12;
            while (p + 8 <= bytes.Length)
            {
                string id = Encoding.ASCII.GetString(bytes, p, 4);
                int size = BitConverter.ToInt32(bytes, p + 4);
                int body = p + 8;
                if (size < 0 || body + size > bytes.Length) size = bytes.Length - body;

                if (id == "fmt ")
                {
                    format = BitConverter.ToUInt16(bytes, body);
                    channels = BitConverter.ToUInt16(bytes, body + 2);
                    sampleRate = BitConverter.ToInt32(bytes, body + 4);
                    bitsPerSample = BitConverter.ToUInt16(bytes, body + 14);
                }
                else if (id == "data")
                {
                    dataOffset = body;
                    dataLength = size;
                }

                p = body + size + (size & 1); // chunks are word-aligned
            }

            if (dataOffset < 0 || channels < 1 || sampleRate < 1) return false;
            if (format != 1 && format != 0xFFFE) return false; // PCM / extensible-PCM only
            int bytesPerSample = bitsPerSample / 8;
            if (bytesPerSample < 1 || bytesPerSample > 4) return false;

            int frameSize = bytesPerSample * channels;
            int frames = dataLength / frameSize;
            const int maxFrames = 44100 * 4; // 4s is plenty to characterise a one-shot
            int used = Mathf.Min(frames, maxFrames);

            mono = new float[used];
            for (int f = 0; f < used; f++)
            {
                float sum = 0f;
                int baseIdx = dataOffset + f * frameSize;
                for (int ch = 0; ch < channels; ch++)
                    sum += ReadSample(bytes, baseIdx + ch * bytesPerSample, bytesPerSample);
                mono[f] = sum / channels;
            }

            return true;
        }

        private static float ReadSample(byte[] b, int i, int bytesPerSample)
        {
            switch (bytesPerSample)
            {
                case 1: return (b[i] - 128) / 128f;                                  // 8-bit is unsigned
                case 2: return (short)(b[i] | (b[i + 1] << 8)) / 32768f;
                case 3:
                    int v24 = b[i] | (b[i + 1] << 8) | (b[i + 2] << 16);
                    if ((v24 & 0x800000) != 0) v24 |= unchecked((int)0xFF000000);
                    return v24 / 8388608f;
                case 4: return BitConverter.ToInt32(b, i) / 2147483648f;
                default: return 0f;
            }
        }

        /// <summary>Fallback for non-wav clips (e.g. .ogg): ask Unity for the decoded samples.</summary>
        private static bool TryReadClipMono(AudioClip clip, out float[] mono, out int sampleRate)
        {
            mono = null;
            sampleRate = clip.frequency;

            if (clip.samples <= 0 || clip.channels <= 0) return false;

            clip.LoadAudioData();
            var interleaved = new float[clip.samples * clip.channels];
            if (!clip.GetData(interleaved, 0)) return false;

            int frames = Mathf.Min(clip.samples, clip.frequency * 4);
            mono = new float[frames];
            for (int f = 0; f < frames; f++)
            {
                float sum = 0f;
                for (int ch = 0; ch < clip.channels; ch++) sum += interleaved[f * clip.channels + ch];
                mono[f] = sum / clip.channels;
            }

            return true;
        }

        // ------------------------------------------------------------------ types

        private sealed class Slot
        {
            public readonly string Label;
            private readonly Func<ClipFeatures, float> _score;

            public Slot(string label, Func<ClipFeatures, float> score)
            {
                Label = label;
                _score = score;
            }

            public float ScoreOf(ClipFeatures c) => _score(c);
        }

        private sealed class ClipFeatures
        {
            public AudioClip Clip;
            public string Path;
            public string Name;
            public string Haystack;      // lowercased path, for keyword bonuses

            public float Length;
            public float Peak;
            public float Rms;
            public float Crest;          // peak / rms — high for transients, ~1 for sustained tone
            public float AttackSeconds;
            public float LowRatio;       // energy below ~180 Hz / total energy
            public float ZeroCrossingRate;
            public float ZcrFirstHalf;
            public float ZcrSecondHalf;
            public float SustainRatio;   // fraction of the body that stays loud

            public string Summary() =>
                $"{Length:0.00}s, crest {Crest:0.0}, low {LowRatio:0.00}, zcr {ZeroCrossingRate:0}, sustain {SustainRatio:0.00}";

            public static ClipFeatures Analyse(AudioClip clip, string path, float[] x, int sampleRate)
            {
                int n = x.Length;

                float peak = 0f;
                double sumSq = 0.0;
                for (int i = 0; i < n; i++)
                {
                    float a = Mathf.Abs(x[i]);
                    if (a > peak) peak = a;
                    sumSq += (double)x[i] * x[i];
                }
                float rms = (float)Math.Sqrt(sumSq / Math.Max(1, n));

                // Attack: first sample reaching half the peak.
                int attackIdx = 0;
                for (int i = 0; i < n; i++)
                {
                    if (Mathf.Abs(x[i]) >= 0.5f * peak) { attackIdx = i; break; }
                }

                // One-pole low-pass at ~180 Hz; ratio of low-passed energy to total.
                float rc = 1f / (2f * Mathf.PI * 180f);
                float dt = 1f / sampleRate;
                float alpha = dt / (rc + dt);
                float lp = 0f;
                double lowSq = 0.0;
                for (int i = 0; i < n; i++)
                {
                    lp += alpha * (x[i] - lp);
                    lowSq += (double)lp * lp;
                }
                float lowRatio = sumSq > 1e-12 ? (float)(lowSq / sumSq) : 0f;

                // Zero-crossing rate over the whole clip and over each half.
                float zcrAll = ZeroCrossings(x, 0, n) / Mathf.Max(Seconds(n, sampleRate), 1e-4f);
                int half = n / 2;
                float zcr1 = ZeroCrossings(x, 0, half) / Mathf.Max(Seconds(half, sampleRate), 1e-4f);
                float zcr2 = ZeroCrossings(x, half, n) / Mathf.Max(Seconds(n - half, sampleRate), 1e-4f);

                // Sustain: windowed RMS, trimmed to the loud body, fraction of windows still above 30% of the
                // loudest window.
                float sustain = ComputeSustain(x, sampleRate);

                return new ClipFeatures
                {
                    Clip = clip,
                    Path = path,
                    Name = System.IO.Path.GetFileNameWithoutExtension(path),
                    Haystack = path.ToLowerInvariant(),
                    Length = Seconds(n, sampleRate),
                    Peak = peak,
                    Rms = rms,
                    Crest = rms > 1e-6f ? peak / rms : 1f,
                    AttackSeconds = attackIdx / (float)sampleRate,
                    LowRatio = lowRatio,
                    ZeroCrossingRate = zcrAll,
                    ZcrFirstHalf = zcr1,
                    ZcrSecondHalf = zcr2,
                    SustainRatio = sustain,
                };
            }

            private static float Seconds(int samples, int sampleRate) => samples / (float)sampleRate;

            private static int ZeroCrossings(float[] x, int from, int to)
            {
                int count = 0;
                for (int i = from + 1; i < to; i++)
                    if ((x[i - 1] < 0f) != (x[i] < 0f)) count++;
                return count;
            }

            private static float ComputeSustain(float[] x, int sampleRate)
            {
                int win = Mathf.Max(256, sampleRate / 100); // ~10 ms
                int hop = win / 2;
                var windows = new List<float>();

                for (int start = 0; start + win <= x.Length; start += hop)
                {
                    double s = 0.0;
                    for (int i = start; i < start + win; i++) s += (double)x[i] * x[i];
                    windows.Add((float)Math.Sqrt(s / win));
                }
                if (windows.Count == 0) return 0f;

                float loudest = windows.Max();
                if (loudest < 1e-5f) return 0f;

                int first = windows.FindIndex(w => w > 0.1f * loudest);
                int last = windows.FindLastIndex(w => w > 0.1f * loudest);
                if (first < 0 || last <= first) return 0f;

                int body = last - first + 1;
                int loud = 0;
                for (int i = first; i <= last; i++)
                    if (windows[i] > 0.3f * loudest) loud++;

                return loud / (float)body;
            }
        }
    }
}
