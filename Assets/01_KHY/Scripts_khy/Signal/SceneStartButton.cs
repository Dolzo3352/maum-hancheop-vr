using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 시작 버튼 클릭 시 페이드 아웃 후 지정 씬으로 이동.
/// StartScene의 "시작하기" 버튼 OnClick에 연결.
/// </summary>
public class SceneStartButton : MonoBehaviour
{
    [Tooltip("이동할 씬 이름 (Build Settings에 등록된 이름)")]
    [SerializeField] private string targetSceneName = "Scene_Master";

    public void OnStartClicked()
    {
        if (FadeController.Instance != null)
        {
            FadeController.Instance.FadeOut(() =>
            {
                SceneManager.LoadScene(targetSceneName);
            });
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }
    }
}
