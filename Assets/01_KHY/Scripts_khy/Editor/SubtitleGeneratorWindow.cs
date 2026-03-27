using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Timeline 오디오 트랙에서 자막 데이터를 자동 생성하는 에디터 창.
///
/// 사용법:
///   Tools → DearBrave → Subtitle Generator
/// </summary>
public class SubtitleGeneratorWindow : EditorWindow
{
    // ─── 필드 ───

    private PlayableDirector director;
    private SubtitleData targetData;
    private bool appendMode = false;

    private List<PreviewEntry> previews = new List<PreviewEntry>();
    private Vector2 scrollPos;

    // ─── 메뉴 등록 ───

    [MenuItem("Tools/DearBrave/Subtitle Generator")]
    public static void Open()
    {
        var window = GetWindow<SubtitleGeneratorWindow>("Subtitle Generator");
        window.minSize = new Vector2(480, 500);
        window.Show();
    }

    // ─── GUI ───

    private void OnGUI()
    {
        GUILayout.Label("📝 자막 자동 생성기", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        DrawInputSection();
        EditorGUILayout.Space(8);

        if (GUILayout.Button("🔍 타임라인에서 오디오 클립 읽기", GUILayout.Height(32)))
            ScanTimeline();

        EditorGUILayout.Space(8);

        if (previews.Count > 0)
        {
            DrawPreviewSection();
            EditorGUILayout.Space(8);
            DrawGenerateButton();
        }
    }

    private void DrawInputSection()
    {
        EditorGUILayout.LabelField("설정", EditorStyles.boldLabel);

        director = (PlayableDirector)EditorGUILayout.ObjectField(
            "PlayableDirector", director, typeof(PlayableDirector), true);

        targetData = (SubtitleData)EditorGUILayout.ObjectField(
            "SubtitleData (대상)", targetData, typeof(SubtitleData), false);

        appendMode = EditorGUILayout.Toggle(
            new GUIContent("이어붙이기 모드", "ON: 기존 항목 유지하고 추가 / OFF: 기존 항목 전부 교체"),
            appendMode);

        if (director == null)
        {
            EditorGUILayout.HelpBox("Hierarchy에서 PlayableDirector를 드래그해주세요.", MessageType.Info);
        }
        if (targetData == null)
        {
            EditorGUILayout.HelpBox("Project에서 SubtitleData 에셋을 드래그해주세요.\n없으면 새로 만드세요: 우클릭 → Create → DearBrave → SubtitleData", MessageType.Info);
        }
    }

    // ─── 타임라인 스캔 ───

    private void ScanTimeline()
    {
        previews.Clear();

        if (director == null)
        {
            EditorUtility.DisplayDialog("오류", "PlayableDirector를 먼저 연결해주세요.", "확인");
            return;
        }

        var timelineAsset = director.playableAsset as TimelineAsset;
        if (timelineAsset == null)
        {
            EditorUtility.DisplayDialog("오류", "PlayableDirector에 TimelineAsset이 없습니다.", "확인");
            return;
        }

        foreach (var track in timelineAsset.GetOutputTracks())
        {
            if (track is AudioTrack audioTrack)
            {
                foreach (var clip in audioTrack.GetClips())
                {
                    previews.Add(new PreviewEntry
                    {
                        startTime = (float)clip.start,
                        endTime   = (float)clip.end,
                        clipName  = clip.displayName,
                        koText    = "",
                        enText    = ""
                    });
                }
            }
        }

        if (previews.Count == 0)
        {
            EditorUtility.DisplayDialog("결과 없음", "타임라인에서 오디오 클립을 찾지 못했습니다.\nAudio Track이 있는지 확인해주세요.", "확인");
        }
        else
        {
            previews.Sort((a, b) => a.startTime.CompareTo(b.startTime));
        }
    }

    // ─── 미리보기 ───

    private void DrawPreviewSection()
    {
        EditorGUILayout.LabelField($"오디오 클립 {previews.Count}개 발견", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("자막 텍스트를 입력하세요. 시작/끝 시간은 자동으로 채워집니다.", MessageType.None);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(320));

        for (int i = 0; i < previews.Count; i++)
        {
            var p = previews[i];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 헤더
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"[{i + 1}]  {p.startTime:F2}s → {p.endTime:F2}s   ({p.clipName})",
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            // 텍스트 입력
            p.koText = EditorGUILayout.TextField("한국어", p.koText);
            p.enText = EditorGUILayout.TextField("English", p.enText);

            previews[i] = p;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();
    }

    // ─── 생성 버튼 ───

    private void DrawGenerateButton()
    {
        if (targetData == null)
        {
            EditorGUILayout.HelpBox("SubtitleData 에셋을 연결해야 생성할 수 있습니다.", MessageType.Warning);
            return;
        }

        Color prev = GUI.color;
        GUI.color = new Color(0.6f, 1f, 0.6f);
        if (GUILayout.Button("✅ SubtitleData에 저장", GUILayout.Height(36)))
            Generate();
        GUI.color = prev;
    }

    private void Generate()
    {
        Undo.RecordObject(targetData, "Generate Subtitle Data");

        if (!appendMode)
            targetData.entries.Clear();

        foreach (var p in previews)
        {
            targetData.entries.Add(new SubtitleEntry
            {
                startTime = p.startTime,
                endTime   = p.endTime,
                koText    = p.koText,
                enText    = p.enText
            });
        }

        // startTime 기준 정렬
        targetData.entries.Sort((a, b) => a.startTime.CompareTo(b.startTime));

        EditorUtility.SetDirty(targetData);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "완료",
            $"{previews.Count}개의 자막 항목이 저장되었습니다.\n텍스트가 비어있는 항목은 나중에 Inspector에서 입력하세요.",
            "확인");

        previews.Clear();
    }

    // ─── 내부 구조체 ───

    private struct PreviewEntry
    {
        public float  startTime;
        public float  endTime;
        public string clipName;
        public string koText;
        public string enText;
    }
}
