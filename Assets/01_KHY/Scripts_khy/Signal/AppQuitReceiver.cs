using UnityEngine;

/// <summary>
/// 앱 강제 종료.
///
/// Timeline Signal에서 호출하여 빌드 앱을 종료합니다.
/// SignalReceiver에 이 컴포넌트의 Quit()을 연결하세요.
///
/// 사용법:
///   1. 아무 GameObject에 부착 + SignalReceiver 추가
///   2. Timeline에 Signal Emitter 배치
///   3. SignalReceiver에서 해당 Signal → Quit() 연결
/// </summary>
public class AppQuitReceiver : MonoBehaviour
{
    public void Quit()
    {
        Debug.Log("[AppQuitReceiver] 앱 종료 요청");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
