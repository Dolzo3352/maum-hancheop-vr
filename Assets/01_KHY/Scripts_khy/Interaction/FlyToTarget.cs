using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 오브젝트를 지정 대상까지 포물선으로 날려 보내는 범용 컴포넌트.
///
/// Timeline Signal이나 UnityEvent에서 Launch()를 호출하면
/// delay 후 target까지 arc 궤적으로 이동합니다.
///
/// 사용 예:
///   - 감/들꽃 → 가마솥 근처로 이동
///   - 완성된 약 → 아이 손으로 전달
/// </summary>
public class FlyToTarget : MonoBehaviour
{
    [Header("목표")]
    [Tooltip("도착 위치")]
    [SerializeField] private Transform target;

    [Header("이동 설정")]
    [Tooltip("Launch() 호출 후 대기 시간 (초)")]
    [SerializeField] private float delay;

    [Tooltip("비행 시간 (초)")]
    [SerializeField] private float duration = 2f;

    [Tooltip("포물선 높이 (0이면 직선)")]
    [SerializeField] private float arcHeight = 2f;

    [Tooltip("비행 중 target 방향으로 회전")]
    [SerializeField] private bool lookAtTarget;

    [Header("VFX")]
    [Tooltip("비행 중 트레일 파티클 (선택)")]
    [SerializeField] private ParticleSystem trailParticle;

    [Header("이벤트")]
    [Tooltip("도착 시 호출")]
    [SerializeField] private UnityEvent onArrived;

    private bool isFlying;

    /// <summary>
    /// 외부에서 호출 — Timeline Signal UnityEvent 또는 코드.
    /// </summary>
    public void Launch()
    {
        if (isFlying || target == null) return;
        StartCoroutine(FlyCoroutine());
    }

    private IEnumerator FlyCoroutine()
    {
        isFlying = true;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        Vector3 startPos = transform.position;

        if (trailParticle != null)
            trailParticle.Play();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = t * t * (3f - 2f * t); // smoothstep

            Vector3 linearPos = Vector3.Lerp(startPos, target.position, smoothT);
            float arc = arcHeight * 4f * t * (1f - t); // parabolic
            transform.position = linearPos + Vector3.up * arc;

            if (lookAtTarget)
            {
                float nextT = Mathf.Clamp01(t + 0.05f);
                float nextSmooth = nextT * nextT * (3f - 2f * nextT);
                Vector3 nextPos = Vector3.Lerp(startPos, target.position, nextSmooth);
                float nextArc = arcHeight * 4f * nextT * (1f - nextT);
                transform.LookAt(nextPos + Vector3.up * nextArc);
            }

            yield return null;
        }

        transform.position = target.position;

        if (trailParticle != null)
            trailParticle.Stop();

        onArrived?.Invoke();
        isFlying = false;
    }
}
