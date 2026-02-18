using System;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 스케일 클립 데이터.
///
///   |← transitionIn →|←── Hold ──→|
///   | fromScale → to  |  to 유지    |
/// </summary>
[Serializable]
public class DioramaScaleBehaviour : PlayableBehaviour
{
    [Header("스케일")]
    public float fromScale = 1f;
    public float targetScale = 1f;

    [Header("전환")]
    [Tooltip("전환 시간(초). 0이면 즉시.")]
    [Min(0f)]
    public float transitionIn = 0.5f;

    [Header("이징")]
    public EaseType easeType = EaseType.ExaggeratedBounce;

    [Range(0f, 0.8f)]
    public float overshootStrength = 0.35f;

    [Range(0f, 0.5f)]
    public float anticipationStrength = 0.15f;

    /// <summary>
    /// 클립 경과 시간 → 전환 진행도 (0~1). 유지 구간이면 1.
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
}
