#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DioramaScaleClip))]
public class DioramaScaleClipEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var tp = serializedObject.FindProperty("_template");

        // ── 스케일 ──
        EditorGUILayout.LabelField("스케일", EditorStyles.boldLabel);

        var fromProp = tp.FindPropertyRelative("fromScale");
        var toProp = tp.FindPropertyRelative("targetScale");

        EditorGUILayout.Slider(fromProp, 0.1f, 3f, new GUIContent("시작 (from)"));
        EditorGUILayout.Slider(toProp, 0.1f, 3f, new GUIContent("목표 (to)"));

        // from → to 바
        Rect bar = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(bar, new Color(0.15f, 0.15f, 0.15f));

        float fromN = Mathf.InverseLerp(0.1f, 3f, fromProp.floatValue);
        float toN = Mathf.InverseLerp(0.1f, 3f, toProp.floatValue);

        // from (파랑), to (주황) 마커
        DrawMarker(bar, fromN, new Color(0.3f, 0.7f, 1f));
        DrawMarker(bar, toN, new Color(0.98f, 0.55f, 0.15f));

        // 방향 표시
        float minX = bar.x + bar.width * Mathf.Min(fromN, toN);
        float maxX = bar.x + bar.width * Mathf.Max(fromN, toN);
        Rect arrow = new Rect(minX, bar.y + 6, maxX - minX, 4);
        Color arrowCol = toProp.floatValue > fromProp.floatValue
            ? new Color(0.98f, 0.55f, 0.15f, 0.4f)
            : new Color(0.3f, 0.7f, 1f, 0.4f);
        EditorGUI.DrawRect(arrow, arrowCol);

        var labelStyle = new GUIStyle(EditorStyles.miniLabel)
        { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        EditorGUI.LabelField(bar,
            $"{fromProp.floatValue:F1}x → {toProp.floatValue:F1}x", labelStyle);

        EditorGUILayout.Space(8);

        // ── 전환 시간 ──
        EditorGUILayout.LabelField("전환", EditorStyles.boldLabel);
        var transIn = tp.FindPropertyRelative("transitionIn");
        EditorGUILayout.PropertyField(transIn, new GUIContent("전환 시간(초)"));

        // 전환/유지 다이어그램
        Rect diagram = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(diagram, new Color(0.15f, 0.15f, 0.15f));

        if (transIn.floatValue > 0f)
        {
            float ratio = Mathf.Clamp(transIn.floatValue / (transIn.floatValue + 1f), 0.08f, 0.5f);
            Rect transRect = diagram; transRect.width *= ratio;
            Rect holdRect = diagram; holdRect.xMin = transRect.xMax;

            EditorGUI.DrawRect(transRect, new Color(0.98f, 0.55f, 0.15f, 0.5f));
            EditorGUI.DrawRect(holdRect, new Color(0.3f, 0.8f, 0.4f, 0.3f));
            EditorGUI.LabelField(transRect, $"{transIn.floatValue:F1}s", labelStyle);
            EditorGUI.LabelField(holdRect, $"{toProp.floatValue:F1}x 유지", labelStyle);
        }
        else
        {
            EditorGUI.DrawRect(diagram, new Color(0.3f, 0.8f, 0.4f, 0.3f));
            EditorGUI.LabelField(diagram, $"즉시 {toProp.floatValue:F1}x → 유지", labelStyle);
        }

        EditorGUILayout.Space(8);

        // ── 이징 ──
        EditorGUILayout.LabelField("이징", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(tp.FindPropertyRelative("easeType"),
            new GUIContent("타입"));

        if (tp.FindPropertyRelative("easeType").enumValueIndex
            == (int)DioramaScaleBehaviour.EaseType.ExaggeratedBounce)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(tp.FindPropertyRelative("overshootStrength"),
                new GUIContent("오버슈트"));
            EditorGUILayout.PropertyField(tp.FindPropertyRelative("anticipationStrength"),
                new GUIContent("예비 동작"));
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawMarker(Rect bar, float normalized, Color color)
    {
        Rect marker = bar;
        marker.x += bar.width * normalized - 1.5f;
        marker.width = 3f;
        EditorGUI.DrawRect(marker, color);
    }
}
#endif
