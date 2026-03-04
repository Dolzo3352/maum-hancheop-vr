using System.Collections;
using UnityEngine;

/// <summary>
/// Scene 09: 아이의 보따리 → 진심 에너지 추출 인터랙션.
///
/// 링 충전 완료 시 보따리에서 분홍빛 에너지가 추출되어 솥으로 이동합니다.
/// 약 완성 VFX(폭발 광원)가 이어집니다.
/// </summary>
public class HeartEnergyInteractable : RingInteractable
{
    [Header("에너지 설정")]
    [Tooltip("에너지가 시작되는 위치 (보따리)")]
    [SerializeField] private Transform energyStartPoint;

    [Tooltip("에너지가 도착하는 위치 (솥)")]
    [SerializeField] private Transform energyEndPoint;

    [Tooltip("에너지 오브젝트 (빛/파티클)")]
    [SerializeField] private GameObject energyObject;

    [Tooltip("에너지 이동 시간 (초)")]
    [SerializeField] private float energyTravelDuration = 2.0f;

    [Tooltip("이동 경로 높이")]
    [SerializeField] private float arcHeight = 2f;

    [Header("에너지 파티클")]
    [Tooltip("보따리에서 추출될 때 파티클")]
    [SerializeField] private ParticleSystem extractParticle;

    [Tooltip("에너지 이동 트레일 파티클")]
    [SerializeField] private ParticleSystem trailParticle;

    [Header("약 완성 VFX")]
    [Tooltip("약 완성 시 폭발 파티클")]
    [SerializeField] private ParticleSystem completionExplosion;

    [Tooltip("약 완성 시 폭발 Light")]
    [SerializeField] private Light completionLight;

    [Tooltip("폭발 Light 최대 강도")]
    [SerializeField] private float explosionLightIntensity = 10f;

    [Tooltip("폭발 빛 지속 시간 (초)")]
    [SerializeField] private float explosionDuration = 2f;

    protected override void Awake()
    {
        base.Awake();

        if (energyObject != null)
            energyObject.SetActive(false);
        if (completionLight != null)
            completionLight.intensity = 0f;
    }

    public override void Execute()
    {
        base.Execute();
        StartCoroutine(HeartEnergySequence());
    }

    private IEnumerator HeartEnergySequence()
    {
        // 1. 에너지 추출 파티클
        if (extractParticle != null)
            extractParticle.Play();

        yield return new WaitForSeconds(0.5f);

        // 2. 에너지 오브젝트 활성화 + 이동
        if (energyObject != null && energyStartPoint != null && energyEndPoint != null)
        {
            energyObject.SetActive(true);
            energyObject.transform.position = energyStartPoint.position;

            if (trailParticle != null)
                trailParticle.Play();

            // 포물선 이동
            float elapsed = 0f;
            while (elapsed < energyTravelDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / energyTravelDuration;
                float smoothT = t * t * (3f - 2f * t);

                Vector3 linearPos = Vector3.Lerp(energyStartPoint.position, energyEndPoint.position, smoothT);
                float arc = arcHeight * 4f * t * (1f - t);
                energyObject.transform.position = linearPos + Vector3.up * arc;

                yield return null;
            }

            // 에너지 도착
            energyObject.transform.position = energyEndPoint.position;
        }

        // 3. 약 완성 폭발!
        if (completionExplosion != null)
            completionExplosion.Play();

        // 폭발 빛
        if (completionLight != null)
        {
            float elapsed = 0f;
            // 빠르게 밝아지기
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                completionLight.intensity = Mathf.Lerp(0f, explosionLightIntensity, elapsed / 0.3f);
                yield return null;
            }

            // 서서히 줄어들기
            elapsed = 0f;
            while (elapsed < explosionDuration)
            {
                elapsed += Time.deltaTime;
                completionLight.intensity = Mathf.Lerp(explosionLightIntensity, 1f, elapsed / explosionDuration);
                yield return null;
            }
        }

        // 에너지 오브젝트 숨김
        if (energyObject != null)
            energyObject.SetActive(false);

        // 완료 알림
        NotifyInteractionDone();
    }
}
