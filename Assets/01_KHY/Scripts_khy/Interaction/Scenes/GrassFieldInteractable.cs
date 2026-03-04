using System.Collections;
using UnityEngine;

/// <summary>
/// Scene 06: 억새풀밭 급성장 인터랙션.
///
/// 링 충전 완료 시 억새풀이 급성장하여 시야를 차단합니다.
/// 기존 GrassGroupManager를 활용하여 성장시킵니다.
///
/// 기존 SilverGrass/GrassGroupManager 시스템을 링 충전과 연결하는 어댑터 역할.
/// </summary>
public class GrassFieldInteractable : RingInteractable
{
    [Header("억새풀 설정")]
    [Tooltip("기존 GrassGroupManager 참조")]
    [SerializeField] private GrassGroupManager grassGroup;

    [Tooltip("성장 완료 후 추가 파티클 (선택)")]
    [SerializeField] private ParticleSystem growthCompleteParticle;

    [Tooltip("성장 완료까지 대기 시간 (GrassGroupManager의 성장 시간 고려)")]
    [SerializeField] private float completionDelay = 2.0f;

    public override void Execute()
    {
        base.Execute();

        // 기존 GrassGroupManager의 GrowAll 호출
        if (grassGroup != null)
        {
            grassGroup.GrowAll();
        }

        StartCoroutine(WaitForGrowthComplete());
    }

    private IEnumerator WaitForGrowthComplete()
    {
        // GrassGroupManager가 모든 풀을 성장시킬 시간 대기
        yield return new WaitForSeconds(completionDelay);

        if (growthCompleteParticle != null)
            growthCompleteParticle.Play();

        NotifyInteractionDone();
    }
}
