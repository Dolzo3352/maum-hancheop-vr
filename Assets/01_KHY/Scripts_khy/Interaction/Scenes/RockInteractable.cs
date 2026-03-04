using System.Collections;
using UnityEngine;

/// <summary>
/// Scene 03: 바위 파괴 인터랙션.
///
/// 링 충전 완료 시 바위가 파괴됩니다.
/// Fractured Mesh(분리된 파편)를 활성화하고 원본 바위를 숨깁니다.
/// 파편에 물리를 적용하여 자연스럽게 흩어지게 합니다.
/// </summary>
public class RockInteractable : RingInteractable
{
    [Header("바위 파괴 설정")]
    [Tooltip("원본 바위 메시")]
    [SerializeField] private GameObject originalRock;

    [Tooltip("파괴된 파편 메시 (미리 분리해둔 것)")]
    [SerializeField] private GameObject fracturedRock;

    [Tooltip("파편에 가할 폭발력")]
    [SerializeField] private float explosionForce = 300f;

    [Tooltip("폭발 반경")]
    [SerializeField] private float explosionRadius = 2f;

    [Tooltip("폭발 파티클 (선택)")]
    [SerializeField] private ParticleSystem destroyParticle;

    [Tooltip("파편이 사라지기까지 시간 (초)")]
    [SerializeField] private float debrisFadeTime = 3f;

    protected override void Awake()
    {
        base.Awake();

        // 초기 상태: 파편 숨김
        if (fracturedRock != null)
            fracturedRock.SetActive(false);
    }

    public override void Execute()
    {
        base.Execute();
        StartCoroutine(DestroyRockCoroutine());
    }

    private IEnumerator DestroyRockCoroutine()
    {
        // 파괴 파티클
        if (destroyParticle != null)
            destroyParticle.Play();

        // 원본 숨기고 파편 표시
        if (originalRock != null)
            originalRock.SetActive(false);

        if (fracturedRock != null)
        {
            fracturedRock.SetActive(true);

            // 파편에 폭발력 적용
            Vector3 explosionCenter = transform.position;
            foreach (var rb in fracturedRock.GetComponentsInChildren<Rigidbody>())
            {
                rb.AddExplosionForce(explosionForce, explosionCenter, explosionRadius);
            }
        }

        // 잠시 대기 후 완료 알림
        yield return new WaitForSeconds(1.0f);
        NotifyInteractionDone();

        // 파편 서서히 제거 (선택)
        yield return new WaitForSeconds(debrisFadeTime);
        if (fracturedRock != null)
            fracturedRock.SetActive(false);
    }
}
