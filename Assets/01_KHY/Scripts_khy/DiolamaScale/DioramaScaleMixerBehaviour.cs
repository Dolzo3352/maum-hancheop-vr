using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 스케일 클립들을 블렌딩하여 Transform에 적용합니다.
/// 원본 스케일은 DioramaScaleOrigin에서 가져옵니다.
///
///   원본 = (1,1,1)
///   클립: from 1.0, to 0.7  → localScale = (1,1,1) * 0.7
///   다음 클립: from 0.7, to 1.5  → localScale = (1,1,1) * 1.5
///   → 항상 원본 기준, 누적 없음
/// </summary>
public class DioramaScaleMixerBehaviour : PlayableBehaviour
{
    private Transform _target;
    private Vector3 _baseScale;
    private bool _initialized;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        _target = playerData as Transform;
        if (_target == null) return;

        if (!_initialized)
        {
            var origin = _target.GetComponent<DioramaScaleOrigin>();
            if (origin == null)
            {
                origin = _target.gameObject.AddComponent<DioramaScaleOrigin>();
            }

            _baseScale = origin.baseScale;

            // 에디터 프리뷰 오염 리셋 (런타임 체이닝에서는 건드리지 않음)
            if (!Application.isPlaying || !origin.hasBeenUsed)
            {
                _target.localScale = _baseScale;
            }
            origin.hasBeenUsed = true;

            _initialized = true;
        }

        int inputCount = playable.GetInputCount();
        float blendedScale = 0f;
        float totalWeight = 0f;

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var input = (ScriptPlayable<DioramaScaleBehaviour>)playable.GetInput(i);
            var b = input.GetBehaviour();

            float t = b.GetTransitionProgress(input.GetTime());
            float easedT = (t < 1f) ? ApplyEase(b, t) : 1f;

            float scale = Mathf.LerpUnclamped(b.fromScale, b.targetScale, easedT);
            blendedScale += scale * weight;
            totalWeight += weight;
        }

        if (totalWeight <= 0f) return;

        // weight 합이 1 미만이면 현재 비율 유지
        if (totalWeight < 1f)
        {
            float current = (_baseScale.x != 0f)
                ? _target.localScale.x / _baseScale.x
                : 1f;
            blendedScale += current * (1f - totalWeight);
        }

        _target.localScale = _baseScale * blendedScale;
    }

    // 타임라인 끝나도 스케일 유지
    public override void OnPlayableDestroy(Playable playable) { }

    // ─── 이징 ───

    private float ApplyEase(DioramaScaleBehaviour b, float t)
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
                return Bounce(t, b.anticipationStrength, b.overshootStrength);

            case DioramaScaleBehaviour.EaseType.Elastic:
                return Elastic(t);

            default: return t;
        }
    }

    private float Bounce(float t, float anti, float over)
    {
        if (t < 0.12f)
            return -Mathf.Sin(t / 0.12f * Mathf.PI) * anti;
        if (t < 0.75f)
            return Mathf.Lerp(0f, 1f + over, 1f - Mathf.Pow(1f - (t - 0.12f) / 0.63f, 3f));
        if (t < 0.88f)
            return Mathf.Lerp(1f + over, 1f - over * 0.3f, Mathf.Sin((t - 0.75f) / 0.13f * Mathf.PI));

        float p = (t - 0.88f) / 0.12f;
        return Mathf.Lerp(1f - over * 0.3f, 1f, p * p * (3f - 2f * p));
    }

    private float Elastic(float t)
    {
        if (t <= 0f) return 0f;
        if (t >= 1f) return 1f;
        return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - 0.1f) * Mathf.PI * 2f / 0.4f) + 1f;
    }
}
