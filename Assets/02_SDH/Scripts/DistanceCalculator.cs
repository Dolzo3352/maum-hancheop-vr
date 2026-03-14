using UnityEngine;
#if UNITY_EDITOR
using UnityEditor; // 에디터 화면에 글자를 그리기 위해 필요합니다.
#endif

[ExecuteAlways] // 중요: Play를 누르지 않아도 에디터에서 코드가 실행되게 합니다.
public class DistanceCalculator : MonoBehaviour
{
    [Header("거리 측정 대상")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("시각 설정")]
    public Color lineColor = Color.yellow;
    [Range(10, 30)] public int fontSize = 15;

#if UNITY_EDITOR
    // 이 함수는 씬 뷰가 업데이트될 때마다 자동으로 호출됩니다.
    void OnDrawGizmos()
    {
        if (startPoint == null || endPoint == null) return;

        // 1. 두 지점 사이에 선 그리기
        Gizmos.color = lineColor;
        Gizmos.DrawLine(startPoint.position, endPoint.position);

        // 2. 거리 계산
        float distance = Vector3.Distance(startPoint.position, endPoint.position);

        // 3. 텍스트 표시 위치 계산 (두 지점의 중간)
        Vector3 midPoint = (startPoint.position + endPoint.position) / 2f;

        // 4. 씬 뷰에 거리 수치 텍스트 표시
        GUIStyle style = new GUIStyle();
        style.normal.textColor = lineColor;
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;

        // 레이블 표시 (살짝 위로 띄움)
        Handles.Label(midPoint + Vector3.up * 0.2f, $"{distance:F2}m", style);

        // 5. 각 포인트에 이름 표시
        style.fontSize = fontSize - 3;
        Handles.Label(startPoint.position + Vector3.up * 0.4f, startPoint.name, style);
        Handles.Label(endPoint.position + Vector3.up * 0.4f, endPoint.name, style);
    }
#endif
}