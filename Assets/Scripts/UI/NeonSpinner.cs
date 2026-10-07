using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A spinning ring of neon dots, used while the game is waiting on something (like Find Match).
/// A bright "comet" sweeps around the ring and the dots cycle through the neon colors.
/// </summary>
public class NeonSpinner : MonoBehaviour
{
    private const int DotCount = 12;
    private const float TurnsPerSecond = 0.9f;

    private static readonly Color[] Colors = { NeonTheme.Pink, NeonTheme.Cyan, NeonTheme.Lime, NeonTheme.Yellow };

    private Image[] dots;
    private Image halo;

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
        // Soft glow disc behind the dots
        halo = NewDot("Halo", size);

        float radius = size * 0.38f;
        float dotSize = size * 0.17f;
        dots = new Image[DotCount];
        for (int i = 0; i < DotCount; i++)
        {
            float angle = i * Mathf.PI * 2f / DotCount;
            Image dot = NewDot("Dot", dotSize);
            dot.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
            dots[i] = dot;
        }
        Animate();
    }

    private Image NewDot(string name, float size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var image = go.AddComponent<Image>();
        image.sprite = NeonTheme.Circle;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = new Vector2(size, size);
        return image;
    }

    private void Update() => Animate();

    private void Animate()
    {
        if (dots == null) return;
        float t = Time.unscaledTime;
        float head = t * TurnsPerSecond * DotCount;           // which dot the comet is on
        Color tint = ColorAt(t * 0.5f);                         // slowly cycles pink → cyan → lime → yellow

        for (int i = 0; i < DotCount; i++)
        {
            // How far behind the comet's head this dot is, 0..1 around the ring
            float behind = Mathf.Repeat(head - i, DotCount) / DotCount;
            float glow = Mathf.Pow(1f - behind, 2.2f);
            Color c = Color.Lerp(NeonTheme.Muted, tint, glow);
            c.a = Mathf.Lerp(0.35f, 1f, glow);
            dots[i].color = c;
            dots[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(0.55f, 1.15f, glow);
        }

        // The halo breathes gently in the same color
        float pulse = 0.12f + 0.1f * (0.5f + 0.5f * Mathf.Sin(t * 4f));
        halo.color = new Color(tint.r, tint.g, tint.b, pulse);
    }

    private static Color ColorAt(float t)
    {
        float f = Mathf.Repeat(t, Colors.Length);
        int a = Mathf.FloorToInt(f);
        return Color.Lerp(Colors[a], Colors[(a + 1) % Colors.Length], Mathf.SmoothStep(0f, 1f, f - a));
    }
}
