using UnityEngine;

/// <summary>
/// 오브젝트의 원본 스케일(1x 기준)을 기억합니다.
///
/// 인스펙터에서 baseScale을 직접 지정합니다. 기본값 (1,1,1).
/// 타임라인 프리뷰나 Play 모드에서 localScale이 바뀌어도
/// 이 값은 절대 오염되지 않습니다.
///
/// 없으면 Mixer가 자동으로 추가합니다.
/// </summary>
public class DioramaScaleOrigin : MonoBehaviour
{
    [Tooltip("이 오브젝트의 원본 스케일 (1x 기준). 타임라인은 이 값을 기준으로 배율을 적용합니다.")]
    public Vector3 baseScale = Vector3.one;

    /// <summary>
    /// 런타임에서 첫 번째 타임라인이 이미 사용했는지 여부.
    /// true이면 다음 타임라인은 localScale을 리셋하지 않고 이어받습니다.
    /// Play 모드 진입 시 자동으로 false로 시작 (비직렬화).
    /// </summary>
    [System.NonSerialized] public bool hasBeenUsed;

    public void ResetToOriginal() => transform.localScale = baseScale;

#if UNITY_EDITOR
    [ContextMenu("Capture Current Scale")]
    private void ContextMenuCapture()
    {
        baseScale = transform.localScale;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[DioramaScaleOrigin] baseScale = {baseScale}", this);
    }
#endif
}
