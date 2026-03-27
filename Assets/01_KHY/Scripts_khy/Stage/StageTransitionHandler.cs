using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Stage 전환 시 시각 효과 (페이드, 디졸브 등).
///
/// NarrativeSequencer가 Stage 전환 시 호출합니다.
/// 실제 페이드는 FadeController 싱글톤에 위임합니다.
///
/// 사용법:
///   NarrativeSequencer와 같은 GameObject 또는 매니저 오브젝트에 부착합니다.
/// </summary>
public class StageTransitionHandler : MonoBehaviour
{
    [Header("페이드 설정")]
    [Tooltip("기본 페이드 시간 (초)")]
    [SerializeField] private float defaultFadeDuration = 0.5f;

    // ─── 이벤트 ───

    /// <summary>페이드 아웃 완료 시 발행 (화면이 완전히 가려짐).</summary>
    public event Action OnFadeOutComplete;

    /// <summary>페이드 인 완료 시 발행 (화면이 완전히 보임).</summary>
    public event Action OnFadeInComplete;

    // ─── 프로퍼티 ───

    /// <summary>전환 진행 중 여부.</summary>
    public bool IsTransitioning { get; private set; }

    // ─── 공개 메서드 ───

    /// <summary>화면을 서서히 가립니다 (투명 → 불투명).</summary>
    public Coroutine FadeOut(float duration = -1f)
    {
        float d = duration < 0f ? defaultFadeDuration : duration;
        return StartCoroutine(FadeOutRoutine(d));
    }

    /// <summary>화면을 서서히 보여줍니다 (불투명 → 투명).</summary>
    public Coroutine FadeIn(float duration = -1f)
    {
        float d = duration < 0f ? defaultFadeDuration : duration;
        return StartCoroutine(FadeInRoutine(d));
    }

    /// <summary>
    /// TransitionType에 따라 적절한 전환을 실행합니다.
    /// </summary>
    public Coroutine ExecuteTransition(TransitionType type, float duration)
    {
        switch (type)
        {
            case TransitionType.Fade:
                return FadeOut(duration);

            case TransitionType.Dissolve:
                Debug.Log("[StageTransitionHandler] Dissolve → Fade로 대체", this);
                return FadeOut(duration);

            case TransitionType.Physical:
                Debug.Log("[StageTransitionHandler] Physical 전환: 페이드 없음", this);
                return null;

            default:
                return FadeOut(duration);
        }
    }

    // ─── 내부 ───

    private IEnumerator FadeOutRoutine(float duration)
    {
        IsTransitioning = true;
        yield return FadeController.Instance?.FadeOutCoroutine(duration);
        IsTransitioning = false;
        OnFadeOutComplete?.Invoke();
    }

    private IEnumerator FadeInRoutine(float duration)
    {
        IsTransitioning = true;
        yield return FadeController.Instance?.FadeInCoroutine(duration);
        IsTransitioning = false;
        OnFadeInComplete?.Invoke();
    }
}
