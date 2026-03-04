using System.Collections;
using UnityEngine;

/// <summary>
/// Scene 01: 호롱불 밝히기 인터랙션.
///
/// 링 충전 완료 시 호롱불에 불이 밝혀집니다.
/// Light 컴포넌트의 intensity를 점진적으로 올리고,
/// 선택적으로 파티클(불꽃)을 활성화합니다.
/// </summary>
public class LanternInteractable : RingInteractable
{
    [Header("호롱불 설정")]
    [Tooltip("밝힐 Light 컴포넌트")]
    [SerializeField] private Light lanternLight;

    [Tooltip("최종 밝기")]
    [SerializeField] private float targetIntensity = 3f;

    [Tooltip("밝아지는 시간 (초)")]
    [SerializeField] private float lightUpDuration = 1.0f;

    [Tooltip("불꽃 파티클 (선택)")]
    [SerializeField] private ParticleSystem fireParticle;

    [Tooltip("불꽃 이미시브 머티리얼 (선택)")]
    [SerializeField] private Renderer emissiveRenderer;

    [Tooltip("이미시브 색상")]
    [SerializeField] private Color emissiveColor = new Color(1f, 0.6f, 0.1f) * 3f;

    protected override void Awake()
    {
        base.Awake();

        // 초기 상태: 불 꺼짐
        if (lanternLight != null)
            lanternLight.intensity = 0f;
    }

    public override void Execute()
    {
        base.Execute();
        StartCoroutine(LightUpCoroutine());
    }

    private IEnumerator LightUpCoroutine()
    {
        // 파티클 시작
        if (fireParticle != null)
            fireParticle.Play();

        // Light 점진적 밝히기
        float elapsed = 0f;
        while (elapsed < lightUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lightUpDuration;
            float smoothT = t * t * (3f - 2f * t); // SmoothStep

            if (lanternLight != null)
                lanternLight.intensity = Mathf.Lerp(0f, targetIntensity, smoothT);

            // 이미시브 머티리얼
            if (emissiveRenderer != null)
            {
                emissiveRenderer.material.SetColor("_EmissionColor", emissiveColor * smoothT);
                emissiveRenderer.material.EnableKeyword("_EMISSION");
            }

            yield return null;
        }

        if (lanternLight != null)
            lanternLight.intensity = targetIntensity;

        // 인터랙션 완료 알림
        NotifyInteractionDone();
    }
}
