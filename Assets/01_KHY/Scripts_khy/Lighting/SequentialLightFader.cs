using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 라이트를 리스트 순서대로 순차적으로 밝히는 컨트롤러.
///
/// 타임라인 시그널이나 UnityEvent에서 Play()를 호출하면
/// 리스트의 라이트가 하나씩 순서대로 0 → 목표 intensity로 페이드인됩니다.
///
/// 비활성화된 라이트도 지원합니다.
/// 각 라이트의 목표 intensity를 Inspector에서 직접 설정합니다.
///
/// 사용법:
///   1. 타임라인 GameObject에 부착
///   2. lightEntries 배열에 라이트 + 목표 intensity 등록
///   3. SignalReceiver에서 Play() 연결
/// </summary>
public class SequentialLightFader : MonoBehaviour
{
    [Serializable]
    public class LightEntry
    {
        [Tooltip("대상 라이트")]
        public Light light;

        [Tooltip("목표 intensity (이 값까지 페이드인됩니다)")]
        public float targetIntensity = 1f;

        [Tooltip("Play() 시 비활성 라이트를 자동으로 켭니다")]
        public bool activateOnPlay = true;
    }

    [Header("대상 라이트")]
    [Tooltip("순서대로 밝아질 라이트들\n" +
             "배열 순서 = 밝아지는 순서")]
    [SerializeField] private LightEntry[] lightEntries;

    [Header("페이드 설정")]
    [Tooltip("각 라이트의 페이드인 시간 (초)")]
    [SerializeField] private float fadeDuration = 1.0f;

    [Tooltip("라이트 간 시간 간격 (초)\n" +
             "0이면 이전 라이트 페이드 완료 후 바로 다음 시작\n" +
             "0.5면 이전 시작 후 0.5초 뒤에 다음 시작 (겹칠 수 있음)")]
    [SerializeField] private float staggerDelay = 0.5f;

    [Tooltip("true면 이전 라이트 완료를 기다리지 않고 staggerDelay 간격으로 시작\n" +
             "false면 이전 라이트 완료 후 staggerDelay만큼 대기 후 다음 시작")]
    [SerializeField] private bool overlapFades = true;

    [Header("커브")]
    [Tooltip("페이드인 커브")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    /// <summary>전체 페이드 시퀀스 완료 시</summary>
    public event Action OnSequenceComplete;

    private Coroutine activeCoroutine;

    /// <summary>
    /// 순차 페이드인을 시작합니다.
    /// </summary>
    [ContextMenu("테스트: 순차 페이드인")]
    public void Play()
    {
        if (lightEntries == null || lightEntries.Length == 0) return;

        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        activeCoroutine = StartCoroutine(SequentialFadeIn());
    }

    /// <summary>
    /// 모든 라이트를 즉시 목표 밝기로 복원합니다.
    /// </summary>
    public void RestoreAll()
    {
        if (lightEntries == null) return;

        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        foreach (var entry in lightEntries)
        {
            if (entry.light != null)
            {
                if (entry.activateOnPlay && !entry.light.gameObject.activeSelf)
                    entry.light.gameObject.SetActive(true);
                entry.light.intensity = entry.targetIntensity;
            }
        }
    }

    /// <summary>
    /// 모든 라이트를 intensity 0으로 즉시 끕니다.
    /// </summary>
    public void TurnOffAll()
    {
        if (lightEntries == null) return;
        foreach (var entry in lightEntries)
        {
            if (entry.light != null)
                entry.light.intensity = 0f;
        }
    }

    /// <summary>
    /// 모든 라이트를 서서히 끕니다.
    /// 시그널이나 UnityEvent에서 호출하세요.
    /// </summary>
    [ContextMenu("테스트: 페이드아웃")]
    public void FadeOut()
    {
        if (lightEntries == null || lightEntries.Length == 0) return;

        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        activeCoroutine = StartCoroutine(FadeOutAll());
    }

    private IEnumerator FadeOutAll()
    {
        // 현재 intensity 저장
        float[] currentIntensities = new float[lightEntries.Length];
        for (int i = 0; i < lightEntries.Length; i++)
        {
            if (lightEntries[i].light != null)
                currentIntensities[i] = lightEntries[i].light.intensity;
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float curveValue = fadeCurve.Evaluate(t);

            for (int i = 0; i < lightEntries.Length; i++)
            {
                if (lightEntries[i].light != null)
                    lightEntries[i].light.intensity = Mathf.Lerp(currentIntensities[i], 0f, curveValue);
            }

            yield return null;
        }

        // 최종 0 보장
        TurnOffAll();
        activeCoroutine = null;
    }

    private IEnumerator SequentialFadeIn()
    {
        if (overlapFades)
        {
            int completedCount = 0;

            for (int i = 0; i < lightEntries.Length; i++)
            {
                if (i > 0)
                    yield return new WaitForSeconds(staggerDelay);

                int index = i;
                StartCoroutine(FadeInLight(index, () => completedCount++));
            }

            while (completedCount < lightEntries.Length)
                yield return null;
        }
        else
        {
            for (int i = 0; i < lightEntries.Length; i++)
            {
                yield return FadeInLight(i, null);

                if (i < lightEntries.Length - 1 && staggerDelay > 0f)
                    yield return new WaitForSeconds(staggerDelay);
            }
        }

        OnSequenceComplete?.Invoke();
        activeCoroutine = null;
    }

    private IEnumerator FadeInLight(int index, Action onComplete)
    {
        var entry = lightEntries[index];
        if (entry.light == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        // 비활성 라이트 활성화
        if (entry.activateOnPlay && !entry.light.gameObject.activeSelf)
            entry.light.gameObject.SetActive(true);

        // intensity 0에서 시작
        entry.light.intensity = 0f;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float curveValue = fadeCurve.Evaluate(t);

            entry.light.intensity = Mathf.LerpUnclamped(0f, entry.targetIntensity, curveValue);
            yield return null;
        }

        entry.light.intensity = entry.targetIntensity;
        onComplete?.Invoke();
    }
}
