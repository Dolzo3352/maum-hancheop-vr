using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 호버 진입 시 3D 효과음 재생.
///
/// InteractableHoverRim의 OnHoverEnter 이벤트를 구독하여
/// 인터랙터블 오브젝트 위치에서 효과음을 재생합니다.
///
/// 빠른 호버 진입/이탈 반복 시 쿨다운으로 중복 재생을 방지합니다.
///
/// 사용법:
///   매니저 오브젝트에 부착. InteractableHoverRim을 연결합니다.
/// </summary>
public class InteractionHoverSFX : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("호버 감지를 담당하는 InteractableHoverRim")]
    [SerializeField] private InteractableHoverRim hoverRim;

    [Header("호버 SFX")]
    [Tooltip("호버 진입 시 재생할 효과음")]
    [SerializeField] private AudioClip hoverEnterClip;

    [Tooltip("호버 효과음 볼륨")]
    [SerializeField] [Range(0f, 1f)] private float hoverVolume = 0.4f;

    [Tooltip("같은 오브젝트 재진입 시 쿨다운 (초)")]
    [SerializeField] private float cooldownTime = 0.15f;

    // 오브젝트별 마지막 재생 시각
    private readonly Dictionary<RingInteractable, float> lastPlayTime = new();

    private void OnEnable()
    {
        if (hoverRim != null)
            hoverRim.OnHoverEnter += HandleHoverEnter;
    }

    private void OnDisable()
    {
        if (hoverRim != null)
            hoverRim.OnHoverEnter -= HandleHoverEnter;
    }

    private void HandleHoverEnter(RingInteractable target)
    {
        if (hoverEnterClip == null || target == null) return;

        // 쿨다운 체크
        float now = Time.time;
        if (lastPlayTime.TryGetValue(target, out float lastTime))
        {
            if (now - lastTime < cooldownTime) return;
        }

        lastPlayTime[target] = now;

        // 오브젝트 위치에서 3D 효과음 재생
        AudioSource.PlayClipAtPoint(hoverEnterClip, target.transform.position, hoverVolume);
    }
}
