using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

/// <summary>
/// 링 충전 시 XR Ray의 색상을 RingType에 맞게 변경합니다.
///
/// XRInteractorLineVisual의 validColorGradient / invalidColorGradient를 직접 변경합니다.
/// (LineRenderer를 직접 바꾸면 XRInteractorLineVisual이 매 프레임 덮어씁니다)
///
/// 충전 시작 → 레이 색상을 RingType 색상으로 변경
/// 충전 중   → progress에 따라 intensity 증가 (선택)
/// 완료/취소 → 원래 색상 복원
///
/// 사용법:
///   RingChargeSystem과 같은 오브젝트에 부착하고 참조를 연결합니다.
/// </summary>
public class RingRayVisual : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RingChargeSystem ringChargeSystem;

    [Header("색상 설정")]
    [Tooltip("충전 진행에 따라 intensity를 높일지")]
    [SerializeField] private bool intensifyWithProgress = true;

    [Tooltip("최소 intensity 배수 (충전 시작)")]
    [SerializeField] private float minIntensity = 1f;

    [Tooltip("최대 intensity 배수 (충전 완료)")]
    [SerializeField] private float maxIntensity = 3f;

    // XRInteractorLineVisual 참조
    private XRInteractorLineVisual activeLineVisual;

    // 원래 그라디언트 백업
    private Gradient originalValidGradient;
    private Gradient originalInvalidGradient;
    private bool isActive;

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

        RestoreOriginal();
    }

    // ─── 이벤트 핸들러 ───

    private void HandleChargeStarted(RingType ringType, RingInteractable target)
    {
        var rayInteractor = ringChargeSystem.ActiveRayInteractor;
        if (rayInteractor == null) return;

        activeLineVisual = rayInteractor.GetComponent<XRInteractorLineVisual>();
        if (activeLineVisual == null) return;

        // 원래 그라디언트 백업
        originalValidGradient = activeLineVisual.validColorGradient;
        originalInvalidGradient = activeLineVisual.invalidColorGradient;

        // RingType 색상으로 변경
        float intensity = intensifyWithProgress ? minIntensity : maxIntensity;
        ApplyColor(ringType, intensity);
        isActive = true;
    }

    private void HandleChargeUpdated(float progress, RingType ringType)
    {
        if (!isActive || activeLineVisual == null) return;
        if (!intensifyWithProgress) return;

        float intensity = Mathf.Lerp(minIntensity, maxIntensity, progress);
        ApplyColor(ringType, intensity);
    }

    private void HandleRingComplete(RingType ringType, RingInteractable target)
    {
        RestoreOriginal();
    }

    private void HandleChargeCancelled(RingType ringType)
    {
        RestoreOriginal();
    }

    // ─── 색상 적용 ───

    private void ApplyColor(RingType ringType, float intensity)
    {
        if (activeLineVisual == null) return;

        Color baseColor = RingTypeColors.GetColor(ringType);
        Color hdrColor = baseColor * intensity;

        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(hdrColor, 0f),
                new GradientColorKey(hdrColor, 0.7f),
                new GradientColorKey(baseColor, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.7f),
                new GradientAlphaKey(0.3f, 1f)
            }
        );

        activeLineVisual.validColorGradient = gradient;
        activeLineVisual.invalidColorGradient = gradient;
    }

    private void RestoreOriginal()
    {
        if (activeLineVisual != null)
        {
            if (originalValidGradient != null)
                activeLineVisual.validColorGradient = originalValidGradient;
            if (originalInvalidGradient != null)
                activeLineVisual.invalidColorGradient = originalInvalidGradient;
        }

        activeLineVisual = null;
        originalValidGradient = null;
        originalInvalidGradient = null;
        isActive = false;
    }
}
