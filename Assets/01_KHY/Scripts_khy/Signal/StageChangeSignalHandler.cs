using UnityEngine;

/// <summary>
/// 타임라인 시그널 수신 시 지정한 스테이지로 전환합니다.
/// SignalReceiver → HandleStageChange() 연결.
/// </summary>
public class StageChangeSignalHandler : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private DioramaStageManager stageManager;

    [Header("설정")]
    [Tooltip("전환할 대상 스테이지 인덱스")]
    [SerializeField] private int targetStageIndex;

    [Tooltip("전환 전 현재 스테이지 인덱스 (-1이면 자동 감지)")]
    [SerializeField] private int currentStageIndex = -1;

    public void HandleStageChange()
    {
        FadeController.Instance.FadeOutIn(() =>
        {
            if (currentStageIndex >= 0)
                stageManager.DeactivateStage(currentStageIndex);

            stageManager.ActivateStage(targetStageIndex);
        });
    }
}
