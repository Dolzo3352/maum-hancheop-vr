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
/// 동작 흐름 (The Line VR 스타일 크로스페이드):
///   1. TimelineController.OnTimelineFinished           → Stage 타임라인 종료 감지
///   2. DioramaStageManager.ActivateStageAdditive(next) → 다음 Stage 추가 활성화 (양쪽 동시)
///   3. StageLightingController.CrossfadeLighting       → 라이팅 크로스페이드 (시선 유도)
///   4. StageTransitionHandler.FadeOut                  → 짧은 암전
///   5. DioramaStageManager.DeactivateStage(prev)       → 이전 Stage만 끔
///   6. 텔레포트 (추후 시그널 연결)
///   7. TimelineController.SetDirector + Play           → 다음 Stage Director 재생
///   8. StageTransitionHandler.FadeIn                   → 화면 보임
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

    [Tooltip("라이팅 크로스페이드 컨트롤러 (없으면 기존 하드컷 방식)")]
    [SerializeField] private StageLightingController lightingController;

    [Tooltip("웨이포인트 텔레포터 (없으면 텔레포트 건너뜀)")]
    [SerializeField] private WaypointTeleporter waypointTeleporter;

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
        bool isFirstStage = currentStageIndex < 0;
        int prevStageIndex = currentStageIndex >= 0 ? stages[currentStageIndex].stageIndex : -1;

        // ─── 첫 Stage: 바로 활성화 + 재생 ───
        if (isFirstStage)
        {
            stageManager.ActivateStage(data.stageIndex);
            stageManager.ResetToDefaultScale();
            PlayStageTimeline(data);

            currentStageIndex = index;
            OnStageStart?.Invoke(index, data);
            Debug.Log($"[NarrativeSequencer] Stage {index} 시작: {data.stageName}", this);

            if (transitionHandler != null && fadeInOnStart)
                yield return transitionHandler.FadeIn(data.transitionDuration);

            isTransitioning = false;
            yield break;
        }

        // ─── 이후 Stage: 크로스페이드 전환 ───

        // 1. 다음 스테이지 추가 활성화 (이전 스테이지도 켜진 상태 유지)
        stageManager.ActivateStageAdditive(data.stageIndex);
        Debug.Log($"[NarrativeSequencer] 크로스페이드 시작: Stage {prevStageIndex} → {data.stageIndex}", this);

        // 2. 라이팅 크로스페이드 (있으면)
        if (lightingController != null)
        {
            lightingController.TurnOffStageLights(data.stageIndex);
            yield return lightingController.CrossfadeLighting(prevStageIndex, data.stageIndex);
        }

        // 3. 페이드 아웃 (짧은 암전)
        if (transitionHandler != null)
        {
            yield return transitionHandler.FadeOut(data.transitionDuration);
        }

        // 4. 암전 중: 이전 스테이지 비활성화
        stageManager.DeactivateStage(prevStageIndex);

        // 5. 스케일 초기화
        stageManager.ResetToDefaultScale();

        // 5-1. 텔레포트 (암전 중 사용자 이동)
        if (waypointTeleporter != null)
        {
            waypointTeleporter.TeleportImmediate(data.stageIndex);
            Debug.Log($"[NarrativeSequencer] 텔레포트: WP {data.stageIndex}", this);
        }

        // 6. 타임라인 재생
        PlayStageTimeline(data);

        // 7. 인덱스 갱신 및 이벤트 발행
        currentStageIndex = index;
        OnStageStart?.Invoke(index, data);
        Debug.Log($"[NarrativeSequencer] Stage {index} 시작: {data.stageName}", this);

        // 8. 페이드 인
        if (transitionHandler != null)
        {
            yield return transitionHandler.FadeIn(data.transitionDuration);
        }

        isTransitioning = false;
    }

    /// <summary>
    /// 스테이지의 PlayableDirector를 찾아서 재생합니다.
    /// </summary>
    private void PlayStageTimeline(StageData data)
    {
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
