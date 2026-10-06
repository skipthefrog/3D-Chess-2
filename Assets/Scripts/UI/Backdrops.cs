using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The scenery the board floats in. Each backdrop is a 360° sky around the camera.
/// Add a new one by adding a value to Kind, a name in DisplayName and a case in Build.
/// </summary>
public static class Backdrops
{
    public enum Kind
    {
        NeonGlow = 0,
        DeepSpace = 1,
    }

    private const string PrefKey = "Backdrop";

    public static Kind Current
    {
        get => (Kind)PlayerPrefs.GetInt(PrefKey, (int)Kind.DeepSpace);
        set
        {
            PlayerPrefs.SetInt(PrefKey, (int)value);
            PlayerPrefs.Save();
        }
    }

    public static string DisplayName(Kind kind)
    {
        switch (kind)
        {
            case Kind.DeepSpace: return "Deep Space";
            default: return "Neon Glow";
        }
    }

    private static Material spaceSky;
    private static Task<Color32[]> spaceTask;
    private const int SpaceWidth = 2048, SpaceHeight = 1024;

    /// <summary>
    /// Apply the chosen backdrop to a camera. Returns false while a sky is still being
    /// generated; call again next frame (the plain neon color shows meanwhile).
    /// </summary>
    public static bool Apply(Camera cam, Kind kind)
    {
        if (kind == Kind.NeonGlow)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = NeonTheme.Ground;
            RenderSettings.skybox = null;
            if (probe != null)
            {
                probe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;
                probe.backgroundColor = NeonTheme.Ground;
                probe.RenderProbe();
                UseProbeAsEnvironment();
            }
            return true;
        }

        if (spaceSky == null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(4, 2, 14, 255);

            // Generate the texture off the main thread so the game doesn't hitch
            if (spaceTask == null) spaceTask = Task.Run(() => SpaceSky.Generate(SpaceWidth, SpaceHeight, 20261006));
            if (!spaceTask.IsCompleted) return false;
            if (spaceTask.IsFaulted)
            {
                Debug.LogError($"Backdrops: space sky failed: {spaceTask.Exception}");
                spaceTask = null;
                return true;
            }

            var tex = new Texture2D(SpaceWidth, SpaceHeight, TextureFormat.RGBA32, true);
            tex.wrapModeU = TextureWrapMode.Repeat;
            tex.wrapModeV = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Trilinear;
            tex.SetPixels32(spaceTask.Result);
            tex.Apply(true, true);

            Shader shader = Shader.Find("Skybox/Panoramic");
            if (shader == null)
            {
                Debug.LogError("Backdrops: Skybox/Panoramic shader missing from build");
                return true;
            }
            spaceSky = new Material(shader);
            spaceSky.SetTexture("_MainTex", tex);
            spaceSky.SetFloat("_Mapping", 1f);   // latitude-longitude layout
            spaceSky.SetFloat("_ImageType", 0f); // 360°
            spaceSky.SetFloat("_Exposure", 1f);
        }

        RenderSettings.skybox = spaceSky;
        cam.clearFlags = CameraClearFlags.Skybox;
        RefreshReflections();
        return true;
    }

    private static ReflectionProbe probe;

    /// <summary>
    /// Capture the sky into a reflection probe so shiny piece sets (Chrome, Crystal)
    /// reflect the current backdrop.
    /// </summary>
    private static void RefreshReflections()
    {
        // Mobile quality levels often switch realtime probes off
        QualitySettings.realtimeReflectionProbes = true;
        if (probe == null)
        {
            var go = new GameObject("Backdrop Reflections");
            probe = go.AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.cullingMask = 0; // sky only
            probe.size = new Vector3(500, 500, 500);
            probe.resolution = 128;
        }
        probe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.Skybox;
        probe.RenderProbe();
        UseProbeAsEnvironment();
    }

    // Also make the captured sky the scene's default reflection, so every renderer
    // picks it up even outside the probe's blending
    private static void UseProbeAsEnvironment()
    {
        if (probe != null && probe.realtimeTexture != null)
        {
            RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = probe.realtimeTexture;
        }
    }

    /// <summary>Slowly turn the sky so space feels alive</summary>
    public static void Drift(float degreesPerSecond)
    {
        if (spaceSky != null && RenderSettings.skybox == spaceSky)
        {
            spaceSky.SetFloat("_Rotation", (Time.time * degreesPerSecond) % 360f);
        }
    }
}

/// <summary>
/// Procedural equirectangular space sky: dark gradient, coloured nebula clouds,
/// a few spiral galaxies and thousands of stars. Pure C# so it can run on a worker thread.
/// </summary>
public static class SpaceSky
{
    public static Color32[] Generate(int width, int height, int seed)
    {
        var rng = new System.Random(seed);
        var pixels = new float[width * height * 3];

        // Nebula: a few coloured clouds made of layered value noise
        var clouds = new (float u, float v, float radius, float r, float g, float b, float strength)[]
        {
            (0.18f, 0.42f, 0.22f, 0.85f, 0.18f, 0.60f, 0.55f),  // magenta
            (0.55f, 0.60f, 0.25f, 0.10f, 0.55f, 0.85f, 0.50f),  // cyan-blue
            (0.80f, 0.35f, 0.18f, 0.45f, 0.20f, 0.85f, 0.45f),  // violet
            (0.38f, 0.75f, 0.14f, 0.95f, 0.45f, 0.30f, 0.30f),  // ember
        };

        for (int y = 0; y < height; y++)
        {
            float v = (y + 0.5f) / height;
            for (int x = 0; x < width; x++)
            {
                float u = (x + 0.5f) / width;
                int i = (y * width + x) * 3;

                // Deep space base, a touch lighter around the galactic band
                float band = (float)Math.Exp(-Math.Pow((v - 0.5f - 0.08f * Math.Sin(u * Math.PI * 2)) / 0.12f, 2));
                float r = 0.012f + 0.03f * band, g = 0.008f + 0.02f * band, b = 0.035f + 0.05f * band;

                float n = Fbm(u * 8f, v * 4f, seed);
                float dust = Fbm(u * 22f + 7f, v * 11f + 3f, seed + 1);
                foreach (var c in clouds)
                {
                    float du = WrapDelta(u - c.u), dv = (v - c.v) * 0.5f;
                    float d = (float)Math.Sqrt(du * du + dv * dv) / c.radius;
                    float falloff = Math.Max(0f, 1f - d * (0.6f + 0.8f * n));
                    float density = falloff * falloff * (0.5f + 0.9f * n) * (0.75f + 0.5f * dust) * c.strength;
                    r += c.r * density; g += c.g * density; b += c.b * density;
                }

                // Band brightness from unresolved stars
                float glow = band * 0.08f * (0.6f + 0.8f * dust);
                r += glow; g += glow * 0.9f; b += glow;

                pixels[i] = r; pixels[i + 1] = g; pixels[i + 2] = b;
            }
        }

        // Spiral galaxies, kept away from the poles where the map stretches
        int galaxies = 5;
        for (int k = 0; k < galaxies; k++)
        {
            float cu = (float)rng.NextDouble();
            float cv = 0.3f + 0.4f * (float)rng.NextDouble();
            float size = 0.018f + 0.03f * (float)rng.NextDouble();
            float tilt = (float)(rng.NextDouble() * Math.PI);
            float squash = 0.35f + 0.5f * (float)rng.NextDouble();
            float hue = (float)rng.NextDouble();
            DrawGalaxy(pixels, width, height, cu, cv, size, tilt, squash, hue, rng);
        }

        // Stars: spread evenly over the sphere, brighter ones rarer
        int stars = 9000;
        for (int k = 0; k < stars; k++)
        {
            double z = rng.NextDouble() * 2 - 1;
            double phi = rng.NextDouble() * Math.PI * 2;
            float u = (float)(phi / (Math.PI * 2));
            float v = (float)(Math.Acos(z) / Math.PI);
            double t = rng.NextDouble();
            float brightness = (float)(0.25 + 1.6 * Math.Pow(t, 6));
            float temp = (float)rng.NextDouble();
            float sr = temp < 0.2f ? 0.75f : 1f, sg = temp < 0.2f ? 0.85f : (temp > 0.85f ? 0.85f : 1f), sb = temp > 0.85f ? 0.7f : 1f;
            float radius = brightness > 1.2f ? 1.6f : 0.9f;
            // Stars near the poles are drawn wider so they stay round on screen
            float stretch = (float)(1.0 / Math.Max(0.15, Math.Sin(v * Math.PI)));
            Stamp(pixels, width, height, u * width, v * height, radius * stretch, radius, brightness, sr, sg, sb);
        }

        var result = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            // Unity textures start at the bottom row
            int row = height - 1 - y;
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 3;
                result[row * width + x] = new Color32(ToByte(pixels[i]), ToByte(pixels[i + 1]), ToByte(pixels[i + 2]), 255);
            }
        }
        return result;
    }

    private static void DrawGalaxy(float[] pixels, int width, int height, float cu, float cv, float size, float tilt, float squash, float hue, System.Random rng)
    {
        float coreR = 1f, coreG = 0.85f + 0.1f * hue, coreB = 0.7f + 0.3f * hue;
        float armR = 0.55f + 0.4f * (1 - hue), armG = 0.6f, armB = 0.9f + 0.1f * hue;
        float cos = (float)Math.Cos(tilt), sin = (float)Math.Sin(tilt);
        int samples = 2600;
        for (int s = 0; s < samples; s++)
        {
            // Two logarithmic spiral arms with scatter
            int arm = s % 2;
            float t = (float)rng.NextDouble();
            float angle = t * 9f + arm * (float)Math.PI;
            float rad = (float)Math.Exp(t * 1.6f) - 1f;
            rad /= (float)Math.Exp(1.6f) - 1f;
            float jitter = 0.12f * (float)(rng.NextDouble() - 0.5);
            float px = (rad + jitter) * (float)Math.Cos(angle);
            float py = (rad + jitter) * (float)Math.Sin(angle) * squash;
            float rx = px * cos - py * sin, ry = px * sin + py * cos;
            float u = cu + rx * size, v = cv + ry * size * 2f;
            float bright = 0.35f * (1f - t) + 0.08f;
            float mix = Math.Min(1f, t * 1.5f);
            Stamp(pixels, width, height, u * width, v * height, 1.4f, 1.4f, bright,
                coreR + (armR - coreR) * mix, coreG + (armG - coreG) * mix, coreB + (armB - coreB) * mix);
        }
        // Bright core
        Stamp(pixels, width, height, cu * width, cv * height, size * width * 0.12f, size * height * 0.24f, 0.9f, coreR, coreG, coreB);
    }

    // Additive soft dot (Gaussian), wrapping horizontally
    private static void Stamp(float[] pixels, int width, int height, float cx, float cy, float rx, float ry, float brightness, float r, float g, float b)
    {
        int x0 = (int)Math.Floor(cx - rx * 2.5f), x1 = (int)Math.Ceiling(cx + rx * 2.5f);
        int y0 = Math.Max(0, (int)Math.Floor(cy - ry * 2.5f)), y1 = Math.Min(height - 1, (int)Math.Ceiling(cy + ry * 2.5f));
        for (int y = y0; y <= y1; y++)
        {
            float dy = (y + 0.5f - cy) / ry;
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x + 0.5f - cx) / rx;
                float w = (float)Math.Exp(-(dx * dx + dy * dy)) * brightness;
                if (w < 0.002f) continue;
                int wx = ((x % width) + width) % width;
                int i = (y * width + wx) * 3;
                pixels[i] += r * w; pixels[i + 1] += g * w; pixels[i + 2] += b * w;
            }
        }
    }

    private static float WrapDelta(float d)
    {
        if (d > 0.5f) d -= 1f;
        if (d < -0.5f) d += 1f;
        return d;
    }

    private static byte ToByte(float value)
    {
        // Soft shoulder so bright cores don't clip harshly
        float mapped = value / (1f + value * 0.35f) * 1.35f;
        return (byte)Math.Max(0, Math.Min(255, (int)(mapped * 255f)));
    }

    // Value noise with several octaves; tiles horizontally every 8 units of u at the base scale
    private static float Fbm(float x, float y, int seed)
    {
        float sum = 0f, amp = 0.5f, freq = 1f;
        for (int o = 0; o < 5; o++)
        {
            sum += amp * ValueNoise(x * freq, y * freq, seed + o * 101, (int)(8 * freq));
            amp *= 0.5f;
            freq *= 2f;
        }
        return sum;
    }

    private static float ValueNoise(float x, float y, int seed, int period)
    {
        int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
        float xf = x - xi, yf = y - yi;
        float u = xf * xf * (3 - 2 * xf), v = yf * yf * (3 - 2 * yf);
        float a = Hash(xi, yi, seed, period), b = Hash(xi + 1, yi, seed, period);
        float c = Hash(xi, yi + 1, seed, period), d = Hash(xi + 1, yi + 1, seed, period);
        return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
    }

    private static float Hash(int x, int y, int seed, int period)
    {
        if (period > 0) x = ((x % period) + period) % period; // seamless where the sky wraps around
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + seed * -2048144777;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0xFFFFFF;
        }
    }
}
