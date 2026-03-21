using UnityEngine;
using VLB;

/// <summary>
/// Timeline Animation Track에서 Light.intensity를 키프레임으로 변경할 때,
/// VolumetricLightBeamHD가 Play 모드에서도 빔을 업데이트하도록 동기화합니다.
///
/// HD 버전은 trackChangesDuringPlaytime이 없고 OnDidApplyAnimationProperties() 콜백에
/// 의존하는데, Timeline Playable API는 이 콜백을 안정적으로 호출하지 않습니다.
/// 이 스크립트가 매 프레임 intensity 변경을 감지하여 VLB에 알려줍니다.
/// </summary>
[RequireComponent(typeof(Light))]
public class VLBIntensitySync : MonoBehaviour
{
    private Light _light;
    private VolumetricLightBeamHD _vlb;
    private float _lastIntensity;
    private Color _lastColor;

    void Awake()
    {
        _light = GetComponent<Light>();
        _vlb = GetComponent<VolumetricLightBeamHD>();
    }

    void LateUpdate()
    {
        if (_light == null || _vlb == null) return;

        if (!Mathf.Approximately(_light.intensity, _lastIntensity) ||
            _light.color != _lastColor)
        {
            _vlb.UpdateAfterManualPropertyChange();
            _lastIntensity = _light.intensity;
            _lastColor = _light.color;
        }
    }
}
