using UnityEngine;

/// <summary>
/// 타임라인 시그널 발동 시 아웃라인을 활성화하여
/// 인터랙션 가능 오브젝트를 시각적으로 안내합니다.
///
/// 동작:
///   시그널 발동 → Activate() → 아웃라인 ON ("이 오브젝트와 상호작용 가능")
///   호버 → InteractableHoverRim이 림컬러 처리 (별도 컴포넌트)
///   인터랙션 완료 → Complete() → 아웃라인 OFF
/// </summary>
[RequireComponent(typeof(Outline))]
public class InteractableOutline : MonoBehaviour
{
    [Header("Outline 설정")]
    [SerializeField] private Color outlineColor = new Color(1f, 0.8f, 0f);
    [SerializeField] private float outlineWidth = 4f;
    [SerializeField] private Outline.Mode outlineMode = Outline.Mode.OutlineVisible;

    private Outline _outline;
    private bool _isCompleted;

    void Awake()
    {
        _outline = GetComponent<Outline>();
        _outline.OutlineColor = outlineColor;
        _outline.OutlineWidth = outlineWidth;
        _outline.OutlineMode = outlineMode;
        _outline.enabled = false;
    }

    /// <summary>
    /// 시그널 발동 시 호출 — 아웃라인 활성화
    /// </summary>
    public void Activate()
    {
        if (_isCompleted) return;
        _outline.enabled = true;
    }

    /// <summary>
    /// 인터랙션 완료 — 아웃라인 비활성화
    /// </summary>
    public void Complete()
    {
        _isCompleted = true;
        _outline.enabled = false;
    }

    /// <summary>
    /// 상태 초기화 (재사용 시)
    /// </summary>
    public void ResetState()
    {
        _isCompleted = false;
        _outline.enabled = false;
    }
}
