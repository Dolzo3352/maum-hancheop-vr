using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 가마솥 약재 넣기 인터랙션의 시그널 핸들러.
///
/// InteractionSignalHandler와 동일한 역할이지만,
/// RingChargeSystem 대신 XR Grab + CauldronDropZone을 사용합니다.
///
/// 동작:
///   1. 타임라인 시그널 수신 → HandleInteractionSignal()
///   2. 모든 약재 그랩 활성화 + 아웃라인 ON
///   3. Timeline → Idle 크로스페이드
///   4. 모든 약재 투입 대기
///   5. 완료 → Idle → Timeline 크로스페이드
///
/// 사용법:
///   SignalReceiver에서 InteractionSignal → HandleInteractionSignal() 연결.
/// </summary>
public class CauldronIngredientHandler : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PlayableDirector director;
    [SerializeField] private CauldronDropZone dropZone;
    [SerializeField] private TimelineAnimationBlender blender;
    [Tooltip("가마솥 인터랙션 중 링 충전을 비활성화하기 위한 참조")]
    [SerializeField] private RingChargeSystem ringChargeSystem;

    [Header("약재")]
    [Tooltip("그랩 가능한 약재들 (아웃라인/림 활성화용)")]
    [SerializeField] private GrabIngredient[] ingredients;

    [Header("대기 애니메이션")]
    [SerializeField] private AnimationClip idleClip;

    [Header("설정")]
    [SerializeField] private float resumeDelay = 0.5f;
    [SerializeField] private bool debugLog = true;

    private bool isWaiting;

    public bool IsWaiting => isWaiting;

    /// <summary>
    /// 타임라인 시그널에서 호출합니다.
    /// SignalReceiver → InteractionSignal → 이 메서드.
    /// </summary>
    public void HandleInteractionSignal()
    {
        if (isWaiting) return;
        StartCoroutine(WaitForAllIngredients());
    }

    private IEnumerator WaitForAllIngredients()
    {
        isWaiting = true;

        // ── 링 충전 시스템 비활성화 (그립이 그랩에 사용되어야 하므로) ──
        if (ringChargeSystem != null)
            ringChargeSystem.IsEnabled = false;

        // ── 약재 활성화: 그랩 가능 + 아웃라인 ON + 호버 림 반응 ──
        foreach (var ingredient in ingredients)
        {
            if (ingredient == null) continue;

            // 그랩 활성화
            ingredient.SetGrabEnabled(true);

            // 아웃라인 활성화
            var outline = ingredient.GetComponent<InteractableOutline>();
            if (outline != null) outline.Activate();

            // RingInteractable 활성화 (호버 림 반응용)
            var ringInteractable = ingredient.GetComponent<RingInteractable>();
            if (ringInteractable != null) ringInteractable.ActivateInteraction();
        }

        // ── Timeline → Idle 크로스페이드 ──
        bool fadeOutDone = false;
        blender.FadeToIdle(idleClip, () => fadeOutDone = true);
        while (!fadeOutDone) yield return null;

        Log($"대기 중: 약재 {dropZone.InsertedCount}/{dropZone.TotalCount}");

        // ── 모든 약재 투입 대기 ──
        bool allDone = false;
        void OnAllInserted() { allDone = true; }
        dropZone.OnAllIngredientsInserted += OnAllInserted;

        // 이미 다 들어갔을 수도 있으므로 체크
        if (dropZone.AllInserted)
            allDone = true;

        while (!allDone) yield return null;
        dropZone.OnAllIngredientsInserted -= OnAllInserted;

        Log("모든 약재 투입 완료!");

        if (resumeDelay > 0f)
            yield return new WaitForSeconds(resumeDelay);

        // ── Idle → Timeline 크로스페이드 ──
        bool fadeInDone = false;
        blender.FadeToTimeline(() => fadeInDone = true);
        while (!fadeInDone) yield return null;

        Log("타임라인 재개 완료");
        isWaiting = false;
    }

    private void Log(string message)
    {
        if (debugLog) Debug.Log($"[CauldronIngredientHandler] {message}", this);
    }

    private void OnDisable()
    {
        if (isWaiting)
        {
            StopAllCoroutines();
            isWaiting = false;
        }
    }
}
