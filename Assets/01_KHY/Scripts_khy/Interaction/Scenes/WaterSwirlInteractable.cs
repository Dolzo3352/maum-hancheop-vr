using System.Collections;
using UnityEngine;

/// <summary>
/// Scene 05: 냇가 소용돌이 생성 인터랙션.
///
/// 링 충전 완료 시 냇물에 소용돌이가 생성됩니다.
/// 파티클 시스템 또는 셰이더 기반으로 소용돌이를 표현합니다.
/// </summary>
public class WaterSwirlInteractable : RingInteractable
{
    [Header("소용돌이 설정")]
    [Tooltip("소용돌이 파티클")]
    [SerializeField] private ParticleSystem swirlParticle;

    [Tooltip("소용돌이 오브젝트 (메시 또는 셰이더 기반)")]
    [SerializeField] private GameObject swirlObject;

    [Tooltip("소용돌이 생성 시간 (초)")]
    [SerializeField] private float swirlBuildDuration = 1.5f;

    [Tooltip("소용돌이 유지 시간 (초)")]
    [SerializeField] private float swirlHoldDuration = 3.0f;

    [Tooltip("물 머티리얼 (셰이더 파라미터 제어용, 선택)")]
    [SerializeField] private Renderer waterRenderer;

    [Tooltip("소용돌이 강도 셰이더 프로퍼티명")]
    [SerializeField] private string swirlIntensityProperty = "_SwirlIntensity";

    protected override void Awake()
    {
        base.Awake();

        if (swirlObject != null)
            swirlObject.SetActive(false);
    }

    public override void Execute()
    {
        base.Execute();
        StartCoroutine(CreateSwirlCoroutine());
    }

    private IEnumerator CreateSwirlCoroutine()
    {
        // 소용돌이 오브젝트/파티클 활성화
        if (swirlObject != null)
            swirlObject.SetActive(true);

        if (swirlParticle != null)
            swirlParticle.Play();

        // 소용돌이 점진적 생성 (셰이더 기반)
        float elapsed = 0f;
        while (elapsed < swirlBuildDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swirlBuildDuration;
            float smoothT = t * t * (3f - 2f * t);

            if (waterRenderer != null)
                waterRenderer.material.SetFloat(swirlIntensityProperty, smoothT);

            yield return null;
        }

        // 인터랙션 완료 알림 (아이가 반응할 수 있도록)
        NotifyInteractionDone();

        // 소용돌이 유지
        yield return new WaitForSeconds(swirlHoldDuration);

        // 소용돌이 서서히 소멸
        elapsed = 0f;
        while (elapsed < swirlBuildDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / swirlBuildDuration);

            if (waterRenderer != null)
                waterRenderer.material.SetFloat(swirlIntensityProperty, t);

            yield return null;
        }

        if (swirlParticle != null)
            swirlParticle.Stop();
        if (swirlObject != null)
            swirlObject.SetActive(false);
    }
}
