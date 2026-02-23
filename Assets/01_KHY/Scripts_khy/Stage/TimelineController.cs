using System;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// PlayableDirector 래퍼.
///
/// 개별 Stage의 타임라인 재생을 제어합니다.
/// NarrativeSequencer가 OnTimelineFinished를 구독하여 다음 Stage로 진행합니다.
///
/// 사용법:
///   Stage 타임라인을 재생하는 PlayableDirector가 있는 GameObject에 부착합니다.
/// </summary>
[RequireComponent(typeof(PlayableDirector))]
public class TimelineController : MonoBehaviour
{
    // ─── 필드 ───

    private PlayableDirector director;

    // ─── 이벤트 ───

    /// <summary>타임라인 재생 완료 시 발행. NarrativeSequencer가 구독합니다.</summary>
    public event Action OnTimelineFinished;

    /// <summary>타임라인 재생 시작 시 발행.</summary>
    public event Action OnTimelineStarted;

    // ─── 프로퍼티 ───

    /// <summary>현재 재생 중인지 여부.</summary>
    public bool IsPlaying => director != null && director.state == PlayState.Playing;

    /// <summary>현재 재생 시간 (초).</summary>
    public double CurrentTime => director != null ? director.time : 0;

    /// <summary>타임라인 전체 길이 (초).</summary>
    public double Duration => director != null && director.playableAsset != null
        ? director.playableAsset.duration
        : 0;

    // ─── 초기화 ───

    private void Awake()
    {
        director = GetComponent<PlayableDirector>();
        director.stopped += HandleTimelineStopped;
    }

    private void OnDestroy()
    {
        if (director != null)
            director.stopped -= HandleTimelineStopped;
    }

    // ─── 공개 메서드 ───

    /// <summary>
    /// 타임라인 에셋을 설정합니다. Stage 전환 시 NarrativeSequencer가 호출합니다.
    /// </summary>
    public void SetTimeline(PlayableAsset timelineAsset)
    {
        if (director == null) return;
        director.playableAsset = timelineAsset;
    }

    /// <summary>
    /// 현재 설정된 타임라인을 처음부터 재생합니다.
    /// </summary>
    public void Play()
    {
        if (director == null || director.playableAsset == null)
        {
            Debug.LogWarning("[TimelineController] PlayableAsset이 없습니다.", this);
            return;
        }

        director.time = 0;
        director.Play();
        OnTimelineStarted?.Invoke();

        Debug.Log($"[TimelineController] 재생 시작: {director.playableAsset.name}", this);
    }

    /// <summary>일시정지.</summary>
    public void Pause()
    {
        if (director == null) return;
        director.Pause();
    }

    /// <summary>일시정지 해제.</summary>
    public void Resume()
    {
        if (director == null) return;
        director.Resume();
    }

    /// <summary>정지. 타임라인을 끝냅니다.</summary>
    public void Stop()
    {
        if (director == null) return;
        director.Stop();
    }

    /// <summary>
    /// 특정 시간으로 이동합니다. 디버그/테스트용.
    /// </summary>
    public void JumpTo(double time)
    {
        if (director == null) return;
        director.time = Math.Clamp(time, 0, Duration);
        director.Evaluate();
    }

    // ─── 내부 ───

    private void HandleTimelineStopped(PlayableDirector stoppedDirector)
    {
        Debug.Log($"[TimelineController] 재생 완료: {stoppedDirector.playableAsset?.name}", this);
        OnTimelineFinished?.Invoke();
    }
}
