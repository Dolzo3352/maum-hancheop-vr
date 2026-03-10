using UnityEngine;

/// <summary>
/// 타임라인 시그널 발동 시 아웃라인을 활성화하여
/// 인터랙션 가능 오브젝트를 시각적으로 안내합니다.
///
/// 동작:
///   시그널 발동 → Activate() → 아웃라인 ON ("이 오브젝트와 상호작용 가능")
///   호버 → InteractableHoverRim이 림컬러 처리 (별도 컴포넌트)
///   인터랙션 완료 → Complete() → 아웃라인 OFF
///
/// includeChildren이 true이면 하위 오브젝트의 Outline도 함께 제어합니다.
/// </summary>
public class InteractableOutline : MonoBehaviour
{
    [Header("Outline 설정")]
    [SerializeField] private Color outlineColor = new Color(1f, 0.8f, 0f);
    [SerializeField] private float outlineWidth = 4f;
    [SerializeField] private Outline.Mode outlineMode = Outline.Mode.OutlineVisible;

    [Header("하위 오브젝트")]
    [Tooltip("true면 자신 + 하위 오브젝트의 Outline을 모두 제어합니다")]
    [SerializeField] private bool includeChildren = true;

    private Outline[] _outlines;
    private bool _isCompleted;

    void Awake()
    {
        // 자신 + 하위 Outline 수집
        if (includeChildren)
            _outlines = GetComponentsInChildren<Outline>(true);
        else
            _outlines = GetComponents<Outline>();

        // 초기 설정
        foreach (var outline in _outlines)
        {
            outline.OutlineColor = outlineColor;
            outline.OutlineWidth = outlineWidth;
            outline.OutlineMode = outlineMode;
            outline.enabled = false;
        }
    }

    /// <summary>
    /// 시그널 발동 시 호출 — 아웃라인 활성화
    /// </summary>
    public void Activate()
    {
        if (_isCompleted) return;
        SetOutlinesEnabled(true);
    }

    /// <summary>
    /// 인터랙션 완료 — 아웃라인 비활성화
    /// </summary>
    public void Complete()
    {
        _isCompleted = true;
        SetOutlinesEnabled(false);
    }

    /// <summary>
    /// 상태 초기화 (재사용 시)
    /// </summary>
    public void ResetState()
    {
        _isCompleted = false;
        SetOutlinesEnabled(false);
    }

    private void SetOutlinesEnabled(bool enabled)
    {
        if (_outlines == null) return;
        foreach (var outline in _outlines)
        {
            if (outline != null)
                outline.enabled = enabled;
        }
    }
}
