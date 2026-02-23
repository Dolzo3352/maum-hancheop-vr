using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Stage 전환 시 시각 효과 (페이드, 디졸브 등).
///
/// NarrativeSequencer가 Stage 전환 시 호출합니다.
/// VR 환경에서는 Camera 앞에 페이드용 쿼드를 배치하거나
/// URP Volume의 Color Adjustments를 조작하는 방식을 사용합니다.
///
/// 사용법:
///   NarrativeSequencer와 같은 GameObject 또는 매니저 오브젝트에 부착합니다.
/// </summary>
public class StageTransitionHandler : MonoBehaviour
{
    // ─── 필드 ───

    [Header("페이드 설정")]
    [Tooltip("페이드용 머터리얼 (Unlit, Transparent). 카메라 앞 쿼드에 사용합니다.")]
    [SerializeField] private Material fadeMaterial;

    [Tooltip("기본 페이드 시간 (초)")]
    [SerializeField] private float defaultFadeDuration = 0.5f;

    [Header("페이드 색상")]
    [SerializeField] private Color fadeColor = Color.black;

    // 현재 페이드 알파 (0 = 투명, 1 = 완전 불투명)
    private float currentAlpha;

    // 진행 중인 전환 코루틴
    private Coroutine activeTransition;

    // ─── 이벤트 ───

    /// <summary>페이드 아웃 완료 시 발행 (화면이 완전히 가려짐).</summary>
    public event Action OnFadeOutComplete;

    /// <summary>페이드 인 완료 시 발행 (화면이 완전히 보임).</summary>
    public event Action OnFadeInComplete;

    // ─── 프로퍼티 ───

    /// <summary>전환 진행 중 여부.</summary>
    public bool IsTransitioning => activeTransition != null;

    // ─── 공개 메서드 ───

    /// <summary>
    /// 화면을 서서히 가립니다 (투명 → 불투명).
    /// </summary>
    public Coroutine FadeOut(float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeDuration;
        StopActiveTransition();
        activeTransition = StartCoroutine(FadeRoutine(0f, 1f, duration, OnFadeOutComplete));
        return activeTransition;
    }

    /// <summary>
    /// 화면을 서서히 보여줍니다 (불투명 → 투명).
    /// </summary>
    public Coroutine FadeIn(float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeDuration;
        StopActiveTransition();
        activeTransition = StartCoroutine(FadeRoutine(1f, 0f, duration, OnFadeInComplete));
        return activeTransition;
    }

    /// <summary>
    /// TransitionType에 따라 적절한 전환을 실행합니다.
    /// NarrativeSequencer가 StageData.entryTransition을 전달합니다.
    /// </summary>
    public Coroutine ExecuteTransition(TransitionType type, float duration)
    {
        switch (type)
        {
            case TransitionType.Fade:
                return FadeOut(duration);

            case TransitionType.Dissolve:
                // 디졸브는 페이드로 대체 (추후 구현 가능)
                Debug.Log("[StageTransitionHandler] Dissolve → Fade로 대체", this);
                return FadeOut(duration);

            case TransitionType.Physical:
                // 물리적 전환은 페이드 없이 즉시 (타임라인에서 처리)
                Debug.Log("[StageTransitionHandler] Physical 전환: 페이드 없음", this);
                return null;

            default:
                return FadeOut(duration);
        }
    }

    // ─── 내부 ───

    private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
    {
        float elapsed = 0f;
        currentAlpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            currentAlpha = Mathf.Lerp(from, to, elapsed / duration);
            ApplyFade(currentAlpha);
            yield return null;
        }

        currentAlpha = to;
        ApplyFade(currentAlpha);
        activeTransition = null;
        onComplete?.Invoke();

        Debug.Log($"[StageTransitionHandler] 페이드 완료: alpha={to}", this);
    }

    private void ApplyFade(float alpha)
    {
        if (fadeMaterial == null) return;

        Color c = fadeColor;
        c.a = alpha;
        fadeMaterial.color = c;
    }

    private void StopActiveTransition()
    {
        if (activeTransition != null)
        {
            StopCoroutine(activeTransition);
            activeTransition = null;
        }
    }
}
