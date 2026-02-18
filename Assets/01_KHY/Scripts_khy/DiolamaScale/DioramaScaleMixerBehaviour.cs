using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 클립들의 절대 스케일 값을 블렌딩하여 Transform에 적용합니다.
/// 
/// 원본 스케일은 DioramaScaleOrigin 컴포넌트에서 가져옵니다.
/// 어떤 타임라인이 몇 번 재생되든 원본 기준은 항상 동일합니다.
/// 
///   원본 = (1,1,1), 클립 from:1.0 to:0.7
///   → localScale = (1,1,1) * 0.7 = (0.7, 0.7, 0.7)  ✓
///   
///   다음 컷, 클립 from:0.7 to:1.5
///   → localScale = (1,1,1) * 1.5 = (1.5, 1.5, 1.5)  ✓  (0.7 * 1.5가 아님!)
/// </summary>
public class DioramaScaleMixerBehaviour : PlayableBehaviour
{
    private Transform _trackBinding;
    private Vector3 _baseScale;
    private bool _initialized;

    // 시차 효과용 캐시
    private Transform[] _children;
    private Vector3[] _childBaseScales;
    private float[] _staggerOffsets;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        _trackBinding = playerData as Transform;
        if (_trackBinding == null) return;

        if (!_initialized)
        {
            Initialize();
        }

        int inputCount = playable.GetInputCount();
        float blendedScale = 0f;
        float totalWeight = 0f;

        bool anyStagger = false;
        DioramaScaleBehaviour activeStaggerBehaviour = null;
        float activeStaggerProgress = 0f;

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var inputPlayable = (ScriptPlayable<DioramaScaleBehaviour>)playable.GetInput(i);
            var behaviour = inputPlayable.GetBehaviour();

            double clipElapsed = inputPlayable.GetTime();
            float transitionT = behaviour.GetTransitionProgress(clipElapsed);

            float easedT = (transitionT < 1f)
                ? ApplyEasing(behaviour, transitionT)
                : 1f;

            // fromScale → targetScale 절대값 보간
            float currentScale = Mathf.LerpUnclamped(
                behaviour.fromScale,
                behaviour.targetScale,
                easedT);

            blendedScale += currentScale * weight;
            totalWeight += weight;

            if (behaviour.enableStagger)
            {
                anyStagger = true;
                activeStaggerBehaviour = behaviour;
                activeStaggerProgress = transitionT;
            }
        }

        if (totalWeight <= 0f) return;

        if (totalWeight < 1f)
        {
            float currentRatio = _trackBinding.localScale.x / _baseScale.x;
            blendedScale += currentRatio * (1f - totalWeight);
        }

        // ── 최종 적용: 항상 원본 * 절대 배율 ──
        if (anyStagger && activeStaggerBehaviour != null
            && _children != null && _children.Length > 0)
        {
            ApplyStaggerToChildren(activeStaggerBehaviour, activeStaggerProgress);
        }
        else
        {
            _trackBinding.localScale = _baseScale * blendedScale;
        }
    }

    /// <summary>
    /// DioramaScaleOrigin에서 원본 스케일을 가져옵니다.
    /// 컴포넌트가 없으면 자동으로 추가합니다.
    /// </summary>
    private void Initialize()
    {
        var origin = _trackBinding.GetComponent<DioramaScaleOrigin>();
        if (origin == null)
        {
            origin = _trackBinding.gameObject.AddComponent<DioramaScaleOrigin>();
            origin.Capture();
        }

        _baseScale = origin.originalScale;
        CacheChildren();
        _initialized = true;
    }

    // ─────────────────────────────────────────────
    //  이징
    // ─────────────────────────────────────────────

    private float ApplyEasing(DioramaScaleBehaviour b, float t)
    {
        switch (b.easeType)
        {
            case DioramaScaleBehaviour.EaseType.Linear:
                return t;
            case DioramaScaleBehaviour.EaseType.EaseOut:
                return 1f - Mathf.Pow(1f - t, 3f);
            case DioramaScaleBehaviour.EaseType.EaseInOut:
                return t < 0.5f
                    ? 4f * t * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
            case DioramaScaleBehaviour.EaseType.ExaggeratedBounce:
                return ExaggeratedBounce(t, b.anticipationStrength, b.overshootStrength);
            case DioramaScaleBehaviour.EaseType.Elastic:
                return ElasticOut(t);
            default: return t;
        }
    }

    private float ExaggeratedBounce(float t, float anti, float over)
    {
        if (t < 0.12f)
        {
            float p = t / 0.12f;
            return -Mathf.Sin(p * Mathf.PI) * anti;
        }
        else if (t < 0.75f)
        {
            float p = (t - 0.12f) / 0.63f;
            return Mathf.Lerp(0f, 1f + over, 1f - Mathf.Pow(1f - p, 3f));
        }
        else if (t < 0.88f)
        {
            float p = (t - 0.75f) / 0.13f;
            return Mathf.Lerp(1f + over, 1f - over * 0.3f, Mathf.Sin(p * Mathf.PI));
        }
        else
        {
            float p = (t - 0.88f) / 0.12f;
            return Mathf.Lerp(1f - over * 0.3f, 1f, p * p * (3f - 2f * p));
        }
    }

    private float ElasticOut(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        return Mathf.Pow(2f, -10f * t)
             * Mathf.Sin((t - 0.1f) * (2f * Mathf.PI) / 0.4f) + 1f;
    }

    // ─────────────────────────────────────────────
    //  시차(Stagger)
    // ─────────────────────────────────────────────

    private void CacheChildren()
    {
        if (_trackBinding == null) return;
        int count = _trackBinding.childCount;
        _children = new Transform[count];
        _childBaseScales = new Vector3[count];

        for (int i = 0; i < count; i++)
        {
            _children[i] = _trackBinding.GetChild(i);

            // 자식도 DioramaScaleOrigin에서 원본 가져오기
            var childOrigin = _children[i].GetComponent<DioramaScaleOrigin>();
            if (childOrigin == null)
            {
                childOrigin = _children[i].gameObject.AddComponent<DioramaScaleOrigin>();
                childOrigin.Capture();
            }
            _childBaseScales[i] = childOrigin.originalScale;
        }
    }

    private void ApplyStaggerToChildren(
        DioramaScaleBehaviour behaviour, float overallProgress)
    {
        if (_children == null) return;

        if (_staggerOffsets == null || _staggerOffsets.Length != _children.Length)
            _staggerOffsets = BuildOffsets(_children.Length, behaviour.staggerDirection);

        float totalRange = behaviour.staggerDelay * Mathf.Max(1, _children.Length - 1);
        float effectiveTime = behaviour.transitionIn + totalRange;

        for (int i = 0; i < _children.Length; i++)
        {
            if (_children[i] == null) continue;

            float childT;
            if (behaviour.transitionIn <= 0f)
            {
                childT = 1f;
            }
            else
            {
                float delay = _staggerOffsets[i] * totalRange;
                float elapsed = overallProgress * effectiveTime - delay;
                childT = Mathf.Clamp01(elapsed / behaviour.transitionIn);
            }

            float easedT = (childT < 1f) ? ApplyEasing(behaviour, childT) : 1f;
            float s = Mathf.LerpUnclamped(behaviour.fromScale, behaviour.targetScale, easedT);
            _children[i].localScale = _childBaseScales[i] * s;
        }
    }

    private float[] BuildOffsets(int count, DioramaScaleBehaviour.StaggerDirection dir)
    {
        float[] o = new float[count];
        float max = Mathf.Max(1, count - 1);
        switch (dir)
        {
            case DioramaScaleBehaviour.StaggerDirection.FirstToLast:
                for (int i = 0; i < count; i++) o[i] = i / max;
                break;
            case DioramaScaleBehaviour.StaggerDirection.LastToFirst:
                for (int i = 0; i < count; i++) o[i] = (count - 1 - i) / max;
                break;
            case DioramaScaleBehaviour.StaggerDirection.CenterOut:
                float c = (count - 1) / 2f;
                for (int i = 0; i < count; i++) o[i] = Mathf.Abs(i - c) / Mathf.Max(1f, c);
                break;
            case DioramaScaleBehaviour.StaggerDirection.Random:
                for (int i = 0; i < count; i++) o[i] = Random.value;
                break;
        }
        return o;
    }

    public override void OnPlayableDestroy(Playable playable) { }
}
