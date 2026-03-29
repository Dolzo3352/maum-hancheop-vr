using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// SEQ1 CUT03: 기운이 부족하여 링 충전이 실패하는 나무 인터랙션.
///
/// RingInteractable을 상속하므로 기존 기능이 모두 적용됩니다:
///   - 시그널 발동 시 아웃라인 활성화 (InteractableOutline)
///   - 호버 시 림컬러 변경 (InteractableHoverRim)
///   - 플레이어 그립으로 링 충전 시작 (RingChargeSystem)
///
/// 차이점:
///   충전이 maxFailProgress에 도달하면 100%까지 가지 않고 실패합니다.
///   실패 시: 링 떨림 + 빨간색 전환 + 강한 햅틱 → 파티클 흩어짐
/// </summary>
public class FailedTreeInteractable : RingInteractable
{
    [Header("실패 설정")]
    [Tooltip("이 진행도에서 실패 (0~1). 0.6 = 60%에서 실패")]
    [SerializeField] private float maxFailProgress = 0.6f;

    [Header("실패 VFX")]
    [Tooltip("실패 시 흩어지는 파티클")]
    [SerializeField] private ParticleSystem scatterParticle;

    [Tooltip("실패 시 색상 (빨간)")]
    [SerializeField] private Color failureColor = new Color(1f, 0.15f, 0.1f);

    [Tooltip("실패 색상 HDR intensity")]
    [SerializeField] private float failureHdrIntensity = 2f;

    [Header("실패 타이밍")]
    [Tooltip("실패 시점 떨림 시간")]
    [SerializeField] private float jitterDuration = 0.8f;

    [Tooltip("떨림 강도")]
    [SerializeField] private float jitterIntensity = 0.02f;

    [Tooltip("실패 색상 전환 시간")]
    [SerializeField] private float colorTransitionDuration = 0.3f;

    [Tooltip("링 페이드아웃 시간")]
    [SerializeField] private float failFadeOutDuration = 0.4f;

    [Header("햅틱")]
    [Tooltip("실패 시 햅틱 강도 (0~1). 1.0 = 최대")]
    [SerializeField] private float failureHapticAmplitude = 1.0f;

    [Tooltip("실패 시 햅틱 지속 시간")]
    [SerializeField] private float failureHapticDuration = 0.5f;

    [Header("실패 SFX")]
    [Tooltip("실패 효과음을 재생할 3D AudioSource")]
    [SerializeField] private AudioSource failAudioSource;

    [Tooltip("실패 시 재생할 효과음 클립")]
    [SerializeField] private AudioClip failClip;

    [Header("참조")]
    [Tooltip("충전을 강제 취소하기 위한 RingChargeSystem 참조")]
    [SerializeField] private RingChargeSystem ringChargeSystem;

    [Tooltip("링 메시 실패 연출을 위한 RingChargeVFX 참조")]
    [SerializeField] private RingChargeVFX ringChargeVFX;

    private bool hasFailed;
    private bool isFailureSequenceRunning;

    // XR 디바이스 (햅틱용)
    private InputDevice leftDevice;
    private InputDevice rightDevice;

    /// <summary>
    /// 충전 진행 중 호출. maxFailProgress에 도달하면 실패 시퀀스 발동.
    /// </summary>
    public override void OnChargeUpdate(float progress)
    {
        if (hasFailed || isFailureSequenceRunning) return;

        if (progress >= maxFailProgress)
        {
            isFailureSequenceRunning = true;

            // 1. 링 VFX를 실패 모드로 전환 (떨림 + 빨간색 → 페이드아웃)
            if (ringChargeVFX != null)
            {
                ringChargeVFX.PlayFailure(
                    failureColor * failureHdrIntensity,
                    jitterDuration,
                    jitterIntensity,
                    colorTransitionDuration,
                    failFadeOutDuration
                );
            }

            // 2. RingChargeSystem 강제 취소 (RingChargeVFX는 실패 모드라 무시함)
            if (ringChargeSystem != null)
                ringChargeSystem.ForceCancel();

            // 3. 햅틱 + 파티클 + 완료 처리
            StartCoroutine(FailureSequence());
        }
    }

    public override void Execute()
    {
        // 실패 인터랙션이므로 실행하지 않음
    }

    public override void OnChargeCancelled()
    {
        if (isFailureSequenceRunning) return;
        base.OnChargeCancelled();
    }

    private IEnumerator FailureSequence()
    {
        // 실패 SFX 재생
        if (failAudioSource != null && failClip != null)
            failAudioSource.PlayOneShot(failClip);

        // 강한 양손 햅틱
        FindXRDevices();
        SendHapticBothHands(failureHapticAmplitude, failureHapticDuration);

        // 떨림 시간 대기
        yield return new WaitForSeconds(jitterDuration);

        // 흩어지는 파티클 (파티클 자체 위치 사용, VFXSpawnPoint 무관)
        if (scatterParticle != null)
        {
            var main = scatterParticle.main;
            main.startColor = failureColor * failureHdrIntensity;
            scatterParticle.Play();
        }

        // 페이드아웃 대기
        yield return new WaitForSeconds(failFadeOutDuration);

        // 완료 처리
        hasFailed = true;
        isFailureSequenceRunning = false;
        NotifyInteractionDone();
    }

    public override void ResetInteraction()
    {
        base.ResetInteraction();
        hasFailed = false;
        isFailureSequenceRunning = false;
    }

    // ─── 햅틱 ───

    private void SendHapticBothHands(float amplitude, float duration)
    {
        if (leftDevice.isValid)
            leftDevice.SendHapticImpulse(0, amplitude, duration);
        if (rightDevice.isValid)
            rightDevice.SendHapticImpulse(0, amplitude, duration);
    }

    private void FindXRDevices()
    {
        var devices = new List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
        if (devices.Count > 0) leftDevice = devices[0];

        devices.Clear();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
        if (devices.Count > 0) rightDevice = devices[0];
    }
}
