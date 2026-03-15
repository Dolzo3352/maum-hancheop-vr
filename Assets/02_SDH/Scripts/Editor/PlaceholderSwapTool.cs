using UnityEngine;
using UnityEditor;
using System.Linq;

/// <summary>
/// PlaceholderObject 교체를 위한 에디터 도구.
/// Inspector에서 바로 교체하거나, 메뉴에서 일괄 교체할 수 있습니다.
/// </summary>
[CustomEditor(typeof(PlaceholderObject))]
public class PlaceholderSwapTool : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var placeholder = (PlaceholderObject)target;

        EditorGUILayout.Space(10);

        // 교체 버튼
        if (placeholder.replacementPrefab != null && placeholder.IsPlaceholder)
        {
            EditorGUILayout.HelpBox(
                $"'{placeholder.replacementPrefab.name}'(으)로 교체할 준비가 되었습니다.\n위치/회전/스케일이 유지됩니다.",
                MessageType.Info);

            if (GUILayout.Button("이 오브젝트 교체", GUILayout.Height(30)))
            {
                SwapObject(placeholder);
            }
        }

        if (!placeholder.IsPlaceholder)
        {
            EditorGUILayout.HelpBox("최종 오브젝트입니다.", MessageType.None);
        }
    }

    static void SwapObject(PlaceholderObject placeholder)
    {
        Undo.SetCurrentGroupName("Placeholder Swap");
        int group = Undo.GetCurrentGroup();

        var go = placeholder.gameObject;
        var parent = go.transform.parent;
        var pos = go.transform.localPosition;
        var rot = go.transform.localRotation;
        var scale = go.transform.localScale;
        var siblingIndex = go.transform.GetSiblingIndex();
        var name = go.name;
        var stageIndex = placeholder.stageIndex;
        var category = placeholder.category;
        var objectName = placeholder.objectName;

        // 새 오브젝트 생성
        var newGo = (GameObject)PrefabUtility.InstantiatePrefab(placeholder.replacementPrefab);
        Undo.RegisterCreatedObjectUndo(newGo, "Create replacement");

        newGo.transform.SetParent(parent);
        newGo.transform.localPosition = pos;
        newGo.transform.localRotation = rot;
        newGo.transform.localScale = scale;
        newGo.transform.SetSiblingIndex(siblingIndex);
        newGo.name = name;

        // PlaceholderObject 컴포넌트 추가 (finalized 상태로)
        var newPlaceholder = newGo.AddComponent<PlaceholderObject>();
        newPlaceholder.category = category;
        newPlaceholder.objectName = objectName;
        newPlaceholder.stageIndex = stageIndex;
        newPlaceholder.MarkAsFinalized();

        // 기존 오브젝트 삭제
        Undo.DestroyObjectImmediate(go);
        Undo.CollapseUndoOperations(group);

        Selection.activeGameObject = newGo;
        Debug.Log($"[PlaceholderSwap] '{name}' 교체 완료");
    }

    // ===== 메뉴 도구 =====

    [MenuItem("Dear Brave/Placeholder/선택된 오브젝트 일괄 교체")]
    static void SwapSelected()
    {
        var placeholders = Selection.gameObjects
            .Select(go => go.GetComponent<PlaceholderObject>())
            .Where(p => p != null && p.IsPlaceholder && p.replacementPrefab != null)
            .ToArray();

        if (placeholders.Length == 0)
        {
            Debug.LogWarning("[PlaceholderSwap] 교체 가능한 Placeholder가 선택되지 않았습니다.");
            return;
        }

        foreach (var p in placeholders)
            SwapObject(p);

        Debug.Log($"[PlaceholderSwap] {placeholders.Length}개 오브젝트 교체 완료");
    }

    [MenuItem("Dear Brave/Placeholder/씬 전체 Placeholder 현황")]
    static void ShowPlaceholderStats()
    {
        var all = Object.FindObjectsByType<PlaceholderObject>(FindObjectsSortMode.None);
        var placeholders = all.Where(p => p.IsPlaceholder).ToArray();
        var finalized = all.Where(p => !p.IsPlaceholder).ToArray();

        var byCategory = placeholders.GroupBy(p => p.category);
        var byStage = placeholders.GroupBy(p => p.stageIndex);

        string msg = $"=== Placeholder 현황 ===\n";
        msg += $"총: {all.Length}개 (임시: {placeholders.Length} / 완료: {finalized.Length})\n\n";

        msg += "--- 카테고리별 ---\n";
        foreach (var g in byCategory)
            msg += $"  {g.Key}: {g.Count()}개\n";

        msg += "\n--- 스테이지별 ---\n";
        foreach (var g in byStage.OrderBy(g => g.Key))
            msg += $"  Stage {g.Key:D2}: {g.Count()}개\n";

        Debug.Log(msg);
    }

    [MenuItem("Dear Brave/Placeholder/교체 준비된 것만 선택")]
    static void SelectReady()
    {
        var ready = Object.FindObjectsByType<PlaceholderObject>(FindObjectsSortMode.None)
            .Where(p => p.IsPlaceholder && p.replacementPrefab != null)
            .Select(p => p.gameObject)
            .ToArray();

        Selection.objects = ready;
        Debug.Log($"[PlaceholderSwap] 교체 준비된 {ready.Length}개 선택됨");
    }
}
