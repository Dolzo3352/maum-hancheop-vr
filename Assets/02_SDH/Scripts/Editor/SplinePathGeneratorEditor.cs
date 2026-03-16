using UnityEngine;
using UnityEditor;

/// <summary>
/// SplinePathGenerator의 커스텀 인스펙터.
/// 메시 생성/갱신 버튼과 빠른 생성 메뉴를 제공합니다.
/// </summary>
[CustomEditor(typeof(SplinePathGenerator))]
public class SplinePathGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var generator = (SplinePathGenerator)target;

        EditorGUILayout.Space(10);

        // 메시 생성 버튼
        GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
        if (GUILayout.Button("메시 생성 / 갱신", GUILayout.Height(35)))
        {
            Undo.RecordObject(generator.GetComponent<MeshFilter>(), "Generate Spline Path Mesh");
            generator.GenerateMesh();
            EditorUtility.SetDirty(generator);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);

        // 메시 저장 버튼
        var meshFilter = generator.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            if (GUILayout.Button("메시를 에셋으로 저장"))
            {
                SaveMeshAsset(meshFilter.sharedMesh, generator.pathType.ToString());
            }

            EditorGUILayout.HelpBox(
                $"정점: {meshFilter.sharedMesh.vertexCount}\n삼각형: {meshFilter.sharedMesh.triangles.Length / 3}",
                MessageType.None);
        }
    }

    void SaveMeshAsset(Mesh mesh, string typeName)
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "메시 저장",
            $"SplinePath_{typeName}",
            "asset",
            "메시를 에셋으로 저장합니다.");

        if (string.IsNullOrEmpty(path)) return;

        var copy = Object.Instantiate(mesh);
        copy.name = System.IO.Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(copy, path);
        AssetDatabase.SaveAssets();

        // 저장한 에셋으로 교체
        var generator = (SplinePathGenerator)target;
        generator.GetComponent<MeshFilter>().sharedMesh = copy;

        Debug.Log($"[SplinePathGenerator] 메시 저장: {path}");
    }

    // ===== 빠른 생성 메뉴 =====

    [MenuItem("Dear Brave/레벨 디자인/강 경로 생성 (Spline)")]
    static void CreateRiver()
    {
        CreateSplinePath("River_Path", SplinePathGenerator.PathType.River, 4f, 0.4f);
    }

    [MenuItem("Dear Brave/레벨 디자인/산길 생성 (Spline)")]
    static void CreateTrail()
    {
        CreateSplinePath("Trail_Path", SplinePathGenerator.PathType.Trail, 2f, 0.1f);
    }

    [MenuItem("Dear Brave/레벨 디자인/도로 생성 (Spline)")]
    static void CreateRoad()
    {
        CreateSplinePath("Road_Path", SplinePathGenerator.PathType.Road, 5f, 0.15f);
    }

    static void CreateSplinePath(string name, SplinePathGenerator.PathType type, float width, float depth)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");

        // SplineContainer 추가 (기본 직선 스플라인)
        var splineContainer = go.AddComponent<UnityEngine.Splines.SplineContainer>();

        var generator = go.AddComponent<SplinePathGenerator>();
        generator.pathType = type;
        generator.width = width;
        generator.profileDepth = depth;

        // SceneView 중앙에 배치
        if (SceneView.lastActiveSceneView != null)
        {
            var cam = SceneView.lastActiveSceneView.camera;
            go.transform.position = cam.transform.position + cam.transform.forward * 20f;
        }

        Selection.activeGameObject = go;
        Debug.Log($"[SplinePathGenerator] '{name}' 생성됨. Spline 포인트를 편집하여 경로를 그리세요.");
    }
}
