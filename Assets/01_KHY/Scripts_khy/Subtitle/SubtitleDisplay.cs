using UnityEngine;
using UnityEngine.Playables;
using TMPro;

/// <summary>
/// 타임라인 재생 시간에 맞춰 자막을 표시합니다.
///
/// TimelineController.CurrentTime을 매 프레임 폴링하여
/// SubtitleData에서 해당 시간의 자막을 찾아 표시합니다.
/// NarrativeSequencer.OnStageStart를 구독하여 스테이지 전환 시
/// SubtitleData를 자동 교체합니다.
///
/// 사용법:
///   World Space Canvas 하위에 배치하고, Inspector에서 참조를 연결합니다.
/// </summary>
public class SubtitleDisplay : MonoBehaviour
{
    // ─── 언어 ───

    public enum Language { Korean, English }

    private static Language currentLanguage = Language.Korean;

    /// <summary>현재 자막 언어. 변경 시 PlayerPrefs에 저장됩니다.</summary>
    public static Language CurrentLanguage
    {
        get => currentLanguage;
        set
        {
            currentLanguage = value;
            PlayerPrefs.SetInt("SubtitleLang", (int)value);
        }
    }

    // ─── 필드 ───

    [Header("참조 (멀티 스테이지 씬)")]
    [SerializeField] private TimelineController timelineController;
    [SerializeField] private NarrativeSequencer narrativeSequencer;

    [Header("참조 (단일 타임라인 씬)")]
    [Tooltip("TimelineController 없이 PlayableDirector를 직접 참조할 때")]
    [SerializeField] private PlayableDirector directDirector;
    [Tooltip("NarrativeSequencer 없을 때 사용할 자막 데이터")]
    [SerializeField] private SubtitleData initialSubtitleData;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("설정")]
    [Tooltip("페이드 속도 (높을수록 빠름)")]
    [SerializeField] private float fadeSpeed = 5f;

    // ─── 상태 ───

    private SubtitleData currentData;
    private int lastEntryIndex = -1;
    private float targetAlpha;

    // ─── 초기화 ───

    private void Awake()
    {
        currentLanguage = (Language)PlayerPrefs.GetInt("SubtitleLang", 0);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        // NarrativeSequencer 없는 단일 타임라인 씬이면 초기 자막 데이터 바로 사용
        if (narrativeSequencer == null && initialSubtitleData != null)
            SetSubtitleData(initialSubtitleData);
    }

    private void OnEnable()
    {
        if (narrativeSequencer != null)
            narrativeSequencer.OnStageStart += HandleStageStart;
    }

    private void OnDisable()
    {
        if (narrativeSequencer != null)
            narrativeSequencer.OnStageStart -= HandleStageStart;
    }

    // ─── 스테이지 전환 ───

    private void HandleStageStart(int index, StageData data)
    {
        SetSubtitleData(data != null ? data.subtitleData : null);
    }

    /// <summary>자막 데이터를 교체합니다.</summary>
    public void SetSubtitleData(SubtitleData data)
    {
        currentData = data;
        lastEntryIndex = -1;
        ClearText();
    }

    // ─── 매 프레임 업데이트 ───

    private void Update()
    {
        UpdateSubtitle();
        UpdateFade();
    }

    private void UpdateSubtitle()
    {
        if (currentData == null) return;

        // 시간 소스 결정: TimelineController 우선, 없으면 직접 Director
        bool isPlaying;
        double time;

        if (timelineController != null)
        {
            isPlaying = timelineController.IsPlaying;
            time = timelineController.CurrentTime;
        }
        else if (directDirector != null)
        {
            isPlaying = directDirector.state == PlayState.Playing;
            time = directDirector.time;
        }
        else
        {
            return;
        }

        if (!isPlaying)
        {
            // 재생 중이 아니면 자막 숨김
            if (lastEntryIndex >= 0)
            {
                lastEntryIndex = -1;
                ClearText();
            }
            return;
        }
        int index = FindEntry(time);

        if (index == lastEntryIndex) return;
        lastEntryIndex = index;

        if (index >= 0)
        {
            var entry = currentData.entries[index];
            subtitleText.text = currentLanguage == Language.Korean
                ? entry.koText
                : entry.enText;
            targetAlpha = 1f;
        }
        else
        {
            ClearText();
        }
    }

    /// <summary>현재 시간에 해당하는 자막 인덱스를 찾습니다. 없으면 -1.</summary>
    private int FindEntry(double time)
    {
        if (currentData == null) return -1;

        var entries = currentData.entries;
        for (int i = 0; i < entries.Count; i++)
        {
            if (time >= entries[i].startTime && time < entries[i].endTime)
                return i;
        }
        return -1;
    }

    private void ClearText()
    {
        if (subtitleText != null)
            subtitleText.text = "";
        targetAlpha = 0f;
    }

    private void UpdateFade()
    {
        if (canvasGroup == null) return;
        canvasGroup.alpha = Mathf.MoveTowards(
            canvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
    }
}
