using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// XR Ray가 RingInteractable 위에 있을 때 _RimColor / _RimPower를 변경합니다.
///
/// XR Simple Interactable이나 Affordance System 없이,
/// XRRayInteractor 레이캐스트를 직접 체크하여 호버를 감지합니다.
///
/// 동작:
///   레이 호버 진입 → _RimColor를 RingType 색상으로, _RimPower 증가
///   레이 호버 이탈 → 원래 값 복원
///   인터랙션 완료  → 원래 값 복원 + 더 이상 호버 반응 안 함
///
/// 사용법:
///   매니저 오브젝트에 부착. 왼손/오른손 XRRayInteractor를 연결합니다.
/// </summary>
public class InteractableHoverRim : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("왼손 XR Ray Interactor")]
    [SerializeField] private XRRayInteractor leftRayInteractor;

    [Tooltip("오른손 XR Ray Interactor")]
    [SerializeField] private XRRayInteractor rightRayInteractor;

    [Header("Rim 설정")]
    [Tooltip("호버 시 _RimPower 값")]
    [SerializeField] private float hoverRimPower = 4f;

    [Tooltip("RimColor HDR intensity")]
    [SerializeField] private float hdrIntensity = 2f;

    [Tooltip("Rim 전환 속도 (높을수록 빠름)")]
    [SerializeField] private float transitionSpeed = 10f;

    // 셰이더 프로퍼티 ID
    private static readonly int RimColorID = Shader.PropertyToID("_RimColor");
    private static readonly int RimPowerID = Shader.PropertyToID("_RimPower");

    // 호버 상태 추적
    private RingInteractable leftHovered;
    private RingInteractable rightHovered;

    // 활성 림 이펙트 (대상 → 상태)
    private readonly Dictionary<RingInteractable, RimState> activeRims = new();

    // 제거 예정 목록 (Update 중 Dictionary 수정 방지)
    private readonly List<RingInteractable> toRemove = new();

    private class RimState
    {
        public Renderer[] renderers;
        public MaterialPropertyBlock mpb;
        public Color targetColor;
        public float targetPower;
        public float currentLerp; // 0 = 원래, 1 = 호버
        public bool shouldBeActive;
        public Color originalRimColor;
        public float originalRimPower;
    }

    private void Update()
    {
        // 현재 프레임 호버 대상 감지
        var newLeft = FindHoveredInteractable(leftRayInteractor);
        var newRight = FindHoveredInteractable(rightRayInteractor);

        // 이전에 호버됐지만 이제 아닌 것들 비활성화 (다른 손이 여전히 호버 중이면 유지)
        if (leftHovered != null && leftHovered != newLeft && leftHovered != newRight)
            SetRimActive(leftHovered, false);
        if (rightHovered != null && rightHovered != newRight && rightHovered != newLeft)
            SetRimActive(rightHovered, false);

        // 새로 호버된 것들 활성화
        if (newLeft != null)
            SetRimActive(newLeft, true);
        if (newRight != null)
            SetRimActive(newRight, true);

        leftHovered = newLeft;
        rightHovered = newRight;

        // 림 lerp 업데이트
        UpdateAllRims();
    }

    // ─── 호버 감지 ───

    private RingInteractable FindHoveredInteractable(XRRayInteractor ray)
    {
        if (ray == null) return null;

        if (ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            var interactable = hit.collider.GetComponentInParent<RingInteractable>();
            if (interactable != null && interactable.IsInteractionActive && !interactable.IsCompleted)
                return interactable;
        }

        return null;
    }

    private void SetRimActive(RingInteractable target, bool active)
    {
        if (!activeRims.TryGetValue(target, out var state))
        {
            if (!active) return; // 비활성화할 대상이 없으면 무시

            // 새로 추가
            state = CreateRimState(target);
            if (state == null) return;

            activeRims[target] = state;

            // 인터랙션 완료 이벤트 구독
            target.OnInteractionDone += () => OnInteractionComplete(target);
        }

        state.shouldBeActive = active;
    }

    private RimState CreateRimState(RingInteractable target)
    {
        var renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return null;

        var mpb = new MaterialPropertyBlock();

        // 첫 번째 렌더러에서 원래 값 읽기
        renderers[0].GetPropertyBlock(mpb);
        Color originalColor = mpb.GetColor(RimColorID);
        float originalPower = mpb.GetFloat(RimPowerID);

        // RingType 색상으로 타겟 설정
        Color hdrColor = RingTypeColors.GetColor(target.RingType) * hdrIntensity;

        return new RimState
        {
            renderers = renderers,
            mpb = mpb,
            targetColor = hdrColor,
            targetPower = hoverRimPower,
            currentLerp = 0f,
            shouldBeActive = true,
            originalRimColor = originalColor,
            originalRimPower = originalPower
        };
    }

    // ─── 림 업데이트 ───

    private void UpdateAllRims()
    {
        toRemove.Clear();

        foreach (var kvp in activeRims)
        {
            var target = kvp.Key;
            var state = kvp.Value;

            // 대상이 파괴되었으면 제거
            if (target == null)
            {
                toRemove.Add(target);
                continue;
            }

            // lerp 진행
            float lerpTarget = state.shouldBeActive ? 1f : 0f;
            state.currentLerp = Mathf.MoveTowards(state.currentLerp, lerpTarget, transitionSpeed * Time.deltaTime);

            // 셰이더 프로퍼티 적용
            Color rimColor = Color.Lerp(state.originalRimColor, state.targetColor, state.currentLerp);
            float rimPower = Mathf.Lerp(state.originalRimPower, state.targetPower, state.currentLerp);

            foreach (var renderer in state.renderers)
            {
                if (renderer == null) continue;
                renderer.GetPropertyBlock(state.mpb);
                state.mpb.SetColor(RimColorID, rimColor);
                state.mpb.SetFloat(RimPowerID, rimPower);
                renderer.SetPropertyBlock(state.mpb);
            }

            // 완전히 페이드아웃되면 제거
            if (!state.shouldBeActive && state.currentLerp <= 0f)
                toRemove.Add(target);
        }

        foreach (var key in toRemove)
            activeRims.Remove(key);
    }

    // ─── 인터랙션 완료 ───

    private void OnInteractionComplete(RingInteractable target)
    {
        if (activeRims.TryGetValue(target, out var state))
        {
            state.shouldBeActive = false;
        }

        // 호버 참조도 정리
        if (leftHovered == target) leftHovered = null;
        if (rightHovered == target) rightHovered = null;
    }
}
