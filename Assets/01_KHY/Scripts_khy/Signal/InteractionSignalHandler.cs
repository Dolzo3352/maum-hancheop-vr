using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class InteractionSignalHandler : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private RingInteractable targetInteractable;
    [SerializeField] private RingChargeSystem ringChargeSystem;
    [SerializeField] private TimelineAnimationBlender blender;

    [Header("대기 애니메이션")]
    [SerializeField] private AnimationClip idleClip;

    [Header("설정")]
    [SerializeField] private bool autoEnableRingSystem = true;
    [SerializeField] private float resumeDelay = 0.5f;
    [SerializeField] private bool debugLog = true;

    private bool isWaitingForInteraction;

    public bool IsWaiting => isWaitingForInteraction;

    public void HandleInteractionSignal()
    {
        if (isWaitingForInteraction) return;
        if (targetInteractable == null || targetInteractable.IsCompleted) return;
        StartCoroutine(WaitForInteractionCoroutine());
    }

    private IEnumerator WaitForInteractionCoroutine()
    {
        isWaitingForInteraction = true;

        // ── Timeline → Idle 크로스페이드 ──
        bool fadeOutDone = false;
        blender.FadeToIdle(idleClip, () => fadeOutDone = true);
        while (!fadeOutDone) yield return null;
        Log($"Idle 전환 완료. 대상: {targetInteractable.name}");

        // 링 충전 시스템 활성화
        if (autoEnableRingSystem && ringChargeSystem != null)
            ringChargeSystem.IsEnabled = true;

        // 인터랙션 완료 대기
        bool interactionDone = false;
        void OnDone() { interactionDone = true; }
        targetInteractable.OnInteractionDone += OnDone;
        while (!interactionDone) yield return null;
        targetInteractable.OnInteractionDone -= OnDone;
        Log($"인터랙션 완료! 대상: {targetInteractable.name}");

        if (resumeDelay > 0f)
            yield return new WaitForSeconds(resumeDelay);

        // ── Idle → Timeline 크로스페이드 ──
        bool fadeInDone = false;
        blender.FadeToTimeline(() => fadeInDone = true);
        while (!fadeInDone) yield return null;
        Log("타임라인 재개 완료");

        isWaitingForInteraction = false;
    }

    private void Log(string message)
    {
        if (debugLog) Debug.Log($"[InteractionSignalHandler] {message}", this);
    }

    private void OnDisable()
    {
        if (isWaitingForInteraction)
        {
            StopAllCoroutines();
            isWaitingForInteraction = false;
        }
    }
}
