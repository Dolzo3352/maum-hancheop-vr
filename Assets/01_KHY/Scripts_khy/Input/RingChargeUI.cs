using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 링 충전 게이지 UI.
///
/// RingChargeSystem의 충전 진행도를 시각적으로 표시합니다.
/// World Space Canvas의 Slider 또는 Image(fillAmount)로 게이지를 표현합니다.
///
/// 사용법:
///   Canvas(World Space) 하위에 이 컴포넌트를 부착합니다.
///   Inspector에서 RingChargeSystem과 UI 요소들을 연결합니다.
/// </summary>
public class RingChargeUI : MonoBehaviour
{
    // ─── 참조 ───

    [Header("참조")]
    [Tooltip("링 충전 시스템")]
    [SerializeField] private RingChargeSystem ringChargeSystem;

    // ─── UI 요소 ───

    [Header("UI 요소")]
    [Tooltip("충전 게이지 슬라이더")]
    [SerializeField] private Slider chargeSlider;

    [Tooltip("슬라이더의 Fill 이미지 (색상 변경용)")]
    [SerializeField] private Image fillImage;

    [Tooltip("게이지 배경 이미지 (선택)")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("완료 시 표시할 아이콘 (선택)")]
    [SerializeField] private GameObject completeIcon;

    // ─── 설정 ───

    [Header("설정")]
    [Tooltip("충전 시작/종료 시 페이드 시간")]
    [SerializeField] private float fadeDuration = 0.2f;

    [Tooltip("완료 후 UI가 사라지기까지 대기 시간")]
    [SerializeField] private float completeDisplayDuration = 0.5f;

    // ─── 상태 ───

    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    // ─── 초기화 ───

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // 초기 상태: 숨김
        canvasGroup.alpha = 0f;
        if (chargeSlider != null)
            chargeSlider.value = 0f;
        if (completeIcon != null)
            completeIcon.SetActive(false);
    }

    private void OnEnable()
    {
        if (ringChargeSystem != null)
        {
            ringChargeSystem.OnChargeStarted += HandleChargeStarted;
            ringChargeSystem.OnChargeUpdated += HandleChargeUpdated;
            ringChargeSystem.OnRingComplete += HandleRingComplete;
            ringChargeSystem.OnChargeCancelled += HandleChargeCancelled;
        }
    }

    private void OnDisable()
    {
        if (ringChargeSystem != null)
        {
            ringChargeSystem.OnChargeStarted -= HandleChargeStarted;
            ringChargeSystem.OnChargeUpdated -= HandleChargeUpdated;
            ringChargeSystem.OnRingComplete -= HandleRingComplete;
            ringChargeSystem.OnChargeCancelled -= HandleChargeCancelled;
        }
    }

    // ─── 이벤트 핸들러 ───

    private void HandleChargeStarted(RingType ringType, RingInteractable target)
    {
        // 링 색상 적용
        Color ringColor = RingTypeColors.GetColor(ringType);
        if (fillImage != null)
            fillImage.color = ringColor;

        // 슬라이더 초기화
        if (chargeSlider != null)
            chargeSlider.value = 0f;

        // 완료 아이콘 숨김
        if (completeIcon != null)
            completeIcon.SetActive(false);

        // 페이드 인
        FadeTo(1f);
    }

    private void HandleChargeUpdated(float progress, RingType ringType)
    {
        // 슬라이더 업데이트
        if (chargeSlider != null)
            chargeSlider.value = progress;
    }

    private void HandleRingComplete(RingType ringType, RingInteractable target)
    {
        // 슬라이더 100%
        if (chargeSlider != null)
            chargeSlider.value = 1f;

        // 완료 아이콘 표시
        if (completeIcon != null)
            completeIcon.SetActive(true);

        // 잠시 후 페이드 아웃
        StartCoroutine(CompleteAndHide());
    }

    private void HandleChargeCancelled(RingType ringType)
    {
        // 즉시 숨김
        FadeTo(0f);

        // 슬라이더 초기화
        if (chargeSlider != null)
            chargeSlider.value = 0f;
    }

    // ─── UI 페이드 ───

    private void FadeTo(float targetAlpha)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeCoroutine(targetAlpha));
    }

    private IEnumerator FadeCoroutine(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        fadeCoroutine = null;
    }

    private IEnumerator CompleteAndHide()
    {
        yield return new WaitForSeconds(completeDisplayDuration);
        FadeTo(0f);
    }
}
