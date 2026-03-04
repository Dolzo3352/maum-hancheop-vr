using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

/// <summary>
/// VR 컨트롤러 입력 매니저.
///
/// 양손 Grip 버튼 입력을 관리하며, 한쪽이 Active이면 다른 쪽을 잠급니다.
/// Grip Hold 중 진동(햅틱) 피드백을 제공하며, 진행도에 비례하여 강도가 증가합니다.
///
/// 동작 로직:
///   1. 왼손/오른손 Grip 버튼 감지
///   2. 먼저 누른 쪽이 Active → 다른 쪽 잠금
///   3. Active 쪽의 gripValue(0~1) 실시간 제공
///   4. gripValue에 비례한 햅틱 진동
///   5. Grip 놓으면 Active 해제 → 양쪽 다시 사용 가능
///
/// 사용법:
///   XR Origin과 같은 매니저 오브젝트에 부착합니다.
///   Inspector에서 Left/Right Grip Action을 연결합니다.
/// </summary>
public class VRInputManager : MonoBehaviour
{
    // ─── 입력 액션 ───

    [Header("Grip 입력 액션")]
    [Tooltip("왼손 Grip 버튼 (InputActionReference)")]
    [SerializeField] private InputActionReference leftGripAction;

    [Tooltip("오른손 Grip 버튼 (InputActionReference)")]
    [SerializeField] private InputActionReference rightGripAction;

    // ─── 햅틱 설정 ───

    [Header("햅틱 설정")]
    [Tooltip("햅틱 진동 최소 강도 (Grip 누르기 시작)")]
    [SerializeField] private float hapticMinAmplitude = 0.05f;

    [Tooltip("햅틱 진동 최대 강도 (Grip 100%)")]
    [SerializeField] private float hapticMaxAmplitude = 0.6f;

    [Tooltip("햅틱 진동 지속 시간 (초). 매 프레임 갱신되므로 짧게 설정")]
    [SerializeField] private float hapticDuration = 0.1f;

    [Tooltip("햅틱 강도 커브. X축: gripValue(0~1), Y축: 강도 비율(0~1)")]
    [SerializeField] private AnimationCurve hapticCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // ─── XR 디바이스 (자동 탐색) ───

    // ActionBasedController 참조 불필요 — XR InputDevice API로 직접 햅틱 전송
    private UnityEngine.XR.InputDevice leftDevice;
    private UnityEngine.XR.InputDevice rightDevice;
    private bool devicesFound;

    // ─── Grip 임계값 ───

    [Header("입력 설정")]
    [Tooltip("Grip이 '눌림'으로 인식되는 최소값 (0~1)")]
    [SerializeField] private float gripThreshold = 0.1f;

    // ─── 상태 ───

    private HandSide activeHand = HandSide.None;
    private float currentGripValue;
    private bool isGripActive;

    // ─── 이벤트 ───

    /// <summary>Grip이 시작될 때. (어떤 손인지)</summary>
    public event Action<HandSide> OnGripStarted;

    /// <summary>Grip 진행 중 매 프레임. (어떤 손, gripValue 0~1)</summary>
    public event Action<HandSide, float> OnGripHeld;

    /// <summary>Grip이 해제될 때. (어떤 손이었는지)</summary>
    public event Action<HandSide> OnGripReleased;

    // ─── 프로퍼티 ───

    /// <summary>현재 Active 손. None이면 양쪽 다 놓은 상태.</summary>
    public HandSide ActiveHand => activeHand;

    /// <summary>현재 Grip 값 (0~1). Active가 아니면 0.</summary>
    public float CurrentGripValue => currentGripValue;

    /// <summary>Grip이 Active 상태인지.</summary>
    public bool IsGripActive => isGripActive;

    // ─── 초기화 ───

    private void OnEnable()
    {
        EnableAction(leftGripAction);
        EnableAction(rightGripAction);
        FindXRDevices();
    }

    private void OnDisable()
    {
        // Grip 해제 처리
        if (isGripActive)
            ReleaseGrip();

        DisableAction(leftGripAction);
        DisableAction(rightGripAction);
    }

    // ─── 매 프레임 입력 처리 ───

    private void Update()
    {
        float leftValue = ReadGripValue(leftGripAction);
        float rightValue = ReadGripValue(rightGripAction);

        switch (activeHand)
        {
            // 아무 손도 Active가 아닐 때: 먼저 누른 쪽이 Active
            case HandSide.None:
                if (leftValue > gripThreshold && rightValue > gripThreshold)
                {
                    // 동시 입력 시 값이 큰 쪽 우선
                    ActivateGrip(leftValue >= rightValue ? HandSide.Left : HandSide.Right);
                }
                else if (leftValue > gripThreshold)
                {
                    ActivateGrip(HandSide.Left);
                }
                else if (rightValue > gripThreshold)
                {
                    ActivateGrip(HandSide.Right);
                }
                break;

            // 왼손이 Active: 왼손 값만 추적, 놓으면 해제
            case HandSide.Left:
                if (leftValue <= gripThreshold)
                {
                    ReleaseGrip();
                }
                else
                {
                    UpdateGrip(leftValue);
                }
                break;

            // 오른손이 Active: 오른손 값만 추적, 놓으면 해제
            case HandSide.Right:
                if (rightValue <= gripThreshold)
                {
                    ReleaseGrip();
                }
                else
                {
                    UpdateGrip(rightValue);
                }
                break;
        }
    }

    // ─── 내부 로직 ───

    private void ActivateGrip(HandSide hand)
    {
        activeHand = hand;
        isGripActive = true;
        currentGripValue = ReadGripValue(hand == HandSide.Left ? leftGripAction : rightGripAction);

        OnGripStarted?.Invoke(hand);
        Debug.Log($"[VRInputManager] Grip 시작: {hand}");
    }

    private void UpdateGrip(float value)
    {
        currentGripValue = value;

        // 햅틱 피드백 (진행도에 비례)
        SendHaptic(activeHand, value);

        // 매 프레임 이벤트
        OnGripHeld?.Invoke(activeHand, value);
    }

    private void ReleaseGrip()
    {
        HandSide releasedHand = activeHand;

        // 햅틱 정지
        StopHaptic(releasedHand);

        activeHand = HandSide.None;
        currentGripValue = 0f;
        isGripActive = false;

        OnGripReleased?.Invoke(releasedHand);
        Debug.Log($"[VRInputManager] Grip 해제: {releasedHand}");
    }

    // ─── 햅틱 (XR InputDevice API) ───

    private void SendHaptic(HandSide hand, float gripValue)
    {
        if (!devicesFound) FindXRDevices();

        var device = hand == HandSide.Left ? leftDevice : rightDevice;
        if (!device.isValid) return;

        // 커브를 통해 진행도 → 강도 변환
        float curveValue = hapticCurve.Evaluate(gripValue);
        float amplitude = Mathf.Lerp(hapticMinAmplitude, hapticMaxAmplitude, curveValue);

        device.SendHapticImpulse(0, amplitude, hapticDuration);
    }

    private void StopHaptic(HandSide hand)
    {
        var device = hand == HandSide.Left ? leftDevice : rightDevice;
        if (!device.isValid) return;

        device.StopHaptics();
    }

    /// <summary>
    /// XR 디바이스를 자동 탐색합니다. 런타임에 컨트롤러가 연결되면 찾습니다.
    /// </summary>
    private void FindXRDevices()
    {
        var devices = new List<UnityEngine.XR.InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller,
            devices);
        if (devices.Count > 0)
            leftDevice = devices[0];

        devices.Clear();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller,
            devices);
        if (devices.Count > 0)
            rightDevice = devices[0];

        devicesFound = leftDevice.isValid || rightDevice.isValid;
    }

    // ─── 유틸리티 ───

    private float ReadGripValue(InputActionReference actionRef)
    {
        if (actionRef == null || actionRef.action == null) return 0f;
        return actionRef.action.ReadValue<float>();
    }

    private void EnableAction(InputActionReference actionRef)
    {
        if (actionRef != null && actionRef.action != null)
            actionRef.action.Enable();
    }

    private void DisableAction(InputActionReference actionRef)
    {
        if (actionRef != null && actionRef.action != null)
            actionRef.action.Disable();
    }
}

/// <summary>
/// 어떤 손인지 구분하는 열거형.
/// </summary>
public enum HandSide
{
    None,
    Left,
    Right
}
