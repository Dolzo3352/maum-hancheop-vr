using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace DearBrave.Editor
{
    public class RicePatchGenerator : EditorWindow
    {
        // Patch settings
        float patchSize = 1.5f;
        int density = 14;
        float stalkHeight = 0.4f;
        float stalkWidth = 0.08f;
        float earDroop = 0.1f;
        float randomness = 0.4f;
        int leavesPerStalk = 3;

        // Colors
        Color stalkColor = new Color(0.55f, 0.58f, 0.25f);
        Color earColor = new Color(0.78f, 0.66f, 0.30f);
        Color stalkColor2 = new Color(0.50f, 0.55f, 0.22f);
        Color earColor2 = new Color(0.72f, 0.58f, 0.25f);

        // Fill settings
        float fillPadding = 0.1f;

        string savePath = "Assets/01_KHY/Model_khy/00_주변에셋/prefab/Grass/벼";

        Vector2 scrollPos;

        [MenuItem("Tools/Dear Brave/Rice Patch Generator")]
        static void ShowWindow()
        {
            GetWindow<RicePatchGenerator>("Rice Patch Generator");
        }

        void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            GUILayout.Label("벼 패치 메쉬 생성기", EditorStyles.boldLabel);
            GUILayout.Space(5);

            GUILayout.Label("── 패치 설정 ──", EditorStyles.miniBoldLabel);
            patchSize = EditorGUILayout.Slider("패치 크기 (m)", patchSize, 0.5f, 3f);
            density = EditorGUILayout.IntSlider("밀도 (한 줄당)", density, 5, 25);
            stalkHeight = EditorGUILayout.Slider("벼 높이 (m)", stalkHeight, 0.15f, 0.8f);
            stalkWidth = EditorGUILayout.Slider("벼 폭 (m)", stalkWidth, 0.03f, 0.2f);
            earDroop = EditorGUILayout.Slider("이삭 처짐", earDroop, 0f, 0.25f);
            leavesPerStalk = EditorGUILayout.IntSlider("잎 수 (포기당)", leavesPerStalk, 1, 5);
            randomness = EditorGUILayout.Slider("랜덤 변화량", randomness, 0f, 1f);

            GUILayout.Space(5);
            GUILayout.Label("── 색상 ──", EditorStyles.miniBoldLabel);
            stalkColor = EditorGUILayout.ColorField("줄기/잎 색 1", stalkColor);
            stalkColor2 = EditorGUILayout.ColorField("줄기/잎 색 2", stalkColor2);
            earColor = EditorGUILayout.ColorField("이삭 색 1", earColor);
            earColor2 = EditorGUILayout.ColorField("이삭 색 2", earColor2);

            GUILayout.Space(5);
            savePath = EditorGUILayout.TextField("저장 경로", savePath);

            GUILayout.Space(5);
            int totalStalks = density * density;
            int trisPerStalk = 4 + leavesPerStalk * 4 + 4; // cross stalk + leaves + ear
            EditorGUILayout.HelpBox(
                $"벼 {totalStalks}포기, ~{totalStalks * trisPerStalk} tris/patch",
                MessageType.Info);

            GUILayout.Space(5);
            if (GUILayout.Button("패치 프리팹 생성/갱신", GUILayout.Height(30)))
                GeneratePatch();

            if (GUILayout.Button("씬에 미리보기 생성", GUILayout.Height(25)))
                GeneratePreview();

            // --- Fill surface section ---
            GUILayout.Space(15);
            GUILayout.Label("── 논 표면에 자동 채우기 ──", EditorStyles.boldLabel);

            fillPadding = EditorGUILayout.Slider("가장자리 여백 (m)", fillPadding, 0f, 0.5f);

            var selected = Selection.activeGameObject;
            if (selected != null)
                EditorGUILayout.HelpBox($"선택: {selected.name}", MessageType.Info);
            else
                EditorGUILayout.HelpBox("씬에서 논 표면 오브젝트를 선택하세요.", MessageType.Warning);

            GUI.enabled = selected != null;
            if (GUILayout.Button("선택한 표면에 벼 채우기", GUILayout.Height(30)))
                FillSurface(selected);

            GUILayout.Space(3);
            if (GUILayout.Button("선택 오브젝트 하위 벼 전부 삭제", GUILayout.Height(25)))
            {
                if (selected != null) ClearRice(selected);
            }
            GUI.enabled = true;

            EditorGUILayout.EndScrollView();
        }

        void AddQuad(List<Vector3> verts, List<int> tris, List<Color> colors,
                     Vector3 bl, Vector3 br, Vector3 tl, Vector3 tr, Color col)
        {
            int vi = verts.Count;
            verts.Add(bl); verts.Add(br); verts.Add(tl); verts.Add(tr);
            colors.Add(col); colors.Add(col); colors.Add(col); colors.Add(col);
            // Front
            tris.Add(vi); tris.Add(vi + 2); tris.Add(vi + 1);
            tris.Add(vi + 1); tris.Add(vi + 2); tris.Add(vi + 3);
            // Back
            tris.Add(vi + 1); tris.Add(vi + 2); tris.Add(vi);
            tris.Add(vi + 3); tris.Add(vi + 2); tris.Add(vi + 1);
        }

        void AddTri(List<Vector3> verts, List<int> tris, List<Color> colors,
                    Vector3 a, Vector3 b, Vector3 c, Color col)
        {
            int vi = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            colors.Add(col); colors.Add(col); colors.Add(col);
            tris.Add(vi); tris.Add(vi + 1); tris.Add(vi + 2);
            tris.Add(vi + 2); tris.Add(vi + 1); tris.Add(vi);
        }

        Mesh BuildPatchMesh()
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var colors = new List<Color>();

            float step = patchSize / density;
            float halfPatch = patchSize / 2f;

            for (int ix = 0; ix < density; ix++)
            {
                for (int iz = 0; iz < density; iz++)
                {
                    float baseX = -halfPatch + ix * step + step * 0.5f;
                    float baseZ = -halfPatch + iz * step + step * 0.5f;

                    float rx = baseX + Random.Range(-step * 0.35f, step * 0.35f) * randomness;
                    float rz = baseZ + Random.Range(-step * 0.35f, step * 0.35f) * randomness;

                    float heightMult = 1f + Random.Range(-0.25f, 0.25f) * randomness;
                    float widthMult = 1f + Random.Range(-0.2f, 0.2f) * randomness;
                    float h = stalkHeight * heightMult;
                    float w = stalkWidth * widthMult;

                    float t = Random.value;
                    Color sc = Color.Lerp(stalkColor, stalkColor2, t);
                    Color ec = Color.Lerp(earColor, earColor2, t);

                    float angle = Random.Range(0f, Mathf.PI * 2f);
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);

                    // === Cross-shaped stalk (2 perpendicular quads) ===
                    float hw = w * 0.3f;
                    float hwTop = w * 0.15f;

                    // Quad 1
                    AddQuad(verts, tris, colors,
                        new Vector3(rx - hw * cos, 0f, rz - hw * sin),
                        new Vector3(rx + hw * cos, 0f, rz + hw * sin),
                        new Vector3(rx - hwTop * cos, h, rz - hwTop * sin),
                        new Vector3(rx + hwTop * cos, h, rz + hwTop * sin),
                        sc);

                    // Quad 2 (perpendicular)
                    AddQuad(verts, tris, colors,
                        new Vector3(rx - hw * sin, 0f, rz + hw * cos),
                        new Vector3(rx + hw * sin, 0f, rz - hw * cos),
                        new Vector3(rx - hwTop * sin, h, rz + hwTop * cos),
                        new Vector3(rx + hwTop * sin, h, rz - hwTop * cos),
                        sc);

                    // === Multiple leaves ===
                    for (int li = 0; li < leavesPerStalk; li++)
                    {
                        float leafH = h * Random.Range(0.2f, 0.7f);
                        float leafLen = w * Random.Range(3f, 5f);
                        float leafAngle = angle + (Mathf.PI * 2f / leavesPerStalk) * li
                                          + Random.Range(-0.4f, 0.4f);
                        float lcos = Mathf.Cos(leafAngle);
                        float lsin = Mathf.Sin(leafAngle);
                        float leafW = w * 0.4f;

                        // Leaf as a quad for more volume
                        Vector3 lb1 = new Vector3(rx - lsin * leafW * 0.5f, leafH, rz + lcos * leafW * 0.5f);
                        Vector3 lb2 = new Vector3(rx + lsin * leafW * 0.5f, leafH, rz - lcos * leafW * 0.5f);
                        Vector3 lt1 = new Vector3(rx + lcos * leafLen - lsin * leafW * 0.2f,
                                                  leafH * 0.5f,
                                                  rz + lsin * leafLen + lcos * leafW * 0.2f);
                        Vector3 lt2 = new Vector3(rx + lcos * leafLen + lsin * leafW * 0.2f,
                                                  leafH * 0.5f,
                                                  rz + lsin * leafLen - lcos * leafW * 0.2f);

                        Color leafCol = Color.Lerp(sc, ec, 0.2f + Random.Range(0f, 0.2f));
                        AddQuad(verts, tris, colors, lb1, lb2, lt1, lt2, leafCol);
                    }

                    // === Ear (drooping from top) ===
                    float droopAngle = angle + Random.Range(-0.4f, 0.4f);
                    float dcos = Mathf.Cos(droopAngle);
                    float dsin = Mathf.Sin(droopAngle);
                    float earLen = w * Random.Range(3f, 5f);
                    float droop = earDroop * (1f + Random.Range(-0.3f, 0.3f) * randomness);
                    float earW = w * 0.35f;

                    Vector3 eb1 = new Vector3(rx - dsin * earW * 0.5f, h, rz + dcos * earW * 0.5f);
                    Vector3 eb2 = new Vector3(rx + dsin * earW * 0.5f, h, rz - dcos * earW * 0.5f);
                    Vector3 et = new Vector3(rx + dcos * earLen, h - droop, rz + dsin * earLen);

                    AddTri(verts, tris, colors, eb1, eb2, et, ec);
                }
            }

            var mesh = new Mesh();
            mesh.name = "RicePatch";
            if (verts.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetColors(colors);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        Material GetOrCreateMaterial()
        {
            string matPath = $"{savePath}/RicePatch_VertexColor.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var shader = Shader.Find("DearBrave/VertexColor");
                if (shader == null)
                {
                    EditorUtility.DisplayDialog("오류",
                        "DearBrave/VertexColor 셰이더를 찾을 수 없습니다.\n" +
                        "Assets/01_KHY/Shaders/VertexColorUnlit.shader 파일을 확인하세요.",
                        "확인");
                    return null;
                }

                mat = new Material(shader);
                mat.name = "RicePatch_VertexColor";

                if (!AssetDatabase.IsValidFolder(savePath))
                {
                    System.IO.Directory.CreateDirectory(
                        System.IO.Path.Combine(Application.dataPath, "..", savePath));
                    AssetDatabase.Refresh();
                }

                AssetDatabase.CreateAsset(mat, matPath);
            }
            return mat;
        }

        void GeneratePatch()
        {
            if (!AssetDatabase.IsValidFolder(savePath))
            {
                System.IO.Directory.CreateDirectory(
                    System.IO.Path.Combine(Application.dataPath, "..", savePath));
                AssetDatabase.Refresh();
            }

            Mesh mesh = BuildPatchMesh();

            string meshPath = $"{savePath}/RicePatch_Mesh.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(mesh, existing);
                mesh = existing;
            }
            else
            {
                AssetDatabase.CreateAsset(mesh, meshPath);
            }

            Material mat = GetOrCreateMaterial();
            if (mat == null) return;

            string prefabPath = $"{savePath}/RicePatch.prefab";
            var go = new GameObject("RicePatch");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = true;

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            DestroyImmediate(go);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[RicePatchGenerator] 패치 프리팹 생성 완료: {prefabPath}");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
        }

        void GeneratePreview()
        {
            Mesh mesh = BuildPatchMesh();
            Material mat = GetOrCreateMaterial();
            if (mat == null) return;

            var go = new GameObject("RicePatch_Preview");
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Undo.RegisterCreatedObjectUndo(go, "Rice Patch Preview");
            Selection.activeGameObject = go;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        void FillSurface(GameObject surface)
        {
            var renderer = surface.GetComponent<Renderer>();
            if (renderer == null)
            {
                EditorUtility.DisplayDialog("오류", "선택한 오브젝트에 Renderer가 없습니다.", "확인");
                return;
            }

            // Ensure patch prefab exists
            string prefabPath = $"{savePath}/RicePatch.prefab";
            var patchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (patchPrefab == null)
            {
                if (EditorUtility.DisplayDialog("패치 없음",
                    "패치 프리팹이 없습니다. 먼저 생성할까요?", "생성", "취소"))
                {
                    GeneratePatch();
                    patchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                    if (patchPrefab == null) return;
                }
                else return;
            }

            // Clear existing
            ClearRice(surface);

            // Create parent
            string parentName = $"{surface.name}_벼";
            var parent = new GameObject(parentName);
            parent.transform.SetParent(surface.transform);
            parent.transform.localPosition = Vector3.zero;
            parent.transform.localRotation = Quaternion.identity;
            parent.transform.localScale = Vector3.one;
            Undo.RegisterCreatedObjectUndo(parent, "Fill Rice");

            Bounds bounds = renderer.bounds;
            float rayHeight = bounds.max.y + 5f;

            // Temp layer for raycast
            int origLayer = surface.layer;
            surface.layer = 31;
            int layerMask = 1 << 31;

            MeshCollider tempCollider = null;
            if (surface.GetComponent<Collider>() == null)
                tempCollider = surface.AddComponent<MeshCollider>();

            int placedCount = 0;
            float minX = bounds.min.x + fillPadding;
            float maxX = bounds.max.x - fillPadding;
            float minZ = bounds.min.z + fillPadding;
            float maxZ = bounds.max.z - fillPadding;

            for (float x = minX; x <= maxX; x += patchSize * 0.9f)
            {
                for (float z = minZ; z <= maxZ; z += patchSize * 0.9f)
                {
                    // Check center of patch hits the surface
                    var ray = new Ray(new Vector3(x, rayHeight, z), Vector3.down);
                    if (Physics.Raycast(ray, out RaycastHit hit, rayHeight * 2f, layerMask))
                    {
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(patchPrefab);
                        instance.transform.SetParent(parent.transform);
                        instance.transform.position = hit.point;
                        instance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        placedCount++;
                    }
                }

                float progress = (x - minX) / (maxX - minX + 0.01f);
                if (EditorUtility.DisplayCancelableProgressBar("벼 채우는 중...",
                    $"{placedCount}개 패치 배치됨", progress))
                    break;
            }

            EditorUtility.ClearProgressBar();

            surface.layer = origLayer;
            if (tempCollider != null)
                DestroyImmediate(tempCollider);

            Debug.Log($"[RicePatchGenerator] {surface.name} 위에 패치 {placedCount}개 배치 완료");
        }

        void ClearRice(GameObject surface)
        {
            string parentName = $"{surface.name}_벼";
            var existing = surface.transform.Find(parentName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
                Debug.Log($"[RicePatchGenerator] {parentName} 삭제 완료");
            }
        }
    }
}
