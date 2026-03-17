using UnityEngine;

namespace DearBrave
{
    /// <summary>
    /// 플레이어(또는 XR Rig) 위치를 풀 셰이더에 전달하여
    /// 캐릭터가 지나가면 풀이 눕혀지도록 합니다.
    /// 뿌리 기준 회전이라 메쉬가 늘어나지 않습니다.
    /// </summary>
    public class GrassInteractor : MonoBehaviour
    {
        [Header("대상")]
        [Tooltip("풀이 반응할 대상 (비우면 이 오브젝트 자신)")]
        [SerializeField] Transform target;

        [Header("인터랙션 설정")]
        [Tooltip("풀이 밀리는 반경 (m)")]
        [SerializeField] float radius = 1.0f;

        [Tooltip("꺾이는 강도 (3~5: 풀숲 헤치기)")]
        [SerializeField] float strength = 4.0f;

        [Header("복구 설정")]
        [Tooltip("플레이어가 떠난 후 복구 속도 (높을수록 빠름)")]
        [SerializeField] float recoverySpeed = 5.0f;

        [Tooltip("복구 시 탄성 흔들림 (0=없음, 1=최대)")]
        [SerializeField] float bounciness = 0.3f;

        static readonly int PlayerPosID = Shader.PropertyToID("_GrassPlayerPos");
        static readonly int RadiusID = Shader.PropertyToID("_GrassInteractionRadius");
        static readonly int StrengthID = Shader.PropertyToID("_GrassInteractionStrength");

        // 부드러운 추적용
        Vector3 smoothedPos;
        Vector3 velocity;

        void Awake()
        {
            if (target == null)
                target = transform;
            smoothedPos = target.position;
        }

        void Update()
        {
            Vector3 targetPos = target.position;

            // SmoothDamp로 위치 추적 → 복구 효과
            // 플레이어에 가까울 때는 즉시 반응, 멀어지면 부드럽게 복구
            float smoothTime = 1f / Mathf.Max(recoverySpeed, 0.1f);
            smoothedPos = Vector3.SmoothDamp(smoothedPos, targetPos, ref velocity, smoothTime);

            // 탄성 효과: 복구 중일 때 약간의 오버슈트
            Vector3 finalPos = smoothedPos;
            if (bounciness > 0f)
            {
                Vector3 overshoot = velocity * bounciness * 0.1f;
                finalPos += overshoot;
            }

            Shader.SetGlobalVector(PlayerPosID, new Vector4(finalPos.x, finalPos.y, finalPos.z, 0f));
            Shader.SetGlobalFloat(RadiusID, radius);
            Shader.SetGlobalFloat(StrengthID, strength);
        }

        void OnDisable()
        {
            Shader.SetGlobalVector(PlayerPosID, new Vector4(9999f, 9999f, 9999f, 0f));
        }
    }
}
