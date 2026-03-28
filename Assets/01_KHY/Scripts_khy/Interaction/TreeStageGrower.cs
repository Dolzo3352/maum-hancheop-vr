using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 나무 3단계 성장 컨트롤러.
///
/// 3개의 서로 다른 나무 모델을 순차적으로 교체하며 성장 연출합니다.
/// 각 인터랙션 완료 시 GrowToNextStage()를 호출하면
/// 현재 단계 축소 → 다음 단계 활성화 → 성장 애니메이션이 재생됩니다.
///
/// 사용법:
///   1. 같은 위치에 3개 나무 모델 배치
///   2. stages 배열에 순서대로 등록
///   3. RingInteractable의 onExecute에서 GrowToNextStage() 연결
///
/// Hierarchy:
///   [나무_성장] ← 이 컴포넌트
///     ├── Tree_Small   (stages[0])
///     ├── Tree_Medium  (stages[1])
///     └── Tree_Large   (stages[2])
/// </summary>
public class TreeStageGrower : MonoBehaviour
{
    [Serializable]
    public class TreeStage
    {
        [Tooltip("이 단계의 나무 모델")]
        public GameObject model;

        [Tooltip("성장 시간 (초)")]
        public float growDuration = 1.5f;

        [Tooltip("성장 커브")]
        public AnimationCurve growCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("모핑 (Squash & Stretch) 활성화")]
        public bool enableMorph = true;

        [Tooltip("모핑 강도")]
        [Range(0f, 0.5f)]
        public float morphIntensity = 0.15f;

        [Tooltip("크로스페이드 시간 (이전 축소 + 다음 성장이 겹치는 구간)")]
        public float crossfadeDuration = 0.5f;

        [Tooltip("성장 시 재생할 파티클 (Play On Awake 해제)")]
        public ParticleSystem growthParticle;

        [Header("성장 후 축소")]
        [Tooltip("성장 완료 후 서서히 작아지는 애니메이션 사용 여부")]
        public bool shrinkAfterGrow = false;

        [Tooltip("성장 완료 후 축소 시작까지 대기 시간 (초)")]
        public float shrinkDelay = 1.0f;

        [Tooltip("목표 크기 비율 (originalScale 기준). 예: 4/7 ≈ 0.571")]
        [Range(0.01f, 1f)]
        public float shrinkTargetMultiplier = 0.57f;

        [Tooltip("축소 애니메이션 시간 (초)")]
        public float shrinkDuration = 1.5f;

        [Tooltip("축소 커브")]
        public AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // 런타임
        [HideInInspector] public Vector3 originalScale;
    }

    [Header("성장 단계")]
    [Tooltip("순서대로: 작은 나무 → 중간 나무 → 큰 나무")]
    [SerializeField] private TreeStage[] stages;

    [Header("설정")]
    [Tooltip("시작 시 첫 번째 단계를 자동 표시")]
    [SerializeField] private bool showFirstOnAwake = true;

    [Header("디버그")]
    [SerializeField] private bool debugLog = true;

    private int currentStage = -1;
    private bool isTransitioning;

    /// <summary>현재 단계 인덱스 (-1이면 아직 시작 안 함)</summary>
    public int CurrentStage => currentStage;

    /// <summary>모든 단계 완료 여부</summary>
    public bool AllStagesComplete => currentStage >= stages.Length - 1;

    /// <summary>단계 전환 완료 시 호출 (새 단계 인덱스)</summary>
    public event Action<int> OnStageComplete;

    private void Awake()
    {
        if (stages == null || stages.Length == 0) return;

        // 원래 스케일 저장 + 전부 비활성화
        foreach (var stage in stages)
        {
            if (stage.model == null) continue;
            stage.originalScale = stage.model.transform.localScale;
            stage.model.transform.localScale = Vector3.zero;
            stage.model.SetActive(false);
        }

        // 첫 번째 단계 표시
        if (showFirstOnAwake)
        {
            currentStage = 0;
            var first = stages[0];
            first.model.SetActive(true);
            first.model.transform.localScale = first.originalScale;
        }
    }

    /// <summary>
    /// 다음 단계로 성장합니다.
    /// RingInteractable의 onExecute UnityEvent에서 연결하세요.
    /// </summary>
    [ContextMenu("테스트: 다음 단계 성장")]
    public void GrowToNextStage()
    {
        if (isTransitioning) return;
        if (stages == null || stages.Length == 0) return;

        int nextStage = currentStage + 1;
        if (nextStage >= stages.Length)
        {
            Log("이미 최종 단계입니다.");
            return;
        }

        StartCoroutine(TransitionToStage(nextStage));
    }

    /// <summary>
    /// 특정 단계로 즉시 이동 (디버그용).
    /// </summary>
    public void SetStageImmediate(int index)
    {
        if (stages == null || index < 0 || index >= stages.Length) return;

        // 전부 숨기기
        foreach (var stage in stages)
        {
            if (stage.model == null) continue;
            stage.model.SetActive(false);
            stage.model.transform.localScale = Vector3.zero;
        }

        // 해당 단계만 표시
        var target = stages[index];
        target.model.SetActive(true);
        target.model.transform.localScale = target.originalScale;
        currentStage = index;
    }

    private IEnumerator TransitionToStage(int nextIndex)
    {
        isTransitioning = true;

        var current = currentStage >= 0 ? stages[currentStage] : null;
        var next = stages[nextIndex];

        // next.model null 체크 — current를 끄기 전에 확인
        if (next.model == null)
        {
            Debug.LogError($"[TreeStageGrower] stages[{nextIndex}].model이 null입니다. Inspector에서 모델을 할당해주세요.", this);
            isTransitioning = false;
            yield break;
        }

        Log($"성장: Stage {currentStage} → Stage {nextIndex}");

        // 파티클 재생
        if (next.growthParticle != null)
        {
            next.growthParticle.transform.position = next.model.transform.position;
            next.growthParticle.Play();
        }

        // 이전 단계 크기에서 시작 (바톤 터치)
        Vector3 currentOrigScale = current != null ? current.originalScale : Vector3.zero;
        Vector3 nextOrigScale = next.originalScale;
        Vector3 nextStartScale = current != null ? currentOrigScale : Vector3.zero;

        // 이전 단계 즉시 제거
        if (current != null && current.model != null)
        {
            current.model.transform.localScale = Vector3.zero;
            current.model.SetActive(false);
        }

        // 다음 단계 활성화 (이전 크기에서 시작)
        next.model.SetActive(true);
        next.model.transform.localScale = nextStartScale;

        // ── 성장 ──
        float crossfadeTime = next.crossfadeDuration;
        float growTime = next.growDuration;
        float totalTime = growTime;
        float elapsed = 0f;

        while (elapsed < totalTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / totalTime);
            float curveValue = next.growCurve.Evaluate(t);

            // ── 다음 단계: 이전 크기 → 목표 크기로 성장 ──
            Vector3 nextScale = Vector3.Lerp(nextStartScale, nextOrigScale, curveValue);

            if (next.enableMorph && next.morphIntensity > 0f)
            {
                float morphT = Mathf.Sin(t * Mathf.PI);
                float morphAmount = morphT * next.morphIntensity;
                nextScale.y *= (1f + morphAmount);
                nextScale.x *= (1f - morphAmount * 0.5f);
                nextScale.z *= (1f - morphAmount * 0.5f);
            }

            next.model.transform.localScale = nextScale;

            // ── 현재 단계: 크로스페이드 구간 동안 축소 ──
            if (current != null && current.model != null && crossfadeTime > 0f)
            {
                float shrinkT = Mathf.Clamp01(elapsed / crossfadeTime);
                // SmoothStep으로 자연스럽게
                shrinkT = shrinkT * shrinkT * (3f - 2f * shrinkT);
                current.model.transform.localScale = Vector3.Lerp(currentOrigScale, Vector3.zero, shrinkT);
            }

            yield return null;
        }

        // 최종 정리
        next.model.transform.localScale = nextOrigScale;

        if (current != null && current.model != null)
        {
            current.model.SetActive(false);
            current.model.transform.localScale = Vector3.zero;
        }

        currentStage = nextIndex;
        isTransitioning = false;

        Log($"Stage {nextIndex} 성장 완료");
        OnStageComplete?.Invoke(nextIndex);

        // 성장 후 축소
        if (next.shrinkAfterGrow)
            StartCoroutine(ShrinkAfterGrow(next));
    }

    private IEnumerator ShrinkAfterGrow(TreeStage stage)
    {
        yield return new WaitForSeconds(stage.shrinkDelay);

        Vector3 fromScale = stage.model.transform.localScale;
        Vector3 toScale   = stage.originalScale * stage.shrinkTargetMultiplier;

        float elapsed = 0f;
        while (elapsed < stage.shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stage.shrinkDuration);
            stage.model.transform.localScale = Vector3.Lerp(fromScale, toScale, stage.shrinkCurve.Evaluate(t));
            yield return null;
        }

        stage.model.transform.localScale = toScale;
        Log($"축소 완료 → {toScale}");
    }

    private void Log(string message)
    {
        if (debugLog) Debug.Log($"[TreeStageGrower] {message}", this);
    }
}
