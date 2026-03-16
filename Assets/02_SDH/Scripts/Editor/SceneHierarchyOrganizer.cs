using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Scene_SDH 계층 구조 정리 도구.
/// 메뉴: Dear Brave > Scene 정리 > 계층 구조 정리 실행
///
/// 정리 규칙:
///   ── Environment (자연물)
///   │   ├── Trees
///   │   ├── Rocks
///   │   ├── Grass
///   │   └── Water
///   ── Buildings (건물)
///   ── Landscape (지형 - Terrain, 논밭, 산)
///   ── Stages (스테이지별 묶음)
///   │   ├── Stage_01_마을
///   │   ├── Stage_02_아이집
///   │   ...
///   ── Lighting (라이트 통합)
///   ── Characters (캐릭터)
///   ── System (Manager, XR, EventSystem 등)
///   ── _Unused (미사용/정리 대상)
/// </summary>
public class SceneHierarchyOrganizer : EditorWindow
{
    private bool dryRun = true;
    private Vector2 scrollPos;
    private List<string> log = new List<string>();

    [MenuItem("Dear Brave/Scene 정리/계층 구조 정리 윈도우")]
    public static void ShowWindow()
    {
        GetWindow<SceneHierarchyOrganizer>("Scene 정리").minSize = new Vector2(400, 500);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Scene Hierarchy Organizer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "현재 씬의 루트 오브젝트들을 카테고리별로 정리합니다.\n" +
            "Undo 지원 (Ctrl+Z로 되돌리기 가능)",
            MessageType.Info);

        EditorGUILayout.Space(5);
        dryRun = EditorGUILayout.Toggle("미리보기만 (이동 안 함)", dryRun);

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("1. 현재 루트 오브젝트 분석", GUILayout.Height(30)))
        {
            AnalyzeRootObjects();
        }
        if (GUILayout.Button("2. 정리 실행", GUILayout.Height(30)))
        {
            if (dryRun)
                OrganizeHierarchy(true);
            else if (EditorUtility.DisplayDialog("확인",
                "씬 계층 구조를 정리합니다.\nUndo로 되돌릴 수 있습니다.", "실행", "취소"))
                OrganizeHierarchy(false);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        if (GUILayout.Button("중복 스테이지 통합 (--Stage-- → --0311_Stage--)"))
        {
            if (EditorUtility.DisplayDialog("확인",
                "--Stage-- 하위 오브젝트를 --0311_Stage-- 대응 스테이지로 이동합니다.", "실행", "취소"))
                MergeStages();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("로그", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var line in log)
        {
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
        }
        EditorGUILayout.EndScrollView();
    }

    void Log(string msg)
    {
        log.Add(msg);
        Debug.Log($"[SceneOrganizer] {msg}");
    }

    void AnalyzeRootObjects()
    {
        log.Clear();
        var roots = GetRootObjects();

        Log($"=== 루트 오브젝트 분석 ({roots.Count}개) ===\n");

        var categories = new Dictionary<string, List<string>>
        {
            { "스테이지", new List<string>() },
            { "환경(자연물)", new List<string>() },
            { "건물", new List<string>() },
            { "지형", new List<string>() },
            { "라이트", new List<string>() },
            { "캐릭터", new List<string>() },
            { "시스템", new List<string>() },
            { "기타/정리필요", new List<string>() },
        };

        foreach (var go in roots)
        {
            string cat = CategorizeObject(go);
            string childCount = go.transform.childCount > 0 ? $" ({go.transform.childCount} children)" : "";
            categories[cat].Add($"  {go.name}{childCount}");
        }

        foreach (var kvp in categories)
        {
            if (kvp.Value.Count > 0)
            {
                Log($"\n[{kvp.Key}] - {kvp.Value.Count}개");
                foreach (var name in kvp.Value)
                    Log(name);
            }
        }
    }

    void OrganizeHierarchy(bool preview)
    {
        log.Clear();
        if (!preview)
        {
            Undo.SetCurrentGroupName("Scene Hierarchy Organize");
        }

        var roots = GetRootObjects();

        // 카테고리별 부모 오브젝트 생성/찾기
        var parentMap = new Dictionary<string, string>
        {
            { "스테이지", "── Stages" },
            { "환경(자연물)", "── Environment" },
            { "건물", "── Buildings" },
            { "지형", "── Landscape" },
            { "라이트", "── Lighting" },
            { "캐릭터", "── Characters" },
            { "시스템", "── System" },
            { "기타/정리필요", "── _Unused" },
        };

        // 이미 정리용 부모인 것들은 건너뛰기
        var parentNames = new HashSet<string>(parentMap.Values);

        foreach (var go in roots)
        {
            if (parentNames.Contains(go.name)) continue;

            string cat = CategorizeObject(go);
            string parentName = parentMap[cat];

            if (preview)
            {
                Log($"[미리보기] '{go.name}' → {parentName}");
            }
            else
            {
                var parent = FindOrCreateParent(parentName);
                Undo.SetTransformParent(go.transform, parent.transform, "Move to category");
                Log($"이동: '{go.name}' → {parentName}");
            }
        }

        if (!preview)
        {
            // 순서 정렬
            var desiredOrder = new[] {
                "── Stages", "── Environment", "── Buildings",
                "── Landscape", "── Lighting", "── Characters",
                "── System", "── _Unused"
            };
            for (int i = 0; i < desiredOrder.Length; i++)
            {
                var obj = GameObject.Find(desiredOrder[i]);
                if (obj != null && obj.transform.parent == null)
                    obj.transform.SetSiblingIndex(i);
            }

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
            Log("\n✓ 정리 완료 (Ctrl+Z로 되돌리기 가능)");
        }
        else
        {
            Log("\n(미리보기 모드 - 실제 이동 없음)");
        }
    }

    string CategorizeObject(GameObject go)
    {
        string name = go.name.ToLower();

        // 스테이지 관련
        if (name.Contains("stage") || name.Contains("s_") ||
            name.StartsWith("#") || name.StartsWith("--0311") ||
            name.Contains("골목") || name.Contains("큰길") ||
            name.Contains("약방") || name.Contains("천계") ||
            name.Contains("지하") || name.Contains("아이집") ||
            name.Contains("마을->") || name.Contains("-> 마을") ||
            name.Contains("-> 아이집"))
            return "스테이지";

        // 라이트
        if (name.Contains("light") || name.Contains("--light"))
            return "라이트";

        // 시스템
        if (name.Contains("manager") || name.Contains("--setting") ||
            name.Contains("eventsystem") || name.Contains("xr") ||
            name.Contains("timeline") || name.Contains("waypoint") ||
            name.Contains("narration") || name.Contains("distance") ||
            name.Contains("teleport") || name.Contains("spline") ||
            name.Contains("----"))
            return "시스템";

        // 지형
        if (name.Contains("terrain") || name.Contains("논") ||
            name.Contains("밭") || name.Contains("산") ||
            name.Contains("plane") || name.Contains("마을_"))
            return "지형";

        // 캐릭터
        if (name.Contains("character"))
            return "캐릭터";

        // 건물
        if (name.Contains("house") || name.Contains("집") ||
            name.Contains("버스정류장") || name.Contains("villa"))
            return "건물";

        // 환경 - 자연물
        if (name.Contains("tree") || name.Contains("rock") ||
            name.Contains("grass") || name.Contains("flower") ||
            name.Contains("숲") || name.Contains("감나무"))
            return "환경(자연물)";

        // ProBuilder 메시 → 기타
        if (name.Contains("pb_mesh") || name.Contains("polyshape"))
            return "기타/정리필요";

        // 이름 없는 Cube 등 → 기타
        if (name.StartsWith("cube") || name.StartsWith("floor") ||
            name.StartsWith("wall"))
            return "기타/정리필요";

        return "기타/정리필요";
    }

    void MergeStages()
    {
        log.Clear();

        var oldStage = GameObject.Find("--Stage--");
        var newStage = GameObject.Find("--0311_Stage--");

        if (oldStage == null || newStage == null)
        {
            Log("--Stage-- 또는 --0311_Stage-- 를 찾을 수 없습니다.");
            return;
        }

        Undo.SetCurrentGroupName("Merge Stages");

        // --Stage-- 하위를 _Old_Stage로 이름 변경 후 --0311_Stage-- 하위로 이동
        Undo.RecordObject(oldStage, "Rename old stage");
        oldStage.name = "_Old_Stage (참고용)";
        Undo.SetTransformParent(oldStage.transform, newStage.transform, "Move old stage");

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        Log("--Stage-- → --0311_Stage--/_Old_Stage (참고용) 으로 이동 완료");
        Log("내용 확인 후 불필요하면 삭제하세요.");
    }

    List<GameObject> GetRootObjects()
    {
        var scene = SceneManager.GetActiveScene();
        var roots = new List<GameObject>();
        scene.GetRootGameObjects(roots);
        return roots;
    }

    GameObject FindOrCreateParent(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null && existing.transform.parent == null)
            return existing;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.transform.position = Vector3.zero;
        return go;
    }
}
