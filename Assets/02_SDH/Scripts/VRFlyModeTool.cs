using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

/// <summary>
/// VR 플레이 모드에서 씬뷰처럼 XR Origin 위치를 자유롭게 이동할 수 있는 디버그 툴.
///
/// ★ XR Origin의 rotation은 건드리지 않음 (HMD 트래킹 충돌 방지)
///    이동 방향만 마우스 우클릭 드래그로 제어하는 "나침반" 방식
///
/// - Enter 두 번 빠르게: Fly Mode 토글 (ON ↔ OFF)
/// - Fly Mode ON: WASD 이동, QE 상하, 마우스 우클릭+드래그로 이동방향, Shift 가속
/// - R 키: 현재 위치에 웨이포인트 GameObject 생성 (카메라 오프셋 보정 포함)
/// - T 키: 저장된 트랜스폼으로 복원
/// </summary>
public class VRFlyModeTool : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float shiftMultiplier = 3f;
    [SerializeField] private float mouseSensitivity = 2f;

    [Header("더블 엔터 감지")]
    [SerializeField] private float doubleEnterInterval = 0.5f;

    [Header("UI")]
    [SerializeField] private bool showOverlay = true;

    private XROrigin xrOrigin;
    private Camera xrCamera;
    private bool flyModeActive = false;
    private float lastEnterTime = -999f;

    // 이동 방향 제어용 (XR Origin 회전과 별개)
    private float navYaw = 0f;
    private float navPitch = 0f;

    // 웨이포인트 생성 카운터
    private int waypointCount = 0;
    private Transform waypointParent;

    // 위치 저장/복원용
    private Vector3 savedPos;
    private float savedRotY;
    private bool hasSavedPos = false;

    private Keyboard kb;
    private Mouse mouse;

    private void Start()
    {
        xrOrigin = FindAnyObjectByType<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("[VRFlyModeTool] XR Origin을 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        xrCamera = xrOrigin.Camera;
        if (xrCamera == null)
            xrCamera = Camera.main;

        navYaw = xrOrigin.transform.eulerAngles.y;
        navPitch = 0f;

        // 웨이포인트 부모 오브젝트
        waypointParent = new GameObject("── VRFly Waypoints ──").transform;

        Debug.Log("[VRFlyModeTool] 준비 완료");
        Debug.Log("  Enter×2: Fly Mode 토글");
        Debug.Log("  R: 현재 위치에 웨이포인트 생성 (WaypointTeleporter용)");
        Debug.Log("  T: 마지막 저장 위치로 복원");
    }

    private void Update()
    {
        kb = Keyboard.current;
        mouse = Mouse.current;
        if (kb == null) return;

        DetectDoubleEnter();
        DetectSaveLoad();

        if (flyModeActive)
        {
            HandleNavDirection();
            HandleMovement();
        }
    }

    // ─────────────────────────────────────────
    // 더블 엔터 감지
    // ─────────────────────────────────────────
    private void DetectDoubleEnter()
    {
        bool enterPressed = kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;

        if (enterPressed)
        {
            float now = Time.unscaledTime;
            if (now - lastEnterTime < doubleEnterInterval)
            {
                ToggleFlyMode();
                lastEnterTime = -999f;
            }
            else
            {
                lastEnterTime = now;
            }
        }
    }

    private void ToggleFlyMode()
    {
        flyModeActive = !flyModeActive;

        if (flyModeActive)
        {
            navYaw = xrOrigin.transform.eulerAngles.y;
            navPitch = 0f;
            Debug.Log("[VRFlyModeTool] ★ <color=lime>Fly Mode ON</color> ★  WASD:이동 | QE:상하 | 우클릭+드래그:방향 | Shift:가속");
        }
        else
        {
            Debug.Log("[VRFlyModeTool] ★ <color=red>Fly Mode OFF</color> ★  VR 뷰 모드");
        }
    }

    // ─────────────────────────────────────────
    // 이동 방향 (마우스 우클릭 드래그)
    // ─────────────────────────────────────────
    private void HandleNavDirection()
    {
        if (mouse == null) return;

        if (mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            navYaw += delta.x * mouseSensitivity * 0.1f;
            navPitch -= delta.y * mouseSensitivity * 0.1f;
            navPitch = Mathf.Clamp(navPitch, -89f, 89f);
        }
    }

    // ─────────────────────────────────────────
    // WASD QE 이동 (position만 변경)
    // ─────────────────────────────────────────
    private void HandleMovement()
    {
        float speed = moveSpeed;
        if (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)
            speed *= shiftMultiplier;

        Quaternion navRot = Quaternion.Euler(navPitch, navYaw, 0f);
        Vector3 forward = navRot * Vector3.forward;
        Vector3 right = navRot * Vector3.right;

        Vector3 move = Vector3.zero;

        if (kb.wKey.isPressed) move += forward;
        if (kb.sKey.isPressed) move -= forward;
        if (kb.aKey.isPressed) move -= right;
        if (kb.dKey.isPressed) move += right;
        if (kb.eKey.isPressed) move += Vector3.up;
        if (kb.qKey.isPressed) move -= Vector3.up;

        if (move.sqrMagnitude > 0.001f)
            xrOrigin.transform.position += move.normalized * speed * Time.unscaledDeltaTime;
    }

    // ─────────────────────────────────────────
    // R: 웨이포인트 생성 + 위치 저장
    // T: 위치 복원
    // ─────────────────────────────────────────
    private void DetectSaveLoad()
    {
        if (kb.rKey.wasPressedThisFrame)
            CreateWaypoint();

        if (kb.tKey.wasPressedThisFrame)
            LoadTransform();
    }

    /// <summary>
    /// R키: XR Origin의 월드 좌표를 그대로 저장 + 디버그 로그 출력.
    /// 콘솔 로그에서 XR Origin 위치와 카메라 위치를 비교해서 오차 원인을 파악.
    /// </summary>
    private void CreateWaypoint()
    {
        if (xrOrigin == null) return;

        waypointCount++;
        string wpName = $"WP_{waypointCount:D2}_VRFly";

        Vector3 originPos = xrOrigin.transform.position;
        Quaternion originRot = xrOrigin.transform.rotation;
        Vector3 camPos = xrCamera != null ? xrCamera.transform.position : originPos;
        Vector3 camLocalPos = xrCamera != null ? xrCamera.transform.localPosition : Vector3.zero;

        // --- 웨이포인트 GameObject 생성 (루트에 생성, 부모 없음) ---
        GameObject wp = new GameObject(wpName);
        // 부모 없이 루트에 생성 → Inspector 좌표 = 월드 좌표
        wp.transform.position = originPos;
        wp.transform.rotation = originRot;

        // 위치 저장 (T키 복원용)
        savedPos = originPos;
        savedRotY = xrOrigin.transform.eulerAngles.y;
        hasSavedPos = true;

        Debug.Log($"[VRFlyModeTool] ═══════════════════════════════════");
        Debug.Log($"[VRFlyModeTool] <color=cyan>웨이포인트 생성: {wpName}</color>");
        Debug.Log($"  XR Origin 월드 위치 (이 값을 S_1에 입력): {originPos}");
        Debug.Log($"  XR Origin 월드 회전: {xrOrigin.transform.eulerAngles}");
        Debug.Log($"  ────────────────────");
        Debug.Log($"  카메라 월드 위치: {camPos}");
        Debug.Log($"  카메라 로컬 위치 (HMD 오프셋): {camLocalPos}");
        Debug.Log($"  카메라↔Origin 차이: {camPos - originPos}");
        Debug.Log($"[VRFlyModeTool] ═══════════════════════════════════");
    }

    private void LoadTransform()
    {
        if (xrOrigin == null) return;

        if (!hasSavedPos)
        {
            Debug.LogWarning("[VRFlyModeTool] 저장된 위치가 없습니다. R키로 먼저 저장하세요.");
            return;
        }

        xrOrigin.transform.position = savedPos;
        xrOrigin.transform.rotation = Quaternion.Euler(0f, savedRotY, 0f);
        navYaw = savedRotY;
        navPitch = 0f;

        Debug.Log($"[VRFlyModeTool] <color=yellow>위치 복원됨</color> Pos: {savedPos}, RotY: {savedRotY:F1}°");
    }

    // ─────────────────────────────────────────
    // 화면 오버레이
    // ─────────────────────────────────────────
    private void OnGUI()
    {
        if (!showOverlay) return;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.fontSize = 14;
        boxStyle.richText = true;
        boxStyle.alignment = TextAnchor.MiddleLeft;
        boxStyle.normal.textColor = Color.white;
        boxStyle.padding = new RectOffset(12, 12, 6, 6);

        float w = 560;
        float h = flyModeActive ? 80 : 36;
        Rect rect = new Rect(10, Screen.height - h - 10, w, h);

        Color oldBg = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0, 0, 0, 0.75f);
        GUI.Box(rect, "", boxStyle);
        GUI.backgroundColor = oldBg;

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14;
        labelStyle.richText = true;
        labelStyle.normal.textColor = Color.white;

        if (flyModeActive)
        {
            GUI.Label(new Rect(rect.x + 12, rect.y + 4, w - 24, 20),
                "<color=lime>● FLY MODE ON</color>  <color=#aaa>Enter×2: OFF</color>", labelStyle);

            GUIStyle smallStyle = new GUIStyle(labelStyle);
            smallStyle.fontSize = 12;
            smallStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

            GUI.Label(new Rect(rect.x + 12, rect.y + 26, w - 24, 18),
                "WASD:이동  QE:상하  우클릭+드래그:방향  Shift:가속", smallStyle);

            GUI.Label(new Rect(rect.x + 12, rect.y + 44, w - 24, 18),
                $"R:웨이포인트생성  T:위치복원  |  방향 Yaw:{navYaw:F0}° Pitch:{navPitch:F0}°", smallStyle);

            // 생성된 웨이포인트 수 표시
            if (waypointCount > 0)
            {
                GUI.Label(new Rect(rect.x + 12, rect.y + 60, w - 24, 18),
                    $"<color=cyan>생성된 웨이포인트: {waypointCount}개</color>", smallStyle);
            }
        }
        else
        {
            GUI.Label(new Rect(rect.x + 12, rect.y + 6, w - 24, 22),
                "<color=#888>○ FLY MODE OFF</color>  <color=#666>Enter×2: ON | R:웨이포인트생성 | T:복원</color>", labelStyle);
        }
    }
}
