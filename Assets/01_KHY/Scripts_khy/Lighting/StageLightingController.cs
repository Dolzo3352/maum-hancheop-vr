using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 스테이지별 라이팅 + Volume을 제어합니다.
///
/// 각 Stage GameObject의 자식에 있는 Light와 Volume 컴포넌트를 찾아
/// intensity/weight를 조절하여 크로스페이드 효과를 만듭니다.
///
/// The Line VR 스타일:
///   글로벌 앰비언트 = 거의 검정
///   + 각 스테이지 로컬 Spot/Area Light + Volume
///   → 라이트 intensity + Volume weight 동시 조절로 자연스러운 전환
///
/// 사용법:
///   NarrativeManager 오브젝트에 부착하고, DioramaStageManager를 연결합니다.
/// </summary>
public class StageLightingController : MonoBehaviour
{
    // ─── 필드 ───

    [Header("참조")]
    [SerializeField] private DioramaStageManager stageManager;

    [Header("설정")]
    [Tooltip("라이팅 크로스페이드 기본 시간 (초)")]
    [SerializeField] private float defaultCrossfadeDuration = 1.5f;

    // 스테이지별 캐시
    private Dictionary<int, List<LightData>> stageLightCache = new Dictionary<int, List<LightData>>();
    private Dictionary<int, List<Volume>> stageVolumeCache = new Dictionary<int, List<Volume>>();

    private struct LightData
    {
        public Light light;
        public float originalIntensity;
    }

    // ─── 공개 메서드 ───

    /// <summary>
    /// 해당 스테이지의 모든 라이트와 Volume을 탐색하고 캐시합니다.
    /// 스테이지가 활성화된 상태에서 호출해야 합니다.
    /// </summary>
    public void CacheStageComponents(int stageIndex)
    {
        var stageObj = stageManager.GetStage(stageIndex);
        if (stageObj == null) return;

        // 라이트 캐시
        var lights = stageObj.GetComponentsInChildren<Light>(true);
        var lightDataList = new List<LightData>();
        foreach (var light in lights)
        {
            lightDataList.Add(new LightData
            {
                light = light,
                originalIntensity = light.intensity
            });
        }
        stageLightCache[stageIndex] = lightDataList;

        // Volume 캐시
        var volumes = stageObj.GetComponentsInChildren<Volume>(true);
        stageVolumeCache[stageIndex] = new List<Volume>(volumes);

        Debug.Log($"[StageLightingController] Stage {stageIndex} 캐시: Light {lights.Length}개, Volume {volumes.Length}개", this);
    }

    /// <summary>
    /// 하위호환용. CacheStageComponents를 호출합니다.
    /// </summary>
    public void CacheStageLights(int stageIndex)
    {
        CacheStageComponents(stageIndex);
    }

    /// <summary>
    /// 스테이지의 라이트 intensity + Volume weight를 동시에 설정합니다.
    /// multiplier 0 = 꺼짐, 1 = 원본 밝기/풀 적용.
    /// </summary>
    public void SetStageIntensity(int stageIndex, float multiplier)
    {
        // 라이트
        if (stageLightCache.ContainsKey(stageIndex))
        {
            foreach (var data in stageLightCache[stageIndex])
            {
                if (data.light != null)
                    data.light.intensity = data.originalIntensity * multiplier;
            }
        }

        // Volume weight
        if (stageVolumeCache.ContainsKey(stageIndex))
        {
            foreach (var vol in stageVolumeCache[stageIndex])
            {
                if (vol != null)
                    vol.weight = multiplier;
            }
        }
    }

    /// <summary>
    /// 두 스테이지 간 라이팅 + Volume 크로스페이드.
    /// fromStage는 밝음→어둠, toStage는 어둠→밝음으로 전환됩니다.
    /// </summary>
    public IEnumerator CrossfadeLighting(int fromIndex, int toIndex, float duration = -1f)
    {
        if (duration < 0f) duration = defaultCrossfadeDuration;

        // 캐시 확인 (없으면 새로 캐시)
        if (!stageLightCache.ContainsKey(fromIndex) || !stageVolumeCache.ContainsKey(fromIndex))
            CacheStageComponents(fromIndex);
        if (!stageLightCache.ContainsKey(toIndex) || !stageVolumeCache.ContainsKey(toIndex))
            CacheStageComponents(toIndex);

        // 시작: from=1(밝음), to=0(어둠)
        SetStageIntensity(toIndex, 0f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 부드러운 전환을 위해 SmoothStep 사용
            float smooth = Mathf.SmoothStep(0f, 1f, t);

            SetStageIntensity(fromIndex, 1f - smooth);
            SetStageIntensity(toIndex, smooth);

            yield return null;
        }

        // 최종 상태 보장
        SetStageIntensity(fromIndex, 0f);
        SetStageIntensity(toIndex, 1f);

        Debug.Log($"[StageLightingController] 크로스페이드 완료: Stage {fromIndex} → {toIndex}", this);
    }

    /// <summary>
    /// 스테이지 라이트 + Volume을 즉시 끕니다 (크로스페이드 시작 전 준비용).
    /// </summary>
    public void TurnOffStageLights(int stageIndex)
    {
        CacheStageComponents(stageIndex);
        SetStageIntensity(stageIndex, 0f);
    }

    /// <summary>
    /// 스테이지 라이트 + Volume을 원본으로 복원합니다.
    /// </summary>
    public void RestoreStageLights(int stageIndex)
    {
        SetStageIntensity(stageIndex, 1f);
    }
}
