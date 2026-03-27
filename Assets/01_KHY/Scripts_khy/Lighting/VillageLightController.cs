using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마을 조명을 타임라인 시그널로 제어하는 컨트롤러 (v3).
///
/// ── 핵심 개념 ──
///   그룹   = 동시에 제어되는 조명 묶음 (논, 마을, 산, 나무, 한약방 등)
///   커맨드 = "그룹X를 켜라/끄라/밝기 바꿔라" 같은 명령 하나
///   스텝   = 한 시그널에 실행될 커맨드 묶음 (여러 그룹 동시 제어 가능!)
///
/// ── 예시 ──
///   스텝0: [논 FadeIn 0.8]
///   스텝1: [마을 FadeIn 0.9] + [나무 FadeOut]
///   스텝2: [산 FadeIn 0.8]
///   스텝3: [한약방 FadeIn 1.0] + [논 Dim 0.6] + [마을 Dim 0.7] + [산 Dim 0.7]
///   스텝4: [논 Dim 0.8] + [마을 Dim 0.9] + [산 Dim 0.8]
///
/// ── 사용법 ──
///   1. 빈 오브젝트 → 이 스크립트 붙이기
///   2. Light Groups: 그룹 만들기
///   3. Steps: 스텝 만들기 (각 스텝 안에 커맨드 여러 개 가능)
///   4. 타임라인 시그널마다 Next() 연결
/// </summary>
public class VillageLightController : MonoBehaviour
{
    // ─── 그룹 정의 ───

    [Serializable]
    public class LightGroup
    {
        [Tooltip("이 그룹의 이름 (메모용)")]
        public string groupName = "그룹";

        [Tooltip("동시에 제어될 조명들")]
        public Light[] lights;

        [Tooltip("조명마다 개별 목표 밝기.\n비워두면 각 조명의 원래 밝기를 사용합니다.")]
        public float[] targetIntensities;

        [HideInInspector] public float[] originalIntensities;

        public void CacheOriginalIntensities()
        {
            if (lights == null) return;
            originalIntensities = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                    originalIntensities[i] = lights[i].intensity;
            }
        }

        /// <summary>
        /// 해당 조명의 기본 밝기(배율 적용 전)를 반환합니다.
        /// </summary>
        public float GetBaseIntensity(int index)
        {
            if (targetIntensities != null && index < targetIntensities.Length && targetIntensities[index] > 0f)
                return targetIntensities[index];

            if (originalIntensities != null && index < originalIntensities.Length && originalIntensities[index] > 0f)
                return originalIntensities[index];

            return 1f;
        }
    }

    // ─── 액션 종류 ───

    public enum CommandAction
    {
        [Tooltip("서서히 켜기 (0 → 목표 밝기)")]
        FadeIn,
        [Tooltip("서서히 끄기 (현재 → 0)")]
        FadeOut,
        [Tooltip("밝기 변경 (이미 켜진 그룹의 밝기를 서서히 바꿈)")]
        Dim,
        [Tooltip("즉시 켜기")]
        TurnOn,
        [Tooltip("즉시 끄기")]
        TurnOff
    }

    // ─── 커맨드 (하나의 명령) ───

    [Serializable]
    public class LightCommand
    {
        [Tooltip("할 일")]
        public CommandAction action = CommandAction.FadeIn;

        [Tooltip("대상 그룹 번호 (0부터 시작)")]
        public int groupIndex;

        [Tooltip("이 커맨드에서 사용할 밝기 배율.\nFadeIn: 0.8이면 원래 밝기의 80%로 켜짐\nDim: 0.6이면 현재 밝기에서 60%로 변경\n-1이면 그룹 기본값(1.0) 사용")]
        public float intensityMultiplier = -1f;

        [Tooltip("페이드 시간 (-1이면 기본값 사용)")]
        public float overrideFadeDuration = -1f;
    }

    // ─── 스텝 (한 시그널에 실행될 커맨드 묶음) ───

    [Serializable]
    public class LightStep
    {
        [Tooltip("이 스텝의 이름 (메모용)")]
        public string stepName = "스텝";

        [Tooltip("이 스텝에서 동시에 실행할 커맨드들")]
        public LightCommand[] commands;
    }

    // ─── Inspector 설정 ───

    [Header("─── 처음 켜져 있을 조명 ───")]
    [Tooltip("게임 시작 시 이 조명만 켜져 있습니다.")]
    [SerializeField] private Light alwaysOnLight;

    [Tooltip("Always On Light의 밝기")]
    [SerializeField] private float alwaysOnIntensity = 1f;

    [Header("─── 조명 그룹 ───")]
    [Tooltip("그룹을 먼저 만드세요.")]
    [SerializeField] private LightGroup[] lightGroups;

    [Header("─── 실행 스텝 (순서대로) ───")]
    [Tooltip("타임라인 시그널이 올 때마다 다음 스텝이 실행됩니다.\n각 스텝 안에 여러 커맨드를 넣으면 동시에 실행됩니다.")]
    [SerializeField] private LightStep[] steps;

    [Header("─── 기본 타이밍 ───")]
    [Tooltip("FadeIn / Dim 의 기본 페이드 시간 (초)")]
    [SerializeField] private float defaultFadeInDuration = 1.0f;

    [Tooltip("FadeOut 의 기본 페이드 시간 (초)")]
    [SerializeField] private float defaultFadeOutDuration = 1.0f;

    [Header("─── 페이드 커브 ───")]
    [Tooltip("켜질 때 / 밝기 변경 시 느낌")]
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("꺼질 때 느낌")]
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ─── 런타임 상태 ───

    private int currentStepIndex;
    private bool hasCachedOriginals = false;
    private Dictionary<int, Coroutine> activeGroupCoroutines = new Dictionary<int, Coroutine>();

    // ─── 생명주기 ───

    private void Start()
    {
        if (!hasCachedOriginals)
        {
            if (lightGroups != null)
            {
                foreach (var group in lightGroups)
                    group.CacheOriginalIntensities();
            }
            hasCachedOriginals = true;
        }

        InitializeLights();
    }

    private void InitializeLights()
    {
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
    /// </summary>
    [ContextMenu("테스트: 다음 스텝 실행")]
    public void Next()
    {
        if (steps == null || currentStepIndex >= steps.Length) return;

        ExecuteStep(steps[currentStepIndex]);
        currentStepIndex++;
    }

    [ContextMenu("테스트: 처음으로 리셋")]
    public void ResetAll()
    {
        foreach (var pair in activeGroupCoroutines)
        {
            if (pair.Value != null)
                StopCoroutine(pair.Value);
        }
        activeGroupCoroutines.Clear();
        InitializeLights();
    }

    [ContextMenu("테스트: 상시조명 끄기")]
    public void FadeOutAlwaysOnLight()
    {
        if (alwaysOnLight != null)
            StartCoroutine(FadeSingleLight(alwaysOnLight, alwaysOnLight.intensity, 0f, defaultFadeOutDuration, fadeOutCurve));
    }

    [ContextMenu("테스트: 상시조명 켜기")]
    public void FadeInAlwaysOnLight()
    {
        if (alwaysOnLight != null)
        {
            if (!alwaysOnLight.gameObject.activeSelf)
                alwaysOnLight.gameObject.SetActive(true);
            StartCoroutine(FadeSingleLight(alwaysOnLight, alwaysOnLight.intensity, alwaysOnIntensity, defaultFadeInDuration, fadeInCurve));
        }
    }

    // ─── 스텝 실행 ───

    private void ExecuteStep(LightStep step)
    {
        if (step.commands == null) return;

        foreach (var cmd in step.commands)
            ExecuteCommand(cmd);
    }

    private void ExecuteCommand(LightCommand cmd)
    {
        if (!IsValidGroup(cmd.groupIndex)) return;

        var group = lightGroups[cmd.groupIndex];
        float multiplier = cmd.intensityMultiplier >= 0f ? cmd.intensityMultiplier : 1f;

        // 이 그룹에 진행 중인 코루틴 중지
        if (activeGroupCoroutines.TryGetValue(cmd.groupIndex, out var running) && running != null)
            StopCoroutine(running);

        switch (cmd.action)
        {
            case CommandAction.FadeIn:
            {
                float duration = cmd.overrideFadeDuration >= 0f ? cmd.overrideFadeDuration : defaultFadeInDuration;
                activeGroupCoroutines[cmd.groupIndex] = StartCoroutine(
                    FadeGroupTo(group, multiplier, duration, fadeInCurve, cmd.groupIndex, activateObjects: true));
                break;
            }

            case CommandAction.FadeOut:
            {
                float duration = cmd.overrideFadeDuration >= 0f ? cmd.overrideFadeDuration : defaultFadeOutDuration;
                activeGroupCoroutines[cmd.groupIndex] = StartCoroutine(
                    FadeGroupToZero(group, duration, fadeOutCurve, cmd.groupIndex));
                break;
            }

            case CommandAction.Dim:
            {
                float duration = cmd.overrideFadeDuration >= 0f ? cmd.overrideFadeDuration : defaultFadeInDuration;
                activeGroupCoroutines[cmd.groupIndex] = StartCoroutine(
                    FadeGroupTo(group, multiplier, duration, fadeInCurve, cmd.groupIndex, activateObjects: false));
                break;
            }

            case CommandAction.TurnOn:
                SetGroupImmediate(group, multiplier);
                break;

            case CommandAction.TurnOff:
                SetGroupOff(group);
                break;
        }
    }

    // ─── 코루틴 ───

    /// <summary>
    /// 그룹의 각 조명을 현재 밝기 → (기본밝기 × multiplier) 로 서서히 변경합니다.
    /// FadeIn과 Dim 모두 이 메서드를 사용합니다.
    /// </summary>
    private IEnumerator FadeGroupTo(LightGroup group, float multiplier, float duration, AnimationCurve curve, int groupIndex, bool activateObjects)
    {
        float[] startValues = new float[group.lights.Length];
        float[] endValues = new float[group.lights.Length];

        for (int i = 0; i < group.lights.Length; i++)
        {
            if (group.lights[i] == null) continue;

            if (activateObjects && !group.lights[i].gameObject.activeSelf)
                group.lights[i].gameObject.SetActive(true);

            startValues[i] = group.lights[i].intensity;
            endValues[i] = group.GetBaseIntensity(i) * multiplier;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveValue = curve.Evaluate(t);

            for (int i = 0; i < group.lights.Length; i++)
            {
                if (group.lights[i] != null)
                    group.lights[i].intensity = Mathf.Lerp(startValues[i], endValues[i], curveValue);
            }

            yield return null;
        }

        for (int i = 0; i < group.lights.Length; i++)
        {
            if (group.lights[i] != null)
                group.lights[i].intensity = endValues[i];
        }

        activeGroupCoroutines.Remove(groupIndex);
    }

    /// <summary>
    /// 그룹의 각 조명을 현재 밝기 → 0 으로 서서히 끕니다.
    /// </summary>
    private IEnumerator FadeGroupToZero(LightGroup group, float duration, AnimationCurve curve, int groupIndex)
    {
        float[] startValues = new float[group.lights.Length];

        for (int i = 0; i < group.lights.Length; i++)
        {
            if (group.lights[i] != null)
                startValues[i] = group.lights[i].intensity;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveValue = curve.Evaluate(t);

            for (int i = 0; i < group.lights.Length; i++)
            {
                if (group.lights[i] != null)
                    group.lights[i].intensity = Mathf.Lerp(startValues[i], 0f, curveValue);
            }

            yield return null;
        }

        for (int i = 0; i < group.lights.Length; i++)
        {
            if (group.lights[i] != null)
                group.lights[i].intensity = 0f;
        }

        activeGroupCoroutines.Remove(groupIndex);
    }

    private IEnumerator FadeSingleLight(Light light, float from, float to, float duration, AnimationCurve curve)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            light.intensity = Mathf.Lerp(from, to, curve.Evaluate(t));
            yield return null;
        }
        light.intensity = to;
    }

    // ─── 즉시 제어 ───

    private void SetGroupImmediate(LightGroup group, float multiplier)
    {
        for (int i = 0; i < group.lights.Length; i++)
        {
            if (group.lights[i] == null) continue;

            if (!group.lights[i].gameObject.activeSelf)
                group.lights[i].gameObject.SetActive(true);

            group.lights[i].intensity = group.GetBaseIntensity(i) * multiplier;
        }
    }

    private void SetGroupOff(LightGroup group)
    {
        for (int i = 0; i < group.lights.Length; i++)
        {
            if (group.lights[i] != null)
                group.lights[i].intensity = 0f;
        }
    }

    // ─── 유틸 ───

    private bool IsValidGroup(int index)
    {
        return lightGroups != null && index >= 0 && index < lightGroups.Length;
    }
}
