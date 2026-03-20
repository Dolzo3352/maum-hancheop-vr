using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// SplineAnimate의 NormalizedTime을 Timeline Animation Track에서
/// 키프레임으로 제어할 수 있게 중계하는 래퍼.
///
/// SplineAnimate.NormalizedTime은 C# 프로퍼티(비직렬화)라서
/// Timeline Record 모드에서 직접 키프레임이 안 찍힘.
/// 이 컴포넌트의 progress(serialized float)를 키프레임하면 해결.
/// </summary>
[ExecuteAlways]
public class SplineProgressDriver : MonoBehaviour
{
    [SerializeField] private SplineAnimate splineAnimate;

    [Range(0f, 1f)]
    [Tooltip("Timeline Animation Track에서 이 값을 키프레임하세요.")]
    public float progress;

    private void Awake()
    {
        if (splineAnimate != null)
            splineAnimate.Pause();
    }

    private void LateUpdate()
    {
        if (splineAnimate != null)
            splineAnimate.NormalizedTime = progress;
    }
}
