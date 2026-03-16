using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 식생 배치 에디터 도구.
/// 메뉴: Dear Brave > 레벨 디자인 > 식생 배치 도구
///
/// 기능:
///   - 지정 영역(BoxCollider 또는 수동 범위) 내에 프리팹 산포 배치
///   - 레이어별 배치 (나무, 억새 등)
///   - Raycast로 지형 표면에 맞춤
///   - PlaceholderObject 컴포넌트 자동 추가 (교체 용이)
///   - Undo 지원
/// </summary>
public class VegetationPlacerWindow : EditorWindow
{
    // ── 배치 레이어 설정 ──
    [System.Serializable]
    public class VegetationLayer
    {
        public string name = "새 레이어";
        public GameObject[] prefabs = new GameObject[0];
        public PlaceholderObject.ObjectCategory category = PlaceholderObject.ObjectCategory.Tree;
        public int count = 50;
        public float minScale = 0.8f;
        public float maxScale = 1.2f;
        public float minSpacing = 2f;
        public bool randomRotationY = true;
        public bool alignToSurface = false;
        public bool foldout = true;
    }

    // ── 상태 ──
    private Transform targetParent;
    private Vector3 areaCenter = Vector3.zero;
    private Vector3 areaSize = new Vector3(100, 50, 100);
    private LayerMask groundLayer = ~0;
    private float raycastHeight = 200f;
    private int seed = 42;

    private List<VegetationLayer> layers = new List<VegetationLayer>();
    private Vector2 scrollPos;
    private bool showAreaGizmo = true;

    // ── 프리셋 ──
    private enum Preset { None, TreeForest, SilverGrassField, Mixed }
    private Preset selectedPreset = Preset.None;

    [MenuItem("Dear Brave/레벨 디자인/식생 배치 도구")]
    public static void ShowWindow()
    {
        var w = GetWindow<VegetationPlacerWindow>("식생 배치");
        w.minSize = new Vector2(420, 600);
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        if (layers.Count == 0)
            AddDefaultLayers();
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void AddDefaultLayers()
    {
        layers.Add(new VegetationLayer
        {
            name = "나무 (Tree)",
            category = PlaceholderObject.ObjectCategory.Tree,
            count = 30,
            minScale = 0.7f,
            maxScale = 1.3f,
            minSpacing = 5f
        });
        layers.Add(new VegetationLayer
        {
            name = "억새 (Silver Grass)",
            category = PlaceholderObject.ObjectCategory.Grass,
            count = 200,
            minScale = 0.6f,
            maxScale = 1.0f,
            minSpacing = 1f
        });
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("식생 배치 도구", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "영역을 지정하고 프리팹을 설정한 뒤 '배치 실행'을 누르세요.\n" +
            "PlaceholderObject가 자동 추가되어 나중에 프리팹 교체가 쉽습니다.\n" +
            "Undo 지원 (Ctrl+Z)",
            MessageType.Info);

        EditorGUILayout.Space(5);

        // ── 영역 설정 ──
        EditorGUILayout.LabelField("배치 영역", EditorStyles.boldLabel);
        targetParent = (Transform)EditorGUILayout.ObjectField("부모 오브젝트", targetParent, typeof(Transform), true);
        areaCenter = EditorGUILayout.Vector3Field("영역 중심", areaCenter);
        areaSize = EditorGUILayout.Vector3Field("영역 크기 (XYZ)", areaSize);
        showAreaGizmo = EditorGUILayout.Toggle("Scene에 영역 표시", showAreaGizmo);

        if (targetParent != null && GUILayout.Button("부모 위치로 영역 중심 맞추기"))
        {
            areaCenter = targetParent.position;
        }

        EditorGUILayout.Space(5);

        // ── Raycast 설정 ──
        EditorGUILayout.LabelField("Raycast 설정", EditorStyles.boldLabel);
        groundLayer = EditorGUILayout.MaskField("지면 레이어", groundLayer, UnityEditorInternal.InternalEditorUtility.layers);
        raycastHeight = EditorGUILayout.FloatField("Raycast 높이", raycastHeight);

        EditorGUILayout.Space(5);

        // ── 시드 ──
        seed = EditorGUILayout.IntField("랜덤 시드", seed);

        EditorGUILayout.Space(10);

        // ── 레이어 목록 ──
        EditorGUILayout.LabelField("배치 레이어", EditorStyles.boldLabel);

        for (int i = 0; i < layers.Count; i++)
        {
            DrawLayerGUI(i);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ 레이어 추가"))
        {
            layers.Add(new VegetationLayer());
        }
        if (layers.Count > 0 && GUILayout.Button("- 마지막 레이어 삭제"))
        {
            layers.RemoveAt(layers.Count - 1);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(15);

        // ── 실행 버튼 ──
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("배치 실행", GUILayout.Height(40)))
        {
            ExecutePlacement();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.9f, 0.6f, 0.3f);
        if (GUILayout.Button("부모 하위의 Placeholder만 전부 삭제", GUILayout.Height(25)))
        {
            if (targetParent == null)
            {
                EditorUtility.DisplayDialog("오류", "부모 오브젝트를 지정하세요.", "확인");
            }
            else if (EditorUtility.DisplayDialog("확인",
                $"'{targetParent.name}' 하위의 PlaceholderObject를 모두 삭제합니다.", "삭제", "취소"))
            {
                ClearPlaceholders();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    void DrawLayerGUI(int index)
    {
        var layer = layers[index];

        layer.foldout = EditorGUILayout.Foldout(layer.foldout, $"[{index}] {layer.name}", true, EditorStyles.foldoutHeader);
        if (!layer.foldout) return;

        EditorGUI.indentLevel++;

        layer.name = EditorGUILayout.TextField("레이어 이름", layer.name);
        layer.category = (PlaceholderObject.ObjectCategory)EditorGUILayout.EnumPopup("카테고리", layer.category);
        layer.count = EditorGUILayout.IntSlider("배치 수", layer.count, 1, 2000);
        layer.minSpacing = EditorGUILayout.FloatField("최소 간격", layer.minSpacing);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("스케일 범위", GUILayout.Width(EditorGUIUtility.labelWidth));
        layer.minScale = EditorGUILayout.FloatField(layer.minScale, GUILayout.Width(50));
        EditorGUILayout.LabelField("~", GUILayout.Width(15));
        layer.maxScale = EditorGUILayout.FloatField(layer.maxScale, GUILayout.Width(50));
        EditorGUILayout.EndHorizontal();

        layer.randomRotationY = EditorGUILayout.Toggle("Y축 랜덤 회전", layer.randomRotationY);
        layer.alignToSurface = EditorGUILayout.Toggle("표면 법선 정렬", layer.alignToSurface);

        // 프리팹 배열
        EditorGUILayout.LabelField("프리팹 목록");
        int newSize = EditorGUILayout.IntField("  개수", layer.prefabs.Length);
        if (newSize != layer.prefabs.Length)
        {
            var old = layer.prefabs;
            layer.prefabs = new GameObject[newSize];
            for (int i = 0; i < Mathf.Min(old.Length, newSize); i++)
                layer.prefabs[i] = old[i];
        }
        for (int i = 0; i < layer.prefabs.Length; i++)
        {
            layer.prefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                $"  [{i}]", layer.prefabs[i], typeof(GameObject), false);
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(5);
    }

    void ExecutePlacement()
    {
        if (targetParent == null)
        {
            EditorUtility.DisplayDialog("오류", "부모 오브젝트를 지정하세요.", "확인");
            return;
        }

        bool anyPrefab = false;
        foreach (var layer in layers)
        {
            foreach (var p in layer.prefabs)
            {
                if (p != null) { anyPrefab = true; break; }
            }
            if (anyPrefab) break;
        }
        if (!anyPrefab)
        {
            EditorUtility.DisplayDialog("오류", "최소 하나의 프리팹을 지정하세요.", "확인");
            return;
        }

        Undo.SetCurrentGroupName("Vegetation Placement");
        int undoGroup = Undo.GetCurrentGroup();
        int totalPlaced = 0;

        Random.State oldState = Random.state;
        Random.InitState(seed);

        foreach (var layer in layers)
        {
            var validPrefabs = GetValidPrefabs(layer.prefabs);
            if (validPrefabs.Length == 0) continue;

            // 레이어별 컨테이너
            var container = new GameObject($"__{layer.name}");
            Undo.RegisterCreatedObjectUndo(container, "Create layer container");
            container.transform.SetParent(targetParent);
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;

            var placed = new List<Vector3>();
            int attempts = 0;
            int maxAttempts = layer.count * 10;

            while (placed.Count < layer.count && attempts < maxAttempts)
            {
                attempts++;

                float x = areaCenter.x + Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f);
                float z = areaCenter.z + Random.Range(-areaSize.z * 0.5f, areaSize.z * 0.5f);

                // 최소 간격 체크
                Vector3 candidate = new Vector3(x, 0, z);
                bool tooClose = false;
                foreach (var p in placed)
                {
                    if (Vector2.Distance(new Vector2(x, z), new Vector2(p.x, p.z)) < layer.minSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                // Raycast로 지면 찾기
                Vector3 origin = new Vector3(x, areaCenter.y + raycastHeight, z);
                Vector3 finalPos;
                Quaternion finalRot = Quaternion.identity;

                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayer))
                {
                    finalPos = hit.point;
                    if (layer.alignToSurface)
                        finalRot = Quaternion.FromToRotation(Vector3.up, hit.normal);

                    // 높이 범위 체크
                    if (finalPos.y < areaCenter.y - areaSize.y * 0.5f ||
                        finalPos.y > areaCenter.y + areaSize.y * 0.5f)
                        continue;
                }
                else
                {
                    // Raycast 실패 시 영역 중심 높이에 배치
                    finalPos = new Vector3(x, areaCenter.y, z);
                }

                // Y축 랜덤 회전
                if (layer.randomRotationY)
                    finalRot *= Quaternion.Euler(0, Random.Range(0f, 360f), 0);

                // 랜덤 스케일
                float scale = Random.Range(layer.minScale, layer.maxScale);

                // 프리팹 선택
                var prefab = validPrefabs[Random.Range(0, validPrefabs.Length)];

                // 인스턴스 생성
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(instance, "Place vegetation");

                instance.transform.SetParent(container.transform);
                instance.transform.position = finalPos;
                instance.transform.rotation = finalRot;
                instance.transform.localScale = Vector3.one * scale;
                instance.name = $"{prefab.name}_{placed.Count:D3}";

                // PlaceholderObject 추가
                var placeholder = instance.AddComponent<PlaceholderObject>();
                placeholder.category = layer.category;
                placeholder.objectName = prefab.name;

                placed.Add(finalPos);
                totalPlaced++;
            }

            Debug.Log($"[VegetationPlacer] '{layer.name}': {placed.Count}/{layer.count} 배치 (시도: {attempts})");
        }

        Random.state = oldState;
        Undo.CollapseUndoOperations(undoGroup);

        Debug.Log($"[VegetationPlacer] 총 {totalPlaced}개 배치 완료 (시드: {seed})");
        EditorUtility.DisplayDialog("완료", $"총 {totalPlaced}개 오브젝트를 배치했습니다.\nCtrl+Z로 되돌릴 수 있습니다.", "확인");
    }

    void ClearPlaceholders()
    {
        Undo.SetCurrentGroupName("Clear Placeholders");
        int undoGroup = Undo.GetCurrentGroup();
        int count = 0;

        var placeholders = targetParent.GetComponentsInChildren<PlaceholderObject>(true);
        foreach (var p in placeholders)
        {
            if (p.IsPlaceholder)
            {
                Undo.DestroyObjectImmediate(p.gameObject);
                count++;
            }
        }

        // 빈 컨테이너 정리
        for (int i = targetParent.childCount - 1; i >= 0; i--)
        {
            var child = targetParent.GetChild(i);
            if (child.name.StartsWith("__") && child.childCount == 0)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"[VegetationPlacer] {count}개 Placeholder 삭제");
    }

    GameObject[] GetValidPrefabs(GameObject[] prefabs)
    {
        var valid = new List<GameObject>();
        foreach (var p in prefabs)
            if (p != null) valid.Add(p);
        return valid.ToArray();
    }

    // ── Scene View 영역 표시 ──
    void OnSceneGUI(SceneView sceneView)
    {
        if (!showAreaGizmo) return;

        Handles.color = new Color(0.2f, 0.8f, 0.2f, 0.15f);
        Handles.DrawSolidDisc(areaCenter, Vector3.up, Mathf.Max(areaSize.x, areaSize.z) * 0.5f);

        Handles.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        var matrix = Handles.matrix;
        Handles.matrix = Matrix4x4.TRS(areaCenter, Quaternion.identity, Vector3.one);
        Handles.DrawWireCube(Vector3.zero, areaSize);
        Handles.matrix = matrix;

        // 핸들로 영역 조절
        EditorGUI.BeginChangeCheck();
        var newCenter = Handles.PositionHandle(areaCenter, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            areaCenter = newCenter;
            Repaint();
        }
    }
}
