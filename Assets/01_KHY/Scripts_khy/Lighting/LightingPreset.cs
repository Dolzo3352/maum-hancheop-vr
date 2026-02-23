using UnityEngine;

/// <summary>
/// 라이팅 프리셋.
///
/// Stage별 조명 분위기를 정의합니다.
/// LightingPresetManager가 Stage 전환 시 이 프리셋을 적용합니다.
///
/// 사용법:
///   Project 창 우클릭 → Create → DearBrave → LightingPreset
/// </summary>
[CreateAssetMenu(fileName = "LightingPreset", menuName = "DearBrave/LightingPreset")]
public class LightingPreset : ScriptableObject
{
    [Header("앰비언트")]
    public Color ambientColor = new Color(0.5f, 0.5f, 0.5f);
    public float ambientIntensity = 1f;

    [Header("디렉셔널 라이트")]
    public Color directionalColor = Color.white;
    public float directionalIntensity = 1f;
    public Quaternion lightRotation = Quaternion.Euler(50f, -30f, 0f);

    [Header("스카이박스")]
    public Material skyboxMaterial;

    [Header("안개")]
    public float fogDensity;
    public Color fogColor = Color.gray;
}
