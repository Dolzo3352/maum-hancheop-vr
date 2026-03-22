using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// SplineAnimate의 NormalizedTime을 Timeline Animation Track에서
/// 키프레임으로 제어할 수 있게 중계하는 래퍼.
///
/// SplineAnimate.NormalizedTime은 C# 프로퍼티(비직렬화)라서
/// Timeline Record 모드에서 직접 키프레임이 안 찍힘.
/// 이 컴포넌트의 progress(serialized float)를 키프레임하면 해결.
///
/// FadeToIdle 시 Timeline Track weight가 0이 되면 progress가
/// 기본값으로 돌아가는 문제를 Hold/Resume으로 해결.
/// </summary>
[ExecuteAlways]
public class SplineProgressDriver : MonoBehaviour
{
    [SerializeField] private SplineAnimate splineAnimate;

    [Range(0f, 1f)]
    [Tooltip("Timeline Animation Track에서 이 값을 키프레임하세요.")]
    public float progress;

    private float holdProgress;
    private bool isHolding;

    private void Awake()
    {
        if (splineAnimate != null)
            splineAnimate.Pause();
    }

    /// <summary>
    /// 현재 progress 값을 고정합니다.
    /// FadeToIdle 전에 호출하면 Timeline weight가 0이 되어도 위치 유지.
    /// Timeline Signal UnityEvent에서 호출하세요.
    /// </summary>
    public void Hold()
    {
        holdProgress = progress;
        isHolding = true;
    }

    /// <summary>
    /// Hold 해제. FadeToTimeline 후 호출.
    /// Timeline Signal UnityEvent에서 호출하세요.
    /// </summary>
    public void Resume()
    {
        isHolding = false;
    }

    private void LateUpdate()
    {
        if (splineAnimate != null)
            splineAnimate.NormalizedTime = isHolding ? holdProgress : progress;
    }
}
