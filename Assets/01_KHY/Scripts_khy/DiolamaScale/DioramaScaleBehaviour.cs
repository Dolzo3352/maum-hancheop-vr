using System;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 스케일 클립 하나의 데이터.
/// 
/// fromScale → targetScale을 절대 배율로 직접 지정합니다.
/// 이전 클립의 상태를 기억하거나 누적하지 않습니다.
/// 
/// 타임라인에서 보이는 모습:
///   |── 1.0x → 0.7x ──|── 0.7x → 1.5x ──|── 1.5x → 1.0x ──|
///       클립1               클립2               클립3
/// </summary>
[Serializable]
public class DioramaScaleBehaviour : PlayableBehaviour
{
    [Header("스케일")]
    [Tooltip("클립 시작 시 스케일 배율")]
    public float fromScale = 1f;

    [Tooltip("클립 끝(전환 완료) 시 스케일 배율")]
    public float targetScale = 1f;

    [Header("타이밍")]
    [Tooltip("전환 시간(초). 나머지 클립 구간은 targetScale을 유지합니다. 0이면 즉시.")]
    [Min(0f)]
    public float transitionIn = 0.5f;

    [Header("과장 애니메이션")]
    public EaseType easeType = EaseType.ExaggeratedBounce;

    [Range(0f, 0.8f)]
    public float overshootStrength = 0.35f;

    [Range(0f, 0.5f)]
    public float anticipationStrength = 0.15f;

    [Header("시차 효과 (Stagger)")]
    public bool enableStagger = false;

    [Range(0f, 0.5f)]
    public float staggerDelay = 0.05f;

    public StaggerDirection staggerDirection = StaggerDirection.CenterOut;

    // ── 헬퍼 ──

    /// <summary>
    /// 클립 경과 시간 → 전환 진행도 (0~1).
    /// 전환 완료 후(유지 구간)에는 1 반환.
    /// </summary>
    public float GetTransitionProgress(double clipElapsed)
    {
        if (transitionIn <= 0f) return 1f;
        if (clipElapsed >= transitionIn) return 1f;
        return Mathf.Clamp01((float)clipElapsed / transitionIn);
    }

    public enum EaseType
    {
        Linear,
        EaseOut,
        EaseInOut,
        ExaggeratedBounce,
        Elastic
    }

    public enum StaggerDirection
    {
        FirstToLast,
        LastToFirst,
        CenterOut,
        Random
    }
}
