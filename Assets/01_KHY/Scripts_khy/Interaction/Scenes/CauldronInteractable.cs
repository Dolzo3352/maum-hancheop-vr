using System.Collections;
using UnityEngine;

/// <summary>
/// Scene 08: 아궁이 불 지피기 인터랙션.
///
/// 링 충전으로 불을 지핍니다.
/// 별도로 약재를 솥에 넣는 것은 XR Grab Interactable 기반입니다.
/// </summary>
public class CauldronInteractable : RingInteractable
{
    [Header("아궁이 설정")]
    [Tooltip("아궁이 불 Light")]
    [SerializeField] private Light fireLight;

    [Tooltip("불 최종 강도")]
    [SerializeField] private float fireIntensity = 4f;

    [Tooltip("불 파티클")]
    [SerializeField] private ParticleSystem fireParticle;

    [Tooltip("불 켜지는 시간 (초)")]
    [SerializeField] private float ignitionDuration = 1.5f;

    [Header("솥")]
    [Tooltip("솥 안의 액체 (색상 변화용, 선택)")]
    [SerializeField] private Renderer liquidRenderer;

    [Tooltip("끓는 파티클 (선택)")]
    [SerializeField] private ParticleSystem boilingParticle;

    [Header("실패 연출")]
    [Tooltip("재료 부족 시 빛이 사그라드는 연출 활성화")]
    [SerializeField] private bool playFailureSequence = true;

    [Tooltip("실패 시 사그라드는 시간 (초)")]
    [SerializeField] private float failureFadeDuration = 2.0f;

    protected override void Awake()
    {
        base.Awake();

        if (fireLight != null)
            fireLight.intensity = 0f;
    }

    public override void Execute()
    {
        base.Execute();
        StartCoroutine(IgniteCoroutine());
    }

    private IEnumerator IgniteCoroutine()
    {
        // 불 파티클
        if (fireParticle != null)
            fireParticle.Play();

        // 불 점진적 밝히기
        float elapsed = 0f;
        while (elapsed < ignitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / ignitionDuration;

            if (fireLight != null)
                fireLight.intensity = Mathf.Lerp(0f, fireIntensity, t);

            yield return null;
        }

        // 끓는 파티클
        if (boilingParticle != null)
            boilingParticle.Play();

        // 인터랙션 완료 알림
        NotifyInteractionDone();

        // 실패 연출 (재료 부족)
        if (playFailureSequence)
        {
            yield return new WaitForSeconds(3f);
            yield return FailureSequence();
        }
    }

    private IEnumerator FailureSequence()
    {
        float elapsed = 0f;
        float startIntensity = fireLight != null ? fireLight.intensity : 0f;

        while (elapsed < failureFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / failureFadeDuration;

            if (fireLight != null)
                fireLight.intensity = Mathf.Lerp(startIntensity, 0f, t);

            yield return null;
        }

        if (fireParticle != null)
            fireParticle.Stop();
        if (boilingParticle != null)
            boilingParticle.Stop();
    }
}
