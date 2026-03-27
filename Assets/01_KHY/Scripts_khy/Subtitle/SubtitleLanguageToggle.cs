using UnityEngine;
using TMPro;

/// <summary>
/// 자막 언어를 토글하는 UI 버튼 헬퍼.
///
/// 사용법:
///   Button의 OnClick에 ToggleLanguage()를 연결합니다.
/// </summary>
public class SubtitleLanguageToggle : MonoBehaviour
{
    [Tooltip("현재 언어를 표시할 텍스트 (없으면 무시)")]
    [SerializeField] private TextMeshProUGUI buttonLabel;

    private void Start()
    {
        UpdateLabel();
    }

    /// <summary>한국어 ↔ 영어를 토글합니다. Button.OnClick에 연결하세요.</summary>
    public void ToggleLanguage()
    {
        SubtitleDisplay.CurrentLanguage =
            SubtitleDisplay.CurrentLanguage == SubtitleDisplay.Language.Korean
                ? SubtitleDisplay.Language.English
                : SubtitleDisplay.Language.Korean;

        UpdateLabel();
    }

    private void UpdateLabel()
    {
        if (buttonLabel != null)
        {
            buttonLabel.text = SubtitleDisplay.CurrentLanguage == SubtitleDisplay.Language.Korean
                ? "한국어"
                : "English";
        }
    }
}
