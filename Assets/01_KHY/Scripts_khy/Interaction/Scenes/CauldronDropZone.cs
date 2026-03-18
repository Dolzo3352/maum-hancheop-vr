using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 가마솥 약재 투입 영역.
///
/// 가마솥 위에 트리거 콜라이더를 배치하여,
/// GrabIngredient가 이 영역 안에서 놓이면 투입으로 판정합니다.
///
/// 사용법:
///   가마솥 위에 빈 오브젝트 생성 → BoxCollider(isTrigger) + 이 컴포넌트 부착.
///   requiredIngredients에 필요한 약재들을 등록합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class CauldronDropZone : MonoBehaviour
{
    [Header("약재 목록")]
    [Tooltip("가마솥에 넣어야 할 약재들")]
    [SerializeField] private GrabIngredient[] requiredIngredients;

    [Header("투입 VFX")]
    [Tooltip("약재 투입 시 파티클 (선택)")]
    [SerializeField] private ParticleSystem insertionParticle;

    [Tooltip("투입 시 파티클 소환 위치 (비어있으면 가마솥 중앙)")]
    [SerializeField] private Transform vfxSpawnPoint;

    // 상태
    private int insertedCount;

    /// <summary>모든 약재가 투입되었을 때</summary>
    public event Action OnAllIngredientsInserted;

    /// <summary>약재 하나가 투입되었을 때 (현재 투입 수, 전체 수)</summary>
    public event Action<int, int> OnIngredientInserted;

    public int InsertedCount => insertedCount;
    public int TotalCount => requiredIngredients != null ? requiredIngredients.Length : 0;
    public bool AllInserted => insertedCount >= TotalCount;

    private void Awake()
    {
        // 콜라이더가 트리거인지 확인
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"[CauldronDropZone] Collider가 Trigger가 아닙니다. 자동으로 설정합니다.", this);
            col.isTrigger = true;
        }

        // 약재 투입 이벤트 구독
        if (requiredIngredients != null)
        {
            foreach (var ingredient in requiredIngredients)
            {
                if (ingredient != null)
                    ingredient.OnInserted += OnIngredientInsertedCallback;
            }
        }
    }

    private void OnDestroy()
    {
        if (requiredIngredients != null)
        {
            foreach (var ingredient in requiredIngredients)
            {
                if (ingredient != null)
                    ingredient.OnInserted -= OnIngredientInsertedCallback;
            }
        }
    }

    // ─── 트리거 판정 ───

    private void OnTriggerEnter(Collider other)
    {
        var ingredient = other.GetComponentInParent<GrabIngredient>();
        if (ingredient != null && !ingredient.IsInserted)
        {
            ingredient.SetInsideZone(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var ingredient = other.GetComponentInParent<GrabIngredient>();
        if (ingredient != null && !ingredient.IsInserted)
        {
            ingredient.SetInsideZone(false);
        }
    }

    // ─── 투입 콜백 ───

    private void OnIngredientInsertedCallback()
    {
        insertedCount++;

        Debug.Log($"[CauldronDropZone] 약재 투입 [{insertedCount}/{TotalCount}]", this);

        // 투입 VFX
        if (insertionParticle != null)
        {
            if (vfxSpawnPoint != null)
                insertionParticle.transform.position = vfxSpawnPoint.position;

            insertionParticle.Play();
        }

        OnIngredientInserted?.Invoke(insertedCount, TotalCount);

        // 모든 약재 투입 확인
        if (AllInserted)
        {
            Debug.Log($"[CauldronDropZone] 모든 약재 투입 완료!", this);
            OnAllIngredientsInserted?.Invoke();
        }
    }

    // ─── 외부 제어 ───

    /// <summary>상태 초기화</summary>
    public void ResetZone()
    {
        insertedCount = 0;

        if (requiredIngredients != null)
        {
            foreach (var ingredient in requiredIngredients)
            {
                if (ingredient != null)
                    ingredient.ResetIngredient();
            }
        }
    }

    /// <summary>등록된 약재인지 확인</summary>
    public bool IsRegisteredIngredient(GrabIngredient ingredient)
    {
        if (requiredIngredients == null) return false;
        foreach (var req in requiredIngredients)
        {
            if (req == ingredient) return true;
        }
        return false;
    }
}
