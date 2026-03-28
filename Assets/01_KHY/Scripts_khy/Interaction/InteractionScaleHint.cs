using System.Collections;
using UnityEngine;

/// <summary>
/// 인터랙션 활성화 시 오브젝트 스케일을 미세하게 맥동시켜 "여기 인터랙션 가능"을 직관적으로 전달합니다.
///
/// 사용법:
///   힌트가 필요한 오브젝트에만 이 컴포넌트를 추가합니다.
///   같은 오브젝트에 RingInteractable이 있어야 합니다.
///
/// 동작 흐름:
///   시그널 발동 → RingInteractable.ActivateInteraction() → 스케일 맥동 시작
///   플레이어 충전 시작 → RingInteractable.OnChargeBegin() → 즉시 원래 크기로 복귀
/// </summary>
public class InteractionScaleHint : MonoBehaviour
{
    [Header("스케일 대상")]
    [Tooltip("스케일을 조작할 Transform. 비워두면 자기 자신을 사용합니다.")]
    [SerializeField] private Transform scaleTarget;

    [Header("맥동 설정")]
    [Tooltip("맥동 크기 (원래 크기의 ±N배). 예: 0.08 → ±8%")]
    [SerializeField] private float pulseAmplitude = 0.08f;

    [Tooltip("맥동 주기 (rad/s). 값이 클수록 빠릅니다.")]
    [SerializeField] private float pulseSpeed = 2.0f;

    [Tooltip("힌트 시작 시 페이드인 시간 (초)")]
    [SerializeField] private float smoothInDuration = 0.5f;

    private RingInteractable interactable;
    private Vector3 originalScale;
    private Coroutine pulseCoroutine;

    private void Awake()
    {
        if (scaleTarget == null)
            scaleTarget = transform;

        originalScale = scaleTarget.localScale;

        interactable = GetComponent<RingInteractable>();
        if (interactable == null)
        {
            Debug.LogError("[InteractionScaleHint] 같은 오브젝트에 RingInteractable이 없습니다.", this);
            return;
        }

        interactable.OnActivated += StartHint;
        interactable.OnChargeBegun += StopHint;
    }

    private void OnDestroy()
    {
        if (interactable == null) return;
        interactable.OnActivated -= StartHint;
        interactable.OnChargeBegun -= StopHint;
    }

    private void StartHint()
    {
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);

        pulseCoroutine = StartCoroutine(PulseCoroutine());
    }

    private void StopHint()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        scaleTarget.localScale = originalScale;
    }

    private IEnumerator PulseCoroutine()
    {
        float elapsed = 0f;

        while (true)
        {
            // smooth in: 시작 직후 amplitude를 서서히 키움
            float amplitudeMultiplier = smoothInDuration > 0f
                ? Mathf.Clamp01(elapsed / smoothInDuration)
                : 1f;

            float currentAmplitude = pulseAmplitude * amplitudeMultiplier;
            float scaleFactor = 1f + Mathf.Sin(Time.time * pulseSpeed) * currentAmplitude;
            scaleTarget.localScale = originalScale * scaleFactor;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>에디터 테스트용 — 힌트 수동 시작</summary>
    [ContextMenu("테스트: 힌트 시작")]
    private void TestStartHint() => StartHint();

    /// <summary>에디터 테스트용 — 힌트 수동 종료</summary>
    [ContextMenu("테스트: 힌트 종료")]
    private void TestStopHint() => StopHint();
}
