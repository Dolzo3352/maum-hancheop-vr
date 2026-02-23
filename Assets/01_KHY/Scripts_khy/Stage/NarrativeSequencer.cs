using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Stage 간 전환을 관리하는 시퀀서.
///
/// 각 Stage의 타임라인을 순차 재생하며, 전환 연출을 조율합니다.
/// 각 Stage GameObject 안에 PlayableDirector가 있어야 하며,
/// 타임라인과 바인딩은 해당 Director의 Inspector에서 직접 설정합니다.
///
/// 동작 흐름:
///   1. TimelineController.OnTimelineFinished     → Stage 타임라인 종료 감지
///   2. StageTransitionHandler.FadeOut             → 화면 가림
///   3. DioramaStageManager.DeactivateAllStages    → 전부 끔
///   4. DioramaStageManager.ActivateStage(next)    → 다음 Stage 켬
///   5. TimelineController.SetDirector + Play      → 해당 Stage의 Director로 재생
///   6. StageTransitionHandler.FadeIn              → 화면 보임
///
/// 사용법:
///   매니저 오브젝트에 부착하고, Inspector에서 참조를 연결합니다.
///   StartNarrative()를 호출하면 첫 Stage부터 시작합니다.
/// </summary>
public class NarrativeSequencer : MonoBehaviour
{
    // ─── 필드 ───

    [Header("Stage 데이터")]
    [Tooltip("재생할 Stage 목록 (순서대로)")]
    [SerializeField] private List<StageData> stages = new List<StageData>();

    [Header("참조")]
    [SerializeField] private DioramaStageManager stageManager;
    [SerializeField] private TimelineController timelineController;
    [SerializeField] private StageTransitionHandler transitionHandler;

    [Header("설정")]
    [Tooltip("첫 Stage 시작 시에도 페이드 인 연출을 적용할지")]
    [SerializeField] private bool fadeInOnStart = true;

    [Tooltip("Play 시 자동으로 내러티브를 시작할지")]
    [SerializeField] private bool autoStart = true;

    // 현재 진행 중인 Stage 인덱스
    private int currentStageIndex = -1;

    // 시퀀서 상태
    private bool isPaused;
    private bool isTransitioning;

    // ─── 프로퍼티 ───

    /// <summary>현재 Stage 인덱스.</summary>
    public int CurrentStageIndex => currentStageIndex;

    /// <summary>현재 Stage 데이터. 시작 전이면 null.</summary>
    public StageData CurrentStageData =>
        currentStageIndex >= 0 && currentStageIndex < stages.Count
            ? stages[currentStageIndex]
            : null;

    /// <summary>일시정지 상태.</summary>
    public bool IsPaused => isPaused;

    // ─── 이벤트 ───

    /// <summary>Stage 시작 시 발행. (인덱스, StageData)</summary>
    public event Action<int, StageData> OnStageStart;

    /// <summary>Stage 종료 시 발행. (인덱스)</summary>
    public event Action<int> OnStageEnd;

    /// <summary>모든 Stage 완료 시 발행.</summary>
    public event Action OnNarrativeComplete;

    // ─── 초기화 ───

    private void Start()
    {
        if (autoStart)
            StartNarrative();
    }

    // ─── 공개 메서드 ───

    /// <summary>
    /// 첫 번째 Stage부터 내러티브를 시작합니다.
    /// </summary>
    public void StartNarrative()
    {
        if (stages.Count == 0)
        {
            Debug.LogWarning("[NarrativeSequencer] Stage 데이터가 없습니다.", this);
            return;
        }

        if (!ValidateReferences()) return;

        currentStageIndex = -1;
        isPaused = false;

        Debug.Log("[NarrativeSequencer] 내러티브 시작", this);
        AdvanceToNextStage();
    }

    /// <summary>
    /// 다음 Stage로 진행합니다.
    /// </summary>
    public void AdvanceToNextStage()
    {
        if (isTransitioning) return;

        int nextIndex = currentStageIndex + 1;

        // 현재 Stage 종료 이벤트
        if (currentStageIndex >= 0)
            OnStageEnd?.Invoke(currentStageIndex);

        // 마지막 Stage였으면 완료
        if (nextIndex >= stages.Count)
        {
            Debug.Log("[NarrativeSequencer] 내러티브 완료", this);
            OnNarrativeComplete?.Invoke();
            return;
        }

        StartCoroutine(TransitionToStage(nextIndex));
    }

    /// <summary>
    /// 특정 Stage로 점프합니다. 디버그/테스트용.
    /// </summary>
    public void GoToStage(int index)
    {
        if (index < 0 || index >= stages.Count)
        {
            Debug.LogWarning($"[NarrativeSequencer] 잘못된 인덱스: {index}", this);
            return;
        }

        if (isTransitioning) return;

        if (currentStageIndex >= 0)
            OnStageEnd?.Invoke(currentStageIndex);

        StartCoroutine(TransitionToStage(index));
    }

    /// <summary>일시정지.</summary>
    public void PauseNarrative()
    {
        isPaused = true;
        timelineController?.Pause();
        Debug.Log("[NarrativeSequencer] 일시정지", this);
    }

    /// <summary>재개.</summary>
    public void ResumeNarrative()
    {
        isPaused = false;
        timelineController?.Resume();
        Debug.Log("[NarrativeSequencer] 재개", this);
    }

    // ─── 타임라인 종료 감지 ───

    private void OnEnable()
    {
        if (timelineController != null)
            timelineController.OnTimelineFinished += HandleTimelineFinished;
    }

    private void OnDisable()
    {
        if (timelineController != null)
            timelineController.OnTimelineFinished -= HandleTimelineFinished;
    }

    private void HandleTimelineFinished()
    {
        if (isPaused) return;
        AdvanceToNextStage();
    }

    // ─── Stage 전환 코루틴 ───

    private IEnumerator TransitionToStage(int index)
    {
        isTransitioning = true;
        StageData data = stages[index];

        // 1. 페이드 아웃 (첫 Stage이고 fadeInOnStart가 false면 건너뜀)
        bool isFirstStage = currentStageIndex < 0;
        if (!isFirstStage && transitionHandler != null)
        {
            yield return transitionHandler.ExecuteTransition(
                data.entryTransition,
                data.transitionDuration
            );
        }

        // 2. 이전 Stage 끄기
        stageManager.DeactivateAllStages();

        // 3. 다음 Stage 켜기
        stageManager.ActivateStage(data.stageIndex);

        // 4. 스케일 초기화
        stageManager.ResetToDefaultScale();

        // 5. 스테이지의 PlayableDirector를 찾아서 재생
        var stageObj = stageManager.GetStage(data.stageIndex);
        var director = stageObj != null ? stageObj.GetComponentInChildren<PlayableDirector>() : null;

        if (director != null)
        {
            timelineController.SetDirector(director);
            timelineController.Play();
        }
        else
        {
            Debug.LogWarning($"[NarrativeSequencer] Stage {data.stageName}에 PlayableDirector가 없습니다.", this);
        }

        // 6. 인덱스 갱신 및 이벤트 발행
        currentStageIndex = index;
        OnStageStart?.Invoke(index, data);

        Debug.Log($"[NarrativeSequencer] Stage {index} 시작: {data.stageName}", this);

        // 7. 페이드 인
        if (transitionHandler != null && (fadeInOnStart || !isFirstStage))
        {
            yield return transitionHandler.FadeIn(data.transitionDuration);
        }

        isTransitioning = false;
    }

    // ─── 검증 ───

    private bool ValidateReferences()
    {
        bool valid = true;

        if (stageManager == null)
        {
            Debug.LogError("[NarrativeSequencer] DioramaStageManager 참조가 없습니다.", this);
            valid = false;
        }
        if (timelineController == null)
        {
            Debug.LogError("[NarrativeSequencer] TimelineController 참조가 없습니다.", this);
            valid = false;
        }

        return valid;
    }
}
