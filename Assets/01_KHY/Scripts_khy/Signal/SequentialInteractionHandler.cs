using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// 순차 인터랙션 핸들러.
/// AnimationMixerPlayable 기반 크로스페이드를 지원합니다.
/// </summary>
public class SequentialInteractionHandler : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private RingChargeSystem ringChargeSystem;

    [Header("인터랙션 순서")]
    [SerializeField] private List<RingInteractable> interactionSequence = new List<RingInteractable>();

    [Header("애니메이션 블렌드")]
    [Tooltip("일시정지 중 재생할 Idle 클립")]
    [SerializeField] private AnimationClip idleClip;
    [Tooltip("타임라인 ↔ Idle 크로스페이드 시간 (초)")]
    [SerializeField] private float blendDuration = 0.3f;

    [Header("설정")]
    [SerializeField] private float resumeDelay = 0.5f;
    [SerializeField] private bool debugLog = true;

    private int currentIndex = 0;
    private bool isWaiting;
    private double savedPauseTime;

    // Blend mixer
    private AnimationMixerPlayable blendMixer;
    private AnimationClipPlayable idlePlayable;
    private Playable savedSourcePlayable;
    private int savedSourcePort;
    private int animOutputIndex = -1;
    private bool mixerActive;

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

        bool useMixer = idleClip != null;

        // ── 일시정지 + 블렌드 인 ──
        if (useMixer)
        {
            if (director != null && director.playableGraph.IsValid())
            {
                savedPauseTime = director.time;
                director.playableGraph.GetRootPlayable(0).SetSpeed(0);
            }
            if (InsertBlendMixer())
                yield return Crossfade(toIdle: true);
        }
        else
        {
            if (director != null)
            {
                savedPauseTime = director.time;
                director.Stop();
            }
        }

        Log($"대기 중 [{currentIndex + 1}/{interactionSequence.Count}]: {target.name}");

        if (ringChargeSystem != null)
            ringChargeSystem.IsEnabled = true;

        // 완료 대기
        bool done = false;
        void OnDone() { done = true; }
        target.OnInteractionDone += OnDone;
        while (!done)
        {
            AdvanceIdleTime();
            yield return null;
        }
        target.OnInteractionDone -= OnDone;

        Log($"완료! [{currentIndex + 1}/{interactionSequence.Count}]: {target.name}");
        currentIndex++;

        if (resumeDelay > 0f)
        {
            float waited = 0f;
            while (waited < resumeDelay)
            {
                AdvanceIdleTime();
                waited += Time.deltaTime;
                yield return null;
            }
        }

        // ── 블렌드 아웃 + 재개 ──
        if (useMixer && mixerActive)
        {
            yield return Crossfade(toIdle: false);
            RemoveBlendMixer();

            if (director != null && director.playableGraph.IsValid())
                director.playableGraph.GetRootPlayable(0).SetSpeed(1);
        }
        else
        {
            if (director != null)
            {
                director.time = savedPauseTime;
                director.Play();
            }
        }

        isWaiting = false;
    }

    // ─── Blend Mixer ───

    private bool InsertBlendMixer()
    {
        if (director == null || !director.playableGraph.IsValid()) return false;

        var graph = director.playableGraph;

        for (int i = 0; i < graph.GetOutputCount(); i++)
        {
            var output = graph.GetOutput(i);
            if (!output.IsOutputValid()) continue;
            if (output.GetPlayableOutputType() != typeof(AnimationPlayableOutput)) continue;

            savedSourcePlayable = output.GetSourcePlayable();
            savedSourcePort = output.GetSourceOutputPort();
            if (!savedSourcePlayable.IsValid()) continue;

            // 포트 유효성 검증
            int srcOutputCount = savedSourcePlayable.GetOutputCount();
            if (savedSourcePort < 0 || savedSourcePort >= srcOutputCount)
                savedSourcePort = 0;

            animOutputIndex = i;

            blendMixer = AnimationMixerPlayable.Create(graph, 2);
            idlePlayable = AnimationClipPlayable.Create(graph, idleClip);

            graph.Connect(savedSourcePlayable, savedSourcePort, blendMixer, 0);
            graph.Connect(idlePlayable, 0, blendMixer, 1);

            blendMixer.SetInputWeight(0, 1f);
            blendMixer.SetInputWeight(1, 0f);

            output.SetSourcePlayable(blendMixer, 0);

            mixerActive = true;
            return true;
        }
        return false;
    }

    private void RemoveBlendMixer()
    {
        if (!mixerActive) return;
        if (director == null || !director.playableGraph.IsValid()) return;

        var graph = director.playableGraph;

        if (animOutputIndex >= 0 && animOutputIndex < graph.GetOutputCount())
        {
            var output = graph.GetOutput(animOutputIndex);
            if (output.IsOutputValid())
                output.SetSourcePlayable(savedSourcePlayable, savedSourcePort);
        }

        if (blendMixer.IsValid())
        {
            graph.Disconnect(blendMixer, 0);
            graph.Disconnect(blendMixer, 1);
            graph.DestroySubgraph(blendMixer);
        }
        if (idlePlayable.IsValid())
            graph.DestroySubgraph(idlePlayable);

        mixerActive = false;
        animOutputIndex = -1;
    }

    private IEnumerator Crossfade(bool toIdle)
    {
        if (!blendMixer.IsValid()) yield break;

        float elapsed = 0f;
        while (elapsed < blendDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / blendDuration);
            blendMixer.SetInputWeight(0, toIdle ? 1f - t : t);
            blendMixer.SetInputWeight(1, toIdle ? t : 1f - t);
            AdvanceIdleTime();
            yield return null;
        }
        blendMixer.SetInputWeight(0, toIdle ? 0f : 1f);
        blendMixer.SetInputWeight(1, toIdle ? 1f : 0f);
    }

    private void AdvanceIdleTime()
    {
        if (!idlePlayable.IsValid() || idleClip == null) return;
        double time = idlePlayable.GetTime() + Time.deltaTime;
        if (idleClip.length > 0f && time >= idleClip.length)
            time %= idleClip.length;
        idlePlayable.SetTime(time);
    }

    // ─── 기타 ───

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
        RemoveBlendMixer();
    }

    private void OnDestroy()
    {
        RemoveBlendMixer();
    }
}
