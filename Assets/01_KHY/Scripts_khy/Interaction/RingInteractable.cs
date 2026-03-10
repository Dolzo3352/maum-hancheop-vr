using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// 링 충전 인터랙션 대상 오브젝트의 베이스 클래스.
///
/// 모든 인터랙터블 오브젝트(바위, 감나무, 냇물 등)가 이 클래스를 상속합니다.
/// RingChargeSystem이 이 컴포넌트를 감지하여 링 충전을 시작합니다.
///
/// 동작 흐름:
///   1. RingChargeSystem이 Ray로 이 오브젝트 감지
///   2. ringType에 따라 링 색상 결정
///   3. Grip Hold → 충전 진행 → OnChargeUpdate(progress)
///   4. 100% → Execute() 호출 → 하위 클래스에서 구체적 인터랙션 실행
///   5. 완료 후 IsCompleted = true → 더 이상 충전 불가
///
/// 사용법:
///   이 클래스를 직접 사용하지 말고, 하위 클래스를 만들어서 사용합니다.
///   예: RockInteractable, TreeInteractable 등
///   또는 Inspector에서 UnityEvent를 연결하여 코드 없이 사용할 수도 있습니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RingInteractable : MonoBehaviour
{
    // ─── 설정 ───

    [Header("링 설정")]
    [Tooltip("이 오브젝트의 링 색상 타입")]
    [SerializeField] private RingType ringType = RingType.Orange;

    [Header("VFX 배치")]
    [Tooltip("프로그레스 VFX가 소환될 위치. 비워두면 오브젝트 위치 + 기본 오프셋")]
    [SerializeField] private Transform vfxSpawnPoint;

    [Header("완료 설정")]
    [Tooltip("true면 Execute() 즉시 완료 처리. 하위 클래스(바위, 감나무 등)는 false로 두고 VFX 끝난 후 직접 호출")]
    [SerializeField] private bool autoCompleteOnExecute = true;

    [Header("이벤트 (코드 없이 사용할 때)")]
    [Tooltip("인터랙션 실행 시 호출할 UnityEvent")]
    [SerializeField] private UnityEngine.Events.UnityEvent onExecute;

    [Tooltip("충전 시작 시 호출할 UnityEvent")]
    [SerializeField] private UnityEngine.Events.UnityEvent onChargeBegin;

    [Tooltip("충전 취소 시 호출할 UnityEvent")]
    [SerializeField] private UnityEngine.Events.UnityEvent onChargeCancelled;

    // ─── 상태 ───

    private bool isCompleted;
    private bool isInteractionActive;

    // ─── 프로퍼티 ───

    /// <summary>이 오브젝트의 링 타입.</summary>
    public RingType RingType => ringType;

    /// <summary>인터랙션 완료 여부. true이면 더 이상 충전 불가.</summary>
    public bool IsCompleted => isCompleted;

    /// <summary>인터랙션 활성 여부. 시그널 발동 후 true, 완료 후 false.</summary>
    public bool IsInteractionActive => isInteractionActive;

    /// <summary>프로그레스 VFX 소환 위치. null이면 기본 오프셋 사용.</summary>
    public Transform VFXSpawnPoint => vfxSpawnPoint;

    // ─── C# 이벤트 ───

    /// <summary>인터랙션 실행 시 (코드에서 구독).</summary>
    public event Action OnExecuted;

    /// <summary>인터랙션 완료 시 (시각적 피드백 끝난 후).</summary>
    public event Action OnInteractionDone;

    // ─── 초기화 ───

    protected virtual void Awake()
    {
        // Collider가 Trigger가 아닌지 확인 (Raycast 감지용)
        var col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"[RingInteractable] Collider가 없습니다: {name}", this);
        }
    }

    // ─── RingChargeSystem에서 호출하는 메서드 ───

    /// <summary>
    /// 충전이 시작될 때 호출됩니다.
    /// </summary>
    public virtual void OnChargeBegin()
    {
        onChargeBegin?.Invoke();
    }

    /// <summary>
    /// 충전 진행 중 매 프레임 호출됩니다.
    /// 하위 클래스에서 오버라이드하여 충전 중 시각적 피드백을 구현합니다.
    /// </summary>
    /// <param name="progress">충전 진행도 (0~1)</param>
    public virtual void OnChargeUpdate(float progress)
    {
        // 하위 클래스에서 오버라이드하여 충전 중 시각적 피드백 구현
    }

    /// <summary>
    /// 충전이 취소될 때 호출됩니다. (Grip 해제 또는 Ray 이탈)
    /// </summary>
    public virtual void OnChargeCancelled()
    {
        onChargeCancelled?.Invoke();
    }

    /// <summary>
    /// 충전 100% 도달 시 호출됩니다. 인터랙션을 실행합니다.
    /// 하위 클래스에서 오버라이드하여 구체적인 인터랙션을 구현합니다.
    /// </summary>
    public virtual void Execute()
    {
        Debug.Log($"[RingInteractable] 인터랙션 실행: {ringType} → {name}", this);

        // UnityEvent 호출 (Inspector 연결용)
        onExecute?.Invoke();

        // C# 이벤트 호출 (코드 연결용)
        OnExecuted?.Invoke();

        // 자동 완료 (하위 클래스가 아닌 베이스 클래스 직접 사용 시)
        if (autoCompleteOnExecute)
            NotifyInteractionDone();
    }

    /// <summary>
    /// 인터랙션의 모든 시각적 피드백이 끝난 후 호출합니다.
    /// 하위 클래스에서 VFX/애니메이션 완료 후 직접 호출해야 합니다.
    /// Signal 시스템이 이 이벤트를 감시하여 타임라인을 재개합니다.
    /// </summary>
    protected void NotifyInteractionDone()
    {
        isCompleted = true;
        isInteractionActive = false;
        OnInteractionDone?.Invoke();
        Debug.Log($"[RingInteractable] 인터랙션 완료: {ringType} → {name}", this);
    }

    // ─── 외부 제어 ───

    /// <summary>
    /// 시그널 발동 시 호출 — 인터랙션 활성화 (호버 림/아웃라인 반응 시작)
    /// </summary>
    public void ActivateInteraction()
    {
        if (isCompleted) return;
        isInteractionActive = true;
    }

    /// <summary>
    /// 인터랙션 상태를 초기화합니다. 재사용이 필요할 때 호출합니다.
    /// </summary>
    public virtual void ResetInteraction()
    {
        isCompleted = false;
        isInteractionActive = false;
    }

    /// <summary>
    /// 에디터 테스트용. Inspector 버튼으로 인터랙션을 직접 실행합니다.
    /// </summary>
    [ContextMenu("테스트: 인터랙션 실행")]
    public void TestExecute()
    {
        Execute();
        NotifyInteractionDone();
    }
}
