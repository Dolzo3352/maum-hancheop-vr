using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class DioramaScaleClip : PlayableAsset, ITimelineClipAsset
{
    [SerializeField]
    private DioramaScaleBehaviour _template = new DioramaScaleBehaviour();

    public ClipCaps clipCaps =>
        ClipCaps.Blending |
        ClipCaps.Extrapolation;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<DioramaScaleBehaviour>.Create(graph, _template);
    }

    // Track에서 클립 이름 표시용
    public float FromScale => _template.fromScale;
    public float TargetScale => _template.targetScale;
}
