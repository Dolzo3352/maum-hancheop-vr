using UnityEngine;

/// <summary>
/// 링 충전 시 레이를 따라 빛가루가 흐르는 파티클 이펙트.
///
/// 손(레이 시작점)에서 대상(레이 끝점) 방향으로 파티클이 흘러갑니다.
/// progress에 따라 파티클 밀도가 증가하고,
/// 대상에 가까워질수록 수렴하여 에너지가 모이는 느낌을 줍니다.
///
/// ParticleSystem 프리팹:
///   - Emission: Rate = 0 (코드에서 수동 Emit)
///   - Simulation Space: World
///   - Renderer: Billboard, Additive 머티리얼
///   - Start Lifetime, Start Size 등은 코드에서 오버라이드됨
///
/// 사용법:
///   매니저에 부착. RingChargeSystem과 ParticleSystem 프리팹을 연결합니다.
/// </summary>
public class RingRayParticle : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RingChargeSystem ringChargeSystem;

    [Tooltip("빛가루 ParticleSystem 프리팹 (Emission Rate 0으로 설정)")]
    [SerializeField] private ParticleSystem particlePrefab;

    [Header("흐름 설정")]
    [Tooltip("파티클 이동 속도 (전진)")]
    [SerializeField] private float flowSpeed = 4f;

    [Tooltip("충전 시작 시 초당 파티클 수")]
    [SerializeField] private float baseEmissionRate = 15f;

    [Tooltip("충전 완료 시 초당 파티클 수")]
    [SerializeField] private float maxEmissionRate = 50f;

    [Tooltip("레이 주변 흩뿌림 반경")]
    [SerializeField] private float spreadRadius = 0.015f;

    [Header("회오리 설정")]
    [Tooltip("회오리 활성화")]
    [SerializeField] private bool enableSwirl = true;

    [Tooltip("회오리 회전 속도 (rad/s)")]
    [SerializeField] private float swirlSpeed = 8f;

    [Tooltip("회오리 반경 (시작점)")]
    [SerializeField] private float swirlRadius = 0.06f;

    [Tooltip("대상에 가까울수록 반경이 줄어드는 비율 (0=일정, 1=완전 수렴)")]
    [SerializeField] [Range(0f, 1f)] private float swirlConverge = 0.8f;

    [Header("파티클 크기")]
    [Tooltip("최소 파티클 크기")]
    [SerializeField] private float minSize = 0.008f;

    [Tooltip("최대 파티클 크기")]
    [SerializeField] private float maxSize = 0.025f;

    [Header("HDR 설정")]
    [Tooltip("파티클 색상 HDR intensity")]
    [SerializeField] private float hdrIntensity = 3f;

    [Header("머티리얼")]
    [Tooltip("커스텀 파티클 머티리얼 (비워두면 URP Additive 자동 생성)")]
    [SerializeField] private Material customParticleMaterial;

    // 인스턴스
    private ParticleSystem particleInstance;
    private ParticleSystem.EmitParams emitParams;
    private bool isCharging;
    private float emissionAccumulator;
    private Color currentColor;
    private Material autoMaterial;

    private void Awake()
    {
        if (particlePrefab != null)
        {
            particleInstance = Instantiate(particlePrefab);
        }
        else
        {
            // 프리팹 없으면 코드로 ParticleSystem 생성
            var go = new GameObject("RingRayParticle_Auto");
            particleInstance = go.AddComponent<ParticleSystem>();
        }

        particleInstance.gameObject.SetActive(false);

        // ── ParticleSystem 설정 강제 적용 ──
        var main = particleInstance.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = Color.white; // EmitParams에서 오버라이드할 것
        main.playOnAwake = false;

        var emission = particleInstance.emission;
        emission.rateOverTime = 0f;

        // Shape 비활성화 (수동 Emit이므로)
        var shape = particleInstance.shape;
        shape.enabled = false;

        // Color over Lifetime 비활성화 (EmitParams 색상 유지)
        var col = particleInstance.colorOverLifetime;
        col.enabled = false;

        // Size over Lifetime: 끝으로 갈수록 작아짐
        var sol = particleInstance.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // ── 머티리얼 설정 ──
        var renderer = particleInstance.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        if (customParticleMaterial != null)
        {
            renderer.material = customParticleMaterial;
        }
        else
        {
            // URP Particles/Unlit Additive 머티리얼 자동 생성
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader != null)
            {
                autoMaterial = new Material(shader);
                autoMaterial.SetFloat("_Surface", 1f);  // Transparent
                autoMaterial.SetFloat("_Blend", 1f);    // Additive
                autoMaterial.SetColor("_BaseColor", Color.white);
                autoMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                autoMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                autoMaterial.renderQueue = 3000;
                renderer.material = autoMaterial;
            }
        }

        emitParams = new ParticleSystem.EmitParams();
    }

    private void OnEnable()
    {
        if (ringChargeSystem != null)
        {
            ringChargeSystem.OnChargeStarted += HandleChargeStarted;
            ringChargeSystem.OnRingComplete += HandleRingComplete;
            ringChargeSystem.OnChargeCancelled += HandleChargeCancelled;
        }
    }

    private void OnDisable()
    {
        if (ringChargeSystem != null)
        {
            ringChargeSystem.OnChargeStarted -= HandleChargeStarted;
            ringChargeSystem.OnRingComplete -= HandleRingComplete;
            ringChargeSystem.OnChargeCancelled -= HandleChargeCancelled;
        }

        StopEffect();
    }

    // ─── 이벤트 핸들러 ───

    private void HandleChargeStarted(RingType ringType, RingInteractable target)
    {
        if (particleInstance == null) return;

        // 파티클 색상 설정
        currentColor = RingTypeColors.GetColor(ringType) * hdrIntensity;

        particleInstance.gameObject.SetActive(true);
        particleInstance.Clear();
        isCharging = true;
        emissionAccumulator = 0f;
    }

    private void HandleRingComplete(RingType ringType, RingInteractable target)
    {
        StopEffect();
    }

    private void HandleChargeCancelled(RingType ringType)
    {
        StopEffect();
    }

    private void StopEffect()
    {
        isCharging = false;
        // 기존 파티클은 자연 소멸하도록 두고 새 emission만 중단
        // 일정 시간 후 비활성화
        if (particleInstance != null && particleInstance.gameObject.activeSelf)
            Invoke(nameof(DeactivateParticle), 1f);
    }

    private void DeactivateParticle()
    {
        if (particleInstance != null && !isCharging)
            particleInstance.gameObject.SetActive(false);
    }

    // ─── 매 프레임 파티클 Emit ───

    private void Update()
    {
        if (!isCharging || particleInstance == null) return;

        var ray = ringChargeSystem.ActiveRayInteractor;
        if (ray == null) return;

        if (!ray.TryGetCurrent3DRaycastHit(out RaycastHit hit)) return;

        Vector3 start = ray.rayOriginTransform != null
            ? ray.rayOriginTransform.position
            : ray.transform.position;
        Vector3 end = hit.point;
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);

        if (distance < 0.01f) return;

        // progress에 따른 emission rate
        float progress = ringChargeSystem.ChargeProgress;
        float rate = Mathf.Lerp(baseEmissionRate, maxEmissionRate, progress);

        // 축적 기반 emit (프레임 레이트 독립)
        emissionAccumulator += rate * Time.deltaTime;
        int emitCount = Mathf.FloorToInt(emissionAccumulator);
        emissionAccumulator -= emitCount;

        // 회오리용 축 계산 (레이 방향에 수직인 두 축)
        Vector3 tangent, bitangent;
        if (enableSwirl)
        {
            tangent = Vector3.Cross(direction, Vector3.up).normalized;
            if (tangent.sqrMagnitude < 0.001f) // direction이 거의 up일 때
                tangent = Vector3.Cross(direction, Vector3.right).normalized;
            bitangent = Vector3.Cross(direction, tangent).normalized;
        }
        else
        {
            tangent = bitangent = Vector3.zero;
        }

        for (int i = 0; i < emitCount; i++)
        {
            // 레이 위 랜덤 위치 (약간 대상 쪽 편향)
            float t = Random.value * Random.value; // 제곱 분포: 시작점 쪽에 더 많이
            Vector3 pos = Vector3.Lerp(start, end, t);

            if (enableSwirl)
            {
                // 회오리: 레이 축 주변 나선형 배치
                float angle = Time.time * swirlSpeed + t * Mathf.PI * 4f + Random.value * 0.5f;
                float radius = swirlRadius * Mathf.Lerp(1f, 1f - swirlConverge, t); // 대상에 가까울수록 수렴
                Vector3 swirlOffset = (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * radius;
                pos += swirlOffset;

                // 속도에도 회전 성분 추가 (접선 방향)
                Vector3 swirlVelocity = (-tangent * Mathf.Sin(angle) + bitangent * Mathf.Cos(angle)) * swirlSpeed * radius * 0.3f;
                emitParams.velocity = direction * flowSpeed + swirlVelocity;
            }
            else
            {
                // 레이 주변 약간 흩뿌림
                pos += Random.insideUnitSphere * spreadRadius;
                emitParams.velocity = direction * flowSpeed;
            }

            // 대상에 도달하면 소멸하도록 lifetime 계산
            float remainingDist = (1f - t) * distance;
            float lifetime = remainingDist / flowSpeed;
            lifetime = Mathf.Max(lifetime, 0.1f); // 최소 수명

            emitParams.position = pos;
            emitParams.startLifetime = lifetime;
            emitParams.startSize = Mathf.Lerp(minSize, maxSize, Random.value);
            emitParams.startColor = currentColor;

            particleInstance.Emit(emitParams, 1);
        }
    }

    // ─── 정리 ───

    private void OnDestroy()
    {
        CancelInvoke();
        if (particleInstance != null)
            Destroy(particleInstance.gameObject);
        if (autoMaterial != null)
            Destroy(autoMaterial);
    }
}
