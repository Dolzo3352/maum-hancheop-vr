using System;

/// <summary>
/// 인터랙션 시그널 이벤트 허브 (static).
///
/// 모든 InteractionSignal 핸들러가 이 클래스를 통해 이벤트를 발행합니다.
/// ControllerGrabButtonHighlight 등 구독자가 이를 통해 피드백을 제공합니다.
///
/// 사용법:
///   핸들러에서: InteractionEvents.FireActivated() / FireCompleted()
///   구독자에서: InteractionEvents.OnInteractionActivated += OnActivated;
/// </summary>
public static class InteractionEvents
{
    /// <summary>인터랙션 시그널이 발동되어 플레이어의 조작이 필요할 때.</summary>
    public static event Action OnInteractionActivated;

    /// <summary>인터랙션이 완료되었을 때.</summary>
    public static event Action OnInteractionCompleted;

    /// <summary>
    /// 인터랙션 활성화 이벤트를 발행합니다.
    /// InteractionSignalHandler, CauldronIngredientHandler 등에서 호출합니다.
    /// </summary>
    public static void FireActivated()
    {
        OnInteractionActivated?.Invoke();
    }

    /// <summary>
    /// 인터랙션 완료 이벤트를 발행합니다.
    /// </summary>
    public static void FireCompleted()
    {
        OnInteractionCompleted?.Invoke();
    }
}
