using System.Collections;
using UnityEngine;

/// <summary>
/// 개별 억새 오브젝트에 붙이는 스크립트
/// - 성장 애니메이션 (시작 스케일 → 1)
/// - 타겟(아이)이 가까이 오면 밀려나듯 회전
/// </summary>
public class SilverGrass : MonoBehaviour
{
    [Header("=== 성장 설정 ===")]
    [Tooltip("성장 시작 스케일 (0이면 안 보임, 0.1이면 작게 보임)")]
    [SerializeField] private float startScale = 0f;

    [Tooltip("성장 애니메이션 시간 (초)")]
    [SerializeField] private float growDuration = 1.5f;

    [Tooltip("성장 시 흔들림 강도 (0이면 흔들림 없음)")]
    [SerializeField] private float growShakeStrength = 5f;

    [Header("=== 벌어짐 설정 ===")]
    [Tooltip("반응할 대상 (아이 오브젝트)")]
    public Transform target;

    [Tooltip("반응 시작 거리")]
    [SerializeField] private float bendRadius = 0.3f;

    [Tooltip("최대 기울기 각도 (클수록 과장됨, 30=자연스러움, 60=과장, 90=거의 눕기)")]
    [SerializeField] private float bendStrength = 60f;

    [Tooltip("반응 속도 (낮을수록 느긋, 높을수록 즉각 반응)")]
    [SerializeField] private float bendSpeed = 8f;

    [Tooltip("복귀 속도 (낮을수록 천천히 돌아옴)")]
    [SerializeField] private float recoverSpeed = 3f;

    [Header("=== 과장 연출 ===")]
    [Tooltip("밀려날 때 추가로 Y축 회전 (비틀림 효과)")]
    [SerializeField] private float twistStrength = 15f;

    [Tooltip("밀려날 때 스케일 변화 (1이면 변화없음, 1.2면 살짝 커짐)")]
    [SerializeField] private float bendScaleMultiplier = 1.1f;

    // 내부 상태
    private Quaternion originalRotation;
    private Vector3 originalScale;
    private bool hasGrown = false;
    private bool isGrowing = false;

    public bool HasGrown => hasGrown;

    private void Start()
    {
        originalRotation = transform.localRotation;
        originalScale = transform.localScale;
        // 시작 스케일 적용
        transform.localScale = originalScale * startScale;
    }

    private void Update()
    {
        if (!hasGrown || isGrowing || target == null) return;

        Vector3 targetPosFlat = new Vector3(target.position.x, 0f, target.position.z);
        Vector3 myPosFlat = new Vector3(transform.position.x, 0f, transform.position.z);
        float dist = Vector3.Distance(targetPosFlat, myPosFlat);

        if (dist < bendRadius)
        {
            Vector3 pushDir = (myPosFlat - targetPosFlat).normalized;
            float bendRatio = 1f - (dist / bendRadius);
            float bendAmount = bendRatio * bendStrength;
            float twist = bendRatio * twistStrength;

            // 밀려남 + 비틀림
            Quaternion targetRot = originalRotation
                * Quaternion.Euler(pushDir.z * bendAmount, twist, -pushDir.x * bendAmount);

            transform.localRotation = Quaternion.Lerp(
                transform.localRotation, targetRot, Time.deltaTime * bendSpeed);

            // 밀려날 때 스케일 약간 변화
            float scaleAmount = Mathf.Lerp(1f, bendScaleMultiplier, bendRatio);
            transform.localScale = Vector3.Lerp(
                transform.localScale, originalScale * scaleAmount, Time.deltaTime * bendSpeed);
        }
        else
        {
            // 복귀 (recoverSpeed로 따로 제어)
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation, originalRotation, Time.deltaTime * recoverSpeed);

            transform.localScale = Vector3.Lerp(
                transform.localScale, originalScale, Time.deltaTime * recoverSpeed);
        }
    }

    public void Grow()
    {
        if (hasGrown || isGrowing) return;
        StartCoroutine(GrowCoroutine());
    }

    private IEnumerator GrowCoroutine()
    {
        isGrowing = true;
        float elapsed = 0f;

        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / growDuration);

            float y = Mathf.SmoothStep(startScale, 1f, t);
            float xz = Mathf.SmoothStep(startScale, 1f, Mathf.Clamp01(t * 1.5f - 0.3f));

            transform.localScale = new Vector3(
                originalScale.x * xz,
                originalScale.y * y,
                originalScale.z * xz);

            if (growShakeStrength > 0f && t > 0.2f && t < 0.9f)
            {
                float shake = Mathf.Sin(elapsed * 15f) * growShakeStrength * (1f - t);
                transform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, shake);
            }

            yield return null;
        }

        transform.localScale = originalScale;
        transform.localRotation = originalRotation;

        isGrowing = false;
        hasGrown = true;
    }
}