using UnityEngine;
using UnityEditor;

namespace DearBrave.Editor
{
    public class RicePaddyPlacer : EditorWindow
    {
        GameObject ricePrefab;
        float spacing = 0.3f;
        float randomOffset = 0.05f;
        float minScale = 0.85f;
        float maxScale = 1.15f;
        float edgePadding = 0.1f;
        bool randomRotation = true;

        [MenuItem("Tools/Dear Brave/Rice Paddy Placer")]
        static void ShowWindow()
        {
            GetWindow<RicePaddyPlacer>("Rice Paddy Placer");
        }

        void OnGUI()
        {
            GUILayout.Label("벼 자동 배치 도구", EditorStyles.boldLabel);
            GUILayout.Space(5);

            ricePrefab = (GameObject)EditorGUIField("벼 프리팹", ricePrefab);
            spacing = EditorGUILayout.FloatField("간격 (m)", spacing);
            randomOffset = EditorGUILayout.FloatField("랜덤 오프셋", randomOffset);
            minScale = EditorGUILayout.FloatField("최소 스케일 배율", minScale);
            maxScale = EditorGUILayout.FloatField("최대 스케일 배율", maxScale);
            edgePadding = EditorGUILayout.FloatField("가장자리 여백 (m)", edgePadding);
            randomRotation = EditorGUILayout.Toggle("Y축 랜덤 회전", randomRotation);

            GUILayout.Space(10);

            var selected = Selection.activeGameObject;
            if (selected != null)
            {
                EditorGUILayout.HelpBox($"선택된 오브젝트: {selected.name}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("씬에서 논 표면 오브젝트를 선택하세요.", MessageType.Warning);
            }

            GUILayout.Space(5);

            GUI.enabled = selected != null && ricePrefab != null;
            if (GUILayout.Button("벼 배치하기", GUILayout.Height(30)))
            {
                PlaceRice(selected);
            }
            GUI.enabled = true;

            GUILayout.Space(5);

            if (GUILayout.Button("선택 오브젝트 하위 벼 전부 삭제", GUILayout.Height(25)))
            {
                if (selected != null)
                    ClearRice(selected);
            }
        }

        Object EditorGUIField(string label, Object obj)
        {
            return EditorGUILayout.ObjectField(label, obj, typeof(GameObject), false);
        }

        void PlaceRice(GameObject surface)
        {
            var meshFilter = surface.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("오류", "선택한 오브젝트에 MeshFilter가 없습니다.", "확인");
                return;
            }

            // Create parent
            string parentName = $"{surface.name}_벼";
            var existingParent = surface.transform.Find(parentName);
            if (existingParent != null)
                DestroyImmediate(existingParent.gameObject);

            var parent = new GameObject(parentName);
            parent.transform.SetParent(surface.transform);
            parent.transform.localPosition = Vector3.zero;
            parent.transform.localRotation = Quaternion.identity;
            parent.transform.localScale = Vector3.one;

            Undo.RegisterCreatedObjectUndo(parent, "Place Rice");

            // Get world-space bounds
            var renderer = surface.GetComponent<Renderer>();
            if (renderer == null)
            {
                EditorUtility.DisplayDialog("오류", "선택한 오브젝트에 Renderer가 없습니다.", "확인");
                DestroyImmediate(parent);
                return;
            }

            Bounds bounds = renderer.bounds;
            float rayHeight = bounds.max.y + 5f;

            // Temporarily set surface to a known layer for raycasting
            int origLayer = surface.layer;
            surface.layer = 31; // temp layer
            int layerMask = 1 << 31;

            // Also handle child colliders - add a temp MeshCollider if needed
            var existingCollider = surface.GetComponent<Collider>();
            MeshCollider tempCollider = null;
            if (existingCollider == null)
            {
                tempCollider = surface.AddComponent<MeshCollider>();
            }

            int placedCount = 0;
            float minX = bounds.min.x + edgePadding;
            float maxX = bounds.max.x - edgePadding;
            float minZ = bounds.min.z + edgePadding;
            float maxZ = bounds.max.z - edgePadding;

            for (float x = minX; x <= maxX; x += spacing)
            {
                for (float z = minZ; z <= maxZ; z += spacing)
                {
                    float ox = x + Random.Range(-randomOffset, randomOffset);
                    float oz = z + Random.Range(-randomOffset, randomOffset);

                    var ray = new Ray(new Vector3(ox, rayHeight, oz), Vector3.down);
                    if (Physics.Raycast(ray, out RaycastHit hit, rayHeight * 2f, layerMask))
                    {
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(ricePrefab);
                        instance.transform.SetParent(parent.transform);
                        instance.transform.position = hit.point;

                        // Random rotation
                        float yRot = randomRotation ? Random.Range(0f, 360f) : 0f;
                        instance.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

                        // Random scale
                        float scaleMult = Random.Range(minScale, maxScale);
                        instance.transform.localScale = instance.transform.localScale * scaleMult;

                        placedCount++;
                    }
                }

                // Progress bar
                float progress = (x - minX) / (maxX - minX);
                if (EditorUtility.DisplayCancelableProgressBar("벼 배치 중...", $"{placedCount}개 배치됨", progress))
                {
                    break;
                }
            }

            EditorUtility.ClearProgressBar();

            // Restore
            surface.layer = origLayer;
            if (tempCollider != null)
                DestroyImmediate(tempCollider);

            Debug.Log($"[RicePaddyPlacer] {surface.name} 위에 벼 {placedCount}개 배치 완료");
        }

        void ClearRice(GameObject surface)
        {
            string parentName = $"{surface.name}_벼";
            var existing = surface.transform.Find(parentName);
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
                Debug.Log($"[RicePaddyPlacer] {parentName} 삭제 완료");
            }
            else
            {
                EditorUtility.DisplayDialog("안내", "삭제할 벼 그룹이 없습니다.", "확인");
            }
        }
    }
}
