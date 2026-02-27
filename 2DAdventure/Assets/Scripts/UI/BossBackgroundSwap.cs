using DG.Tweening;
using UnityEngine;

public class BossBackgroundSwap : MonoBehaviour
{
    public SpriteRenderer targetRenderer;
    public Sprite afterBossDeadSprite;

    public bool fade = true;
    public float fadeDuration = 0.5f;

    private Tween fadeTween;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
    }

    public void OnBossDeadSwapBackground()
    {
        if (targetRenderer == null || afterBossDeadSprite == null)
            return;

        fadeTween?.Kill();

        if (!fade)
        {
            targetRenderer.sprite = afterBossDeadSprite;
            return;
        }

        float startA = targetRenderer.color.a;

        fadeTween = DOTween.Sequence()
            .Append(targetRenderer.DOFade(0f, fadeDuration))
            .AppendCallback(() => targetRenderer.sprite = afterBossDeadSprite)
            .Append(targetRenderer.DOFade(startA, fadeDuration));
    }
}
