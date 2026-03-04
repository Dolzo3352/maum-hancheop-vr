using System.Collections;
using UnityEngine;

/// <summary>
/// Scene 07: 구름 → 비 내리기 + 구름 걷히기 인터랙션.
///
/// 링 충전 완료 시:
///   1. 비 파티클 시작
///   2. 구름이 서서히 사라짐 (Dissolve 또는 Scale/Alpha)
///   3. 하늘이 열리며 빛이 내려옴
/// </summary>
public class CloudInteractable : RingInteractable
{
    [Header("구름 설정")]
    [Tooltip("구름 오브젝트 (사라질 대상)")]
    [SerializeField] private GameObject cloudObject;

    [Tooltip("구름 Renderer (alpha 제어용)")]
    [SerializeField] private Renderer cloudRenderer;

    [Tooltip("구름 소멸 시간 (초)")]
    [SerializeField] private float dissolveDuration = 3.0f;

    [Header("비")]
    [Tooltip("비 파티클")]
    [SerializeField] private ParticleSystem rainParticle;

    [Tooltip("비 지속 시간 (초)")]
    [SerializeField] private float rainDuration = 5.0f;

    [Header("빛")]
    [Tooltip("구름 걷힌 후 내려오는 빛 (Directional 또는 Spot)")]
    [SerializeField] private Light sunLight;

    [Tooltip("빛 최종 강도")]
    [SerializeField] private float sunTargetIntensity = 2f;

    protected override void Awake()
    {
        base.Awake();

        if (sunLight != null)
            sunLight.intensity = 0f;
    }

    public override void Execute()
    {
        base.Execute();
        StartCoroutine(CloudSequenceCoroutine());
    }

    private IEnumerator CloudSequenceCoroutine()
    {
        // 1. 비 시작
        if (rainParticle != null)
            rainParticle.Play();

        // 2. 구름 소멸
        float elapsed = 0f;
        Color originalColor = Color.white;
        if (cloudRenderer != null)
            originalColor = cloudRenderer.material.color;

        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dissolveDuration;
            float smoothT = t * t;

            // 구름 투명도 감소
            if (cloudRenderer != null)
            {
                Color c = originalColor;
                c.a = Mathf.Lerp(1f, 0f, smoothT);
                cloudRenderer.material.color = c;
            }

            // 구름 스케일 감소
            if (cloudObject != null)
            {
                float scale = Mathf.Lerp(1f, 0.1f, smoothT);
                cloudObject.transform.localScale = Vector3.one * scale;
            }

            // 빛 점진적 증가
            if (sunLight != null)
            {
                sunLight.intensity = Mathf.Lerp(0f, sunTargetIntensity, smoothT);
            }

            yield return null;
        }

        // 구름 비활성화
        if (cloudObject != null)
            cloudObject.SetActive(false);

        // 인터랙션 완료 알림
        NotifyInteractionDone();

        // 비 서서히 멈춤
        yield return new WaitForSeconds(rainDuration);
        if (rainParticle != null)
            rainParticle.Stop();
    }
}
