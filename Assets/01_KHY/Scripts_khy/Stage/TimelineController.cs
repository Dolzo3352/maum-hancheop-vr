using System;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// PlayableDirector 래퍼.
///
/// 스테이지별 PlayableDirector를 받아서 재생을 제어합니다.
/// 각 Stage GameObject 안에 PlayableDirector가 있고,
/// NarrativeSequencer가 스테이지 전환 시 SetDirector()로 교체합니다.
///
/// 사용법:
///   NarrativeManager 오브젝트에 부착합니다.
///   (더 이상 RequireComponent(PlayableDirector)가 아닙니다)
/// </summary>
public class TimelineController : MonoBehaviour
{
    // ─── 필드 ───

    // 현재 제어 중인 Director (스테이지마다 교체됨)
    private PlayableDirector currentDirector;

    // ─── 이벤트 ───

    /// <summary>타임라인 재생 완료 시 발행. NarrativeSequencer가 구독합니다.</summary>
    public event Action OnTimelineFinished;

    /// <summary>타임라인 재생 시작 시 발행.</summary>
    public event Action OnTimelineStarted;

    // ─── 프로퍼티 ───

    /// <summary>현재 재생 중인지 여부.</summary>
    public bool IsPlaying => currentDirector != null && currentDirector.state == PlayState.Playing;

    /// <summary>현재 재생 시간 (초).</summary>
    public double CurrentTime => currentDirector != null ? currentDirector.time : 0;

    /// <summary>타임라인 전체 길이 (초).</summary>
    public double Duration => currentDirector != null && currentDirector.playableAsset != null
        ? currentDirector.playableAsset.duration
        : 0;

    /// <summary>현재 제어 중인 PlayableDirector.</summary>
    public PlayableDirector CurrentDirector => currentDirector;

    // ─── 공개 메서드 ───

    /// <summary>
    /// 제어할 PlayableDirector를 교체합니다.
    /// Stage 전환 시 NarrativeSequencer가 호출합니다.
    /// 이전 Director의 이벤트 구독을 해제하고, 새 Director에 구독합니다.
    /// </summary>
    public void SetDirector(PlayableDirector newDirector)
    {
        // 이전 Director 구독 해제
        if (currentDirector != null)
        {
            currentDirector.stopped -= HandleTimelineStopped;
        }

        currentDirector = newDirector;

        // 새 Director 구독
        if (currentDirector != null)
        {
            currentDirector.stopped += HandleTimelineStopped;
            Debug.Log($"[TimelineController] Director 교체: {currentDirector.gameObject.name}", this);
        }
    }

    /// <summary>
    /// 현재 Director의 타임라인을 처음부터 재생합니다.
    /// </summary>
    public void Play()
    {
        if (currentDirector == null || currentDirector.playableAsset == null)
        {
            Debug.LogWarning("[TimelineController] Director 또는 PlayableAsset이 없습니다.", this);
            return;
        }

        currentDirector.time = 0;
        currentDirector.Play();
        OnTimelineStarted?.Invoke();

        Debug.Log($"[TimelineController] 재생 시작: {currentDirector.playableAsset.name}", this);
    }

    /// <summary>일시정지.</summary>
    public void Pause()
    {
        if (currentDirector == null) return;
        currentDirector.Pause();
    }

    /// <summary>일시정지 해제.</summary>
    public void Resume()
    {
        if (currentDirector == null) return;
        currentDirector.Resume();
    }

    /// <summary>정지. 타임라인을 끝냅니다.</summary>
    public void Stop()
    {
        if (currentDirector == null) return;
        currentDirector.Stop();
    }

    /// <summary>
    /// 특정 시간으로 이동합니다. 디버그/테스트용.
    /// </summary>
    public void JumpTo(double time)
    {
        if (currentDirector == null) return;
        currentDirector.time = Math.Clamp(time, 0, Duration);
        currentDirector.Evaluate();
    }

    // ─── 정리 ───

    private void OnDestroy()
    {
        if (currentDirector != null)
            currentDirector.stopped -= HandleTimelineStopped;
    }

    // ─── 내부 ───

    private void HandleTimelineStopped(PlayableDirector stoppedDirector)
    {
        Debug.Log($"[TimelineController] 재생 완료: {stoppedDirector.playableAsset?.name}", this);
        OnTimelineFinished?.Invoke();
    }
}
