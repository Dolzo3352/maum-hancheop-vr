using UnityEngine;

/// <summary>
/// MeshRenderer가 꺼질 때 호출 스택을 로그로 출력합니다.
/// 원인 파악 후 이 컴포넌트는 제거하세요.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class RendererDisableTracker : MonoBehaviour
{
    private Renderer target;

    private void Awake()
    {
        target = GetComponent<Renderer>();
    }

    private void OnDisable()
    {
        if (target != null && !target.enabled)
        {
            Debug.LogWarning(
                $"[RendererDisableTracker] '{name}' Renderer가 꺼졌습니다!\n" +
                $"호출 스택:\n{System.Environment.StackTrace}",
                this);
        }
    }
}
