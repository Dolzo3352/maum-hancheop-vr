using UnityEngine;

/// <summary>
/// 오브젝트의 원본 스케일(1x 기준)을 저장합니다.
/// Awake에서 한 번만 저장되고, 이후 어떤 타임라인이 스케일을 바꿔도
/// 이 값은 변하지 않습니다.
/// 
/// DioramaScaleTrack에 바인딩하는 오브젝트에 붙여주세요.
/// 없으면 Mixer가 자동으로 추가합니다.
/// </summary>
public class DioramaScaleOrigin : MonoBehaviour
{
    [HideInInspector]
    public Vector3 originalScale;

    private bool _captured;

    private void Awake()
    {
        Capture();
    }

    public void Capture()
    {
        if (_captured) return;
        originalScale = transform.localScale;
        _captured = true;
    }

    /// <summary>
    /// 원본 스케일로 되돌립니다 (디버그 / 리셋용).
    /// </summary>
    public void ResetToOriginal()
    {
        transform.localScale = originalScale;
    }
}
