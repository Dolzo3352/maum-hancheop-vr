using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// Timeline ↔ Idle 애니메이션 간 부드러운 크로스페이드를 처리합니다.
///
/// 핵심: Timeline은 매 프레임 AnimationPlayableOutput.SetWeight(1)을 강제하므로,
/// PlayableBehaviour를 삽입하여 ProcessFrame에서 weight를 곱해 우회합니다.
///
/// 사용법:
///   blender.FadeToIdle(idleClip, () => { /* 페이드 완료 */ });
///   blender.FadeToTimeline(() => { /* 페이드 완료 */ });
/// </summary>
public class TimelineAnimationBlender : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private Animator animator;

    [Tooltip("추가로 상태를 제어할 Animator. 없으면 비워두세요.")]
    [SerializeField] private Animator secondaryAnimator;

    [Header("설정")]
    [SerializeField] private float blendDuration = 0.3f;

    // Idle 전용 그래프 — Primary
    private PlayableGraph idleGraph;
    private AnimationPlayableOutput idleOutput;
    private AnimationClipPlayable idlePlayable;

    // Idle 전용 그래프 — Secondary
    private PlayableGraph idleGraphSecondary;
    private AnimationPlayableOutput idleOutputSecondary;
    private AnimationClipPlayable idlePlayableSecondary;

    // Timeline weight 제어
    private ScriptPlayable<TimelineWeightBehaviour> weightPlayable;
    private bool behaviourInjected;

    private Coroutine blendCoroutine;

    public bool IsBlendedToIdle { get; private set; }
    public float BlendDuration { get => blendDuration; set => blendDuration = value; }

    // ─── 공개 API ───

    /// <summary>
    /// Timeline → Idle 크로스페이드.
    /// Timeline은 SetSpeed(0)으로 정지되고, Idle 애니메이션이 루프 재생됩니다.
    /// </summary>
    /// <param name="secondaryIdleClip">secondaryAnimator용 Idle 클립. null이면 secondaryAnimator는 제어하지 않습니다.</param>
    public void FadeToIdle(AnimationClip idleClip, AnimationClip secondaryIdleClip = null, Action onComplete = null)
    {
        if (blendCoroutine != null) StopCoroutine(blendCoroutine);
        SetupIdleGraph(idleClip);
        if (secondaryAnimator != null && secondaryIdleClip != null)
            SetupIdleGraphSecondary(secondaryIdleClip);
        InjectWeightBehaviour();
        blendCoroutine = StartCoroutine(DoFadeToIdle(onComplete));
    }

    /// <summary>
    /// Idle → Timeline 크로스페이드.
    /// Timeline이 SetSpeed(1)로 재개되고, Idle 그래프가 페이드아웃됩니다.
    /// </summary>
    public void FadeToTimeline(Action onComplete = null)
    {
        if (blendCoroutine != null) StopCoroutine(blendCoroutine);
        blendCoroutine = StartCoroutine(DoFadeToTimeline(onComplete));
    }

    // ─── 크로스페이드 코루틴 ───

    private IEnumerator DoFadeToIdle(Action onComplete)
    {
        // 크로스페이드: Timeline weight 1→0, Idle weight 0→1
        float elapsed = 0f;
        while (elapsed < blendDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / blendDuration);
            ApplyWeights(1f - t, t);
            yield return null;
        }
        ApplyWeights(0f, 1f);

        // Timeline 시간 정지 (그래프는 유지)
        FreezeTimeline();
        IsBlendedToIdle = true;
        blendCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator DoFadeToTimeline(Action onComplete)
    {
        // Timeline 시간 재개
        UnfreezeTimeline();

        // 크로스페이드: Timeline weight 0→1, Idle weight 1→0
        float elapsed = 0f;
        while (elapsed < blendDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / blendDuration);
            ApplyWeights(t, 1f - t);
            yield return null;
        }
        ApplyWeights(1f, 0f);

        IsBlendedToIdle = false;
        CleanupIdleGraph();
        CleanupIdleGraphSecondary();
        blendCoroutine = null;
        onComplete?.Invoke();
    }

    // ─── Weight 적용 ───

    private void ApplyWeights(float timelineWeight, float idleWeight)
    {
        // Timeline weight (PlayableBehaviour를 통해 적용)
        if (weightPlayable.IsValid())
            weightPlayable.GetBehaviour().weight = timelineWeight;

        // Idle weight (별도 그래프이므로 직접 설정 가능)
        if (idleGraph.IsValid() && idleOutput.IsOutputValid())
            idleOutput.SetWeight(idleWeight);

        // Secondary Idle weight
        if (idleGraphSecondary.IsValid() && idleOutputSecondary.IsOutputValid())
            idleOutputSecondary.SetWeight(idleWeight);
    }

    // ─── Timeline 시간 제어 ───

    private void FreezeTimeline()
    {
        if (director != null && director.playableGraph.IsValid())
            director.playableGraph.GetRootPlayable(0).SetSpeed(0);
    }

    private void UnfreezeTimeline()
    {
        if (director != null && director.playableGraph.IsValid())
            director.playableGraph.GetRootPlayable(0).SetSpeed(1);
    }

    // ─── Idle 그래프 관리 ───

    private void SetupIdleGraph(AnimationClip clip)
    {
        CleanupIdleGraph();

        idleGraph = PlayableGraph.Create("IdleLoop");
        idleGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        idlePlayable = AnimationClipPlayable.Create(idleGraph, clip);
        idleOutput = AnimationPlayableOutput.Create(idleGraph, "IdleOutput", animator);
        idleOutput.SetSourcePlayable(idlePlayable);
        idleOutput.SetWeight(0f);

        idleGraph.Play();
    }

    private void SetupIdleGraphSecondary(AnimationClip clip)
    {
        CleanupIdleGraphSecondary();

        idleGraphSecondary = PlayableGraph.Create("IdleLoopSecondary");
        idleGraphSecondary.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        idlePlayableSecondary = AnimationClipPlayable.Create(idleGraphSecondary, clip);
        idleOutputSecondary = AnimationPlayableOutput.Create(idleGraphSecondary, "IdleOutputSecondary", secondaryAnimator);
        idleOutputSecondary.SetSourcePlayable(idlePlayableSecondary);
        idleOutputSecondary.SetWeight(0f);

        idleGraphSecondary.Play();
    }

    private void CleanupIdleGraph()
    {
        if (idleGraph.IsValid())
            idleGraph.Destroy();
    }

    private void CleanupIdleGraphSecondary()
    {
        if (idleGraphSecondary.IsValid())
            idleGraphSecondary.Destroy();
    }

    // ─── Timeline Weight Behaviour 삽입 ───

    private void InjectWeightBehaviour()
    {
        if (director == null || !director.playableGraph.IsValid()) return;

        // 이미 유효한 behaviour가 있으면 재사용
        if (behaviourInjected && weightPlayable.IsValid()) return;

        var graph = director.playableGraph;
        weightPlayable = ScriptPlayable<TimelineWeightBehaviour>.Create(graph);
        var output = ScriptPlayableOutput.Create(graph, "WeightControl");
        output.SetSourcePlayable(weightPlayable);

        var behaviour = weightPlayable.GetBehaviour();
        behaviour.weight = 1f;
        behaviour.outputs.Clear();

        // 지정된 Animator(들)의 AnimationPlayableOutput만 수집
        // (다른 트랙 — 위치, 약초 등 — 은 weight를 유지)
        for (int i = 0; i < graph.GetOutputCount(); i++)
        {
            var graphOutput = graph.GetOutput(i);
            if (graphOutput.IsOutputValid() &&
                graphOutput.GetPlayableOutputType() == typeof(AnimationPlayableOutput))
            {
                var animOutput = (AnimationPlayableOutput)graphOutput;
                var target = animOutput.GetTarget();
                if (target == animator || (secondaryAnimator != null && target == secondaryAnimator))
                {
                    behaviour.outputs.Add(animOutput);
                }
            }
        }

        behaviourInjected = true;
    }

    // ─── 정리 ───

    private void OnDestroy()
    {
        CleanupIdleGraph();
        CleanupIdleGraphSecondary();
    }
}

/// <summary>
/// Timeline의 ProcessFrame 이후에 실행되어
/// AnimationPlayableOutput의 weight를 곱합니다.
/// Timeline이 매 프레임 weight를 1.0으로 덮어쓰는 것을 우회합니다.
/// </summary>
public class TimelineWeightBehaviour : PlayableBehaviour
{
    public float weight = 1f;
    public List<AnimationPlayableOutput> outputs = new List<AnimationPlayableOutput>();

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        foreach (var output in outputs)
        {
            if (output.IsOutputValid())
                output.SetWeight(output.GetWeight() * weight);
        }
    }
}
