using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// 1,2,3,4 키 또는 VR 컨트롤러 버튼으로 프리셋 뷰포인트 간 이동
/// 빈 GameObject를 웨이포인트로 배치하고 Inspector에서 할당
/// </summary>
public class WaypointTeleporter : MonoBehaviour
{
    [Header("XR Origin")]
    [Tooltip("XR Origin (XR Rig) 오브젝트를 드래그")]
    public Transform xrOrigin;

    [Tooltip("XR Origin 하위의 Main Camera (HMD)")]
    public Transform xrCamera;

    [Header("웨이포인트 (빈 오브젝트 배치 후 할당)")]
    [Tooltip("흰색 도로 구간에 배치한 뷰포인트들")]
    public Transform[] waypoints;

    [Header("이동 설정")]
    [Tooltip("즉시 이동(false) / 부드러운 이동(true)")]
    public bool smoothMove = true;

    [Tooltip("이동 속도 (초)")]
    [Range(0.1f, 2f)]
    public float moveDuration = 0.5f;

    [Tooltip("이동 중 페이드 효과")]
    public bool useFade = true;

    [Tooltip("페이드 시간 (초)")]
    [Range(0.05f, 0.5f)]
    public float fadeDuration = 0.15f;

    [Header("페이드 (선택)")]
    [Tooltip("카메라 앞에 배치한 Quad의 Renderer (Unlit 머티리얼 사용)")]
    public Renderer fadeQuad;

    [Header("상태")]
    [SerializeField] private int currentWaypoint = 0;
    private bool isMoving = false;
    private Coroutine moveCoroutine;

    void Start()
    {
        if (xrOrigin == null)
        {
            xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>()?.transform;
            if (xrOrigin == null)
                xrOrigin = Camera.main?.transform.parent;
        }

        if (xrCamera == null)
            xrCamera = Camera.main?.transform;

        if (waypoints.Length == 0)
            Debug.LogWarning("[WaypointTeleporter] 웨이포인트가 비어있습니다. Inspector에서 할당하세요.");

        // 페이드 Quad 초기화 (투명하게)
        if (useFade && fadeQuad != null)
            SetFadeAlpha(0f);

        
        // 시작 위치로 이동
        if (waypoints.Length > 0)
            TeleportImmediate(0);
    }

    void Update()
    {
        if (isMoving) return;

        // O: 이전 포인트 / P: 다음 포인트
        if (Keyboard.current != null)
        {
            if (Keyboard.current.oKey.wasPressedThisFrame) TeleportTo(currentWaypoint - 1);
            if (Keyboard.current.pKey.wasPressedThisFrame) TeleportTo(currentWaypoint + 1);
        }
    }

    /// <summary>
    /// 외부에서 호출 가능 (VR 버튼, UI 등)
    /// </summary>
    public void TeleportTo(int index)
    {
        if (waypoints.Length == 0) return;

        // 범위 순환
        index = ((index % waypoints.Length) + waypoints.Length) % waypoints.Length;

        if (index == currentWaypoint) return;
        if (waypoints[index] == null)
        {
            Debug.LogWarning($"[WaypointTeleporter] 웨이포인트 {index}가 null입니다.");
            return;
        }

        currentWaypoint = index;

        if (smoothMove)
        {
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = StartCoroutine(SmoothTeleport(waypoints[index]));
        }
        else
        {
            if (useFade)
            {
                if (moveCoroutine != null) StopCoroutine(moveCoroutine);
                moveCoroutine = StartCoroutine(FadeTeleport(waypoints[index]));
            }
            else
            {
                TeleportImmediate(index);
            }
        }
    }

    /// <summary>
    /// 다음 웨이포인트로 이동
    /// </summary>
    public void TeleportNext()
    {
        TeleportTo(currentWaypoint + 1);
    }

    /// <summary>
    /// 이전 웨이포인트로 이동
    /// </summary>
    public void TeleportPrevious()
    {
        TeleportTo(currentWaypoint - 1);
    }

    /// <summary>
    /// 즉시 텔레포트 (페이드 없음). NarrativeSequencer 등 외부에서 호출 가능.
    /// </summary>
    public void TeleportImmediate(int index)
    {
        if (xrOrigin == null || waypoints[index] == null) return;
        ApplyTeleport(waypoints[index]);
        currentWaypoint = index;
    }

    /// <summary>
    /// HMD 회전을 보정하여 텔레포트합니다.
    /// 플레이어의 실제 시선이 웨이포인트의 forward 방향을 바라보도록 합니다.
    /// </summary>
    private void ApplyTeleport(Transform target)
    {
        // 카메라 자동 탐색 (Inspector에서 연결 안 됐을 때)
        if (xrCamera == null)
            xrCamera = Camera.main?.transform;

        // 카메라의 XR Origin 기준 Y축 회전 (머리가 얼마나 돌아가 있는지)
        float cameraYaw = 0f;
        if (xrCamera != null)
            cameraYaw = xrCamera.eulerAngles.y - xrOrigin.eulerAngles.y;

        Debug.Log($"[WaypointTeleporter] cameraYaw: {cameraYaw}, xrCamera null: {xrCamera == null}");

        // 웨이포인트 방향에서 카메라 회전만큼 빼기
        // → XR Origin을 돌리면 카메라가 정확히 웨이포인트 forward를 바라봄
        Quaternion targetRotation = Quaternion.Euler(0f, target.eulerAngles.y - cameraYaw, 0f);
        xrOrigin.rotation = targetRotation;

        // 위치도 카메라 오프셋 보정
        // XR Origin 내에서 카메라가 중앙이 아닐 수 있음 (룸스케일)
        Vector3 cameraOffset = xrCamera != null
            ? xrOrigin.position - xrCamera.position
            : Vector3.zero;
        cameraOffset.y = 0f; // 높이는 보정하지 않음

        xrOrigin.position = target.position + cameraOffset;
    }

    IEnumerator SmoothTeleport(Transform target)
    {
        isMoving = true;

        // 페이드 아웃
        if (useFade && fadeQuad != null)
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // 페이드 중 즉시 텔레포트 (VR에서 이동 중 회전은 멀미 유발)
        ApplyTeleport(target);

        // 페이드 인
        if (useFade && fadeQuad != null)
            yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        isMoving = false;
    }

    IEnumerator FadeTeleport(Transform target)
    {
        isMoving = true;

        // 페이드 아웃 (검은 화면으로)
        if (fadeQuad != null)
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // 순간 이동 (HMD 회전 보정 포함)
        ApplyTeleport(target);

        // 한 프레임 대기 (렌더링 안정)
        yield return null;

        // 페이드 인
        if (fadeQuad != null)
            yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        isMoving = false;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeQuad == null) return;

        // alpha 0이면 꺼서 렌더링 비용 절약
        fadeQuad.enabled = alpha > 0f;

        var mpb = new MaterialPropertyBlock();
        fadeQuad.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", new Color(0f, 0f, 0f, alpha));
        fadeQuad.SetPropertyBlock(mpb);
    }

    // 기즈모로 에디터에서 웨이포인트 시각화
    void OnDrawGizmos()
    {
        if (waypoints == null) return;

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            // 현재 위치 구체
            Gizmos.color = (i == currentWaypoint) ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);

            // 번호 라벨
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                waypoints[i].position + Vector3.up * 0.5f,
                $"WP {i + 1}",
                new GUIStyle()
                {
                    normal = { textColor = Color.white },
                    fontSize = 14,
                    fontStyle = FontStyle.Bold
                }
            );
#endif

            // 바라보는 방향 화살표
            Gizmos.color = Color.green;
            Gizmos.DrawRay(waypoints[i].position, waypoints[i].forward * 1f);

            // 연결선
            if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
    }
}