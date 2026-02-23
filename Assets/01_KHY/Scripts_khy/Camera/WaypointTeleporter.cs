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
    [Tooltip("없으면 자동 생성")]
    public CanvasGroup fadeCanvas;

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

        if (waypoints.Length == 0)
            Debug.LogWarning("[WaypointTeleporter] 웨이포인트가 비어있습니다. Inspector에서 할당하세요.");

        // 페이드 캔버스 자동 생성
        if (useFade && fadeCanvas == null)
            CreateFadeCanvas();

        
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

    void TeleportImmediate(int index)
    {
        if (xrOrigin == null || waypoints[index] == null) return;
        xrOrigin.position = waypoints[index].position;
        xrOrigin.rotation = waypoints[index].rotation;
        currentWaypoint = index;
    }

    IEnumerator SmoothTeleport(Transform target)
    {
        isMoving = true;

        // 페이드 아웃
        if (useFade && fadeCanvas != null)
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        Vector3 startPos = xrOrigin.position;
        Quaternion startRot = xrOrigin.rotation;
        Vector3 endPos = target.position;
        Quaternion endRot = target.rotation;

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            // ease in-out
            t = t * t * (3f - 2f * t);

            xrOrigin.position = Vector3.Lerp(startPos, endPos, t);
            xrOrigin.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        xrOrigin.position = endPos;
        xrOrigin.rotation = endRot;

        // 페이드 인
        if (useFade && fadeCanvas != null)
            yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        isMoving = false;
    }

    IEnumerator FadeTeleport(Transform target)
    {
        isMoving = true;

        // 페이드 아웃 (검은 화면으로)
        if (fadeCanvas != null)
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // 순간 이동
        xrOrigin.position = target.position;
        xrOrigin.rotation = target.rotation;

        // 한 프레임 대기 (렌더링 안정)
        yield return null;

        // 페이드 인
        if (fadeCanvas != null)
            yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        isMoving = false;
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        fadeCanvas.alpha = to;
    }

    void CreateFadeCanvas()
    {
        // 페이드용 검은 화면 자동 생성
        GameObject canvasObj = new GameObject("FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        fadeCanvas = canvasObj.AddComponent<CanvasGroup>();
        fadeCanvas.alpha = 0f;
        fadeCanvas.blocksRaycasts = false;
        fadeCanvas.interactable = false;

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image image = imageObj.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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