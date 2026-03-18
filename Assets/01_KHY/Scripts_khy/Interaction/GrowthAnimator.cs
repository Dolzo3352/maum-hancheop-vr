using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 코드 기반 성장 애니메이션 컨트롤러.
///
/// 애니메이션 클립 없이 Inspector 설정만으로 오브젝트 성장을 제어합니다.
/// 단일 오브젝트 성장, 그룹 성장(감 열매 등), 여러 단계 순차 실행을 지원합니다.
///
/// ※ 스케일 비율은 씬에 배치된 현재 크기 기준입니다.
///   예: 나무가 (0.5, 0.8, 0.5)로 배치되어 있을 때
///       startScale=0.3 → (0.15, 0.24, 0.15)
///       endScale=1.0   → (0.5, 0.8, 0.5) 원래 크기 그대로
///
/// 사용법:
///   1. 인터랙터블 오브젝트 또는 부모에 부착
///   2. growthStages 배열에 성장 단계 추가
///   3. initializeOnAwake 체크 → 씬 시작 시 startScale 상태로 대기
///   4. RingInteractable의 onExecute UnityEvent에서 Play() 연결
///      → 충전 100% 완료 시 성장 시작
/// </summary>
public class GrowthAnimator : MonoBehaviour
{
    [Serializable]
    public class GrowthStage
    {
        [Header("대상")]
        [Tooltip("성장할 오브젝트의 Transform을 드래그하세요")]
        public Transform target;

        [Tooltip("체크하면 target의 직계 자식들을 각각 시간차로 성장시킵니다\n" +
                 "감 열매처럼 한 부모 아래 여러 오브젝트가 있을 때 사용\n\n" +
                 "해제하면 target 하나만 성장시킵니다")]
        public bool growChildren = false;

        [Tooltip("그룹 모드 시 자식들 간의 랜덤 시간차 범위 (초)\n" +
                 "예: 0.3이면 각 감이 0~0.3초 랜덤 딜레이로 하나씩 톡톡 등장\n" +
                 "0이면 모두 동시에 성장")]
        [Range(0f, 2f)]
        public float childStaggerRange = 0.3f;

        [Header("스케일 (씬 배치 크기 기준 비율)")]
        [Tooltip("성장 시작 크기. 0 = 안 보임, 0.3 = 30% 크기, 1 = 현재 배치 크기")]
        [Range(0f, 3f)]
        public float startScale = 0f;

        [Tooltip("성장 목표 크기. 1 = 현재 배치 크기, 1.5 = 150% 크기")]
        [Range(0f, 3f)]
        public float endScale = 1f;

        [Header("타이밍")]
        [Tooltip("이 단계의 성장 시간 (초)")]
        public float duration = 1.5f;

        [Tooltip("Play() 호출 후 이 단계가 시작되기까지 대기 시간 (초)\n" +
                 "예: 나무 줄기 = 0초, 잎 = 0.5초, 열매 = 1.5초")]
        public float startDelay = 0f;

        [Header("커브")]
        [Tooltip("성장 속도 커브\n" +
                 "- EaseOut: 처음 빠르고 끝 느림 (자연스러운 성장)\n" +
                 "- EaseIn: 처음 느리고 끝 빠름 (가속 성장)\n" +
                 "- Linear: 일정 속도")]
        public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("모핑 (Squash & Stretch)")]
        [Tooltip("체크하면 성장 중 찌그러짐 효과가 추가됩니다\n" +
                 "위로 자랄 때 옆으로 살짝 줄어들었다 복원되는 유기적 느낌")]
        public bool enableMorph = false;

        [Tooltip("모핑 강도. 0.1 = 미세, 0.3 = 뚜렷\n" +
                 "Y축은 이 값만큼 더 늘어나고 XZ축은 줄어듦")]
        [Range(0f, 0.5f)]
        public float morphIntensity = 0.15f;

        [Tooltip("모핑이 최대가 되는 시점 (0~1)\n" +
                 "0.5 = 성장 중간에 가장 찌그러짐, 시작과 끝은 정상 비율")]
        [Range(0f, 1f)]
        public float morphPeak = 0.4f;

        [Header("활성화")]
        [Tooltip("체크하면 성장 시작 시 비활성 오브젝트를 자동 활성화합니다\n" +
                 "감 열매처럼 처음엔 숨겨뒀다 등장시킬 때 사용")]
        public bool activateOnStart = true;

        // 런타임에 저장되는 원래 스케일 (단일 모드)
        [HideInInspector] public Vector3 originalScale;

        // 런타임에 저장되는 자식들의 원래 스케일 (그룹 모드)
        [HideInInspector] public Vector3[] childOriginalScales;
    }

    [Header("성장 단계")]
    [Tooltip("순서대로 실행될 성장 단계들\n" +
             "배열 순서 = Inspector 정리 순서일 뿐,\n" +
             "실제 실행 순서는 각 단계의 startDelay로 결정됩니다")]
    [SerializeField] private GrowthStage[] growthStages;

    [Header("초기화 설정")]
    [Tooltip("체크하면 씬 시작(Awake) 시 모든 대상을 startScale 크기로 세팅합니다\n" +
             "→ 씬 시작부터 작은 상태로 대기, Play() 호출 시 자연스럽게 성장\n\n" +
             "해제하면 씬 배치 크기 그대로 유지, Play() 시 현재 크기에서 시작")]
    [SerializeField] private bool initializeOnAwake = true;

    /// <summary>전체 성장 시퀀스 완료 시 호출</summary>
    public event Action OnGrowthComplete;

    private Coroutine activeCoroutine;

    private void Awake()
    {
        if (growthStages == null) return;

        // 각 단계의 원래 스케일 저장
        foreach (var stage in growthStages)
        {
            if (stage.target == null) continue;

            if (stage.growChildren)
            {
                // 그룹 모드: 직계 자식들의 스케일 저장
                int childCount = stage.target.childCount;
                stage.childOriginalScales = new Vector3[childCount];
                for (int i = 0; i < childCount; i++)
                    stage.childOriginalScales[i] = stage.target.GetChild(i).localScale;
            }
            else
            {
                // 단일 모드
                stage.originalScale = stage.target.localScale;
            }
        }

        if (initializeOnAwake)
            Initialize();
    }

    /// <summary>
    /// 모든 대상을 시작 스케일로 초기화합니다.
    /// </summary>
    public void Initialize()
    {
        if (growthStages == null) return;
        foreach (var stage in growthStages)
        {
            if (stage.target == null) continue;

            if (stage.growChildren)
            {
                for (int i = 0; i < stage.target.childCount; i++)
                {
                    var child = stage.target.GetChild(i);
                    child.localScale = stage.childOriginalScales[i] * stage.startScale;

                    if (stage.startScale <= 0f && stage.activateOnStart)
                        child.gameObject.SetActive(false);
                }
            }
            else
            {
                stage.target.localScale = stage.originalScale * stage.startScale;

                if (stage.startScale <= 0f && stage.activateOnStart)
                    stage.target.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 전체 성장 시퀀스를 재생합니다.
    /// </summary>
    [ContextMenu("테스트: 성장 재생")]
    public void Play()
    {
        if (activeCoroutine != null)
            StopCoroutine(activeCoroutine);

        activeCoroutine = StartCoroutine(PlaySequence());
    }

    /// <summary>
    /// 특정 단계만 재생합니다.
    /// </summary>
    public void PlayStage(int stageIndex)
    {
        if (growthStages == null || stageIndex < 0 || stageIndex >= growthStages.Length) return;
        var stage = growthStages[stageIndex];

        if (stage.growChildren)
            StartCoroutine(AnimateGroup(stage));
        else
            StartCoroutine(AnimateSingle(stage, stage.target, stage.originalScale));
    }

    private IEnumerator PlaySequence()
    {
        if (growthStages == null || growthStages.Length == 0) yield break;

        int completedCount = 0;
        int totalCount = growthStages.Length;

        for (int i = 0; i < growthStages.Length; i++)
        {
            var stage = growthStages[i];
            StartCoroutine(AnimateStageWithCallback(stage, () => completedCount++));
        }

        while (completedCount < totalCount)
            yield return null;

        OnGrowthComplete?.Invoke();
        activeCoroutine = null;
    }

    private IEnumerator AnimateStageWithCallback(GrowthStage stage, Action onComplete)
    {
        if (stage.growChildren)
            yield return AnimateGroup(stage);
        else
            yield return AnimateSingle(stage, stage.target, stage.originalScale);

        onComplete?.Invoke();
    }

    // ─── 그룹 성장 (자식들 시간차 성장) ───

    private IEnumerator AnimateGroup(GrowthStage stage)
    {
        if (stage.target == null) yield break;

        // 그룹 딜레이
        if (stage.startDelay > 0f)
            yield return new WaitForSeconds(stage.startDelay);

        int childCount = stage.target.childCount;
        if (childCount == 0) yield break;

        // 모든 자식을 랜덤 시간차로 동시 시작
        int completedCount = 0;

        for (int i = 0; i < childCount; i++)
        {
            var child = stage.target.GetChild(i);
            var originalScale = stage.childOriginalScales[i];
            float randomDelay = UnityEngine.Random.Range(0f, stage.childStaggerRange);

            StartCoroutine(AnimateChildWithDelay(stage, child, originalScale, randomDelay, () => completedCount++));
        }

        // 모든 자식 완료 대기
        while (completedCount < childCount)
            yield return null;
    }

    private IEnumerator AnimateChildWithDelay(GrowthStage stage, Transform child, Vector3 originalScale, float delay, Action onComplete)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        yield return AnimateSingle(stage, child, originalScale);
        onComplete?.Invoke();
    }

    // ─── 단일 오브젝트 성장 ───

    private IEnumerator AnimateSingle(GrowthStage stage, Transform target, Vector3 originalScale)
    {
        if (target == null) yield break;

        // 딜레이 (그룹 모드가 아닐 때만, 그룹은 AnimateGroup에서 처리)
        if (!stage.growChildren && stage.startDelay > 0f)
            yield return new WaitForSeconds(stage.startDelay);

        // 오브젝트 활성화
        if (stage.activateOnStart && !target.gameObject.activeSelf)
            target.gameObject.SetActive(true);

        Vector3 startScale = originalScale * stage.startScale;
        Vector3 endScale = originalScale * stage.endScale;

        float elapsed = 0f;
        while (elapsed < stage.duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stage.duration);
            float curveValue = stage.curve.Evaluate(t);

            Vector3 scale = Vector3.LerpUnclamped(startScale, endScale, curveValue);

            // 모핑 (Squash & Stretch)
            if (stage.enableMorph && stage.morphIntensity > 0f)
            {
                float morphT;
                if (t <= stage.morphPeak)
                    morphT = t / stage.morphPeak;
                else
                    morphT = 1f - (t - stage.morphPeak) / (1f - stage.morphPeak);

                float morphAmount = Mathf.Sin(morphT * Mathf.PI * 0.5f) * stage.morphIntensity;

                scale.y *= (1f + morphAmount);
                scale.x *= (1f - morphAmount * 0.5f);
                scale.z *= (1f - morphAmount * 0.5f);
            }

            target.localScale = scale;
            yield return null;
        }

        // 최종 스케일 정확히
        target.localScale = endScale;
    }

    /// <summary>
    /// 원래 스케일(endScale)로 즉시 복원합니다.
    /// </summary>
    public void ResetToOriginal()
    {
        if (growthStages == null) return;
        if (activeCoroutine != null)
        {
            StopCoroutine(activeCoroutine);
            activeCoroutine = null;
        }

        foreach (var stage in growthStages)
        {
            if (stage.target == null) continue;

            if (stage.growChildren)
            {
                for (int i = 0; i < stage.target.childCount; i++)
                {
                    var child = stage.target.GetChild(i);
                    child.localScale = stage.childOriginalScales[i] * stage.endScale;
                }
            }
            else
            {
                stage.target.localScale = stage.originalScale * stage.endScale;
            }
        }
    }
}
