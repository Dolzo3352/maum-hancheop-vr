using UnityEngine;

/// <summary>
/// 임시 오브젝트를 나중에 최적화된 오브젝트로 쉽게 교체하기 위한 컴포넌트.
/// 위치/회전/스케일을 유지한 채 메시만 교체할 수 있습니다.
/// </summary>
public class PlaceholderObject : MonoBehaviour
{
    public enum ObjectCategory
    {
        Tree,
        Rock,
        Grass,
        Water,
        Flower,
        Building,
        Landscape,
        Character,
        Prop
    }

    [Header("오브젝트 정보")]
    public ObjectCategory category;
    [Tooltip("나중에 교체할 때 식별용 (예: 감나무, 소나무, 초가집)")]
    public string objectName;
    [Tooltip("어떤 스테이지에 속하는지")]
    public int stageIndex;

    [Header("교체 설정")]
    [Tooltip("교체할 프리팹을 여기에 드래그하면 자동 교체됩니다")]
    public GameObject replacementPrefab;

    [Header("상태")]
    [SerializeField] private bool isPlaceholder = true;
    public bool IsPlaceholder => isPlaceholder;

    /// <summary>
    /// 프리팹으로 교체합니다. 위치/회전/스케일/부모를 유지합니다.
    /// </summary>
    public void MarkAsFinalized()
    {
        isPlaceholder = false;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!isPlaceholder) return;

        // placeholder 오브젝트는 와이어프레임 박스로 표시
        Gizmos.color = GetCategoryColor();
        Gizmos.matrix = transform.localToWorldMatrix;

        var filter = GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
        {
            Gizmos.DrawWireMesh(filter.sharedMesh);
        }
        else
        {
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }

    Color GetCategoryColor()
    {
        return category switch
        {
            ObjectCategory.Tree => new Color(0.2f, 0.8f, 0.2f, 0.5f),
            ObjectCategory.Rock => new Color(0.6f, 0.6f, 0.6f, 0.5f),
            ObjectCategory.Grass => new Color(0.5f, 0.9f, 0.3f, 0.5f),
            ObjectCategory.Water => new Color(0.2f, 0.5f, 0.9f, 0.5f),
            ObjectCategory.Flower => new Color(0.9f, 0.4f, 0.6f, 0.5f),
            ObjectCategory.Building => new Color(0.9f, 0.7f, 0.3f, 0.5f),
            ObjectCategory.Landscape => new Color(0.6f, 0.4f, 0.2f, 0.5f),
            ObjectCategory.Character => new Color(0.9f, 0.3f, 0.3f, 0.5f),
            ObjectCategory.Prop => new Color(0.7f, 0.7f, 0.9f, 0.5f),
            _ => Color.white
        };
    }
#endif
}
