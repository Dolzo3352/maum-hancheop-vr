using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// XR Grab으로 잡을 수 있는 약재 오브젝트.
///
/// 가마솥에 넣는 인터랙션에 사용됩니다.
/// 플레이어가 잡아서 가마솥(CauldronDropZone) 위에서 놓으면 투입,
/// 밖에서 놓으면 원래 위치로 부드럽게 복귀합니다.
///
/// 시각 피드백(아웃라인, 호버 림)은 같은 오브젝트에 붙은
/// RingInteractable + InteractableOutline이 처리합니다.
///
/// 사용법:
///   약재 오브젝트에 부착. XRGrabInteractable + Rigidbody 필요.
///   CauldronDropZone의 requiredIngredients에 등록합니다.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class GrabIngredient : MonoBehaviour
{
    [Header("복귀 설정")]
    [Tooltip("복귀 위치/회전 기준. 비어있으면 Awake 시점의 자기 위치 사용")]
    [SerializeField] private Transform returnPoint;

    [Tooltip("가마솥 밖에서 놓았을 때 원위치 복귀 시간")]
    [SerializeField] private float returnDuration = 0.5f;

    [Tooltip("복귀 애니메이션 커브")]
    [SerializeField] private AnimationCurve returnCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("투입 VFX")]
    [Tooltip("가마솥에 넣을 때 재생할 파티클 (약재 하위에 배치)")]
    [SerializeField] private ParticleSystem insertionParticle;

    // 상태
    private bool isInserted;
    private bool isGrabEnabled;
    private bool isInsideZone;
    private bool isReturning;

    // 원래 위치
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    // 참조
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private Collider[] colliders;

    // 이벤트
    /// <summary>약재가 가마솥에 투입되었을 때</summary>
    public event Action OnInserted;

    // 프로퍼티
    public bool IsInserted => isInserted;
    public bool IsGrabEnabled => isGrabEnabled;
    public bool IsInsideZone => isInsideZone;

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>(true);

        // 원래 위치 저장 (returnPoint가 있으면 해당 위치, 없으면 자기 위치)
        originalPosition = returnPoint != null ? returnPoint.position : transform.position;
        originalRotation = returnPoint != null ? returnPoint.rotation : transform.rotation;

        // 그랩 활성화 전까지 물리 비활성 (FlyToTarget과 충돌 방지)
        rb.isKinematic = true;

        // 시작 시 그랩 비활성화 (시그널 전까지)
        SetGrabEnabled(false);

        // 이벤트 구독
        grabInteractable.selectExited.AddListener(OnSelectExited);
    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
    }

    /// <summary>
    /// 그랩 해제 시 호출. 가마솥 안이면 투입, 밖이면 복귀.
    /// </summary>
    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (isInserted) return;

        if (isInsideZone)
        {
            // 가마솥 안에서 놓음 → 투입 처리
            Insert();
        }
        else
        {
            // 가마솥 밖에서 놓음 → 원위치 복귀
            StartCoroutine(ReturnToOriginal());
        }
    }

    /// <summary>
    /// 약재를 투입 처리합니다.
    /// </summary>
    public void Insert()
    {
        if (isInserted) return;

        isInserted = true;

        // Rigidbody 비활성화
        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 그랩 비활성화
        SetGrabEnabled(false);

        // 아웃라인 비활성화
        var outline = GetComponent<InteractableOutline>();
        if (outline != null) outline.Complete();

        // RingInteractable 완료 처리 (호버 림 중단)
        var ringInteractable = GetComponent<RingInteractable>();
        if (ringInteractable != null)
            ringInteractable.TestExecute(); // IsCompleted = true

        // 투입 VFX 재생 (부모에서 분리하여 약재 비활성화 후에도 유지)
        if (insertionParticle != null)
        {
            insertionParticle.transform.SetParent(null);
            insertionParticle.Play();
        }

        // 오브젝트 비활성화 (솥에 들어간 느낌)
        gameObject.SetActive(false);

        OnInserted?.Invoke();
    }

    /// <summary>
    /// 원래 위치로 부드럽게 복귀합니다.
    /// </summary>
    private IEnumerator ReturnToOriginal()
    {
        if (isReturning) yield break;
        isReturning = true;

        // 물리 비활성화하고 코드로 이동
        rb.isKinematic = true;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = returnCurve.Evaluate(Mathf.Clamp01(elapsed / returnDuration));

            transform.position = Vector3.Lerp(startPos, originalPosition, t);
            transform.rotation = Quaternion.Slerp(startRot, originalRotation, t);

            yield return null;
        }

        transform.position = originalPosition;
        transform.rotation = originalRotation;

        // 물리 복원 (다시 잡을 수 있도록)
        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        isReturning = false;
    }

    // ─── 외부 제어 ───

    /// <summary>
    /// 그랩 가능 여부를 설정합니다. 시그널 핸들러에서 호출.
    /// </summary>
    public void SetGrabEnabled(bool enabled)
    {
        isGrabEnabled = enabled;
        grabInteractable.enabled = enabled;

        // 콜라이더도 함께 제어 (시그널 전 실수 인터랙션 방지)
        if (colliders != null)
            foreach (var col in colliders)
                if (col != null) col.enabled = enabled;
    }

    /// <summary>
    /// 현재 위치를 새 원래 위치로 업데이트합니다.
    /// FlyToTarget.onArrived에서 호출하여 착지 위치를 복귀 위치로 설정합니다.
    /// </summary>
    public void UpdateOriginalPosition()
    {
        originalPosition = returnPoint != null ? returnPoint.position : transform.position;
        originalRotation = returnPoint != null ? returnPoint.rotation : transform.rotation;
    }

    /// <summary>
    /// 상태 초기화.
    /// </summary>
    public void ResetIngredient()
    {
        isInserted = false;
        isInsideZone = false;
        isReturning = false;

        gameObject.SetActive(true);
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        rb.isKinematic = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        SetGrabEnabled(false);
    }

    // ─── 트리거 판정 (CauldronDropZone 진입/이탈 추적) ───

    /// <summary>CauldronDropZone에서 호출</summary>
    public void SetInsideZone(bool inside)
    {
        isInsideZone = inside;
    }
}
