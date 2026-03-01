using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Outline))]
[RequireComponent(typeof(XRBaseInteractable))]
public class InteractableOutline : MonoBehaviour
{
    [Header("Outline 설정")]
    [SerializeField] private Color outlineColor = new Color(1f, 0.8f, 0f);
    [SerializeField] private float outlineWidth = 4f;
    [SerializeField] private Outline.Mode outlineMode = Outline.Mode.OutlineVisible;

    [Header("동작 설정")]
    [SerializeField] private bool disableAfterGrab = true; // 그랩 후 영구 비활성화

    private Outline _outline;
    private bool _isCompleted; // 인터랙션 완료 상태

    void Awake()
    {
        _outline = GetComponent<Outline>();
        _outline.OutlineColor = outlineColor;
        _outline.OutlineWidth = outlineWidth;
        _outline.OutlineMode = outlineMode;
        _outline.enabled = false;

        var interactable = GetComponent<XRBaseInteractable>();
        interactable.hoverEntered.AddListener(OnHoverEnter);
        interactable.hoverExited.AddListener(OnHoverExit);
        interactable.selectExited.AddListener(OnSelectExit);
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (!_isCompleted)
            _outline.enabled = true;
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        if (!_isCompleted)
            _outline.enabled = false;
    }

    private void OnSelectExit(SelectExitEventArgs args)
    {
        if (disableAfterGrab)
            Complete();
    }

    /// <summary>
    /// 외부에서 호출 가능 - 인터랙션 완료 처리
    /// </summary>
    public void Complete()
    {
        _isCompleted = true;
        _outline.enabled = false;
    }

    /// <summary>
    /// 필요 시 다시 활성화 (예: 리셋)
    /// </summary>
    public void ResetState()
    {
        _isCompleted = false;
    }
}
