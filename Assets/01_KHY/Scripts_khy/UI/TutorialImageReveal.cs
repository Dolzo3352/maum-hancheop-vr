using System.Collections;
using UnityEngine;

/// <summary>
/// 튜토리얼 이미지 등장 연출.
///
/// GameObject가 활성화될 때 자동으로 등장 애니메이션을 재생합니다.
/// 페이드 인 + 스케일 팝 + 슬라이드 조합을 Inspector에서 개별 토글할 수 있습니다.
///
/// 사용법:
///   튜토리얼 이미지 GameObject에 부착합니다.
///   CanvasGroup이 필요합니다 (페이드용, 없으면 자동 추가).
///   비활성 상태로 두었다가 SetActive(true) 하면 연출이 재생됩니다.
/// </summary>
public class TutorialImageReveal : MonoBehaviour
{
    // ─── 공통 설정 ───

    [Header("공통")]
    [Tooltip("등장 애니메이션 총 시간 (초)")]
    [SerializeField] private float duration = 0.4f;

    [Tooltip("애니메이션 커브 (기본: EaseOut 느낌)")]
    [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("활성화 후 등장까지 대기 시간 (초)")]
    [SerializeField] private float delay = 0f;

    // ─── 페이드 ───

    [Header("페이드")]
    [SerializeField] private bool useFade = true;

    // ─── 스케일 ───

    [Header("스케일")]
    [SerializeField] private bool useScale = true;

    [Tooltip("시작 스케일 (0이면 안 보이다가 커짐)")]
    [SerializeField] private float startScale = 0.7f;

    // ─── 슬라이드 ───

    [Header("슬라이드")]
    [SerializeField] private bool useSlide = false;

    [Tooltip("시작 오프셋 (로컬 좌표). 예: (0, -30, 0)이면 아래에서 올라옴")]
    [SerializeField] private Vector3 slideOffset = new Vector3(0f, -30f, 0f);

    // ─── 퇴장 ───

    [Header("퇴장 (선택)")]
    [Tooltip("자동 퇴장 사용 여부")]
    [SerializeField] private bool autoHide = false;

    [Tooltip("등장 완료 후 유지 시간 (초)")]
    [SerializeField] private float displayDuration = 3f;

    [Tooltip("퇴장 애니메이션 시간 (초)")]
    [SerializeField] private float hideDuration = 0.3f;

    // ─── 내부 ───

    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private Vector3 originalLocalPos;
    private Coroutine animCoroutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        originalScale = transform.localScale;
        originalLocalPos = transform.localPosition;

        // 초기 상태: 안 보이게
        SetState(0f);
    }

    private void OnEnable()
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);

        SetState(0f);
        animCoroutine = StartCoroutine(RevealRoutine());
    }

    private void OnDisable()
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }
    }

    // ─── 공개 API ───

    /// <summary>수동으로 퇴장 애니메이션을 재생합니다.</summary>
    public void Hide()
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(HideRoutine());
    }

    // ─── 코루틴 ───

    private IEnumerator RevealRoutine()
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = easeCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
            SetState(t);
            yield return null;
        }

        SetState(1f);
        animCoroutine = null;

        // 자동 퇴장
        if (autoHide)
        {
            yield return new WaitForSeconds(displayDuration);
            animCoroutine = StartCoroutine(HideRoutine());
        }
    }

    private IEnumerator HideRoutine()
    {
        float elapsed = 0f;

        while (elapsed < hideDuration)
        {
            elapsed += Time.deltaTime;
            float t = easeCurve.Evaluate(Mathf.Clamp01(elapsed / hideDuration));
            SetState(1f - t); // 역재생
            yield return null;
        }

        SetState(0f);
        animCoroutine = null;
        gameObject.SetActive(false);
    }

    // ─── 상태 적용 ───

    /// <summary>t=0 (안 보임) ~ t=1 (완전 등장) 사이의 상태를 적용합니다.</summary>
    private void SetState(float t)
    {
        // 페이드
        if (useFade && canvasGroup != null)
            canvasGroup.alpha = t;

        // 스케일
        if (useScale)
        {
            float s = Mathf.LerpUnclamped(startScale, 1f, t);
            transform.localScale = originalScale * s;
        }

        // 슬라이드
        if (useSlide)
        {
            transform.localPosition = originalLocalPos + slideOffset * (1f - t);
        }
    }
}
