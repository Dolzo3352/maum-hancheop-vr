using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 순차 인터랙션 핸들러.
///
/// 하나의 스테이지에 여러 인터랙션이 순서대로 발생하는 경우 사용합니다.
/// director.Stop() / director.Play()로 Animator Controller Idle을 허용합니다.
/// </summary>
public class SequentialInteractionHandler : MonoBehaviour
{
    // ─── 참조 ───

    [Header("참조")]
    [Tooltip("이 스테이지의 PlayableDirector")]
    [SerializeField] private PlayableDirector director;

    [Tooltip("링 충전 시스템")]
    [SerializeField] private RingChargeSystem ringChargeSystem;

    [Header("인터랙션 순서")]
    [Tooltip("순서대로 수행할 인터랙션 목록")]
    [SerializeField] private List<RingInteractable> interactionSequence = new List<RingInteractable>();

    [Header("설정")]
    [Tooltip("인터랙션 간 재개 대기 시간")]
    [SerializeField] private float resumeDelay = 0.5f;

    [Tooltip("디버그 로그 출력")]
    [SerializeField] private bool debugLog = true;

    // ─── 상태 ───

    private int currentIndex = 0;
    private bool isWaiting;
    private double savedPauseTime;

    /// <summary>현재 대기 중인 인터랙션 인덱스.</summary>
    public int CurrentIndex => currentIndex;

    /// <summary>모든 인터랙션이 완료되었는지.</summary>
    public bool AllCompleted => currentIndex >= interactionSequence.Count;

    // ─── SignalReceiver에서 호출 ───

    public void HandleNextInteraction()
    {
        if (isWaiting || AllCompleted) return;
        StartCoroutine(WaitForCurrentInteraction());
    }

    private IEnumerator WaitForCurrentInteraction()
    {
        if (currentIndex >= interactionSequence.Count)
        {
            Debug.LogWarning("[SequentialInteractionHandler] 모든 인터랙션이 이미 완료되었습니다.", this);
            yield break;
        }

        isWaiting = true;
        var target = interactionSequence[currentIndex];

        if (target.IsCompleted)
        {
            currentIndex++;
            isWaiting = false;
            yield break;
        }

        // 타임라인 정지 — PlayableGraph 완전 해제
        if (director != null)
        {
            savedPauseTime = director.time;
            director.Stop();
            Log($"타임라인 정지 (time={savedPauseTime:F2}s). 대기 중 [{currentIndex + 1}/{interactionSequence.Count}]: {target.name}");
        }

        // 링 시스템 활성화
        if (ringChargeSystem != null)
            ringChargeSystem.IsEnabled = true;

        // 완료 대기
        bool done = false;
        void OnDone() { done = true; }
        target.OnInteractionDone += OnDone;

        while (!done)
            yield return null;

        target.OnInteractionDone -= OnDone;

        Log($"인터랙션 완료! [{currentIndex + 1}/{interactionSequence.Count}]: {target.name}");

        currentIndex++;

        // 대기 후 타임라인 재개
        if (resumeDelay > 0f)
            yield return new WaitForSeconds(resumeDelay);

        if (director != null)
        {
            director.time = savedPauseTime;
            director.Play();
            Log($"타임라인 재개 (time: {savedPauseTime:F2}s)");
        }

        isWaiting = false;
    }

    /// <summary>인덱스를 초기화합니다. 스테이지 재시작 시 사용.</summary>
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

    // ─── 유틸리티 ───

    private void Log(string message)
    {
        if (debugLog)
            Debug.Log($"[SequentialInteractionHandler] {message}", this);
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
