using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// 인터랙션 Signal 수신 및 처리 핸들러.
///
/// 타임라인 그래프 내부에 AnimationMixerPlayable을 삽입하여
/// Timeline 애니메이션 ↔ Idle 클립 간 부드러운 크로스페이드를 수행합니다.
///
/// Inspector 설정:
///   - Idle Clip을 연결하면 → 타임라인 클립 블렌드처럼 부드러운 전환
///   - 비워두면 → director.Stop()/Play() 폴백 (즉시 전환)
/// </summary>
public class InteractionSignalHandler : MonoBehaviour
{
    // ─── 참조 ───

    [Header("참조")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private RingInteractable targetInteractable;
    [SerializeField] private RingChargeSystem ringChargeSystem;

    // ─── 애니메이션 블렌드 ───

    [Header("애니메이션 블렌드")]
    [Tooltip("일시정지 중 재생할 Idle 클립 (비워두면 블렌드 없이 즉시 전환)")]
    [SerializeField] private AnimationClip idleClip;

    [Tooltip("타임라인 ↔ Idle 크로스페이드 시간 (초)")]
    [SerializeField] private float blendDuration = 0.3f;

    // ─── 설정 ───

    [Header("설정")]
    [SerializeField] private bool autoEnableRingSystem = true;
    [SerializeField] private float resumeDelay = 0.5f;
    [SerializeField] private bool debugLog = true;

    // ─── 상태 ───

    private bool isWaitingForInteraction;
    private double savedPauseTime;

    // Blend mixer (타임라인 그래프 내부에 삽입)
    private AnimationMixerPlayable blendMixer;
    private AnimationClipPlayable idlePlayable;
    private Playable savedSourcePlayable;
    private int savedSourcePort;
    private int animOutputIndex = -1;
    private bool mixerActive;

    public bool IsWaiting => isWaitingForInteraction;

    // ─── SignalReceiver에서 호출 ───

    public void HandleInteractionSignal()
    {
        if (isWaitingForInteraction)
        {
            Log("이미 인터랙션 대기 중입니다.");
            return;
        }
        if (targetInteractable == null || targetInteractable.IsCompleted) return;

        StartCoroutine(WaitForInteractionCoroutine());
    }

    // ─── 대기 코루틴 ───

    private IEnumerator WaitForInteractionCoroutine()
    {
        isWaitingForInteraction = true;

        // ── 1. 타임라인 일시정지 ──
        bool useMixer = idleClip != null;

        if (useMixer)
        {
            // 그래프를 살려두고 speed만 0
            if (director != null && director.playableGraph.IsValid())
            {
                savedPauseTime = director.time;
                director.playableGraph.GetRootPlayable(0).SetSpeed(0);
            }

            // Mixer 삽입 + 크로스페이드 (Timeline → Idle)
            if (InsertBlendMixer())
            {
                yield return Crossfade(toIdle: true);
                Log($"Idle 크로스페이드 완료. 대상: {targetInteractable.name}");
            }
        }
        else
        {
            // 폴백: director.Stop()
            if (director != null)
            {
                savedPauseTime = director.time;
                director.Stop();
                Log($"타임라인 정지 (폴백). 대상: {targetInteractable.name}");
            }
        }

        // ── 2. 링 충전 시스템 활성화 ──
        if (autoEnableRingSystem && ringChargeSystem != null)
            ringChargeSystem.IsEnabled = true;

        // ── 3. 인터랙션 완료 대기 (idle 시간 수동 갱신) ──
        bool interactionDone = false;
        void OnDone() { interactionDone = true; }
        targetInteractable.OnInteractionDone += OnDone;

        while (!interactionDone)
        {
            AdvanceIdleTime();
            yield return null;
        }

        targetInteractable.OnInteractionDone -= OnDone;
        Log($"인터랙션 완료! 대상: {targetInteractable.name}");

        // ── 4. 재개 전 대기 ──
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

        // ── 5. 재개 ──
        if (useMixer && mixerActive)
        {
            // 크로스페이드 (Idle → Timeline)
            yield return Crossfade(toIdle: false);
            RemoveBlendMixer();

            if (director != null && director.playableGraph.IsValid())
            {
                director.playableGraph.GetRootPlayable(0).SetSpeed(1);
                Log($"타임라인 재개 (time: {savedPauseTime:F2}s)");
            }
        }
        else
        {
            // 폴백 재개
            if (director != null)
            {
                director.time = savedPauseTime;
                director.Play();
                Log($"타임라인 재개 - 폴백 (time: {savedPauseTime:F2}s)");
            }
        }

        isWaitingForInteraction = false;
    }

    // ─── Blend Mixer 삽입/제거 ───

    /// <summary>
    /// 타임라인 그래프의 AnimationPlayableOutput에 Mixer를 끼워넣습니다.
    /// Timeline 애니메이션(input 0)과 Idle 클립(input 1)을 블렌드합니다.
    /// </summary>
    private bool InsertBlendMixer()
    {
        if (director == null || !director.playableGraph.IsValid()) return false;

        var graph = director.playableGraph;

        for (int i = 0; i < graph.GetOutputCount(); i++)
        {
            var output = graph.GetOutput(i);
            if (!output.IsOutputValid()) continue;
            if (output.GetPlayableOutputType() != typeof(AnimationPlayableOutput)) continue;

            // 현재 연결 저장
            savedSourcePlayable = output.GetSourcePlayable();
            savedSourcePort = output.GetSourceOutputPort();
            if (!savedSourcePlayable.IsValid()) continue;

            // 포트 유효성 검증 — Timeline 내부 그래프에서
            // GetSourceOutputPort()가 실제 출력 포트 범위를 벗어나는 경우 방어
            int srcOutputCount = savedSourcePlayable.GetOutputCount();
            if (savedSourcePort < 0 || savedSourcePort >= srcOutputCount)
                savedSourcePort = 0;

            animOutputIndex = i;

            // Mixer 생성 (2 입력: timeline, idle)
            blendMixer = AnimationMixerPlayable.Create(graph, 2);

            // Idle 클립 생성
            idlePlayable = AnimationClipPlayable.Create(graph, idleClip);

            // 연결: timeline → mixer[0], idle → mixer[1]
            graph.Connect(savedSourcePlayable, savedSourcePort, blendMixer, 0);
            graph.Connect(idlePlayable, 0, blendMixer, 1);

            // 초기값: 100% timeline, 0% idle
            blendMixer.SetInputWeight(0, 1f);
            blendMixer.SetInputWeight(1, 0f);

            // Mixer를 Output의 소스로 설정
            output.SetSourcePlayable(blendMixer, 0);

            mixerActive = true;
            Log("Blend Mixer 삽입 완료");
            return true;
        }

        Log("AnimationPlayableOutput을 찾을 수 없습니다.");
        return false;
    }

    /// <summary>
    /// Mixer를 제거하고 원래 연결을 복원합니다.
    /// </summary>
    private void RemoveBlendMixer()
    {
        if (!mixerActive) return;
        if (director == null || !director.playableGraph.IsValid()) return;

        var graph = director.playableGraph;

        // Output을 원래 소스로 복원
        if (animOutputIndex >= 0 && animOutputIndex < graph.GetOutputCount())
        {
            var output = graph.GetOutput(animOutputIndex);
            if (output.IsOutputValid())
                output.SetSourcePlayable(savedSourcePlayable, savedSourcePort);
        }

        // Mixer 입력 연결 해제 후 제거
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
        Log("Blend Mixer 제거 완료");
    }

    // ─── 크로스페이드 ───

    private IEnumerator Crossfade(bool toIdle)
    {
        if (!blendMixer.IsValid()) yield break;

        float elapsed = 0f;
        while (elapsed < blendDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / blendDuration);

            float timelineWeight = toIdle ? 1f - t : t;
            float idleWeight = toIdle ? t : 1f - t;

            blendMixer.SetInputWeight(0, timelineWeight);
            blendMixer.SetInputWeight(1, idleWeight);

            AdvanceIdleTime();
            yield return null;
        }

        blendMixer.SetInputWeight(0, toIdle ? 0f : 1f);
        blendMixer.SetInputWeight(1, toIdle ? 1f : 0f);
    }

    /// <summary>
    /// 타임라인 그래프가 멈춰있어도 Idle 클립은 계속 재생되도록 시간을 수동으로 전진.
    /// </summary>
    private void AdvanceIdleTime()
    {
        if (!idlePlayable.IsValid() || idleClip == null) return;

        double time = idlePlayable.GetTime() + Time.deltaTime;
        if (idleClip.length > 0f && time >= idleClip.length)
            time %= idleClip.length;
        idlePlayable.SetTime(time);
    }

    // ─── 유틸리티 ───

    private void Log(string message)
    {
        if (debugLog)
            Debug.Log($"[InteractionSignalHandler] {message}", this);
    }

    private void OnDisable()
    {
        if (isWaitingForInteraction)
        {
            StopAllCoroutines();
            isWaitingForInteraction = false;
        }
        RemoveBlendMixer();
    }

    private void OnDestroy()
    {
        RemoveBlendMixer();
    }
}
