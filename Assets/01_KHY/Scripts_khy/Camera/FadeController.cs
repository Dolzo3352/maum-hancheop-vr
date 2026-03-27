using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 전역 페이드 인/아웃 컨트롤러.
///
/// WaypointTeleporter, 씬 전환 등 어디서든 페이드를 호출할 수 있습니다.
/// FadeController.Instance.FadeOut(onComplete) 형태로 사용합니다.
///
/// 사용법:
///   씬에 하나만 배치. fadeRenderer에 페이드 메쉬 Renderer 연결.
/// </summary>
public class FadeController : MonoBehaviour
{
    public static FadeController Instance { get; private set; }

    [Header("페이드 메쉬")]
    [Tooltip("FadeOverlay 머티리얼이 붙은 Renderer")]
    [SerializeField] private Renderer fadeRenderer;

    [Header("페이드 설정")]
    [SerializeField] private float defaultDuration = 0.3f;

    [Tooltip("씬 시작 시 검정 → 투명으로 페이드 인")]
    [SerializeField] private bool fadeInOnStart = true;

    [Tooltip("씬 시작 페이드 인 시간 (초)")]
    [SerializeField] private float startFadeDuration = 0.5f;

    [Tooltip("페이드 아웃 후 검정 화면 유지 시간 (초)")]
    [SerializeField] private float holdDuration = 0.3f;

    private Material fadeMaterial;
    private Coroutine fadeCoroutine;

    // ─── 초기화 ───

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (fadeRenderer != null)
        {
            fadeMaterial = fadeRenderer.material;
            // 시작은 검정으로
            SetAlpha(1f);
        }
    }

    private void Start()
    {
        // 씬 시작 시 페이드 인
        if (fadeInOnStart)
            FadeIn(duration: startFadeDuration);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ─── 공개 API ───

    /// <summary>페이드 아웃 (투명 → 검정). 완료 시 onComplete 호출.</summary>
    public void FadeOut(Action onComplete = null, float duration = -1f)
    {
        StartFade(0f, 1f, duration < 0 ? defaultDuration : duration, onComplete);
    }

    /// <summary>페이드 인 (검정 → 투명). 완료 시 onComplete 호출.</summary>
    public void FadeIn(Action onComplete = null, float duration = -1f)
    {
        StartFade(1f, 0f, duration < 0 ? defaultDuration : duration, onComplete);
    }

    /// <summary>페이드 아웃 → 액션 실행 → 페이드 인을 순서대로 실행.</summary>
    public void FadeOutIn(Action onMidpoint = null, float duration = -1f)
    {
        float d = duration < 0 ? defaultDuration : duration;
        FadeOut(() =>
        {
            onMidpoint?.Invoke();
            FadeIn();
        }, d);
    }

    /// <summary>코루틴으로 사용할 때.</summary>
    public IEnumerator FadeOutCoroutine(float duration = -1f)
    {
        bool done = false;
        FadeOut(() => done = true, duration);
        while (!done) yield return null;
    }

    public IEnumerator FadeInCoroutine(float duration = -1f)
    {
        bool done = false;
        FadeIn(() => done = true, duration);
        while (!done) yield return null;
    }

    // ─── 내부 ───

    private void StartFade(float from, float to, float duration, Action onComplete)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(from, to, duration, onComplete));
    }

    private IEnumerator FadeRoutine(float from, float to, float duration, Action onComplete)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetAlpha(to);

        // 페이드 아웃(검정) 후 holdDuration만큼 유지
        if (to >= 1f && holdDuration > 0f)
            yield return new WaitForSeconds(holdDuration);

        onComplete?.Invoke();
    }

    private void SetAlpha(float alpha)
    {
        if (fadeMaterial == null) return;
        fadeMaterial.SetColor("_Color", new Color(0f, 0f, 0f, alpha));
    }
}
