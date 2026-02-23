using UnityEngine;

/// <summary>
/// 카메라 효과 프리셋.
///
/// 감정 상태별로 .asset 파일을 만들어 Inspector에서 값을 조정합니다.
/// PsychologicalCameraEffect가 presetName으로 검색하여 적용합니다.
///
/// 사용법:
///   Project 창 우클릭 → Create → DearBrave → CameraEffectPreset
///
/// Signal 연결:
///   타임라인 Signal "CameraEffect_Anxious"
///   → SignalReceiver → ApplyPreset("Anxious")
///   → presetName이 "Anxious"인 에셋을 찾아 적용
/// </summary>
[CreateAssetMenu(fileName = "CameraEffectPreset", menuName = "DearBrave/CameraEffectPreset")]
public class CameraEffectPreset : ScriptableObject
{
    [Header("식별")]
    [Tooltip("프리셋 이름. Signal에서 이 이름으로 호출 (예: Anxious, Fearful)")]
    public string presetName;

    [Header("포스트 프로세싱")]
    [Range(0f, 1f)]
    [Tooltip("비네팅 강도. 0 = 없음, 1 = 최대")]
    public float vignetteIntensity;

    [Range(1000f, 10000f)]
    [Tooltip("색온도 (K). 낮을수록 한랭, 높을수록 따뜻")]
    public float colorTemperature = 6500f;

    [Range(0f, 1f)]
    [Tooltip("돌리 줌 강도. 0 = 없음")]
    public float dollyZoomIntensity;

    [Range(0f, 2f)]
    [Tooltip("블룸 강도")]
    public float bloomIntensity;

    [Header("전환")]
    [Tooltip("이전 효과에서 이 프리셋으로 전환하는 시간 (초)")]
    public float transitionDuration = 0.5f;

    [Tooltip("전환 보간 커브")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
}
