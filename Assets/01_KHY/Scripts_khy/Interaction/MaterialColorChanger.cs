using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 머테리얼의 색상을 서서히 변경하는 범용 컴포넌트.
///
/// Timeline Signal이나 UnityEvent에서 StartTransition()을 호출하면
/// 현재 색상에서 targetColor로 부드럽게 전환됩니다.
///
/// 사용 예:
///   - 가마솥 액체: 탁한 색 → 황금색
///   - 조명 연출: 어두운 색 → 밝은 색
/// </summary>
public class MaterialColorChanger : MonoBehaviour
{
    [Header("대상")]
    [Tooltip("색상을 변경할 Renderer. 비워두면 자신의 Renderer 사용")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("변경할 머테리얼 인덱스 (여러 머테리얼일 때)")]
    [SerializeField] private int materialIndex;

    [Tooltip("변경할 셰이더 프로퍼티 이름")]
    [SerializeField] private string propertyName = "_BaseColor";

    [Header("전환 설정")]
    [Tooltip("목표 색상")]
    [SerializeField] private Color targetColor = Color.white;

    [Tooltip("전환 시간 (초)")]
    [SerializeField] private float duration = 2f;

    [Tooltip("전환 커브")]
    [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("이벤트")]
    [SerializeField] private UnityEvent onComplete;

    private Material mat;
    private Coroutine transitionCoroutine;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
    }

    /// <summary>
    /// 색상 전환 시작. Timeline Signal UnityEvent에서 호출.
    /// </summary>
    public void StartTransition()
    {
        if (targetRenderer == null) return;

        mat = targetRenderer.materials[materialIndex];
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionCoroutine());
    }

    private IEnumerator TransitionCoroutine()
    {
        Color startColor = mat.GetColor(propertyName);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = easingCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            mat.SetColor(propertyName, Color.Lerp(startColor, targetColor, t));
            yield return null;
        }

        mat.SetColor(propertyName, targetColor);
        transitionCoroutine = null;
        onComplete?.Invoke();
    }
}
