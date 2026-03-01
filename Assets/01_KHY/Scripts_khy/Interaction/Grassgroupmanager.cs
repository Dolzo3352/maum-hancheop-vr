using System.Collections;
using UnityEngine;

/// <summary>
/// 억새 군락 매니저
/// - 빈 부모 오브젝트에 붙여서 자식 억새들을 일괄 관리
/// - 순차적 성장 딜레이로 자연스러운 연출
/// - XR 인터랙션 이벤트에서 GrowAll() 호출
/// </summary>
public class GrassGroupManager : MonoBehaviour
{
    [Header("=== 억새 그룹 ===")]
    [Tooltip("관리할 억새들 (순서대로 자라남)")]
    [SerializeField] private SilverGrass[] grasses;

    [Header("=== 성장 연출 ===")]
    [Tooltip("각 억새 사이의 성장 딜레이 (초)")]
    [SerializeField] private float delayBetween = 0.2f;

    [Tooltip("true면 가운데부터, false면 배열 순서대로")]
    [SerializeField] private bool growFromCenter = true;

    [Header("=== 타겟 ===")]
    [Tooltip("아이 오브젝트 (모든 억새에 자동 할당)")]
    [SerializeField] private Transform childTarget;

    [Header("=== 인터랙션 ===")]
    [Tooltip("true면 성장 완료 후 자동으로 벌어짐 반응 활성화")]
    [SerializeField] private bool enableBendAfterGrow = true;

    private bool hasTriggered = false;

    private void Start()
    {
        // 자동으로 자식에서 SilverGrass 찾기 (인스펙터에서 안 넣었을 경우)
        if (grasses == null || grasses.Length == 0)
        {
            grasses = GetComponentsInChildren<SilverGrass>();
        }

        // 타겟 할당
        if (childTarget != null)
        {
            SetTarget(childTarget);
        }
    }

    /// <summary>
    /// 모든 억새에 타겟 할당
    /// </summary>
    public void SetTarget(Transform target)
    {
        childTarget = target;
        foreach (var grass in grasses)
        {
            grass.target = target;
        }
    }

    /// <summary>
    /// 전체 억새 순차 성장 시작
    /// XR Interactable의 Select Entered 이벤트에 연결
    /// </summary>
    public void GrowAll()
    {
        if (hasTriggered) return;
        hasTriggered = true;
        StartCoroutine(GrowSequence());
    }

    private IEnumerator GrowSequence()
    {
        if (growFromCenter)
        {
            // 가운데부터 바깥으로 퍼지며 성장
            int center = grasses.Length / 2;
            int maxDist = Mathf.Max(center, grasses.Length - center - 1);

            for (int d = 0; d <= maxDist; d++)
            {
                int left = center - d;
                int right = center + d;

                if (left >= 0 && left < grasses.Length)
                    grasses[left].Grow();
                if (right != left && right >= 0 && right < grasses.Length)
                    grasses[right].Grow();

                yield return new WaitForSeconds(delayBetween);
            }
        }
        else
        {
            // 배열 순서대로 성장
            for (int i = 0; i < grasses.Length; i++)
            {
                grasses[i].Grow();
                yield return new WaitForSeconds(delayBetween);
            }
        }
    }

    /// <summary>
    /// 에디터에서 억새 배열 자동 채우기 (컨텍스트 메뉴)
    /// </summary>
    [ContextMenu("Auto Find Grasses")]
    private void AutoFindGrasses()
    {
        grasses = GetComponentsInChildren<SilverGrass>();
        Debug.Log($"Found {grasses.Length} SilverGrass components");
    }
}