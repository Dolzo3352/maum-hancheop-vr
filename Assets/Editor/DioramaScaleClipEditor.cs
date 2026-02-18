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

        // from → to 미리보기 바
        Rect barRect = GUILayoutUtility.GetRect(0, 16, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(barRect, new Color(0.15f, 0.15f, 0.15f));

        float fromN = Mathf.InverseLerp(0.1f, 3f, fromProp.floatValue);
        float toN = Mathf.InverseLerp(0.1f, 3f, toProp.floatValue);

        // from 위치 (파란 선)
        Rect fromLine = barRect;
        fromLine.x += barRect.width * fromN - 1f;
        fromLine.width = 3f;
        EditorGUI.DrawRect(fromLine, new Color(0.3f, 0.7f, 1f));

        // to 위치 (주황 선)
        Rect toLine = barRect;
        toLine.x += barRect.width * toN - 1f;
        toLine.width = 3f;
        EditorGUI.DrawRect(toLine, new Color(0.98f, 0.55f, 0.15f));

        // 화살표 영역
        float minX = barRect.x + barRect.width * Mathf.Min(fromN, toN);
        float maxX = barRect.x + barRect.width * Mathf.Max(fromN, toN);
        Rect arrowRect = barRect;
        arrowRect.x = minX;
        arrowRect.width = maxX - minX;
        arrowRect.height = 4f;
        arrowRect.y += 6f;
        Color arrowColor = toProp.floatValue > fromProp.floatValue
            ? new Color(0.98f, 0.55f, 0.15f, 0.4f)   // 확대: 주황
            : new Color(0.3f, 0.7f, 1f, 0.4f);         // 축소: 파랑
        EditorGUI.DrawRect(arrowRect, arrowColor);

        // 라벨
        var labelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        EditorGUI.LabelField(barRect,
            $"{fromProp.floatValue:F1}x → {toProp.floatValue:F1}x", labelStyle);

        EditorGUILayout.Space(8);

        // ── 타이밍 ──
        EditorGUILayout.LabelField("타이밍", EditorStyles.boldLabel);
        var transInProp = tp.FindPropertyRelative("transitionIn");
        EditorGUILayout.PropertyField(transInProp, new GUIContent("전환 시간(초)"));

        // 전환/유지 다이어그램
        EditorGUILayout.Space(4);
        Rect diagramRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(diagramRect, new Color(0.15f, 0.15f, 0.15f));

        float transIn = transInProp.floatValue;
        var style = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        if (transIn > 0f)
        {
            float ratio = Mathf.Clamp(transIn / Mathf.Max(transIn + 1f, 2f), 0.08f, 0.5f);

            Rect transRect = diagramRect;
            transRect.width *= ratio;
            EditorGUI.DrawRect(transRect, new Color(0.98f, 0.55f, 0.15f, 0.5f));
            EditorGUI.LabelField(transRect, $"전환 {transIn:F1}s", style);

            Rect holdRect = diagramRect;
            holdRect.xMin = transRect.xMax;
            EditorGUI.DrawRect(holdRect, new Color(0.3f, 0.8f, 0.4f, 0.3f));
            EditorGUI.LabelField(holdRect, $"{toProp.floatValue:F1}x 유지", style);
        }
        else
        {
            EditorGUI.DrawRect(diagramRect, new Color(0.3f, 0.8f, 0.4f, 0.3f));
            EditorGUI.LabelField(diagramRect, $"즉시 {toProp.floatValue:F1}x → 유지", style);
        }

        EditorGUILayout.Space(8);

        // ── 이징 ──
        EditorGUILayout.LabelField("애니메이션", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(tp.FindPropertyRelative("easeType"),
            new GUIContent("이징 타입"));

        var easeIdx = tp.FindPropertyRelative("easeType").enumValueIndex;
        if (easeIdx == (int)DioramaScaleBehaviour.EaseType.ExaggeratedBounce)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(tp.FindPropertyRelative("overshootStrength"),
                new GUIContent("오버슈트"));
            EditorGUILayout.PropertyField(tp.FindPropertyRelative("anticipationStrength"),
                new GUIContent("예비 동작"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(8);

        // ── 시차 ──
        EditorGUILayout.LabelField("시차 효과", EditorStyles.boldLabel);
        var staggerProp = tp.FindPropertyRelative("enableStagger");
        EditorGUILayout.PropertyField(staggerProp, new GUIContent("시차 사용"));

        if (staggerProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(tp.FindPropertyRelative("staggerDelay"),
                new GUIContent("시차 간격(초)"));
            EditorGUILayout.PropertyField(tp.FindPropertyRelative("staggerDirection"),
                new GUIContent("시차 방향"));
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
