using UnityEngine;

/// <summary>
/// 링 충전 효과음 컨트롤러.
///
/// RingChargeSystem 이벤트를 구독하여 충전 루프음 / 완료음 / 취소음을 재생합니다.
/// 왼손/오른손 각각의 3D AudioSource를 사용하여 손 위치에서 공간음이 재생됩니다.
///
/// RingChargeVFX와 동일한 이벤트 구독 패턴을 따릅니다.
///
/// 사용법:
///   매니저 오브젝트에 부착.
///   왼손/오른손 컨트롤러 하위에 미리 배치한 AudioSource를 연결합니다.
///   3D 오디오 설정(Spatial Blend, Min/Max Distance 등)은 AudioSource에서 직접 세팅합니다.
/// </summary>
public class RingChargeSFX : MonoBehaviour
{
    // ─── 참조 ───

    [Header("참조")]
    [Tooltip("링 충전 시스템")]
    [SerializeField] private RingChargeSystem ringChargeSystem;

    [Tooltip("VR 입력 매니저 (어느 손인지 판별용)")]
    [SerializeField] private VRInputManager inputManager;

    // ─── 손 오디오 소스 ───

    [Header("손 오디오 소스 (미리 배치, 3D 설정 완료)")]
    [Tooltip("왼손 컨트롤러에 배치한 AudioSource")]
    [SerializeField] private AudioSource leftHandSource;

    [Tooltip("오른손 컨트롤러에 배치한 AudioSource")]
    [SerializeField] private AudioSource rightHandSource;

    // ─── 충전 루프 ───

    [Header("충전 루프")]
    [Tooltip("충전 중 재생할 루프 클립")]
    [SerializeField] private AudioClip chargeLoopClip;

    [Tooltip("진행도에 따라 피치를 올릴지")]
    [SerializeField] private bool pitchByProgress = true;

    [Tooltip("피치 최솟값 (충전 시작)")]
    [SerializeField] private float minPitch = 0.8f;

    [Tooltip("피치 최댓값 (충전 완료 직전)")]
    [SerializeField] private float maxPitch = 1.3f;

    // ─── 완료 ───

    [Header("완료")]
    [Tooltip("충전 100% 완료 시 효과음")]
    [SerializeField] private AudioClip completeClip;

    [Tooltip("완료음 볼륨")]
    [SerializeField] [Range(0f, 1f)] private float completeVolume = 0.8f;

    // ─── 취소 ───

    [Header("취소")]
    [Tooltip("충전 취소 시 효과음 (없으면 무음)")]
    [SerializeField] private AudioClip cancelClip;

    [Tooltip("취소음 볼륨")]
    [SerializeField] [Range(0f, 1f)] private float cancelVolume = 0.3f;

    // ─── 상태 ───

    private AudioSource activeSource;

    // ─── 구독 ───

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

        StopActiveSource();
    }

    // ─── 이벤트 핸들러 ───

    private void HandleChargeStarted(RingType ringType, RingInteractable target)
    {
        if (chargeLoopClip == null) return;

        // 어느 손인지 판별
        activeSource = GetSourceForActiveHand();
        if (activeSource == null) return;

        activeSource.clip = chargeLoopClip;
        activeSource.loop = true;
        activeSource.pitch = pitchByProgress ? minPitch : 1f;
        activeSource.Play();
    }

    private void HandleChargeUpdated(float progress, RingType ringType)
    {
        if (!pitchByProgress || activeSource == null || !activeSource.isPlaying) return;

        activeSource.pitch = Mathf.Lerp(minPitch, maxPitch, progress);
    }

    private void HandleRingComplete(RingType ringType, RingInteractable target)
    {
        StopActiveSource();

        if (completeClip != null && activeSource != null)
            activeSource.PlayOneShot(completeClip, completeVolume);
    }

    private void HandleChargeCancelled(RingType ringType)
    {
        StopActiveSource();

        if (cancelClip != null && activeSource != null)
            activeSource.PlayOneShot(cancelClip, cancelVolume);
    }

    // ─── 유틸리티 ───

    private AudioSource GetSourceForActiveHand()
    {
        if (inputManager == null) return rightHandSource;

        return inputManager.ActiveHand switch
        {
            HandSide.Left => leftHandSource,
            HandSide.Right => rightHandSource,
            _ => rightHandSource
        };
    }

    private void StopActiveSource()
    {
        if (activeSource != null && activeSource.isPlaying && activeSource.loop)
        {
            activeSource.Stop();
            activeSource.loop = false;
            activeSource.pitch = 1f;
        }
    }
}
