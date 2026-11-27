using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SI81.MachineLock
{
    /// <summary>
    /// Persistent shuffle-bag selector for the three Machine Lock layout patterns (ADR-0006, Issue #41).
    /// Pure C#: no UnityEngine scene dependency so it can be unit-tested deterministically.
    /// Guarantees that every N-run cycle visits all N patterns without back-to-back repeats across
    /// bag refills or application restarts, while preserving deterministic <c>forcedPatternIndex</c>
    /// overrides for tests and QA.
    /// </summary>
    public sealed class MachineLockPatternSelector
    {
        /// <summary>Default JSON state filename stored under <c>Application.persistentDataPath</c>.</summary>
        public const string DEFAULT_FILE_NAME = "machinelock_pattern_state.json";

        /// <summary>Default number of layout patterns (PATTERN_A, PATTERN_B, PATTERN_C).</summary>
        public const int DEFAULT_PATTERN_COUNT = 3;

        private readonly string storagePath;
        private readonly int patternCount;
        private readonly Func<int, int> randomInt;
        private readonly List<int> remaining = new List<int>();

        /// <summary>
        /// Creates a selector backed by <paramref name="storagePath"/> and immediately loads any valid
        /// persisted state from disk.
        /// </summary>
        /// <param name="storagePath">Path to the local JSON state file. Null or empty disables disk I/O.</param>
        /// <param name="patternCount">Number of available patterns (clamped to at least 1).</param>
        /// <param name="randomInt">Optional random integer generator returning <c>[0, maxExclusive)</c>.</param>
        public MachineLockPatternSelector(
            string storagePath,
            int patternCount = DEFAULT_PATTERN_COUNT,
            Func<int, int> randomInt = null)
        {
            this.storagePath = storagePath;
            this.patternCount = Math.Max(1, patternCount);
            if (randomInt != null)
            {
                this.randomInt = randomInt;
            }
            else
            {
                var rng = new Random();
                this.randomInt = max => rng.Next(max);
            }

            Load();
        }

        /// <summary>Number of patterns managed by this selector.</summary>
        public int PatternCount => patternCount;

        /// <summary>Most recently drawn non-forced pattern index, or -1 if none has been drawn yet.</summary>
        public int LastPatternIndex { get; private set; } = -1;

        /// <summary>
        /// Returns the pattern index for the next run. A non-negative <paramref name="forcedPatternIndex"/>
        /// bypasses and preserves the persisted shuffle-bag state.
        /// </summary>
        /// <param name="forcedPatternIndex">Forced pattern index (0..PatternCount-1), or negative to draw.</param>
        /// <returns>Pattern index in <c>[0, PatternCount - 1]</c>.</returns>
        public int NextPattern(int forcedPatternIndex = -1)
        {
            if (forcedPatternIndex >= 0)
                return ClampIndex(forcedPatternIndex);

            if (patternCount == 1)
            {
                LastPatternIndex = 0;
                Save();
                return 0;
            }

            if (remaining.Count == 0)
                RefillBag();

            int chosen = remaining[0];
            remaining.RemoveAt(0);
            LastPatternIndex = chosen;
            Save();
            return chosen;
        }

        private void RefillBag()
        {
            remaining.Clear();
            for (int i = 0; i < patternCount; i++)
                remaining.Add(i);

            // Fisher-Yates shuffle.
            for (int i = remaining.Count - 1; i > 0; i--)
            {
                int j = NextInt(i + 1);
                int tmp = remaining[i];
                remaining[i] = remaining[j];
                remaining[j] = tmp;
            }

            // Guarantee no back-to-back repeat across bag boundaries.
            if (LastPatternIndex >= 0 && remaining.Count > 1 && remaining[0] == LastPatternIndex)
            {
                int swapIndex = 1 + NextInt(remaining.Count - 1);
                int tmp = remaining[0];
                remaining[0] = remaining[swapIndex];
                remaining[swapIndex] = tmp;
            }
        }

        private int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 1) return 0;
            int raw = randomInt(maxExclusive);
            int mod = raw % maxExclusive;
            return mod < 0 ? mod + maxExclusive : mod;
        }

        private int ClampIndex(int index)
        {
            if (index < 0) return 0;
            return index >= patternCount ? patternCount - 1 : index;
        }

        private void Load()
        {
            LastPatternIndex = -1;
            remaining.Clear();

            if (string.IsNullOrEmpty(storagePath)) return;

            string json;
            try
            {
                if (!File.Exists(storagePath)) return;
                json = File.ReadAllText(storagePath, Encoding.UTF8);
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            if (!TryParseState(json, out int parsedLast, out List<int> parsedRemaining))
                return;

            LastPatternIndex = parsedLast;
            remaining.AddRange(parsedRemaining);
        }

        private void Save()
        {
            if (string.IsNullOrEmpty(storagePath)) return;

            try
            {
                string dir = Path.GetDirectoryName(storagePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(storagePath, SerializeState(), Encoding.UTF8);
            }
            catch (IOException)
            {
                // Persistent storage is best-effort; never fail mission startup on disk I/O errors.
            }
            catch (UnauthorizedAccessException)
            {
                // Ignore read-only storage edge cases gracefully.
            }
        }

        private string SerializeState()
        {
            var sb = new StringBuilder(64);
            sb.Append("{\"lastPatternIndex\":");
            sb.Append(LastPatternIndex.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"remaining\":[");
            for (int i = 0; i < remaining.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(remaining[i].ToString(CultureInfo.InvariantCulture));
            }
            sb.Append("]}");
            return sb.ToString();
        }

        private bool TryParseState(string json, out int parsedLast, out List<int> parsedRemaining)
        {
            parsedLast = -1;
            parsedRemaining = new List<int>();
            if (string.IsNullOrWhiteSpace(json)) return false;

            if (!TryExtractIntField(json, "\"lastPatternIndex\"", out int last))
                return false;
            if (last < -1 || last >= patternCount)
                return false;

            if (!TryExtractIntArrayField(json, "\"remaining\"", parsedRemaining))
                return false;

            var seen = new HashSet<int>();
            for (int i = 0; i < parsedRemaining.Count; i++)
            {
                int val = parsedRemaining[i];
                if (val < 0 || val >= patternCount || !seen.Add(val))
                    return false;
            }

            parsedLast = last;
            return true;
        }

        private static bool TryExtractIntField(string json, string key, out int value)
        {
            value = 0;
            int keyIdx = json.IndexOf(key, StringComparison.Ordinal);
            if (keyIdx < 0) return false;
            int colonIdx = json.IndexOf(':', keyIdx + key.Length);
            if (colonIdx < 0) return false;

            int pos = colonIdx + 1;
            while (pos < json.Length && char.IsWhiteSpace(json[pos])) pos++;
            int start = pos;
            if (pos < json.Length && json[pos] == '-') pos++;
            while (pos < json.Length && char.IsDigit(json[pos])) pos++;
            if (pos == start) return false;

            return int.TryParse(
                json.Substring(start, pos - start),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value);
        }

        private static bool TryExtractIntArrayField(string json, string key, List<int> target)
        {
            int keyIdx = json.IndexOf(key, StringComparison.Ordinal);
            if (keyIdx < 0) return false;
            int openBracket = json.IndexOf('[', keyIdx + key.Length);
            if (openBracket < 0) return false;
            int closeBracket = json.IndexOf(']', openBracket + 1);
            if (closeBracket < 0) return false;

            string body = json.Substring(openBracket + 1, closeBracket - openBracket - 1).Trim();
            if (body.Length == 0) return true;

            string[] parts = body.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int item))
                    return false;
                target.Add(item);
            }
            return true;
        }
    }
}
