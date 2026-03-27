using UnityEngine;

/// <summary>
/// 각 Stage의 메타데이터.
///
/// Stage마다 .asset 파일을 하나씩 만들어 Inspector에서 값을 설정합니다.
/// NarrativeSequencer가 List&lt;StageData&gt;로 재생 순서를 관리합니다.
///
/// 타임라인/바인딩은 각 Stage GameObject의 PlayableDirector에서 직접 설정합니다.
///
/// 사용법:
///   Project 창 우클릭 → Create → DearBrave → StageData
/// </summary>
[CreateAssetMenu(fileName = "StageData", menuName = "DearBrave/StageData")]
public class StageData : ScriptableObject
{
    [Header("식별")]
    [Tooltip("Stage 이름 (예: Stage_03_큰길)")]
    public string stageName;

    [Tooltip("DioramaStageManager.stages 리스트에서의 인덱스 (0-based)")]
    public int stageIndex;

    [Header("디오라마")]
    [Tooltip("이 Stage 시작 시 적용할 기본 스케일 (1.0 = 원본)")]
    public float dioramaScale = 1f;

    [Header("전환")]
    [Tooltip("이 Stage에 진입할 때의 전환 방식")]
    public TransitionType entryTransition = TransitionType.Fade;

    [Tooltip("전환 소요 시간 (초)")]
    public float transitionDuration = 1f;

    [Header("환경")]
    [Tooltip("이 Stage의 라이팅 프리셋")]
    public LightingPreset lightingPreset;

    [Tooltip("이 Stage의 환경음")]
    public AudioClip ambientAudio;

    [Header("자막")]
    [Tooltip("이 Stage의 자막 데이터 (없으면 자막 없음)")]
    public SubtitleData subtitleData;
}
