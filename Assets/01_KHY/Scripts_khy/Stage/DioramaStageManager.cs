using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stage 활성화/비활성화를 관리하는 최상위 컨트롤러.
///
/// 씬 구조:
///   DioramaRoot (이 컴포넌트 부착)
///     ├── Stage_01_집앞      ← stages[0]
///     ├── Stage_02_골목길     ← stages[1]
///     ├── ...
///     └── Stage_09_집도착     ← stages[8]
///
/// 사용법:
///   NarrativeSequencer 등 상위 시스템에서
///   ActivateStage(index)를 호출하여 Stage를 전환합니다.
/// </summary>
public class DioramaStageManager : MonoBehaviour
{
    // ─── 필드 ───

    [Header("디오라마 구조")]
    [Tooltip("디오라마 최상위 Transform. 스케일이 여기에 적용됩니다.")]
    [SerializeField] private Transform dioramaRoot;

    [Tooltip("Stage 오브젝트 목록. Inspector에서 순서대로 등록합니다.")]
    [SerializeField] private List<GameObject> stages = new List<GameObject>();

    [Header("스케일")]
    [Tooltip("기본 스케일 (1.0 = 원본)")]
    [SerializeField] private float defaultScale = 1f;

    [Header("디오라마 위치")]
    [Tooltip("디오라마 중심 월드 좌표 (VR 시점 기준점)")]
    [SerializeField] private Vector3 stageCenter;

    // 현재 활성 Stage 인덱스. -1이면 아무것도 켜져 있지 않음.
    private int activeStageIndex = -1;

    // ─── 프로퍼티 ───

    /// <summary>현재 활성 Stage 인덱스. -1이면 비활성 상태.</summary>
    public int ActiveStageIndex => activeStageIndex;

    /// <summary>등록된 Stage 개수.</summary>
    public int StageCount => stages.Count;

    // ─── 이벤트 ───

    /// <summary>Stage 활성화 직후 발행. (활성화된 인덱스)</summary>
    public event Action<int> OnStageActivated;

    /// <summary>전체 비활성화 직후 발행.</summary>
    public event Action OnAllStagesDeactivated;

    // ─── 초기화 ───

    private void Awake()
    {
        if (dioramaRoot == null)
            dioramaRoot = transform;
    }

    private void Start()
    {
        // 시작 시 전부 끄고 첫 번째 Stage만 활성화
        if (stages.Count > 0)
            ActivateStage(0);
    }

    // ─── 공개 메서드 ───

    /// <summary>
    /// 해당 인덱스의 Stage만 활성화하고 나머지는 비활성화합니다.
    /// </summary>
    public void ActivateStage(int index)
    {
        if (!IsValidIndex(index))
        {
            Debug.LogWarning($"[DioramaStageManager] 잘못된 Stage 인덱스: {index} (총 {stages.Count}개)", this);
            return;
        }

        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i] != null)
                stages[i].SetActive(i == index);
        }

        activeStageIndex = index;
        OnStageActivated?.Invoke(index);

        Debug.Log($"[DioramaStageManager] Stage {index} 활성화: {stages[index].name}", this);
    }

    /// <summary>
    /// 모든 Stage를 비활성화합니다. Stage 전환 중 암전 상태에서 사용합니다.
    /// </summary>
    public void DeactivateAllStages()
    {
        for (int i = 0; i < stages.Count; i++)
        {
            if (stages[i] != null)
                stages[i].SetActive(false);
        }

        activeStageIndex = -1;
        OnAllStagesDeactivated?.Invoke();

        Debug.Log("[DioramaStageManager] 전체 Stage 비활성화", this);
    }

    /// <summary>
    /// 디오라마 루트를 기본 스케일로 복귀합니다.
    /// </summary>
    public void ResetToDefaultScale()
    {
        if (dioramaRoot == null) return;

        var origin = dioramaRoot.GetComponent<DioramaScaleOrigin>();
        if (origin != null)
        {
            dioramaRoot.localScale = origin.baseScale * defaultScale;
        }
        else
        {
            dioramaRoot.localScale = Vector3.one * defaultScale;
        }
    }

    /// <summary>
    /// 현재 디오라마 스케일을 반환합니다.
    /// DioramaScaleOrigin이 있으면 baseScale 기준 배율, 없으면 localScale.x를 반환합니다.
    /// </summary>
    public float GetCurrentScale()
    {
        if (dioramaRoot == null) return 1f;

        var origin = dioramaRoot.GetComponent<DioramaScaleOrigin>();
        if (origin != null && origin.baseScale.x != 0f)
            return dioramaRoot.localScale.x / origin.baseScale.x;

        return dioramaRoot.localScale.x;
    }

    /// <summary>
    /// 인덱스로 Stage GameObject를 반환합니다.
    /// StageData.stageIndex로 씬 오브젝트에 접근할 때 사용합니다.
    /// </summary>
    public GameObject GetStage(int index)
    {
        if (!IsValidIndex(index)) return null;
        return stages[index];
    }

    /// <summary>
    /// 디오라마 중심 좌표를 반환합니다.
    /// </summary>
    public Vector3 GetStageCenter() => stageCenter;

    // ─── 내부 ───

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < stages.Count;
    }
}
