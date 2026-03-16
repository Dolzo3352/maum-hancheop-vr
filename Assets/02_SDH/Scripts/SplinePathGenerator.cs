using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections.Generic;

/// <summary>
/// Spline을 따라 메시를 생성하는 컴포넌트.
/// 강, 산길, 오솔길 등에 사용합니다.
///
/// 사용법:
///   1. GameObject에 SplineContainer + 이 컴포넌트 추가
///   2. Spline 포인트 편집으로 경로 그리기
///   3. Inspector에서 '메시 생성' 클릭
///   4. 머티리얼 할당 (물, 흙길 등)
///
/// PlaceholderObject와 연동하여 나중에 고퀄리티 모델로 교체 가능.
/// </summary>
[RequireComponent(typeof(SplineContainer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SplinePathGenerator : MonoBehaviour
{
    public enum PathType
    {
        River,      // 강/시냇물 - 약간 오목한 단면
        Trail,      // 산길/오솔길 - 평평한 단면
        Road        // 넓은 길 - 살짝 볼록한 단면
    }

    [Header("경로 타입")]
    public PathType pathType = PathType.River;

    [Header("메시 설정")]
    [Tooltip("기본 경로 너비 (구간별 폭이 비어있으면 이 값 사용)")]
    public float width = 3f;

    [Tooltip("스플라인 포인트별 폭 (비어있으면 width 값 사용, 포인트 순서대로)")]
    public float[] widthPerPoint = new float[0];

    [Tooltip("길이 방향 세그먼트 수 (높을수록 부드러움)")]
    [Range(10, 500)]
    public int lengthSegments = 100;

    [Tooltip("너비 방향 세그먼트 수")]
    [Range(1, 10)]
    public int widthSegments = 3;

    [Tooltip("강: 오목한 깊이 / 길: 볼록한 높이")]
    public float profileDepth = 0.3f;

    [Header("UV 설정")]
    public float uvTilingX = 1f;
    public float uvTilingY = 1f;

    [Header("지형 맞춤")]
    [Tooltip("Raycast로 지형 표면에 맞춤")]
    public bool conformToTerrain = true;
    public LayerMask terrainLayer = ~0;
    public float terrainOffset = 0.02f;
    public float raycastHeight = 100f;

    // 캐시
    private SplineContainer splineContainer;
    private MeshFilter meshFilter;

    void OnValidate()
    {
        splineContainer = GetComponent<SplineContainer>();
        meshFilter = GetComponent<MeshFilter>();
    }

    /// <summary>
    /// 스플라인을 따라 메시를 생성합니다.
    /// </summary>
    public void GenerateMesh()
    {
        splineContainer = GetComponent<SplineContainer>();
        meshFilter = GetComponent<MeshFilter>();

        if (splineContainer == null || splineContainer.Spline == null)
        {
            Debug.LogWarning("[SplinePathGenerator] SplineContainer가 없습니다.");
            return;
        }

        var spline = splineContainer.Spline;
        if (spline.Count < 2)
        {
            Debug.LogWarning("[SplinePathGenerator] 스플라인 포인트가 2개 이상 필요합니다.");
            return;
        }

        var mesh = new Mesh();
        mesh.name = $"SplinePath_{pathType}";

        int vertsPerRow = widthSegments + 1;
        int totalVerts = (lengthSegments + 1) * vertsPerRow;
        int totalTris = lengthSegments * widthSegments * 6;

        var vertices = new Vector3[totalVerts];
        var normals = new Vector3[totalVerts];
        var uvs = new Vector2[totalVerts];
        var triangles = new int[totalTris];

        // 버텍스 생성 - right 벡터 일관성 유지
        Vector3 prevRight = Vector3.zero;

        for (int i = 0; i <= lengthSegments; i++)
        {
            float t = (float)i / lengthSegments;

            // 스플라인 위의 점과 방향
            SplineUtility.Evaluate(spline, t, out float3 pos, out float3 tangent, out float3 up);

            Vector3 localPos = (Vector3)pos;
            Vector3 worldPos = transform.TransformPoint(localPos);
            Vector3 forward = transform.TransformDirection(((Vector3)tangent).normalized);

            // right 벡터: 항상 수평면(XZ)에서 계산하여 꼬임 방지
            Vector3 flatForward = new Vector3(forward.x, 0, forward.z).normalized;
            if (flatForward.sqrMagnitude < 0.001f)
                flatForward = new Vector3(forward.x, 0, forward.z + 0.001f).normalized;

            Vector3 right = Vector3.Cross(Vector3.up, flatForward).normalized;

            // 이전 프레임과 방향이 급격히 바뀌면 뒤집힘 보정
            if (prevRight.sqrMagnitude > 0.001f && Vector3.Dot(right, prevRight) < 0)
                right = -right;

            prevRight = right;

            // 구간별 폭 보간
            float currentWidth = GetWidthAtT(t, spline.Count);

            for (int j = 0; j <= widthSegments; j++)
            {
                float widthT = (float)j / widthSegments; // 0 ~ 1
                float offsetX = (widthT - 0.5f) * currentWidth;

                // 단면 프로파일 적용
                float profileY = GetProfileHeight(widthT);

                Vector3 vertWorldPos = worldPos + right * offsetX + Vector3.up * profileY;

                // 지형 맞춤
                if (conformToTerrain)
                {
                    Vector3 rayOrigin = vertWorldPos + Vector3.up * raycastHeight;
                    if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, terrainLayer))
                    {
                        vertWorldPos = hit.point + Vector3.up * (terrainOffset + profileY);
                    }
                }

                int idx = i * vertsPerRow + j;
                vertices[idx] = transform.InverseTransformPoint(vertWorldPos);
                normals[idx] = Vector3.up;
                uvs[idx] = new Vector2(widthT * uvTilingX, t * uvTilingY * lengthSegments * 0.1f);
            }
        }

        // 삼각형 인덱스
        int triIdx = 0;
        for (int i = 0; i < lengthSegments; i++)
        {
            for (int j = 0; j < widthSegments; j++)
            {
                int a = i * vertsPerRow + j;
                int b = a + 1;
                int c = (i + 1) * vertsPerRow + j;
                int d = c + 1;

                triangles[triIdx++] = a;
                triangles[triIdx++] = c;
                triangles[triIdx++] = b;

                triangles[triIdx++] = b;
                triangles[triIdx++] = c;
                triangles[triIdx++] = d;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;
        Debug.Log($"[SplinePathGenerator] '{pathType}' 메시 생성 완료 (정점: {totalVerts}, 삼각형: {totalTris / 3})");
    }

    /// <summary>
    /// 스플라인 t값(0~1)에서의 폭을 반환합니다.
    /// widthPerPoint 배열이 설정되어 있으면 포인트 간 보간, 아니면 기본 width 사용.
    /// </summary>
    float GetWidthAtT(float t, int knotCount)
    {
        if (widthPerPoint == null || widthPerPoint.Length == 0)
            return width;

        // 포인트 수와 배열 길이가 다르면 기본값 사용
        if (widthPerPoint.Length < 2)
            return widthPerPoint.Length == 1 ? widthPerPoint[0] : width;

        // t를 포인트 인덱스로 변환 후 보간
        float pointT = t * (widthPerPoint.Length - 1);
        int idx = Mathf.FloorToInt(pointT);
        idx = Mathf.Clamp(idx, 0, widthPerPoint.Length - 2);
        float lerp = pointT - idx;

        return Mathf.Lerp(widthPerPoint[idx], widthPerPoint[idx + 1], lerp);
    }

    float GetProfileHeight(float t)
    {
        // t: 0(왼쪽) ~ 1(오른쪽), 0.5 = 중앙
        float centered = (t - 0.5f) * 2f; // -1 ~ 1

        switch (pathType)
        {
            case PathType.River:
                // 오목한 U자 - 가장자리가 높고 중앙이 낮음
                return profileDepth * (centered * centered - 1f);

            case PathType.Trail:
                // 거의 평평, 가장자리만 살짝 내려감
                float edge = Mathf.Abs(centered);
                return edge > 0.8f ? -profileDepth * (edge - 0.8f) * 5f : 0f;

            case PathType.Road:
                // 살짝 볼록 (배수용)
                return profileDepth * (1f - centered * centered);

            default:
                return 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();
        if (splineContainer == null || splineContainer.Spline == null) return;

        var spline = splineContainer.Spline;
        if (spline.Count < 2) return;

        // 경로 미리보기
        Gizmos.color = pathType switch
        {
            PathType.River => new Color(0.2f, 0.5f, 0.9f, 0.5f),
            PathType.Trail => new Color(0.6f, 0.4f, 0.2f, 0.5f),
            PathType.Road => new Color(0.7f, 0.7f, 0.6f, 0.5f),
            _ => Color.white
        };

        int previewSegments = 50;
        for (int i = 0; i < previewSegments; i++)
        {
            float t0 = (float)i / previewSegments;
            float t1 = (float)(i + 1) / previewSegments;

            SplineUtility.Evaluate(spline, t0, out float3 p0, out float3 tan0, out float3 up0);
            SplineUtility.Evaluate(spline, t1, out float3 p1, out float3 tan1, out float3 up1);

            Vector3 w0 = transform.TransformPoint((Vector3)p0);
            Vector3 w1 = transform.TransformPoint((Vector3)p1);

            Gizmos.DrawLine(w0, w1);

            // 너비 표시
            if (i % 5 == 0)
            {
                Vector3 fwd = ((Vector3)tan0).normalized;
                Vector3 upVec = ((Vector3)up0).sqrMagnitude > 0.001f ? ((Vector3)up0).normalized : Vector3.up;
                Vector3 right = Vector3.Cross(fwd, upVec).normalized;
                if (right.sqrMagnitude < 0.001f)
                    right = Vector3.Cross(fwd, Vector3.up).normalized;

                float gizmoWidth = GetWidthAtT(t0, spline.Count);
                Gizmos.DrawLine(w0 - right * gizmoWidth * 0.5f, w0 + right * gizmoWidth * 0.5f);
            }
        }
    }
}
