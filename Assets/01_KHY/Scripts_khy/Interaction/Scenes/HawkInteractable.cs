using System.Collections;
using UnityEngine;

/// <summary>
/// Scene 07: 매 호출 인터랙션.
///
/// 링 충전 완료 시 매가 날아와서 열매를 물어옵니다.
/// 매의 비행은 Animator 또는 경로 애니메이션으로 제어합니다.
/// </summary>
public class HawkInteractable : RingInteractable
{
    [Header("매 설정")]
    [Tooltip("매 오브젝트")]
    [SerializeField] private GameObject hawk;

    [Tooltip("매 Animator")]
    [SerializeField] private Animator hawkAnimator;

    [Tooltip("매 비행 시작 위치")]
    [SerializeField] private Transform startPoint;

    [Tooltip("매 도착 위치 (열매를 내려놓는 곳)")]
    [SerializeField] private Transform endPoint;

    [Tooltip("비행 시간 (초)")]
    [SerializeField] private float flightDuration = 3.0f;

    [Tooltip("비행 경로 높이 (포물선)")]
    [SerializeField] private float arcHeight = 5f;

    [Header("열매")]
    [Tooltip("매가 가져올 열매 오브젝트")]
    [SerializeField] private GameObject fruit;

    [Tooltip("열매를 놓을 위치")]
    [SerializeField] private Transform fruitDropPoint;

    [Header("애니메이션 트리거")]
    [SerializeField] private string glideTrigger = "Glide";
    [SerializeField] private string hoverTrigger = "Hover";
    [SerializeField] private string diveTrigger = "Dive";

    protected override void Awake()
    {
        base.Awake();

        // 매 초기 숨김
        if (hawk != null)
            hawk.SetActive(false);
        if (fruit != null)
            fruit.SetActive(false);
    }

    public override void Execute()
    {
        base.Execute();
        StartCoroutine(HawkFlightCoroutine());
    }

    private IEnumerator HawkFlightCoroutine()
    {
        if (hawk == null || startPoint == null || endPoint == null)
        {
            NotifyInteractionDone();
            yield break;
        }

        // 매 등장
        hawk.SetActive(true);
        hawk.transform.position = startPoint.position;
        hawk.transform.LookAt(endPoint);

        // 활강 애니메이션
        if (hawkAnimator != null)
            hawkAnimator.SetTrigger(glideTrigger);

        // 포물선 비행
        float elapsed = 0f;
        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flightDuration;

            // 직선 보간 + 포물선 높이
            Vector3 linearPos = Vector3.Lerp(startPoint.position, endPoint.position, t);
            float arc = arcHeight * 4f * t * (1f - t); // 포물선
            hawk.transform.position = linearPos + Vector3.up * arc;

            // 진행 방향 바라보기
            if (t < 0.95f)
            {
                Vector3 nextPos = Vector3.Lerp(startPoint.position, endPoint.position, t + 0.05f);
                float nextArc = arcHeight * 4f * (t + 0.05f) * (1f - (t + 0.05f));
                hawk.transform.LookAt(nextPos + Vector3.up * nextArc);
            }

            yield return null;
        }

        // 도착: 호버링
        hawk.transform.position = endPoint.position;
        if (hawkAnimator != null)
            hawkAnimator.SetTrigger(hoverTrigger);

        // 열매 놓기
        yield return new WaitForSeconds(0.5f);
        if (fruit != null && fruitDropPoint != null)
        {
            fruit.SetActive(true);
            fruit.transform.position = fruitDropPoint.position;
        }

        // 완료 알림
        NotifyInteractionDone();

        // 매 퇴장 (선택)
        yield return new WaitForSeconds(2f);
        if (hawkAnimator != null)
            hawkAnimator.SetTrigger(glideTrigger);
    }
}
