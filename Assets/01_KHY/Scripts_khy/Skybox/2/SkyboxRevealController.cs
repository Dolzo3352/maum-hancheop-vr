using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Reveal Sphere 방식 스카이박스 전환 컨트롤러
/// Renderer Feature 없이 동작 - Unity 6 URP + VR 호환
///
/// [씬 세팅]
/// 1. Hierarchy → 3D Object → Sphere 생성
///    이름: RevealSphere
///    Scale: (500, 500, 500)  ← 카메라보다 훨씬 크게
///    Layer: 카메라가 볼 수 있는 레이어
///
/// 2. RevealSphere에 Material 할당
///    BirdRevealSphere.shader로 만든 M_RevealSphere
///
/// 3. 이 스크립트가 있는 오브젝트 Inspector 설정:
///    - Black Skybox, Low Poly Skybox 할당
///    - Bird Transform 할당
///    - Reveal Sphere Transform 할당
///
/// 4. Signal Receiver:
///    RevealStartSignal → TriggerReveal()
///    RevealEndSignal   → TriggerClose()
/// </summary>
public class SkyboxRevealController : MonoBehaviour
{
    [Header("Skybox Materials")]
    public Material blackSkybox;
    public Material lowPolySkybox;

    [Header("Bird")]
    public Transform bird;

    [Header("Reveal Sphere")]
    [Tooltip("Scale 500짜리 뒤집힌 구체 Transform")]
    public Transform revealSphere;

    [Header("Timing")]
    [Range(1.0f, 6.0f)]
    public float revealDuration = 2.5f;

    [Range(1.0f, 6.0f)]
    public float closeDuration  = 2.0f;

    // ── Shader Property ID (캐싱) ─────────────────────────────────
    private static readonly int PropRevealProgress = Shader.PropertyToID("_RevealProgress");
    private static readonly int PropBirdDir        = Shader.PropertyToID("_BirdDir");
    private static readonly int PropEdgeSoftness   = Shader.PropertyToID("_RevealEdgeSoftness");

    private Material _sphereMaterial;   // RevealSphere의 Material 인스턴스
    private Vector3  _prevBirdPos;
    private bool     _tracking = false;
    private Coroutine _currentCoroutine;

    // ── Unity 생명주기 ────────────────────────────────────────────

    void Awake()
    {
        // 시작 상태: 검정 스카이박스 + 구체 완전 불투명(Progress=0)
        ApplySkybox(blackSkybox);

        if (revealSphere != null)
        {
            // Material 인스턴스 복사 (원본 에셋 보호)
            var renderer = revealSphere.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                _sphereMaterial = renderer.material; // 자동으로 인스턴스 복사됨
                _sphereMaterial.SetFloat(PropRevealProgress, 0f);
                _sphereMaterial.SetVector(PropBirdDir, Vector3.right);
            }
        }
        else
        {
            Debug.LogWarning("[SkyboxReveal] Reveal Sphere가 할당되지 않았습니다.");
        }
    }

    void Update()
    {
        if (!_tracking || bird == null || _sphereMaterial == null) return;

        Vector3 currentPos = bird.position;
        Vector3 delta      = currentPos - _prevBirdPos;

        if (delta.sqrMagnitude > 0.00001f)
        {
            // 새 이동 방향 → Material에 전달
            _sphereMaterial.SetVector(PropBirdDir, delta.normalized);
        }

        _prevBirdPos = currentPos;
    }

    // ── Timeline Signal 연결 함수 ─────────────────────────────────

    /// <summary>RevealStartSignal → 연결</summary>
    public void TriggerReveal()
    {
        if (bird == null || _sphereMaterial == null) return;

        _prevBirdPos = bird.position;
        _tracking    = true;

        if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
        _currentCoroutine = StartCoroutine(Animate(
            from: 0f, to: 1f,
            duration: revealDuration,
            isOpening: true
        ));

        Debug.Log("[SkyboxReveal] Reveal 시작");
    }

    /// <summary>RevealEndSignal → 연결</summary>
    public void TriggerClose()
    {
        if (_sphereMaterial == null) return;

        if (_currentCoroutine != null) StopCoroutine(_currentCoroutine);
        _currentCoroutine = StartCoroutine(Animate(
            from: 1f, to: 0f,
            duration: closeDuration,
            isOpening: false
        ));

        Debug.Log("[SkyboxReveal] Close 시작");
    }

    // ── 내부 코루틴 ──────────────────────────────────────────────

    private IEnumerator Animate(float from, float to, float duration, bool isOpening)
    {
        if (isOpening)
        {
            // 열릴 때 스카이박스 교체 (Progress=0이라 구체가 가리고 있어서 안 보임)
            ApplySkybox(lowPolySkybox);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t      = Mathf.Clamp01(elapsed / duration);
            float smooth = Mathf.SmoothStep(from, to, t);
            _sphereMaterial.SetFloat(PropRevealProgress, smooth);
            yield return null;
        }

        _sphereMaterial.SetFloat(PropRevealProgress, to);

        if (!isOpening)
        {
            ApplySkybox(blackSkybox);
            _tracking = false;
            Debug.Log("[SkyboxReveal] 검은 배경 복귀 완료");
        }
    }

    private void ApplySkybox(Material mat)
    {
        if (mat == null) return;
        RenderSettings.skybox = mat;
        DynamicGI.UpdateEnvironment();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (_sphereMaterial != null)
            _sphereMaterial.SetFloat(PropEdgeSoftness, 0.08f);
    }
#endif
}