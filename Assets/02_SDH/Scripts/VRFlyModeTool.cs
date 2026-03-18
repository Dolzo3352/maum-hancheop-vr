using UnityEngine;
using Unity.XR.CoreUtils;

/// <summary>
/// VR 플레이 모드에서 씬뷰처럼 XR Origin을 자유롭게 이동/회전할 수 있는 디버그 툴.
/// - Enter 두 번 빠르게 누르기: Fly Mode 토글 (ON/OFF)
/// - Fly Mode ON: WASD 이동, QE 상하, 마우스 우클릭+드래그 회전, Shift 가속
/// - R 키: 현재 XR Origin의 월드 트랜스폼 저장 (PlayerPrefs)
/// - T 키: 저장된 트랜스폼으로 복원
/// </summary>
public class VRFlyModeTool : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float shiftMultiplier = 3f;
    [SerializeField] private float mouseSensitivity = 2f;

    [Header("더블 엔터 감지")]
    [SerializeField] private float doubleEnterInterval = 0.4f;

    [Header("UI")]
    [SerializeField] private bool showOverlay = true;

    private XROrigin xrOrigin;
    private bool flyModeActive = false;
    private float lastEnterTime = -1f;

    private float rotX = 0f;
    private float rotY = 0f;

    private const string PrefKeyPosX = "VRFly_PosX";
    private const string PrefKeyPosY = "VRFly_PosY";
    private const string PrefKeyPosZ = "VRFly_PosZ";
    private const string PrefKeyRotX = "VRFly_RotX";
    private const string PrefKeyRotY = "VRFly_RotY";
    private const string PrefKeyRotZ = "VRFly_RotZ";

    private void Start()
    {
        xrOrigin = FindAnyObjectByType<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("[VRFlyModeTool] XR Origin을 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        Vector3 euler = xrOrigin.transform.eulerAngles;
        rotX = euler.x;
        rotY = euler.y;

        Debug.Log("[VRFlyModeTool] 준비 완료. Enter 두 번으로 Fly Mode 토글, R로 위치 저장, T로 위치 복원");
    }

    private void Update()
    {
        DetectDoubleEnter();
        DetectSaveLoad();

        if (flyModeActive)
        {
            HandleRotation();
            HandleMovement();
        }
    }

    private void DetectDoubleEnter()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            float now = Time.unscaledTime;
            if (now - lastEnterTime < doubleEnterInterval)
            {
                ToggleFlyMode();
                lastEnterTime = -1f;
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
            // 현재 XR Origin 회전값으로 초기화
            Vector3 euler = xrOrigin.transform.eulerAngles;
            rotX = euler.x;
            rotY = euler.y;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[VRFlyModeTool] <color=green>Fly Mode ON</color> - WASD 이동, 우클릭+드래그 회전, Shift 가속");
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Debug.Log("[VRFlyModeTool] <color=red>Fly Mode OFF</color> - VR 뷰 모드");
        }
    }

    private void HandleRotation()
    {
        // 마우스 우클릭 드래그로 회전
        if (Input.GetMouseButton(1))
        {
            rotY += Input.GetAxis("Mouse X") * mouseSensitivity;
            rotX -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            rotX = Mathf.Clamp(rotX, -90f, 90f);

            xrOrigin.transform.rotation = Quaternion.Euler(rotX, rotY, 0f);
        }
    }

    private void HandleMovement()
    {
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed *= shiftMultiplier;

        Vector3 move = Vector3.zero;
        Transform t = xrOrigin.transform;

        // WASD 전후좌우
        if (Input.GetKey(KeyCode.W)) move += t.forward;
        if (Input.GetKey(KeyCode.S)) move -= t.forward;
        if (Input.GetKey(KeyCode.A)) move -= t.right;
        if (Input.GetKey(KeyCode.D)) move += t.right;

        // QE 상하
        if (Input.GetKey(KeyCode.E)) move += Vector3.up;
        if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;

        xrOrigin.transform.position += move.normalized * speed * Time.unscaledDeltaTime;
    }

    private void DetectSaveLoad()
    {
        // Fly Mode가 아닐 때도 저장/복원 가능
        if (Input.GetKeyDown(KeyCode.R))
        {
            SaveTransform();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            LoadTransform();
        }
    }

    private void SaveTransform()
    {
        if (xrOrigin == null) return;

        Vector3 pos = xrOrigin.transform.position;
        Vector3 rot = xrOrigin.transform.eulerAngles;

        PlayerPrefs.SetFloat(PrefKeyPosX, pos.x);
        PlayerPrefs.SetFloat(PrefKeyPosY, pos.y);
        PlayerPrefs.SetFloat(PrefKeyPosZ, pos.z);
        PlayerPrefs.SetFloat(PrefKeyRotX, rot.x);
        PlayerPrefs.SetFloat(PrefKeyRotY, rot.y);
        PlayerPrefs.SetFloat(PrefKeyRotZ, rot.z);
        PlayerPrefs.Save();

        Debug.Log($"[VRFlyModeTool] <color=cyan>위치 저장됨</color> - Pos: {pos}, Rot: {rot}");
    }

    private void LoadTransform()
    {
        if (xrOrigin == null) return;

        if (!PlayerPrefs.HasKey(PrefKeyPosX))
        {
            Debug.LogWarning("[VRFlyModeTool] 저장된 위치가 없습니다. R키로 먼저 저장하세요.");
            return;
        }

        Vector3 pos = new Vector3(
            PlayerPrefs.GetFloat(PrefKeyPosX),
            PlayerPrefs.GetFloat(PrefKeyPosY),
            PlayerPrefs.GetFloat(PrefKeyPosZ)
        );
        Vector3 rot = new Vector3(
            PlayerPrefs.GetFloat(PrefKeyRotX),
            PlayerPrefs.GetFloat(PrefKeyRotY),
            PlayerPrefs.GetFloat(PrefKeyRotZ)
        );

        xrOrigin.transform.position = pos;
        xrOrigin.transform.rotation = Quaternion.Euler(rot);
        rotX = rot.x;
        rotY = rot.y;

        Debug.Log($"[VRFlyModeTool] <color=yellow>위치 복원됨</color> - Pos: {pos}, Rot: {rot}");
    }

    private void OnGUI()
    {
        if (!showOverlay) return;

        // 상태 표시 UI
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 16;
        style.alignment = TextAnchor.MiddleLeft;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(10, 10, 5, 5);

        string status = flyModeActive
            ? "<color=lime>[FLY MODE ON]</color>  WASD:이동  QE:상하  우클릭+드래그:회전  Shift:가속"
            : "<color=gray>[FLY MODE OFF]</color>  Enter×2: 토글";

        string saveInfo = "R:위치저장  T:위치복원";

        float boxWidth = 700;
        float boxHeight = 50;
        Rect rect = new Rect(10, Screen.height - boxHeight - 10, boxWidth, boxHeight);

        GUI.Box(rect, "", style);
        style.richText = true;
        GUI.Label(new Rect(rect.x + 10, rect.y, rect.width - 20, rect.height / 2), status, style);

        GUIStyle smallStyle = new GUIStyle(style);
        smallStyle.fontSize = 13;
        smallStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
        GUI.Label(new Rect(rect.x + 10, rect.y + rect.height / 2 - 3, rect.width - 20, rect.height / 2), saveInfo, smallStyle);
    }
}
