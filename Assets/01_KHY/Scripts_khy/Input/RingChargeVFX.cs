using UnityEngine;

/// <summary>
/// 링 충전 프로그레스 VFX.
///
/// RingChargeSystem 이벤트를 구독하여 프로그레스 메시를 소환/업데이트/숨김합니다.
/// MaterialPropertyBlock으로 머티리얼 공유 상태에서도 인터랙션별 색상/진행도를 개별 제어합니다.
///
/// 셰이더 프로퍼티:
///   _FillAmount  (float)  : 충전 진행도 0~1
///   _GaugeColor  (Color HDR) : 게이지 색상
///
/// 사용법:
///   프로그레스 메시 프리팹을 연결하고, RingChargeSystem을 참조합니다.
///   각 RingInteractable에 vfxSpawnPoint를 설정하면 해당 위치에 소환됩니다.
///   설정하지 않으면 인터랙터블 Transform 위치 + offset에 소환됩니다.
/// </summary>
public class RingChargeVFX : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private RingChargeSystem ringChargeSystem;

    [Header("프로그레스 메시")]
    [Tooltip("VFX_ring 셰이더가 적용된 메시 프리팹")]
    [SerializeField] private GameObject progressMeshPrefab;

    [Header("배치 설정")]
    [Tooltip("스폰 포인트가 없을 때 대상 위치에서의 오프셋")]
    [SerializeField] private Vector3 defaultOffset = new Vector3(0f, 0.5f, 0f);

    [Header("HDR 설정")]
    [Tooltip("GaugeColor에 곱할 HDR intensity")]
    [SerializeField] private float hdrIntensity = 2f;

    [Header("페이드")]
    [Tooltip("소환 시 스케일 페이드인 시간")]
    [SerializeField] private float fadeInDuration = 0.15f;

    [Tooltip("숨김 시 스케일 페이드아웃 시간")]
    [SerializeField] private float fadeOutDuration = 0.1f;

    // 인스턴스
    private GameObject meshInstance;
    private Renderer meshRenderer;
    private MaterialPropertyBlock mpb;
    private Vector3 baseScale;

    // 페이드 상태
    private float fadeProgress;
    private bool isFadingIn;
    private bool isFadingOut;
    private bool isActive;

    // 실패 연출 상태
    private bool isFailureMode;
    private float failureElapsed;
    private float failureJitterDuration;
    private float failureJitterIntensity;
    private float failureColorTransitionDuration;
    private float failureFadeOutDuration;
    private Color failureTargetColor;
    private Color failureBaseColor;
    private Vector3 failureBasePosition;

    // 셰이더 프로퍼티 ID (캐시)
    private static readonly int FillAmountID = Shader.PropertyToID("_FillAmount");
    private static readonly int GaugeColorID = Shader.PropertyToID("_GaugeColor");

    private void Awake()
    {
        mpb = new MaterialPropertyBlock();

        if (progressMeshPrefab != null)
        {
            meshInstance = Instantiate(progressMeshPrefab);
            meshInstance.SetActive(false);
            meshRenderer = meshInstance.GetComponentInChildren<Renderer>();
            baseScale = meshInstance.transform.localScale;
        }
    }

    private void OnEnable()
    {
        if (ringChargeSystem != null)
        {
            ringChargeSystem.OnChargeStarted += HandleChargeStarted;
            ringChargeSystem.OnChargeUpdated += HandleChargeUpdated;
            ringChargeSystem.OnRingComplete += HandleRingComplete;
            ringChargeSystem.OnChargeCancelled += HandleChargeCancelled;
        }
    }

    private void OnDisable()
    {
        if (ringChargeSystem != null)
        {
            ringChargeSystem.OnChargeStarted -= HandleChargeStarted;
            ringChargeSystem.OnChargeUpdated -= HandleChargeUpdated;
            ringChargeSystem.OnRingComplete -= HandleRingComplete;
            ringChargeSystem.OnChargeCancelled -= HandleChargeCancelled;
        }
    }

    // ─── 이벤트 핸들러 ───

    private void HandleChargeStarted(RingType ringType, RingInteractable target)
    {
        if (meshInstance == null) return;

        // 위치 설정: 스폰 포인트 우선, 없으면 대상 위치 + 오프셋
        Transform spawnPoint = target.VFXSpawnPoint;
        if (spawnPoint != null)
        {
            meshInstance.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            meshInstance.transform.position = target.transform.position + defaultOffset;
        }

        // HDR 색상 설정
        Color hdrColor = RingTypeColors.GetColor(ringType) * hdrIntensity;
        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(GaugeColorID, hdrColor);
        mpb.SetFloat(FillAmountID, 1f); // 셰이더: 1=투명, 0=채워짐. 시작은 비어있으므로 1
        meshRenderer.SetPropertyBlock(mpb);

        // 표시 + 페이드인
        meshInstance.SetActive(true);
        isActive = true;
        StartFadeIn();
    }

    private void HandleChargeUpdated(float progress, RingType ringType)
    {
        if (!isActive || meshRenderer == null) return;
        // 실패 모드 중이면 진행도 업데이트 무시
        if (isFailureMode) return;

        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(FillAmountID, 1f - progress); // 셰이더 반전: progress 0→1 = FillAmount 1→0
        meshRenderer.SetPropertyBlock(mpb);
    }

    private void HandleRingComplete(RingType ringType, RingInteractable target)
    {
        if (!isActive) return;

        // 완전히 채워진 상태로 세팅 후 페이드아웃
        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetFloat(FillAmountID, 0f); // 0 = 다 채워짐
        meshRenderer.SetPropertyBlock(mpb);

        StartFadeOut();
    }

    private void HandleChargeCancelled(RingType ringType)
    {
        if (!isActive) return;
        // 실패 모드 중이면 일반 취소 무시 (실패 연출이 직접 처리)
        if (isFailureMode) return;
        StartFadeOut();
    }

    // ─── 실패 연출 ───

    /// <summary>
    /// 현재 표시 중인 링 메시를 실패 연출로 전환합니다.
    /// 떨림 + 색상 전환 → 페이드아웃.
    /// FailedTreeInteractable에서 호출합니다.
    /// </summary>
    public void PlayFailure(Color failColor, float jitterDuration, float jitterIntensity,
                            float colorTransitionDuration = 0.3f, float failFadeOutDuration = 0.4f)
    {
        if (!isActive || meshInstance == null) return;

        isFailureMode = true;
        isFadingIn = false;
        isFadingOut = false;
        failureElapsed = 0f;

        failureJitterDuration = jitterDuration;
        failureJitterIntensity = jitterIntensity;
        failureColorTransitionDuration = colorTransitionDuration;
        failureFadeOutDuration = failFadeOutDuration;
        failureTargetColor = failColor;
        failureBasePosition = meshInstance.transform.position;

        // 현재 색상 저장
        meshRenderer.GetPropertyBlock(mpb);
        failureBaseColor = mpb.GetColor(GaugeColorID);
    }

    private void UpdateFailure()
    {
        failureElapsed += Time.deltaTime;

        if (failureElapsed <= failureJitterDuration)
        {
            // ── 떨림 + 색상 전환 단계 ──
            float t = Mathf.Clamp01(failureElapsed / failureJitterDuration);

            // 색상: 원래 → 빨간
            float colorT = Mathf.Clamp01(failureElapsed / failureColorTransitionDuration);
            Color currentColor = Color.Lerp(failureBaseColor, failureTargetColor, colorT);

            // 떨림 (점점 약해짐)
            float jitterScale = failureJitterIntensity * (1f - t);
            Vector3 jitterOffset = new Vector3(
                Random.Range(-jitterScale, jitterScale),
                Random.Range(-jitterScale, jitterScale),
                Random.Range(-jitterScale, jitterScale)
            );
            meshInstance.transform.position = failureBasePosition + jitterOffset;

            // 충전량 흔들림 (불안정)
            float currentFill = mpb.GetFloat(FillAmountID);
            float fillJitter = Random.Range(-0.02f, 0.02f) * (1f - t);

            meshRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(GaugeColorID, currentColor);
            mpb.SetFloat(FillAmountID, currentFill + fillJitter);
            meshRenderer.SetPropertyBlock(mpb);
        }
        else
        {
            // ── 페이드아웃 단계 ──
            float fadeElapsed = failureElapsed - failureJitterDuration;
            float fadeT = Mathf.Clamp01(fadeElapsed / failureFadeOutDuration);

            meshInstance.transform.position = failureBasePosition;
            meshInstance.transform.localScale = baseScale * (1f - fadeT);

            if (fadeT >= 1f)
            {
                // 실패 연출 완료
                isFailureMode = false;
                isActive = false;
                meshInstance.SetActive(false);
                meshInstance.transform.localScale = baseScale;
            }
        }
    }

    // ─── 페이드 ───

    private void StartFadeIn()
    {
        isFadingIn = true;
        isFadingOut = false;
        fadeProgress = 0f;
        meshInstance.transform.localScale = Vector3.zero;
    }

    private void StartFadeOut()
    {
        isFadingOut = true;
        isFadingIn = false;
        fadeProgress = 0f;
    }

    private void Update()
    {
        // 링 메시가 항상 카메라를 바라보도록
        if (isActive && meshInstance != null)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 lookDir = meshInstance.transform.position - cam.transform.position;
                if (lookDir.sqrMagnitude > 0.001f)
                    meshInstance.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }

        if (isFailureMode)
        {
            UpdateFailure();
            return;
        }

        if (isFadingIn)
        {
            fadeProgress += Time.deltaTime / fadeInDuration;
            if (fadeProgress >= 1f)
            {
                fadeProgress = 1f;
                isFadingIn = false;
            }
            float t = fadeProgress * fadeProgress * (3f - 2f * fadeProgress); // SmoothStep
            meshInstance.transform.localScale = baseScale * t;
        }
        else if (isFadingOut)
        {
            fadeProgress += Time.deltaTime / fadeOutDuration;
            if (fadeProgress >= 1f)
            {
                fadeProgress = 1f;
                isFadingOut = false;
                meshInstance.SetActive(false);
                isActive = false;
            }
            float t = 1f - fadeProgress;
            meshInstance.transform.localScale = baseScale * t;
        }
    }

    // ─── 정리 ───

    private void OnDestroy()
    {
        if (meshInstance != null)
            Destroy(meshInstance);
    }
}
