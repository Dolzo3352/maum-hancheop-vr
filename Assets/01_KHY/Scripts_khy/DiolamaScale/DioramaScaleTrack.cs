using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.98f, 0.55f, 0.15f)]
[TrackBindingType(typeof(Transform))]
[TrackClipType(typeof(DioramaScaleClip))]
public class DioramaScaleTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        // 클립 이름에 "1.0x → 0.7x" 형태로 표시
        foreach (var clip in GetClips())
        {
            var scaleClip = clip.asset as DioramaScaleClip;
            if (scaleClip != null)
                clip.displayName = $"{scaleClip.FromScale:F1}x → {scaleClip.TargetScale:F1}x";
        }

        return ScriptPlayable<DioramaScaleMixerBehaviour>.Create(graph, inputCount);
    }
}
