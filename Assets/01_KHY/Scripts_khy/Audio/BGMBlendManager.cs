using System.Collections;
using UnityEngine;

/// <summary>
/// 2~3개 BGM 트랙을 동시에 재생하면서 볼륨 블렌드를 실시간으로 조절합니다.
///
/// 동작 원리:
///   3개의 AudioSource를 모두 Play() + loop = true 상태로 유지하고,
///   볼륨 비율(블렌드 웨이트)만 부드럽게 바꿔 분위기를 전환합니다.
///
/// 사용법:
///   1. 씬에 빈 오브젝트 → BGMBlendManager 부착
///   2. Clip A/B/C 슬롯에 BGM 클립 연결
///   3. 타임라인 SignalReceiver → FadeToBlend() 연결
///
/// 트랙 2개만 쓸 경우:
///   Clip C를 비워두고 FadeToBlend(a, b, 0, duration) 호출
/// </summary>
public class BGMBlendManager : MonoBehaviour
{
    // ─── 싱글톤 ───

    public static BGMBlendManager Instance { get; private set; }

    // ─── Inspector ───

    [Header("트랙 클립")]
    [Tooltip("트랙 A AudioClip")]
    [SerializeField] private AudioClip clipA;

    [Tooltip("트랙 B AudioClip")]
    [SerializeField] private AudioClip clipB;

    [Tooltip("트랙 C AudioClip (2트랙만 쓸 경우 비워두세요)")]
    [SerializeField] private AudioClip clipC;

    [Header("볼륨")]
    [Tooltip("전체 볼륨 배율 (0~1)")]
    [Range(0f, 1f)]
    [SerializeField] private float masterVolume = 1f;

    [Header("타이밍")]
    [Tooltip("FadeToBlend() 기본 페이드 시간 (초)")]
    [SerializeField] private float defaultFadeDuration = 2f;

    [Header("설정")]
    [Tooltip("Start() 시 자동 재생")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("씬 전환 시 파괴하지 않음")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    // ─── 런타임 ───

    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource sourceC;

    // 현재 목표 블렌드 (0~1)
    private float targetA = 1f;
    private float targetB = 0f;
    private float targetC = 0f;

    // FadeOut 전 블렌드 (FadeIn 복귀용)
    private float savedA = 1f;
    private float savedB = 0f;
    private float savedC = 0f;

    private Coroutine blendCoroutine;

    // ─── 생명주기 ───

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        SetupAudioSources();
    }

    private void Start()
    {
        if (playOnStart)
            Play();
    }

    private void SetupAudioSources()
    {
        sourceA = CreateSource(clipA, "BGM_A");
        sourceB = CreateSource(clipB, "BGM_B");
        sourceC = CreateSource(clipC, "BGM_C");
    }

    private AudioSource CreateSource(AudioClip clip, string sourceName)
    {
        var go = new GameObject(sourceName);
        go.transform.SetParent(transform);

        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;
        src.playOnAwake = false;
        src.volume = 0f;
        src.spatialBlend = 0f; // 2D (BGM)

        return src;
    }

    // ─── 재생 제어 ───

    /// <summary>3트랙 모두 재생 시작 (볼륨은 현재 상태 유지)</summary>
    public void Play()
    {
        if (sourceA.clip != null && !sourceA.isPlaying) sourceA.Play();
        if (sourceB.clip != null && !sourceB.isPlaying) sourceB.Play();
        if (sourceC.clip != null && !sourceC.isPlaying) sourceC.Play();
    }

    /// <summary>3트랙 모두 정지</summary>
    public void Stop()
    {
        sourceA.Stop();
        sourceB.Stop();
        sourceC.Stop();
    }

    // ─── 블렌드 API ───

    /// <summary>
    /// 기본 페이드 시간으로 블렌드를 전환합니다.
    /// 타임라인 SignalReceiver에서 연결하세요.
    /// </summary>
    public void FadeToBlend(float a, float b, float c)
        => FadeToBlend(a, b, c, defaultFadeDuration);

    /// <summary>
    /// 지정한 페이드 시간으로 블렌드를 전환합니다.
    /// </summary>
    public void FadeToBlend(float a, float b, float c, float duration)
    {
        targetA = Mathf.Clamp01(a);
        targetB = Mathf.Clamp01(b);
        targetC = Mathf.Clamp01(c);

        if (blendCoroutine != null) StopCoroutine(blendCoroutine);
        blendCoroutine = StartCoroutine(BlendCoroutine(targetA, targetB, targetC, duration));
    }

    /// <summary>
    /// 전체 볼륨을 서서히 0으로 페이드아웃합니다.
    /// 현재 블렌드 상태를 저장해두었다가 FadeIn()으로 복귀할 수 있습니다.
    /// </summary>
    public void FadeOut(float duration)
    {
        savedA = targetA;
        savedB = targetB;
        savedC = targetC;

        if (blendCoroutine != null) StopCoroutine(blendCoroutine);
        blendCoroutine = StartCoroutine(BlendCoroutine(0f, 0f, 0f, duration));
    }

    /// <summary>
    /// FadeOut() 이전 블렌드 상태로 복귀합니다.
    /// </summary>
    public void FadeIn(float duration)
    {
        targetA = savedA;
        targetB = savedB;
        targetC = savedC;

        if (blendCoroutine != null) StopCoroutine(blendCoroutine);
        blendCoroutine = StartCoroutine(BlendCoroutine(targetA, targetB, targetC, duration));
    }

    /// <summary>
    /// 기본 페이드 시간으로 FadeOut
    /// </summary>
    public void FadeOut() => FadeOut(defaultFadeDuration);

    /// <summary>
    /// 기본 페이드 시간으로 FadeIn
    /// </summary>
    public void FadeIn() => FadeIn(defaultFadeDuration);

    // ─── 코루틴 ───

    private IEnumerator BlendCoroutine(float toA, float toB, float toC, float duration)
    {
        float fromA = sourceA.volume / masterVolume;
        float fromB = sourceB.volume / masterVolume;
        float fromC = sourceC.volume / masterVolume;

        // masterVolume이 0이면 나누기 오류 방지
        if (masterVolume <= 0f)
        {
            fromA = fromB = fromC = 0f;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            sourceA.volume = Mathf.Lerp(fromA, toA, t) * masterVolume;
            sourceB.volume = Mathf.Lerp(fromB, toB, t) * masterVolume;
            sourceC.volume = Mathf.Lerp(fromC, toC, t) * masterVolume;

            yield return null;
        }

        // 최종값 확정
        sourceA.volume = toA * masterVolume;
        sourceB.volume = toB * masterVolume;
        sourceC.volume = toC * masterVolume;

        blendCoroutine = null;
    }

    // ─── 런타임 볼륨 조절 ───

    /// <summary>마스터 볼륨을 즉시 변경합니다.</summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        sourceA.volume = targetA * masterVolume;
        sourceB.volume = targetB * masterVolume;
        sourceC.volume = targetC * masterVolume;
    }

    // ─── 시그널 프리셋 (SignalReceiver에서 연결용, 파라미터 없음) ───
    // Inspector의 Blend Presets 배열에서 값을 설정하고 Signal로 인덱스 호출

    [System.Serializable]
    public class BlendPreset
    {
        public string presetName = "Preset";
        [Range(0f, 1f)] public float weightA = 1f;
        [Range(0f, 1f)] public float weightB = 0f;
        [Range(0f, 1f)] public float weightC = 0f;
        public float fadeDuration = 2f;
    }

    [Header("시그널 프리셋 (SignalReceiver 연결용)")]
    [Tooltip("원하는 블렌드 상태를 미리 정의해두고 ApplyPreset(index)로 호출합니다.")]
    [SerializeField] private BlendPreset[] blendPresets;

    /// <summary>
    /// 인덱스로 프리셋을 적용합니다.
    /// SignalReceiver → ApplyPreset(int) 으로 연결하세요.
    /// </summary>
    public void ApplyPreset(int index)
    {
        if (blendPresets == null || index < 0 || index >= blendPresets.Length)
        {
            Debug.LogWarning($"[BGMBlendManager] 프리셋 인덱스 {index}가 없습니다.", this);
            return;
        }

        var p = blendPresets[index];
        FadeToBlend(p.weightA, p.weightB, p.weightC, p.fadeDuration);
        Debug.Log($"[BGMBlendManager] 프리셋 적용: {p.presetName} (A:{p.weightA} B:{p.weightB} C:{p.weightC})", this);
    }

    // ─── 에디터 테스트 ───

    [ContextMenu("테스트: 트랙A만")]
    private void TestA() => FadeToBlend(1f, 0f, 0f, 2f);

    [ContextMenu("테스트: 트랙B만")]
    private void TestB() => FadeToBlend(0f, 1f, 0f, 2f);

    [ContextMenu("테스트: A+B 블렌드")]
    private void TestAB() => FadeToBlend(0.6f, 0.4f, 0f, 2f);

    [ContextMenu("테스트: FadeOut")]
    private void TestFadeOut() => FadeOut(2f);

    [ContextMenu("테스트: FadeIn")]
    private void TestFadeIn() => FadeIn(2f);
}
