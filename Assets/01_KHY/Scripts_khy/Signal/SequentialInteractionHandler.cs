using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class SequentialInteractionHandler : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private RingChargeSystem ringChargeSystem;
    [SerializeField] private TimelineAnimationBlender blender;

    [Header("인터랙션 순서")]
    [SerializeField] private List<RingInteractable> interactionSequence = new List<RingInteractable>();

    [Header("대기 애니메이션")]
    [SerializeField] private AnimationClip idleClip;

    [Header("설정")]
    [SerializeField] private float resumeDelay = 0.5f;
    [SerializeField] private bool debugLog = true;

    private int currentIndex = 0;
    private bool isWaiting;

    public int CurrentIndex => currentIndex;
    public bool AllCompleted => currentIndex >= interactionSequence.Count;

    public void HandleNextInteraction()
    {
        if (isWaiting || AllCompleted) return;
        StartCoroutine(WaitForCurrentInteraction());
    }

    private IEnumerator WaitForCurrentInteraction()
    {
        if (currentIndex >= interactionSequence.Count) yield break;

        isWaiting = true;
        var target = interactionSequence[currentIndex];

        if (target.IsCompleted)
        {
            currentIndex++;
            isWaiting = false;
            yield break;
        }

        // ── 컨트롤러 하이라이트 + 햅틱 ──
        InteractionEvents.FireActivated();

        // ── 인터랙션 활성화 + 아웃라인 활성화 (남은 인터랙터블 전체) ──
        foreach (var interactable in interactionSequence)
        {
            if (interactable == null || interactable.IsCompleted) continue;
            interactable.ActivateInteraction();
            var ol = interactable.GetComponent<InteractableOutline>();
            if (ol != null) ol.Activate();
        }

        // ── Timeline → Idle 크로스페이드 ──
        bool fadeOutDone = false;
        blender.FadeToIdle(idleClip, onComplete: () => fadeOutDone = true);
        while (!fadeOutDone) yield return null;

        Log($"대기 중 [{currentIndex + 1}/{interactionSequence.Count}]: {target.name}");

        if (ringChargeSystem != null)
            ringChargeSystem.IsEnabled = true;

        bool done = false;
        void OnDone() { done = true; }
        target.OnInteractionDone += OnDone;
        while (!done) yield return null;
        target.OnInteractionDone -= OnDone;

        Log($"완료! [{currentIndex + 1}/{interactionSequence.Count}]: {target.name}");

        // ── 완료된 아웃라인 비활성화 ──
        var outline = target.GetComponent<InteractableOutline>();
        if (outline != null) outline.Complete();

        // ── 컨트롤러 하이라이트 해제 ──
        InteractionEvents.FireCompleted();

        currentIndex++;

        if (resumeDelay > 0f)
            yield return new WaitForSeconds(resumeDelay);

        // ── Idle → Timeline 크로스페이드 ──
        bool fadeInDone = false;
        blender.FadeToTimeline(() => fadeInDone = true);
        while (!fadeInDone) yield return null;

        isWaiting = false;
    }

    public void ResetSequence()
    {
        currentIndex = 0;
        isWaiting = false;
        foreach (var interactable in interactionSequence)
        {
            if (interactable != null)
                interactable.ResetInteraction();
        }
    }

    private void Log(string message)
    {
        if (debugLog) Debug.Log($"[SequentialInteractionHandler] {message}", this);
    }

    private void OnDisable()
    {
        if (isWaiting)
        {
            StopAllCoroutines();
            isWaiting = false;
        }
    }
}
