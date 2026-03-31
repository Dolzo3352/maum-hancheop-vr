using UnityEngine;

/// <summary>
/// 자막 Canvas에 부착하여 카메라를 향해 아주 미세하게 회전합니다.
/// 완전 고정도 아니고 완전 빌보드도 아닌, 부드러운 중간값.
///
/// 사용법:
///   각 스테이지의 자막 Canvas에 부착합니다.
/// </summary>
public class SubtitleSoftFollow : MonoBehaviour
{
    [Header("회전")]
    [Tooltip("카메라를 향해 회전하는 비율 (0 = 완전 고정, 1 = 완전 빌보드)")]
    [Range(0f, 1f)]
    [SerializeField] private float followStrength = 0.15f;

    [Tooltip("회전 부드러움 (낮을수록 느리게 반응)")]
    [SerializeField] private float smoothSpeed = 2f;

    [Tooltip("Y축 회전만 적용 (좌우만 따라감)")]
    [SerializeField] private bool yAxisOnly = true;

    [Header("카메라")]
    [Tooltip("추적할 카메라 Transform (비워두면 Camera.main 사용)")]
    [SerializeField] private Transform targetCamera;

    // 초기 회전값 캐싱
    private Quaternion baseRotation;

    private void Start()
    {
        baseRotation = transform.rotation;

        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main.transform;
    }

    private void LateUpdate()
    {
        if (targetCamera == null) return;

        // 카메라를 바라보는 회전 계산
        Vector3 dirToCamera = targetCamera.position - transform.position;

        if (yAxisOnly)
            dirToCamera.y = 0f;

        if (dirToCamera.sqrMagnitude < 0.001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(dirToCamera);

        // 기본 회전과 카메라 방향 사이를 followStrength 비율로 블렌드
        Quaternion targetRotation = Quaternion.Slerp(baseRotation, lookRotation, followStrength);

        // 부드럽게 보간
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
    }
}
