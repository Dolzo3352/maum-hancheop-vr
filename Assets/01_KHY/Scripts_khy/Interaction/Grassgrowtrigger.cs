using UnityEngine;

/// <summary>
/// 억새 성장 트리거 (트리거 감지 방식)
/// - 특정 태그의 오브젝트가 트리거에 들어오면 억새 성장 시작
/// - VR 컨트롤러 없이 테스트 가능
/// 
/// [셋업 방법]
/// 1. 빈 오브젝트에 이 스크립트 + Collider (Is Trigger 체크) 추가
/// 2. 억새 군락이 자랄 위치에 배치
/// 3. grassGroup에 GrassGroupManager 연결
/// 4. 테스트용 오브젝트에 triggerTag와 동일한 태그 + Rigidbody 붙이기
/// </summary>
public class GrassGrowTrigger : MonoBehaviour
{
    [Header("=== 연결 ===")]
    [Tooltip("성장시킬 억새 군락 매니저")]
    [SerializeField] private GrassGroupManager grassGroup;

    [Header("=== 트리거 설정 ===")]
    [Tooltip("감지할 오브젝트 태그")]
    [SerializeField] private string triggerTag = "Player";

    [Header("=== 힌트 연출 (선택) ===")]
    [Tooltip("인터랙션 가능할 때 보여줄 힌트 오브젝트")]
    [SerializeField] private GameObject interactionHint;

    private bool used = false;

    private void OnTriggerEnter(Collider other)
    {
        if (used) return;
        if (!other.CompareTag(triggerTag)) return;

        used = true;

        // 힌트 끄기
        if (interactionHint != null)
            interactionHint.SetActive(false);

        // 억새 성장 시작
        if (grassGroup != null)
        {
            grassGroup.GrowAll();
        }

        Debug.Log("억새 성장 트리거 발동!");
    }
}