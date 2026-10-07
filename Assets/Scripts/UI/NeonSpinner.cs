using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A spinning ring of neon dots, used while the game is waiting on something (like Find Match).
/// A bright "comet" sweeps around the ring, each dot has a soft glow, and the color
/// steps through the neon palette.
/// </summary>
public class NeonSpinner : MonoBehaviour
{
    private const int DotCount = 12;
    private const float TurnsPerSecond = 0.9f;

    private static readonly Color[] Colors = { NeonTheme.Pink, NeonTheme.Cyan, NeonTheme.Lime, NeonTheme.Yellow };
    private static Sprite softGlow;

    private Image[] dots;
    private Image[] glows;

    /// <summary>Create a spinner of the given size (in canvas units) under parent</summary>
    public static NeonSpinner Create(Transform parent, float size)
    {
        var go = new GameObject("Neon Spinner", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(size, size);
        var spinner = go.AddComponent<NeonSpinner>();
        spinner.Build(size);
        return spinner;
    }

    private void Build(float size)
    {
        float radius = size * 0.38f;
        float dotSize = size * 0.16f;
        dots = new Image[DotCount];
        glows = new Image[DotCount];
        for (int i = 0; i < DotCount; i++)
        {
            float angle = i * Mathf.PI * 2f / DotCount;
            Vector2 at = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
            glows[i] = NewDot("Glow", dotSize * 2.6f, SoftGlow, at);
            dots[i] = NewDot("Dot", dotSize, NeonTheme.Circle, at);
        }
        Animate();
    }

    private Image NewDot(string name, float size, Sprite sprite, Vector2 at)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = new Vector2(size, size);
        image.rectTransform.anchoredPosition = at;
        return image;
    }

    private void Update() => Animate();

    private void Animate()
    {
        if (dots == null) return;
        float t = Time.unscaledTime;
        float head = t * TurnsPerSecond * DotCount;  // which dot the comet is on
        Color tint = ColorAt(t * 0.35f);

        for (int i = 0; i < DotCount; i++)
        {
            // How far behind the comet's head this dot is, 0..1 around the ring
            float behind = Mathf.Repeat(head - i, DotCount) / DotCount;
            float glow = Mathf.Pow(1f - behind, 2.5f);

            // Dots stay fully saturated; the trail just fades out
            dots[i].color = new Color(tint.r, tint.g, tint.b, Mathf.Lerp(0.22f, 1f, Mathf.Clamp01(glow * 1.4f)));
            dots[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.25f, glow);
            glows[i].color = new Color(tint.r, tint.g, tint.b, 0.55f * glow);
        }
    }

    // Each color holds, then snaps quickly to the next, so it never sits on a muddy in-between hue
    private static Color ColorAt(float t)
    {
        float f = Mathf.Repeat(t, Colors.Length);
        int a = Mathf.FloorToInt(f);
        float blend = Mathf.Clamp01((f - a - 0.8f) / 0.2f);
        return Color.Lerp(Colors[a], Colors[(a + 1) % Colors.Length], blend);
    }

    // A soft radial falloff, for the neon bloom around each dot
    private static Sprite SoftGlow
    {
        get
        {
            if (softGlow != null) return softGlow;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(1, 1, 1, a * a));
            }
            tex.Apply();
            softGlow = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return softGlow;
        }
    }
}
