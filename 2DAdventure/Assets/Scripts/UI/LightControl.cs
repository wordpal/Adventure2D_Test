using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightControl : MonoBehaviour
{
    public Light2D globalLight;

    public float brightenDuration = 2f;
    public float targetIntensity = 1f;

    public Color targetColor = Color.white;

    private Tween brightenTween;

    private void Awake()
    {
        if (globalLight == null)
            globalLight = GetComponent<Light2D>();
    }

    public void OnBossDeadBrighten()
    {
        if (globalLight == null)
            return;

        brightenTween?.Kill();

        brightenTween = DOTween.Sequence()
            .Join(DOTween.To(() => globalLight.intensity, x => globalLight.intensity = x, targetIntensity, brightenDuration))
            .Join(DOTween.To(() => globalLight.color, x => globalLight.color = x, targetColor, brightenDuration));
    }
}
