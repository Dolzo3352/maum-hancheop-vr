using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마을 조명을 타임라인 시그널로 "켜기/끄기" 제어하는 컨트롤러.
///
/// ── 핵심 개념 ──
///   1) 그룹 = 동시에 켜지거나 꺼지는 조명 묶음
///   2) 스텝 = "그룹 X를 켜라" 또는 "그룹 Y를 꺼라" 같은 명령 하나
///   3) 타임라인에서 시그널을 보낼 때마다 Next()를 호출하면
///      스텝 리스트의 다음 명령이 실행됩니다.
///
/// ── 예시 ──
///   그룹0: 가로등 3개  /  그룹1: 창문 2개  /  그룹2: 간판 1개
///
///   스텝0: 켜기 → 그룹0 (가로등 3개 동시에 켜짐)
///   스텝1: 켜기 → 그룹1 (창문 2개 동시에 켜짐)
///   스텝2: 끄기 → 그룹0 (가로등 3개 꺼짐)
///   스텝3: 켜기 → 그룹2 (간판 켜짐)
///
/// ── 사용법 (유니티 초보용) ──
///   1. 빈 오브젝트 만들기 → 이름: "VillageLights"
///   2. 이 스크립트 붙이기
///   3. Inspector에서 설정:
///      [Light Groups] → 그룹 만들기 (조명 드래그)
///      [Steps]        → 스텝 만들기 (켜기/끄기 + 그룹 번호)
///      [Always On Light] → 처음부터 켜져 있을 조명 1개 드래그
///   4. 타임라인 시그널마다 Next() 연결
/// </summary>
public class VillageLightController : MonoBehaviour
{
    // ─── 그룹 정의 ───

    [Serializable]
    public class LightGroup
    {
        [Tooltip("이 그룹의 이름 (메모용, 기능에 영향 없음)")]
        public string groupName = "그룹";

        [Tooltip("동시에 켜지거나 꺼질 조명들")]
        public Light[] lights;

        [Tooltip("각 조명의 목표 밝기 (비워두면 모두 1)")]
        public float[] targetIntensities;

        public float GetTargetIntensity(int index)
        {
            if (targetIntensities != null && index < targetIntensities.Length && targetIntensities[index] > 0f)
                return targetIntensities[index];
            return 1f;
        }
    }

    // ─── 스텝(명령) 정의 ───

    public enum StepAction
    {
        [Tooltip("그룹을 서서히 켭니다")]
        FadeIn,
        [Tooltip("그룹을 서서히 끕니다")]
        FadeOut,
        [Tooltip("그룹을 즉시 켭니다")]
        TurnOn,
        [Tooltip("그룹을 즉시 끕니다")]
        TurnOff
    }

    [Serializable]
    public class LightStep
    {
        [Tooltip("이 스텝에서 할 일")]
        public StepAction action = StepAction.FadeIn;

        [Tooltip("대상 그룹 번호 (Light Groups 배열의 인덱스, 0부터 시작)")]
        public int groupIndex;

        [Tooltip("이 스텝만의 페이드 시간 (-1이면 기본값 사용)")]
        public float overrideFadeDuration = -1f;
    }

    // ─── Inspector 설정 ───

    [Header("─── 처음 켜져 있을 조명 ───")]
    [Tooltip("게임 시작 시 이 조명만 켜져 있습니다.\n비워두면 전부 끈 상태로 시작합니다.")]
    [SerializeField] private Light alwaysOnLight;

    [Tooltip("Always On Light의 밝기")]
    [SerializeField] private float alwaysOnIntensity = 1f;

    [Header("─── 조명 그룹 ───")]
    [Tooltip("그룹을 먼저 만드세요.\n같은 타이밍에 켜거나 끌 조명들을 하나의 그룹으로 묶습니다.")]
    [SerializeField] private LightGroup[] lightGroups;

    [Header("─── 실행 스텝 (순서대로) ───")]
    [Tooltip("타임라인 시그널이 올 때마다 다음 스텝이 실행됩니다.\n각 스텝에서 어떤 그룹을 켤지/끌지 정합니다.")]
    [SerializeField] private LightStep[] steps;

    [Header("─── 기본 타이밍 ───")]
    [Tooltip("FadeIn/FadeOut의 기본 페이드 시간 (초)")]
    [SerializeField] private float defaultFadeDuration = 1.0f;

    [Header("─── 페이드 커브 ───")]
    [Tooltip("밝아지거나 어두워지는 느낌 조절")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ─── 런타임 상태 ───

    private int currentStepIndex;
    private Dictionary<int, Coroutine> activeGroupCoroutines = new Dictionary<int, Coroutine>();

    // ─── 생명주기 ───

    private void Start()
    {
        InitializeLights();
    }

    /// <summary>
    /// 모든 그룹 조명을 끄고, alwaysOnLight만 켭니다.
    /// </summary>
    private void InitializeLights()
    {
        // 모든 그룹 조명 끄기
        if (lightGroups != null)
        {
            foreach (var group in lightGroups)
            {
                if (group.lights == null) continue;
                foreach (var light in group.lights)
                {
                    if (light != null)
                        light.intensity = 0f;
                }
            }
        }

        // 항상 켜져 있을 조명
        if (alwaysOnLight != null)
        {
            if (!alwaysOnLight.gameObject.activeSelf)
                alwaysOnLight.gameObject.SetActive(true);
            alwaysOnLight.intensity = alwaysOnIntensity;
        }

        currentStepIndex = 0;
    }

    // ─── 타임라인에서 호출할 메서드 ───

    /// <summary>
    /// 다음 스텝을 실행합니다.
    /// 타임라인 Signal Receiver에서 이 메서드를 연결하세요.
    /// 호출할 때마다 스텝이 하나씩 진행됩니다.
    /// </summary>
    [ContextMenu("테스트: 다음 스텝 실행")]
    public void Next()
    {
        if (steps == null || currentStepIndex >= steps.Length) return;

        ExecuteStep(steps[currentStepIndex]);
        currentStepIndex++;
    }

    /// <summary>
    /// 처음부터 다시 시작합니다.
    /// </summary>
    [ContextMenu("테스트: 처음으로 리셋")]
    public void ResetAll()
    {
        // 진행 중인 코루틴 모두 정지
        foreach (var pair in activeGroupCoroutines)
        {
            if (pair.Value != null)
                StopCoroutine(pair.Value);
        }
        activeGroupCoroutines.Clear();

        InitializeLights();
    }

    /// <summary>
    /// alwaysOnLight를 서서히 끕니다.
    /// 타임라인에서 원할 때 호출하세요.
    /// </summary>
    [ContextMenu("테스트: 상시조명 끄기")]
    public void FadeOutAlwaysOnLight()
    {
        if (alwaysOnLight != null)
            StartCoroutine(FadeSingleLight(alwaysOnLight, alwaysOnLight.intensity, 0f, defaultFadeDuration));
    }

    /// <summary>
    /// alwaysOnLight를 서서히 켭니다.
    /// </summary>
    [ContextMenu("테스트: 상시조명 켜기")]
    public void FadeInAlwaysOnLight()
    {
        if (alwaysOnLight != null)
        {
            if (!alwaysOnLight.gameObject.activeSelf)
                alwaysOnLight.gameObject.SetActive(true);
            StartCoroutine(FadeSingleLight(alwaysOnLight, alwaysOnLight.intensity, alwaysOnIntensity, defaultFadeDuration));
        }
    }

    // ─── 스텝 실행 ───

    private void ExecuteStep(LightStep step)
    {
        if (!IsValidGroup(step.groupIndex)) return;

        var group = lightGroups[step.groupIndex];
        float duration = step.overrideFadeDuration >= 0f ? step.overrideFadeDuration : defaultFadeDuration;

        // 이 그룹에 진행 중인 코루틴이 있으면 중지
        if (activeGroupCoroutines.TryGetValue(step.groupIndex, out var running) && running != null)
            StopCoroutine(running);

        switch (step.action)
        {
            case StepAction.FadeIn:
                activeGroupCoroutines[step.groupIndex] = StartCoroutine(FadeGroup(group, true, duration, step.groupIndex));
                break;

            case StepAction.FadeOut:
                activeGroupCoroutines[step.groupIndex] = StartCoroutine(FadeGroup(group, false, duration, step.groupIndex));
                break;

            case StepAction.TurnOn:
                SetGroupImmediate(group, true);
                break;

            case StepAction.TurnOff:
                SetGroupImmediate(group, false);
                break;
        }
    }

    // ─── 코루틴 ───

    private IEnumerator FadeGroup(LightGroup group, bool fadeIn, float duration, int groupIndex)
    {
        // 시작/종료 intensity 준비
        float[] startValues = new float[group.lights.Length];
        float[] endValues = new float[group.lights.Length];

        for (int i = 0; i < group.lights.Length; i++)
        {
            if (group.lights[i] == null) continue;

            if (fadeIn && !group.lights[i].gameObject.activeSelf)
                group.lights[i].gameObject.SetActive(true);

            startValues[i] = group.lights[i].intensity;
            endValues[i] = fadeIn ? group.GetTargetIntensity(i) : 0f;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveValue = fadeCurve.Evaluate(t);

            for (int i = 0; i < group.lights.Length; i++)
            {
                if (group.lights[i] != null)
                    group.lights[i].intensity = Mathf.Lerp(startValues[i], endValues[i], curveValue);
            }

            yield return null;
        }

        // 최종값 보장
        for (int i = 0; i < group.lights.Length; i++)
        {
            if (group.lights[i] != null)
                group.lights[i].intensity = endValues[i];
        }

        activeGroupCoroutines.Remove(groupIndex);
    }

    private IEnumerator FadeSingleLight(Light light, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            light.intensity = Mathf.Lerp(from, to, fadeCurve.Evaluate(t));
            yield return null;
        }
        light.intensity = to;
    }

    // ─── 즉시 켜기/끄기 ───

    private void SetGroupImmediate(LightGroup group, bool on)
    {
        for (int i = 0; i < group.lights.Length; i++)
        {
            if (group.lights[i] == null) continue;

            if (on && !group.lights[i].gameObject.activeSelf)
                group.lights[i].gameObject.SetActive(true);

            group.lights[i].intensity = on ? group.GetTargetIntensity(i) : 0f;
        }
    }

    // ─── 유틸 ───

    private bool IsValidGroup(int index)
    {
        return lightGroups != null && index >= 0 && index < lightGroups.Length;
    }
}
