using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 타임라인 시그널 수신 시 지정한 씬으로 전환합니다.
/// SignalReceiver → HandleSceneChange() 연결.
/// </summary>
public class SceneChangeSignalHandler : MonoBehaviour
{
    [Tooltip("이동할 씬 이름 (Build Settings에 등록된 이름)")]
    [SerializeField] private string targetSceneName;

    public void HandleSceneChange()
    {
        FadeController.Instance.FadeOut(() =>
        {
            SceneManager.LoadScene(targetSceneName);
        });
    }
}
