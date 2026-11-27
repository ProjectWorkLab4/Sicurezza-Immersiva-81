using System;
using System.Globalization;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Procedural diegetic 7-segment Digital Meter mounted on the Voltage Tester Sub-Station (Issues #45, #46).
    /// Renders a clean industrial panel-mount bezel ("Digital Meter", no non-functional buttons) and a widened
    /// backlit blue-grey LCD showing VOLTAGE (V), CURRENT (A), and POWER (W) without any scene Canvas or
    /// per-frame heap allocations. Starts unlit/OFF until powered on by <see cref="MachineLockEnergyPanel"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SI81/MachineLock/Machine Lock Digital Meter Display")]
    public sealed class MachineLockDigitalMeterDisplay : MonoBehaviour
    {
        private const int TEX_WIDTH = 512;
        private const int TEX_HEIGHT = 432;

        // Widened LCD rectangle inside the 512x432 faceplate texture (pixel coordinates, Y=0 at bottom).
        private const int LCD_LEFT = 122;
        private const int LCD_RIGHT = 476;
        private const int LCD_BOTTOM = 44;
        private const int LCD_TOP = 388;

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // Faceplate palette (clean industrial panel-mount Digital Meter).
        private static readonly Color32 ColOuterBody = new Color32(34, 34, 32, 255);
        private static readonly Color32 ColOuterHighlight = new Color32(58, 58, 55, 255);
        private static readonly Color32 ColOuterShadow = new Color32(18, 18, 17, 255);
        private static readonly Color32 ColBezel = new Color32(158, 165, 170, 255);
        private static readonly Color32 ColBezelLight = new Color32(182, 188, 192, 255);
        private static readonly Color32 ColBezelDark = new Color32(118, 124, 129, 255);
        private static readonly Color32 ColBezelText = new Color32(28, 32, 36, 255);

        // LCD palette: OFF (dark unlit glass) and ON (cool blue-grey backlit with dark-blue 7-segment ink).
        private static readonly Color32 ColLcdRecess = new Color32(82, 92, 102, 255);
        private static readonly Color32 ColLcdOffTop = new Color32(38, 45, 52, 255);
        private static readonly Color32 ColLcdOffBot = new Color32(26, 31, 36, 255);
        private static readonly Color32 ColLcdOffGhost = new Color32(46, 54, 62, 255);
        private static readonly Color32 ColLcdOnLeft = new Color32(110, 140, 170, 255);
        private static readonly Color32 ColLcdOnRight = new Color32(138, 168, 198, 255);
        private static readonly Color32 ColLcdGlow = new Color32(204, 222, 238, 255);
        private static readonly Color32 ColLcdInk = new Color32(18, 44, 122, 255);
        private static readonly Color32 ColLcdGhostOn = new Color32(114, 143, 173, 255);
        private static readonly Color32 ColLcdShadow = new Color32(94, 122, 152, 255);

        private static readonly Color OnEmissionColor = new Color(0.42f, 0.46f, 0.52f, 1f);

        // 7-segment bitmask: bit 0=A(top), 1=B(top-right), 2=C(bot-right), 3=D(bottom), 4=E(bot-left), 5=F(top-left), 6=G(mid).
        private static readonly byte[] DigitSegments =
        {
            0b0111111, // 0
            0b0000110, // 1
            0b1011011, // 2
            0b1001111, // 3
            0b1100110, // 4
            0b1101101, // 5
            0b1111101, // 6
            0b0000111, // 7
            0b1111111, // 8
            0b1101111  // 9
        };

        [Tooltip("MeshRenderer of the diegetic Digital Meter front faceplate quad.")]
        [SerializeField] private Renderer screenRenderer;

        private Texture2D displayTexture;
        private Material runtimeMaterial;
        private Color32[] baseFaceplate;
        private Color32[] workingPixels;

        private bool initialized;
        private bool lastPoweredOn;
        private string lastVoltageText;
        private string lastCurrentText;
        private string lastPowerText;

        /// <summary>true while the meter LCD backlight is powered on.</summary>
        public bool IsPoweredOn { get; private set; }
        /// <summary>Formatted voltage string currently shown on the LCD (empty while OFF).</summary>
        public string FormattedVoltage => IsPoweredOn ? (lastVoltageText ?? string.Empty) : string.Empty;
        /// <summary>Formatted current string currently shown on the LCD (empty while OFF).</summary>
        public string FormattedCurrent => IsPoweredOn ? (lastCurrentText ?? string.Empty) : string.Empty;
        /// <summary>Formatted power string currently shown on the LCD (empty while OFF).</summary>
        public string FormattedPower => IsPoweredOn ? (lastPowerText ?? string.Empty) : string.Empty;

        private void Awake()
        {
            EnsureInitialized();
            SetPoweredOff();
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                if (Application.isPlaying) Destroy(runtimeMaterial);
                else DestroyImmediate(runtimeMaterial);
                runtimeMaterial = null;
            }
            if (displayTexture != null)
            {
                if (Application.isPlaying) Destroy(displayTexture);
                else DestroyImmediate(displayTexture);
                displayTexture = null;
            }
            initialized = false;
        }

        /// <summary>
        /// Renders the unlit powered-off state (dark charcoal LCD glass, no emission).
        /// </summary>
        public void SetPoweredOff()
        {
            EnsureInitialized();
            if (!IsPoweredOn && lastVoltageText == string.Empty) return;

            IsPoweredOn = false;
            lastPoweredOn = false;
            lastVoltageText = string.Empty;
            lastCurrentText = string.Empty;
            lastPowerText = string.Empty;

            RenderOffLcd();
            ApplyMaterialState(false);
        }

        /// <summary>
        /// Powers the LCD on and renders the three electrical rows. Only redraws the GPU texture when
        /// the power state or formatted readout strings change.
        /// </summary>
        /// <param name="voltage">Simulated voltage in volts.</param>
        /// <param name="current">Simulated current in amperes.</param>
        /// <param name="power">Simulated power in watts.</param>
        public void SetPoweredOn(float voltage, float current, float power)
        {
            EnsureInitialized();

            float clampedV = Mathf.Clamp(voltage, 0f, 999f);
            float clampedA = Mathf.Clamp(current, 0f, 99.99f);
            float clampedW = Mathf.Clamp(power, 0f, 9999f);

            string vText = Mathf.RoundToInt(clampedV).ToString(CultureInfo.InvariantCulture);
            string aText = clampedA.ToString("0.00", CultureInfo.InvariantCulture);
            string wText = clampedW >= 999.95f
                ? Mathf.RoundToInt(clampedW).ToString(CultureInfo.InvariantCulture)
                : clampedW.ToString("0.0", CultureInfo.InvariantCulture);

            IsPoweredOn = true;
            if (lastPoweredOn && vText == lastVoltageText && aText == lastCurrentText && wText == lastPowerText)
                return;

            lastPoweredOn = true;
            lastVoltageText = vText;
            lastCurrentText = aText;
            lastPowerText = wText;

            RenderOnLcd(vText, aText, wText);
            ApplyMaterialState(true);
        }

        private void EnsureInitialized()
        {
            if (initialized && displayTexture != null) return;
            if (screenRenderer == null) screenRenderer = GetComponent<Renderer>();

            displayTexture = new Texture2D(TEX_WIDTH, TEX_HEIGHT, TextureFormat.RGBA32, false)
            {
                name = "TEX_DigitalMeterRuntime",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            baseFaceplate = new Color32[TEX_WIDTH * TEX_HEIGHT];
            workingPixels = new Color32[TEX_WIDTH * TEX_HEIGHT];
            BakeStaticFaceplate(baseFaceplate);

            if (screenRenderer != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                runtimeMaterial = new Material(shader) { name = "MAT_DigitalMeterRuntime" };
                runtimeMaterial.SetColor(BaseColorId, Color.white);
                runtimeMaterial.SetFloat("_Metallic", 0.05f);
                runtimeMaterial.SetFloat("_Smoothness", 0.42f);
                runtimeMaterial.SetTexture(BaseMapId, displayTexture);
                runtimeMaterial.SetTexture(MainTexId, displayTexture);
                runtimeMaterial.SetTexture(EmissionMapId, displayTexture);
                runtimeMaterial.EnableKeyword("_EMISSION");
                runtimeMaterial.SetColor(EmissionColorId, Color.black);
                screenRenderer.sharedMaterial = runtimeMaterial;
            }

            // Force first render pass.
            lastPoweredOn = false;
            lastVoltageText = null;
            lastCurrentText = null;
            lastPowerText = null;
            initialized = true;
        }

        private void ApplyMaterialState(bool poweredOn)
        {
            if (runtimeMaterial == null) return;
            runtimeMaterial.SetColor(EmissionColorId, poweredOn ? OnEmissionColor : Color.black);
        }

        // =====================================================================
        // Static Faceplate Baking (runs once per component instance)
        // =====================================================================

        private static void BakeStaticFaceplate(Color32[] pixels)
        {
            // 1. Dark charcoal outer frame with subtle top/left bevel highlight.
            for (int y = 0; y < TEX_HEIGHT; y++)
            {
                int row = y * TEX_WIDTH;
                for (int x = 0; x < TEX_WIDTH; x++)
                {
                    Color32 col = ColOuterBody;
                    if (y >= TEX_HEIGHT - 4 || x < 4) col = ColOuterHighlight;
                    else if (y < 4 || x >= TEX_WIDTH - 4) col = ColOuterShadow;
                    pixels[row + x] = col;
                }
            }

            // 2. Rounded matte light-grey inner bezel.
            FillRoundedRect(pixels, 20, 20, TEX_WIDTH - 20, TEX_HEIGHT - 20, 22, ColBezel);
            DrawRoundedRectOutline(pixels, 20, 20, TEX_WIDTH - 20, TEX_HEIGHT - 20, 22, 2, ColBezelLight, ColBezelDark);

            // 3. Recessed LCD border frame.
            FillRect(pixels, LCD_LEFT - 3, LCD_BOTTOM - 3, LCD_RIGHT + 3, LCD_TOP + 3, ColLcdRecess);

            // 4. Vertical "Digital Meter" label on the left (rotated 90 degrees CCW, bottom to top).
            DrawTextRotated90CCW(pixels, "Digital Meter", 52, 74, 4, ColBezelText);
        }

        // =====================================================================
        // Dynamic LCD Rendering (OFF / ON)
        // =====================================================================

        private void RenderOffLcd()
        {
            Array.Copy(baseFaceplate, workingPixels, workingPixels.Length);

            for (int y = LCD_BOTTOM; y < LCD_TOP; y++)
            {
                float t = (y - LCD_BOTTOM) / (float)(LCD_TOP - LCD_BOTTOM);
                Color32 bg = Color32.Lerp(ColLcdOffBot, ColLcdOffTop, t);
                int row = y * TEX_WIDTH;
                for (int x = LCD_LEFT; x < LCD_RIGHT; x++)
                    workingPixels[row + x] = bg;
            }

            // Faint unpowered ghost segments and labels for physical LCD realism.
            DrawRowLabelsAndUnits(workingPixels, ColLcdOffGhost);
            DrawSevenSegmentString(workingPixels, "888", 422, 306, 42, 70, 9, 12, ColLcdOffGhost, ColLcdOffGhost);
            DrawSevenSegmentString(workingPixels, "8.88", 422, 196, 38, 66, 8, 11, ColLcdOffGhost, ColLcdOffGhost);
            DrawSevenSegmentString(workingPixels, "888.8", 422, 86, 36, 62, 8, 10, ColLcdOffGhost, ColLcdOffGhost);

            displayTexture.SetPixels32(workingPixels);
            displayTexture.Apply(false, false);
        }

        private void RenderOnLcd(string voltageText, string currentText, string powerText)
        {
            Array.Copy(baseFaceplate, workingPixels, workingPixels.Length);

            // Cool blue-grey backlit LCD with soft right-edge side-LED glow lobes.
            for (int y = LCD_BOTTOM; y < LCD_TOP; y++)
            {
                int row = y * TEX_WIDTH;
                float dy1 = (y - 310f) / 72f;
                float dy2 = (y - 158f) / 72f;
                for (int x = LCD_LEFT; x < LCD_RIGHT; x++)
                {
                    float u = (x - LCD_LEFT) / (float)(LCD_RIGHT - LCD_LEFT);
                    Color32 baseBg = Color32.Lerp(ColLcdOnLeft, ColLcdOnRight, u * 0.7f);

                    float dx = (LCD_RIGHT - x) / 56f;
                    float lobe1 = Mathf.Exp(-(dx * dx + dy1 * dy1));
                    float lobe2 = Mathf.Exp(-(dx * dx + dy2 * dy2));
                    float glow = Mathf.Clamp01((lobe1 + lobe2) * 0.58f);

                    workingPixels[row + x] = Color32.Lerp(baseBg, ColLcdGlow, glow);
                }
            }

            // Subtle inner top/left shadow of the LCD recess.
            FillRect(workingPixels, LCD_LEFT, LCD_TOP - 3, LCD_RIGHT, LCD_TOP, ColLcdShadow);
            FillRect(workingPixels, LCD_LEFT, LCD_BOTTOM, LCD_LEFT + 3, LCD_TOP, ColLcdShadow);

            // Labels and units.
            DrawRowLabelsAndUnits(workingPixels, ColLcdInk);

            // Row 1: VOLTAGE (integer up to 3 digits, e.g. 222 V or 0 V).
            DrawSevenSegmentString(workingPixels, voltageText, 422, 306, 42, 70, 9, 12, ColLcdInk, ColLcdGhostOn, drawInactive: false);

            // Row 2: CURRENT (2 decimals, e.g. 0.00 A or 12.40 A).
            int curDigitW = currentText.Length >= 5 ? 34 : 38;
            int curGap = currentText.Length >= 5 ? 9 : 11;
            DrawSevenSegmentString(workingPixels, currentText, 422, 196, curDigitW, 66, 8, curGap, ColLcdInk, ColLcdGhostOn, drawInactive: false);

            // Row 3: POWER (0.0..999.9 or 4-digit integer e.g. 2850 W).
            int powDigitW = powerText.Length >= 5 ? 34 : 36;
            int powGap = powerText.Length >= 5 ? 9 : 10;
            DrawSevenSegmentString(workingPixels, powerText, 422, 86, powDigitW, 62, 8, powGap, ColLcdInk, ColLcdGhostOn, drawInactive: false);

            displayTexture.SetPixels32(workingPixels);
            displayTexture.Apply(false, false);
        }

        private static void DrawRowLabelsAndUnits(Color32[] pixels, Color32 color)
        {
            // Bottom-left row captions across the widened LCD.
            DrawText(pixels, "VOLTAGE", LCD_LEFT + 12, 282, 3, color);
            DrawText(pixels, "CURRENT", LCD_LEFT + 12, 172, 3, color);
            DrawText(pixels, "POWER", LCD_LEFT + 12, 62, 3, color);

            // Right-aligned unit glyphs (V, A large; W compact).
            DrawThickV(pixels, 436, 308, 26, 52, 6, color);
            DrawThickA(pixels, 436, 198, 26, 52, 6, color);
            DrawThickW(pixels, 434, 88, 30, 38, 5, color);
        }

        // =====================================================================
        // Authentic Chamfered 7-Segment Digit Renderer
        // =====================================================================

        private static void DrawSevenSegmentString(Color32[] pixels, string text, int rightX, int bottomY,
            int digitW, int digitH, int thickness, int gap, Color32 onColor, Color32 ghostColor, bool drawInactive = true)
        {
            if (string.IsNullOrEmpty(text)) return;

            int dotWidth = thickness + 4;
            int cursorRight = rightX;

            for (int i = text.Length - 1; i >= 0; i--)
            {
                char c = text[i];
                if (c == '.')
                {
                    int cx = cursorRight - dotWidth / 2;
                    FillCircle(pixels, cx, bottomY + thickness / 2 + 1, thickness / 2 + 1, onColor);
                    cursorRight -= dotWidth + gap / 2;
                    continue;
                }

                if (c < '0' || c > '9') continue;
                byte mask = DigitSegments[c - '0'];
                int leftX = cursorRight - digitW;
                DrawSevenSegmentDigit(pixels, leftX, bottomY, digitW, digitH, thickness, mask, onColor, ghostColor, drawInactive);
                cursorRight = leftX - gap;
            }
        }

        private static void DrawSevenSegmentDigit(Color32[] pixels, int x, int y, int w, int h, int t,
            byte mask, Color32 onColor, Color32 ghostColor, bool drawInactive)
        {
            int halfH = h / 2;
            int pad = 2;

            // A: top horizontal
            if ((mask & 1) != 0 || drawInactive)
                DrawHSegment(pixels, x + pad, y + h - t, w - pad * 2, t, (mask & 1) != 0 ? onColor : ghostColor);
            // G: middle horizontal
            if ((mask & 64) != 0 || drawInactive)
                DrawHSegment(pixels, x + pad, y + halfH - t / 2, w - pad * 2, t, (mask & 64) != 0 ? onColor : ghostColor);
            // D: bottom horizontal
            if ((mask & 8) != 0 || drawInactive)
                DrawHSegment(pixels, x + pad, y, w - pad * 2, t, (mask & 8) != 0 ? onColor : ghostColor);

            // F: top-left vertical
            if ((mask & 32) != 0 || drawInactive)
                DrawVSegment(pixels, x, y + halfH + pad, t, halfH - pad * 2, (mask & 32) != 0 ? onColor : ghostColor);
            // B: top-right vertical
            if ((mask & 2) != 0 || drawInactive)
                DrawVSegment(pixels, x + w - t, y + halfH + pad, t, halfH - pad * 2, (mask & 2) != 0 ? onColor : ghostColor);
            // E: bottom-left vertical
            if ((mask & 16) != 0 || drawInactive)
                DrawVSegment(pixels, x, y + pad, t, halfH - pad * 2, (mask & 16) != 0 ? onColor : ghostColor);
            // C: bottom-right vertical
            if ((mask & 4) != 0 || drawInactive)
                DrawVSegment(pixels, x + w - t, y + pad, t, halfH - pad * 2, (mask & 4) != 0 ? onColor : ghostColor);
        }

        /// <summary>Draws a chamfered hexagonal horizontal 7-segment bar.</summary>
        private static void DrawHSegment(Color32[] pixels, int x, int y, int w, int t, Color32 col)
        {
            int mid = t / 2;
            for (int dy = 0; dy < t; dy++)
            {
                int inset = Mathf.Abs(dy - mid);
                int py = y + dy;
                if (py < 0 || py >= TEX_HEIGHT) continue;
                int row = py * TEX_WIDTH;
                int startX = Mathf.Max(0, x + inset);
                int endX = Mathf.Min(TEX_WIDTH, x + w - inset);
                for (int px = startX; px < endX; px++)
                    pixels[row + px] = col;
            }
        }

        /// <summary>Draws a chamfered hexagonal vertical 7-segment bar.</summary>
        private static void DrawVSegment(Color32[] pixels, int x, int y, int t, int h, Color32 col)
        {
            int mid = t / 2;
            for (int dx = 0; dx < t; dx++)
            {
                int inset = Mathf.Abs(dx - mid);
                int px = x + dx;
                if (px < 0 || px >= TEX_WIDTH) continue;
                int startY = Mathf.Max(0, y + inset);
                int endY = Mathf.Min(TEX_HEIGHT, y + h - inset);
                for (int py = startY; py < endY; py++)
                    pixels[py * TEX_WIDTH + px] = col;
            }
        }

        // =====================================================================
        // Vector Unit Glyphs
        // =====================================================================

        private static void DrawThickV(Color32[] pixels, int x, int y, int w, int h, int stroke, Color32 col)
        {
            DrawLine(pixels, x + stroke / 2, y + h, x + w / 2, y, stroke, col);
            DrawLine(pixels, x + w - stroke / 2, y + h, x + w / 2, y, stroke, col);
        }

        private static void DrawThickA(Color32[] pixels, int x, int y, int w, int h, int stroke, Color32 col)
        {
            DrawLine(pixels, x + stroke / 2, y, x + w / 2, y + h, stroke, col);
            DrawLine(pixels, x + w - stroke / 2, y, x + w / 2, y + h, stroke, col);
            FillRect(pixels, x + w / 5, y + h / 3, x + w - w / 5, y + h / 3 + stroke, col);
        }

        private static void DrawThickW(Color32[] pixels, int x, int y, int w, int h, int stroke, Color32 col)
        {
            int q1 = x + w / 4;
            int mid = x + w / 2;
            int q3 = x + (3 * w) / 4;
            DrawLine(pixels, x + 2, y + h, q1, y, stroke, col);
            DrawLine(pixels, q1, y, mid, y + (h * 3) / 5, stroke, col);
            DrawLine(pixels, mid, y + (h * 3) / 5, q3, y, stroke, col);
            DrawLine(pixels, q3, y, x + w - 2, y + h, stroke, col);
        }

        // =====================================================================
        // Crisp 5x7 Bitmap Font (Horizontal + 90° CCW Vertical)
        // =====================================================================

        private static void DrawText(Color32[] pixels, string text, int x, int y, int scale, Color32 col)
        {
            int cursorX = x;
            for (int i = 0; i < text.Length; i++)
            {
                byte[] glyph = GetGlyph(text[i]);
                for (int r = 0; r < 7; r++)
                {
                    byte bits = glyph[r];
                    for (int c = 0; c < 5; c++)
                    {
                        if ((bits & (1 << (4 - c))) == 0) continue;
                        int px = cursorX + c * scale;
                        int py = y + (6 - r) * scale;
                        FillRect(pixels, px, py, px + scale, py + scale, col);
                    }
                }
                cursorX += 6 * scale;
            }
        }

        private static void DrawTextRotated90CCW(Color32[] pixels, string text, int x, int y, int scale, Color32 col)
        {
            int cursorY = y;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == ' ')
                {
                    cursorY += 4 * scale;
                    continue;
                }
                byte[] glyph = GetGlyph(ch);
                for (int r = 0; r < 7; r++)
                {
                    byte bits = glyph[r];
                    for (int c = 0; c < 5; c++)
                    {
                        if ((bits & (1 << (4 - c))) == 0) continue;
                        // 90° CCW: glyph column c goes upward (+Y), glyph row r goes rightward (+X).
                        int px = x + r * scale;
                        int py = cursorY + c * scale;
                        FillRect(pixels, px, py, px + scale, py + scale, col);
                    }
                }
                cursorY += 6 * scale;
            }
        }

        private static readonly byte[] GlyphBlank = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private static readonly byte[] GlyphA = { 0x0E, 0x11, 0x11, 0x1F, 0x11, 0x11, 0x11 };
        private static readonly byte[] GlyphC = { 0x0E, 0x11, 0x10, 0x10, 0x10, 0x11, 0x0E };
        private static readonly byte[] GlyphD = { 0x1E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x1E };
        private static readonly byte[] GlyphE = { 0x1F, 0x10, 0x10, 0x1E, 0x10, 0x10, 0x1F };
        private static readonly byte[] GlyphG = { 0x0E, 0x11, 0x10, 0x17, 0x11, 0x11, 0x0F };
        private static readonly byte[] GlyphL = { 0x10, 0x10, 0x10, 0x10, 0x10, 0x10, 0x1F };
        private static readonly byte[] GlyphM = { 0x11, 0x1B, 0x15, 0x15, 0x11, 0x11, 0x11 };
        private static readonly byte[] GlyphN = { 0x11, 0x19, 0x15, 0x13, 0x11, 0x11, 0x11 };
        private static readonly byte[] GlyphO = { 0x0E, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E };
        private static readonly byte[] GlyphP = { 0x1E, 0x11, 0x11, 0x1E, 0x10, 0x10, 0x10 };
        private static readonly byte[] GlyphR = { 0x1E, 0x11, 0x11, 0x1E, 0x14, 0x12, 0x11 };
        private static readonly byte[] GlyphT = { 0x1F, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04 };
        private static readonly byte[] GlyphU = { 0x11, 0x11, 0x11, 0x11, 0x11, 0x11, 0x0E };
        private static readonly byte[] GlyphV = { 0x11, 0x11, 0x11, 0x11, 0x11, 0x0A, 0x04 };
        private static readonly byte[] GlyphW = { 0x11, 0x11, 0x11, 0x15, 0x15, 0x1B, 0x11 };
        private static readonly byte[] GlyphLowerA = { 0x00, 0x00, 0x0E, 0x01, 0x0F, 0x11, 0x0F };
        private static readonly byte[] GlyphLowerE = { 0x00, 0x00, 0x0E, 0x11, 0x1F, 0x10, 0x0E };
        private static readonly byte[] GlyphLowerG = { 0x00, 0x00, 0x0F, 0x11, 0x0F, 0x01, 0x0E };
        private static readonly byte[] GlyphLowerI = { 0x04, 0x00, 0x0C, 0x04, 0x04, 0x04, 0x0E };
        private static readonly byte[] GlyphLowerL = { 0x0C, 0x04, 0x04, 0x04, 0x04, 0x04, 0x0E };
        private static readonly byte[] GlyphLowerR = { 0x00, 0x00, 0x16, 0x19, 0x10, 0x10, 0x10 };
        private static readonly byte[] GlyphLowerT = { 0x04, 0x04, 0x1F, 0x04, 0x04, 0x05, 0x02 };

        private static byte[] GetGlyph(char c)
        {
            switch (c)
            {
                case 'A': return GlyphA;
                case 'C': return GlyphC;
                case 'D': return GlyphD;
                case 'E': return GlyphE;
                case 'G': return GlyphG;
                case 'L': return GlyphL;
                case 'M': return GlyphM;
                case 'N': return GlyphN;
                case 'O': return GlyphO;
                case 'P': return GlyphP;
                case 'R': return GlyphR;
                case 'T': return GlyphT;
                case 'U': return GlyphU;
                case 'V': return GlyphV;
                case 'W': return GlyphW;
                case 'a': return GlyphLowerA;
                case 'e': return GlyphLowerE;
                case 'g': return GlyphLowerG;
                case 'i': return GlyphLowerI;
                case 'l': return GlyphLowerL;
                case 'r': return GlyphLowerR;
                case 't': return GlyphLowerT;
                default:  return GlyphBlank;
            }
        }

        // =====================================================================
        // Pixel Drawing Primitives
        // =====================================================================

        private static void FillRect(Color32[] pixels, int x0, int y0, int x1, int y1, Color32 col)
        {
            int sx = Mathf.Max(0, x0);
            int ex = Mathf.Min(TEX_WIDTH, x1);
            int sy = Mathf.Max(0, y0);
            int ey = Mathf.Min(TEX_HEIGHT, y1);
            for (int y = sy; y < ey; y++)
            {
                int row = y * TEX_WIDTH;
                for (int x = sx; x < ex; x++)
                    pixels[row + x] = col;
            }
        }

        private static void FillRoundedRect(Color32[] pixels, int x0, int y0, int x1, int y1, int r, Color32 col)
        {
            int r2 = r * r;
            int sx = Mathf.Max(0, x0);
            int ex = Mathf.Min(TEX_WIDTH, x1);
            int sy = Mathf.Max(0, y0);
            int ey = Mathf.Min(TEX_HEIGHT, y1);
            for (int y = sy; y < ey; y++)
            {
                int row = y * TEX_WIDTH;
                for (int x = sx; x < ex; x++)
                {
                    int dx = x < x0 + r ? (x0 + r - x) : (x >= x1 - r ? (x - (x1 - r - 1)) : 0);
                    int dy = y < y0 + r ? (y0 + r - y) : (y >= y1 - r ? (y - (y1 - r - 1)) : 0);
                    if (dx * dx + dy * dy <= r2) pixels[row + x] = col;
                }
            }
        }

        private static void DrawRoundedRectOutline(Color32[] pixels, int x0, int y0, int x1, int y1, int r,
            int thickness, Color32 topLeftCol, Color32 botRightCol)
        {
            int rOut2 = r * r;
            int rIn = Mathf.Max(0, r - thickness);
            int rIn2 = rIn * rIn;
            int sx = Mathf.Max(0, x0);
            int ex = Mathf.Min(TEX_WIDTH, x1);
            int sy = Mathf.Max(0, y0);
            int ey = Mathf.Min(TEX_HEIGHT, y1);
            for (int y = sy; y < ey; y++)
            {
                int row = y * TEX_WIDTH;
                for (int x = sx; x < ex; x++)
                {
                    int dx = x < x0 + r ? (x0 + r - x) : (x >= x1 - r ? (x - (x1 - r - 1)) : 0);
                    int dy = y < y0 + r ? (y0 + r - y) : (y >= y1 - r ? (y - (y1 - r - 1)) : 0);
                    int d2 = dx * dx + dy * dy;
                    bool inOuter = d2 <= rOut2;
                    bool onStraightEdge = (x < x0 + thickness || x >= x1 - thickness || y < y0 + thickness || y >= y1 - thickness);
                    bool onCornerEdge = (dx > 0 && dy > 0 && d2 >= rIn2 && d2 <= rOut2);
                    if (inOuter && (onStraightEdge || onCornerEdge))
                    {
                        bool topLeft = (y >= (y0 + y1) / 2) || (x < (x0 + x1) / 2 && y >= y0 + thickness);
                        pixels[row + x] = topLeft ? topLeftCol : botRightCol;
                    }
                }
            }
        }

        private static void FillCircle(Color32[] pixels, int cx, int cy, int r, Color32 col)
        {
            int r2 = r * r;
            int sx = Mathf.Max(0, cx - r);
            int ex = Mathf.Min(TEX_WIDTH - 1, cx + r);
            int sy = Mathf.Max(0, cy - r);
            int ey = Mathf.Min(TEX_HEIGHT - 1, cy + r);
            for (int y = sy; y <= ey; y++)
            {
                int dy = y - cy;
                int row = y * TEX_WIDTH;
                for (int x = sx; x <= ex; x++)
                {
                    int dx = x - cx;
                    if (dx * dx + dy * dy <= r2) pixels[row + x] = col;
                }
            }
        }

        private static void DrawLine(Color32[] pixels, int x0, int y0, int x1, int y1, int thickness, Color32 col)
        {
            int steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            int half = Mathf.Max(1, thickness / 2);
            if (steps == 0)
            {
                FillCircle(pixels, x0, y0, half, col);
                return;
            }
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                int px = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int py = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                FillCircle(pixels, px, py, half, col);
            }
        }
    }
}
