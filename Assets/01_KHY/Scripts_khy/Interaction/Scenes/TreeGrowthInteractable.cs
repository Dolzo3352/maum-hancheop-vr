using System.Collections;
using UnityEngine;

/// <summary>
/// Scene 04: 감나무 급성장 인터랙션.
///
/// 링 충전 완료 시 앙상한 감나무가 급성장하여 홍시가 열립니다.
/// BlendShape(Shape Key) 또는 Scale 기반 성장 애니메이션을 사용합니다.
///
/// 충전 중(0~100%): 나무가 살짝 반응 (미세한 흔들림)
/// 충전 완료: 급성장 + 열매 맺기 + 빛가루 VFX
/// </summary>
public class TreeGrowthInteractable : RingInteractable
{
    [Header("성장 설정")]
    [Tooltip("나무 SkinnedMeshRenderer (BlendShape용)")]
    [SerializeField] private SkinnedMeshRenderer treeMesh;

    [Tooltip("성장 BlendShape 인덱스")]
    [SerializeField] private int growthBlendShapeIndex = 0;

    [Tooltip("성장에 걸리는 시간 (초)")]
    [SerializeField] private float growthDuration = 2.0f;

    [Tooltip("성장 애니메이션 커브")]
    [SerializeField] private AnimationCurve growthCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("열매")]
    [Tooltip("성장 완료 후 활성화할 열매 오브젝트들")]
    [SerializeField] private GameObject[] fruits;

    [Tooltip("열매가 나타나기 시작하는 성장 비율 (0~1)")]
    [SerializeField] private float fruitAppearThreshold = 0.7f;

    [Header("VFX")]
    [Tooltip("성장 중 빛가루 파티클")]
    [SerializeField] private ParticleSystem growthParticle;

    [Tooltip("완료 시 화려한 파티클")]
    [SerializeField] private ParticleSystem completeParticle;

    [Header("대체: Scale 기반 성장 (BlendShape 없을 때)")]
    [Tooltip("BlendShape 대신 Scale로 성장할 오브젝트")]
    [SerializeField] private Transform scaleTarget;

    [Tooltip("시작 스케일")]
    [SerializeField] private Vector3 startScale = Vector3.one * 0.3f;

    [Tooltip("최종 스케일")]
    [SerializeField] private Vector3 endScale = Vector3.one;

    protected override void Awake()
    {
        base.Awake();

        // 열매 초기 숨김
        if (fruits != null)
        {
            foreach (var fruit in fruits)
            {
                if (fruit != null) fruit.SetActive(false);
            }
        }

        // Scale 기반 초기화
        if (scaleTarget != null && treeMesh == null)
            scaleTarget.localScale = startScale;
    }

    /// <summary>
    /// 충전 중 미세한 반응 (나뭇잎 흔들림 등).
    /// </summary>
    public override void OnChargeUpdate(float progress)
    {
        base.OnChargeUpdate(progress);

        // 충전 중 미세한 스케일 변화 (숨쉬는 느낌)
        if (scaleTarget != null && treeMesh == null)
        {
            float breathe = 1f + Mathf.Sin(Time.time * 5f) * 0.02f * progress;
            scaleTarget.localScale = startScale * breathe;
        }
    }

    public override void Execute()
    {
        base.Execute();
        StartCoroutine(GrowTreeCoroutine());
    }

    private IEnumerator GrowTreeCoroutine()
    {
        // 성장 파티클 시작
        if (growthParticle != null)
            growthParticle.Play();

        float elapsed = 0f;

        while (elapsed < growthDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / growthDuration);
            float curvedT = growthCurve.Evaluate(t);

            // BlendShape 기반 성장
            if (treeMesh != null)
            {
                treeMesh.SetBlendShapeWeight(growthBlendShapeIndex, curvedT * 100f);
            }
            // Scale 기반 성장
            else if (scaleTarget != null)
            {
                scaleTarget.localScale = Vector3.Lerp(startScale, endScale, curvedT);
            }

            // 열매 표시 (성장 70% 이후)
            if (curvedT >= fruitAppearThreshold && fruits != null)
            {
                foreach (var fruit in fruits)
                {
                    if (fruit != null && !fruit.activeSelf)
                        fruit.SetActive(true);
                }
            }

            yield return null;
        }

        // 성장 파티클 정지
        if (growthParticle != null)
            growthParticle.Stop();

        // 완료 파티클
        if (completeParticle != null)
            completeParticle.Play();

        // 완료 알림
        NotifyInteractionDone();
    }
}
