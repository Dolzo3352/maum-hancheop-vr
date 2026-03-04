using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 인터랙션 Signal 수신 및 처리 핸들러.
///
/// SignalReceiver의 이벤트에 연결하여, 타임라인이 Signal 지점에 도달하면
/// 타임라인을 정지하고, 지정된 인터랙션이 완료될 때까지 기다린 후 재개합니다.
///
/// 동작 흐름:
///   1. Signal 도달 → director.Stop() (PlayableGraph 완전 해제)
///   2. Animator Controller가 자동으로 Idle 재생
///   3. 플레이어 인터랙션 완료 대기
///   4. director.Play() → 저장된 시간부터 재개
///
/// 주의: 캐릭터의 Animator에 Animator Controller(Idle 상태 포함)가
///       설정되어 있어야 정지 중 Idle 애니메이션이 재생됩니다.
/// </summary>
public class InteractionSignalHandler : MonoBehaviour
{
    // ─── 참조 ───

    [Header("참조")]
    [Tooltip("이 스테이지의 PlayableDirector")]
    [SerializeField] private PlayableDirector director;

    [Tooltip("이 Signal이 기다릴 인터랙션 대상")]
    [SerializeField] private RingInteractable targetInteractable;

    [Tooltip("링 충전 시스템 (씬 전체에서 공유)")]
    [SerializeField] private RingChargeSystem ringChargeSystem;

    // ─── 설정 ───

    [Header("설정")]
    [Tooltip("Signal 수신 시 자동으로 링 시스템을 활성화할지")]
    [SerializeField] private bool autoEnableRingSystem = true;

    [Tooltip("인터랙션 완료 후 타임라인 재개까지 대기 시간 (초)")]
    [SerializeField] private float resumeDelay = 0.5f;

    [Tooltip("디버그 로그 출력")]
    [SerializeField] private bool debugLog = true;

    // ─── 상태 ───

    private bool isWaitingForInteraction;
    private double savedPauseTime;

    /// <summary>현재 인터랙션을 기다리고 있는지.</summary>
    public bool IsWaiting => isWaitingForInteraction;

    // ─── SignalReceiver에서 호출하는 메서드 ───

    /// <summary>
    /// SignalReceiver의 이벤트에 연결할 메서드.
    /// 타임라인이 InteractionSignal 지점에 도달하면 호출됩니다.
    /// </summary>
    public void HandleInteractionSignal()
    {
        if (isWaitingForInteraction)
        {
            Log("이미 인터랙션 대기 중입니다. (Signal 재진입 방지)");
            return;
        }

        if (targetInteractable == null)
        {
            Debug.LogWarning("[InteractionSignalHandler] targetInteractable이 없습니다. 타임라인을 계속 재생합니다.", this);
            return;
        }

        if (targetInteractable.IsCompleted)
        {
            Log("이미 완료된 인터랙션입니다. 건너뜁니다.");
            return;
        }

        StartCoroutine(WaitForInteractionCoroutine());
    }

    // ─── 대기 코루틴 ───

    private IEnumerator WaitForInteractionCoroutine()
    {
        isWaitingForInteraction = true;

        // 1. 타임라인 정지 — director.Stop()으로 PlayableGraph 완전 해제
        //    → Animator Controller가 자동으로 Idle 재생
        if (director != null)
        {
            savedPauseTime = director.time;
            director.Stop();
            Log($"타임라인 정지 (time={savedPauseTime:F2}s). 대상: {targetInteractable.name} ({targetInteractable.RingType})");
        }

        // 2. 링 충전 시스템 활성화
        if (autoEnableRingSystem && ringChargeSystem != null)
        {
            ringChargeSystem.IsEnabled = true;
        }

        // 3. 인터랙션 완료 대기
        bool interactionDone = false;

        void OnDone()
        {
            interactionDone = true;
        }

        targetInteractable.OnInteractionDone += OnDone;

        while (!interactionDone)
        {
            yield return null;
        }

        targetInteractable.OnInteractionDone -= OnDone;

        Log($"인터랙션 완료! 대상: {targetInteractable.name}");

        // 4. 재개 전 잠시 대기 (시각적 피드백 여유)
        if (resumeDelay > 0f)
            yield return new WaitForSeconds(resumeDelay);

        // 5. 타임라인 재개 — 저장된 시간부터 다시 Play
        if (director != null)
        {
            director.time = savedPauseTime;
            director.Play();
            Log($"타임라인 재개 (time: {savedPauseTime:F2}s)");
        }

        isWaitingForInteraction = false;
    }

    // ─── 유틸리티 ───

    private void Log(string message)
    {
        if (debugLog)
            Debug.Log($"[InteractionSignalHandler] {message}", this);
    }

    // ─── 정리 ───

    private void OnDisable()
    {
        if (isWaitingForInteraction)
        {
            StopAllCoroutines();
            isWaitingForInteraction = false;
        }
    }
}
