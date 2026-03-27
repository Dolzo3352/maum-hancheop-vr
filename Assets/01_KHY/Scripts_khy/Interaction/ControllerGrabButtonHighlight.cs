using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// 오른손 컨트롤러 그랩버튼 하이라이트 + 햅틱 피드백.
///
/// 타임라인 InteractionSignal이 발동되면:
///   - 그랩버튼 메쉬의 머티리얼을 하이라이트 머티리얼로 교체
///   - 오른손 컨트롤러에 햅틱 진동 1회 전송
///
/// 인터랙션 완료 시:
///   - 원래 머티리얼로 복원
///
/// 사용법:
///   오른쪽 컨트롤러 프리팹에 부착.
///   grabButtonRenderer와 highlightMaterial을 Inspector에서 연결합니다.
/// </summary>
public class ControllerGrabButtonHighlight : MonoBehaviour
{
    [Header("그랩버튼 메쉬")]
    [Tooltip("그랩버튼 메쉬의 Renderer 컴포넌트")]
    [SerializeField] private Renderer grabButtonRenderer;

    [Header("머티리얼")]
    [Tooltip("인터랙션 활성화 시 교체할 하이라이트 머티리얼")]
    [SerializeField] private Material highlightMaterial;

    [Header("햅틱 설정")]
    [Tooltip("햅틱 진동 강도 (0~1)")]
    [SerializeField] private float hapticAmplitude = 0.3f;

    [Tooltip("햅틱 진동 지속 시간 (초)")]
    [SerializeField] private float hapticDuration = 0.15f;

    // 원래 머티리얼 (복원용)
    private Material originalMaterial;

    // 오른손 XR 디바이스
    private InputDevice rightDevice;
    private bool deviceFound;

    // ─── 초기화 ───

    private void Awake()
    {
        // 원래 머티리얼 저장 (sharedMaterial: 인스턴스 생성 없이 원본 참조)
        if (grabButtonRenderer != null)
            originalMaterial = grabButtonRenderer.sharedMaterial;

        FindRightDevice();
    }

    private void OnEnable()
    {
        InteractionEvents.OnInteractionActivated += HandleActivated;
        InteractionEvents.OnInteractionCompleted += HandleCompleted;
    }

    private void OnDisable()
    {
        InteractionEvents.OnInteractionActivated -= HandleActivated;
        InteractionEvents.OnInteractionCompleted -= HandleCompleted;

        // 씬 전환/비활성화 시 원래 상태 복원
        RestoreMaterial();
    }

    // ─── 이벤트 처리 ───

    private void HandleActivated()
    {
        SetHighlight(true);
        SendHaptic();
    }

    private void HandleCompleted()
    {
        SetHighlight(false);
    }

    // ─── 머티리얼 교체 ───

    private void SetHighlight(bool active)
    {
        if (grabButtonRenderer == null) return;

        grabButtonRenderer.sharedMaterial = active ? highlightMaterial : originalMaterial;
    }

    private void RestoreMaterial()
    {
        if (grabButtonRenderer == null || originalMaterial == null) return;
        grabButtonRenderer.sharedMaterial = originalMaterial;
    }

    // ─── 햅틱 ───

    private void SendHaptic()
    {
        if (!deviceFound) FindRightDevice();
        if (!rightDevice.isValid) return;

        rightDevice.SendHapticImpulse(0, hapticAmplitude, hapticDuration);
    }

    private void FindRightDevice()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            devices);

        if (devices.Count > 0)
        {
            rightDevice = devices[0];
            deviceFound = rightDevice.isValid;
        }
    }
}
