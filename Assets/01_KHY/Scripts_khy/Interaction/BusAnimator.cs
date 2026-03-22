using UnityEngine;

/// <summary>
/// 버스 생동감 애니메이션.
/// 바퀴 회전 + 몸통 흔들림(상하 바운스, 좌우 롤, 앞뒤 피치).
/// </summary>
public class BusAnimator : MonoBehaviour
{
    [Header("바퀴")]
    [Tooltip("바퀴 Transform 4개 (FL, FR, BL, BR)")]
    [SerializeField] private Transform[] wheels;

    [Tooltip("바퀴 회전 속도 (도/초)")]
    [SerializeField] private float wheelSpeed = 360f;

    [Tooltip("바퀴 회전축 (로컬)")]
    [SerializeField] private Vector3 wheelAxis = Vector3.right;

    [Header("몸통")]
    [Tooltip("몸통 Transform (없으면 자기 자신)")]
    [SerializeField] private Transform body;

    [Header("상하 바운스")]
    [SerializeField] private float bounceAmount = 0.02f;
    [SerializeField] private float bounceSpeed = 3f;

    [Header("좌우 롤")]
    [SerializeField] private float rollAmount = 0.5f;
    [SerializeField] private float rollSpeed = 2f;

    [Header("앞뒤 피치")]
    [SerializeField] private float pitchAmount = 0.3f;
    [SerializeField] private float pitchSpeed = 2.5f;

    private Vector3 bodyOriginalPos;
    private Quaternion bodyOriginalRot;

    private void Start()
    {
        if (body == null) body = transform;
        bodyOriginalPos = body.localPosition;
        bodyOriginalRot = body.localRotation;
    }

    private void Update()
    {
        float time = Time.time;

        // ── 바퀴 회전 ──
        if (wheels != null)
        {
            foreach (var wheel in wheels)
            {
                if (wheel != null)
                    wheel.Rotate(wheelAxis, wheelSpeed * Time.deltaTime, Space.Self);
            }
        }

        // ── 몸통 흔들림 ──
        if (body != null)
        {
            // 상하 바운스
            float bounce = Mathf.Sin(time * bounceSpeed) * bounceAmount;
            body.localPosition = bodyOriginalPos + Vector3.up * bounce;

            // 좌우 롤 + 앞뒤 피치 (각각 다른 주파수로 자연스럽게)
            float roll = Mathf.Sin(time * rollSpeed) * rollAmount;
            float pitch = Mathf.Sin(time * pitchSpeed + 0.7f) * pitchAmount;
            body.localRotation = bodyOriginalRot * Quaternion.Euler(pitch, 0f, roll);
        }
    }
}
