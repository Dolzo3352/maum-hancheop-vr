using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// 링 충전 시스템 (Unified Ring System).
///
/// VRInputManager의 Grip 이벤트를 받아 게이지를 충전하고,
/// 100% 도달 시 대상 오브젝트의 인터랙션을 발동합니다.
///
/// 동작 흐름:
///   1. VRInputManager.OnGripStarted → 현재 XR Ray가 가리키는 대상 확인
///   2. 대상이 RingInteractable이면 → 충전 시작 (대상의 ringType 색상 적용)
///   3. Grip Hold 유지 → chargeProgress 0→1 증가
///   4. chargeProgress == 1 → OnRingComplete 이벤트 → 대상의 Execute() 호출
///   5. Grip 해제 → 충전 취소, 초기화
///
/// 사용법:
///   VRInputManager와 같은 오브젝트 또는 매니저 오브젝트에 부착.
///   Inspector에서 VRInputManager와 XR Ray Interactor를 연결합니다.
/// </summary>
public class RingChargeSystem : MonoBehaviour
{
    // ─── 참조 ───

    [Header("참조")]
    [Tooltip("VR 입력 매니저")]
    [SerializeField] private VRInputManager inputManager;

    [Tooltip("왼손 XR Ray Interactor")]
    [SerializeField] private XRRayInteractor leftRayInteractor;

    [Tooltip("오른손 XR Ray Interactor")]
    [SerializeField] private XRRayInteractor rightRayInteractor;

    // ─── 충전 설정 ───

    [Header("충전 설정")]
    [Tooltip("0%에서 100%까지 충전에 걸리는 시간 (초)")]
    [SerializeField] private float chargeDuration = 2.0f;

    [Tooltip("충전 진행 커브. X축: 시간(0~1), Y축: 진행도(0~1). 기본은 선형")]
    [SerializeField] private AnimationCurve chargeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    // ─── 상태 ───

    /// <summary>현재 충전 진행도 (0~1). UI에서 읽어갈 수 있습니다.</summary>
    public float ChargeProgress { get; private set; }

    /// <summary>현재 충전 중인지.</summary>
    public bool IsCharging { get; private set; }

    /// <summary>현재 충전 대상의 링 타입.</summary>
    public RingType CurrentRingType { get; private set; }

    /// <summary>시스템 활성화 여부. false이면 Grip 입력을 무시합니다.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>현재 충전에 사용 중인 XRRayInteractor. 충전 중이 아니면 null.</summary>
    public XRRayInteractor ActiveRayInteractor { get; private set; }

    // 충전 경과 시간
    private float chargeElapsed;

    // 현재 충전 대상
    private RingInteractable currentTarget;

    // ─── 이벤트 ───

    /// <summary>충전 시작 시. (링 타입, 대상 오브젝트)</summary>
    public event Action<RingType, RingInteractable> OnChargeStarted;

    /// <summary>충전 진행 중 매 프레임. (진행도 0~1, 링 타입)</summary>
    public event Action<float, RingType> OnChargeUpdated;

    /// <summary>충전 100% 완료 시. (링 타입, 대상 오브젝트)</summary>
    public event Action<RingType, RingInteractable> OnRingComplete;

    /// <summary>충전 취소 시 (Grip 해제). (링 타입)</summary>
    public event Action<RingType> OnChargeCancelled;

    // ─── 초기화 ───

    private void OnEnable()
    {
        if (inputManager != null)
        {
            inputManager.OnGripStarted += HandleGripStarted;
            inputManager.OnGripHeld += HandleGripHeld;
            inputManager.OnGripReleased += HandleGripReleased;
        }
    }

    private void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.OnGripStarted -= HandleGripStarted;
            inputManager.OnGripHeld -= HandleGripHeld;
            inputManager.OnGripReleased -= HandleGripReleased;
        }

        // 충전 중이었으면 취소
        if (IsCharging)
            CancelCharge();
    }

    // ─── Grip 이벤트 핸들러 ───

    private void HandleGripStarted(HandSide hand)
    {
        if (!IsEnabled) return;

        // 현재 Ray가 가리키는 대상 확인
        var target = FindTargetUnderRay(hand);
        if (target == null) return;

        // 이미 완료된 인터랙션이면 무시
        if (target.IsCompleted) return;

        // 충전 시작
        currentTarget = target;
        CurrentRingType = target.RingType;
        ActiveRayInteractor = hand == HandSide.Left ? leftRayInteractor : rightRayInteractor;
        ChargeProgress = 0f;
        chargeElapsed = 0f;
        IsCharging = true;

        // 햅틱 활성화
        if (inputManager != null)
            inputManager.HapticEnabled = true;

        // 대상에 충전 시작 알림
        currentTarget.OnChargeBegin();

        OnChargeStarted?.Invoke(CurrentRingType, currentTarget);
        Debug.Log($"[RingChargeSystem] 충전 시작: {CurrentRingType} → {currentTarget.name}");
    }

    private void HandleGripHeld(HandSide hand, float gripValue)
    {
        if (!IsCharging || currentTarget == null) return;

        // Ray가 여전히 대상을 가리키고 있는지 확인
        var target = FindTargetUnderRay(hand);
        if (target != currentTarget)
        {
            // 대상에서 벗어남 → 충전 취소
            CancelCharge();
            return;
        }

        // 시간 기반 충전 (gripValue는 진동에만 사용, 충전은 시간 기반)
        chargeElapsed += Time.deltaTime;
        float normalizedTime = Mathf.Clamp01(chargeElapsed / chargeDuration);
        ChargeProgress = chargeCurve.Evaluate(normalizedTime);

        // 진행도 업데이트 이벤트
        OnChargeUpdated?.Invoke(ChargeProgress, CurrentRingType);

        // 대상에 진행도 알림
        currentTarget.OnChargeUpdate(ChargeProgress);

        // 100% 도달!
        if (ChargeProgress >= 1f)
        {
            CompleteCharge();
        }
    }

    private void HandleGripReleased(HandSide hand)
    {
        if (!IsCharging) return;

        // 100% 미달 상태에서 놓음 → 취소
        CancelCharge();
    }

    // ─── 충전 완료/취소 ───

    private void CompleteCharge()
    {
        if (currentTarget == null) return;

        Debug.Log($"[RingChargeSystem] 충전 완료! {CurrentRingType} → {currentTarget.name}");

        var completedTarget = currentTarget;
        var completedType = CurrentRingType;

        // 대상의 인터랙션 실행
        completedTarget.Execute();

        // 이벤트 발행
        OnRingComplete?.Invoke(completedType, completedTarget);

        // 상태 초기화
        ResetState();
    }

    private void CancelCharge()
    {
        if (currentTarget != null)
        {
            currentTarget.OnChargeCancelled();
            Debug.Log($"[RingChargeSystem] 충전 취소: {CurrentRingType}");
        }

        OnChargeCancelled?.Invoke(CurrentRingType);
        ResetState();
    }

    private void ResetState()
    {
        currentTarget = null;
        CurrentRingType = RingType.None;
        ChargeProgress = 0f;
        chargeElapsed = 0f;
        IsCharging = false;
        ActiveRayInteractor = null;

        // 햅틱 비활성화
        if (inputManager != null)
            inputManager.HapticEnabled = false;
    }

    // ─── Ray 대상 탐색 ───

    /// <summary>
    /// 해당 손의 XR Ray Interactor가 현재 가리키는 RingInteractable을 찾습니다.
    /// </summary>
    private RingInteractable FindTargetUnderRay(HandSide hand)
    {
        var ray = hand == HandSide.Left ? leftRayInteractor : rightRayInteractor;
        if (ray == null) return null;

        // XR Ray Interactor의 현재 Hover 대상 확인
        if (ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            // hit된 오브젝트에서 RingInteractable 검색
            var interactable = hit.collider.GetComponentInParent<RingInteractable>();
            return interactable;
        }

        return null;
    }

    // ─── 외부 제어 ───

    /// <summary>
    /// 충전을 강제로 취소합니다. Signal 시스템에서 사용할 수 있습니다.
    /// </summary>
    public void ForceCancel()
    {
        if (IsCharging)
            CancelCharge();
    }
}
