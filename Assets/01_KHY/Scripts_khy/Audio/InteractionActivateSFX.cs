using UnityEngine;

/// <summary>
/// 인터랙션 활성화/완료 시 3D 효과음 재생.
///
/// 타임라인 시그널로 인터랙션이 활성화될 때(OnActivated) 활성화 효과음,
/// 충전 100% 완료 시(OnExecuted) 완료 효과음을 재생합니다.
/// 각 인터랙터블 오브젝트 근처에 배치된 3D AudioSource를 사용하여
/// 공간감 있는 효과음을 제공합니다.
///
/// InteractionScaleHint와 동일한 패턴 — RingInteractable에 부착하여 사용.
///
/// 사용법:
///   인터랙터블 오브젝트에 부착.
///   근처에 배치한 3D AudioSource와 효과음 클립을 연결합니다.
///   완료 클립은 인터랙터블별로 다르게 설정 가능합니다.
/// </summary>
[RequireComponent(typeof(RingInteractable))]
public class InteractionActivateSFX : MonoBehaviour
{
    [Header("활성화 SFX")]
    [Tooltip("효과음을 재생할 3D AudioSource (인터랙터블 근처에 배치)")]
    [SerializeField] private AudioSource sfxSource;

    [Tooltip("인터랙션이 가능해질 때 재생할 효과음 클립")]
    [SerializeField] private AudioClip activateClip;

    [Header("완료 SFX")]
    [Tooltip("인터랙션 성공 시 재생할 효과음 클립 (인터랙터블별 다르게 설정 가능)")]
    [SerializeField] private AudioClip completeClip;

    private RingInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<RingInteractable>();
        interactable.OnActivated += HandleActivated;
        interactable.OnExecuted += HandleCompleted;
    }

    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.OnActivated -= HandleActivated;
            interactable.OnExecuted -= HandleCompleted;
        }
    }

    private void HandleActivated()
    {
        if (sfxSource != null && activateClip != null)
            sfxSource.PlayOneShot(activateClip);
    }

    private void HandleCompleted()
    {
        if (sfxSource != null && completeClip != null)
            sfxSource.PlayOneShot(completeClip);
    }
}
