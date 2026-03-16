using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

/// <summary>
/// 라이팅 테스트 에디터 윈도우.
/// 스테이지별 라이팅을 실시간으로 조절하며 확인할 수 있습니다.
/// 메뉴: Dear Brave > Lighting Test Window
/// </summary>
public class LightingTestWindow : EditorWindow
{
    // --- 디렉셔널 라이트 ---
    private float sunIntensity = 1f;
    private Color sunColor = new Color(1f, 0.95f, 0.84f);
    private float sunRotX = 50f;
    private float sunRotY = -30f;

    // --- 앰비언트 ---
    private Color ambientColor = new Color(0.3f, 0.3f, 0.4f);
    private float ambientIntensity = 1f;

    // --- 안개 ---
    private bool fogEnabled;
    private Color fogColor = new Color(0.7f, 0.7f, 0.8f);
    private float fogDensity = 0.02f;

    // --- 프리셋 ---
    private enum TimeOfDay { Morning, Afternoon, Sunset, Night, Custom }
    private TimeOfDay selectedTime = TimeOfDay.Afternoon;

    // --- 스크롤 ---
    private Vector2 scrollPos;

    // --- 자동 적용 ---
    private Light cachedSun;

    [MenuItem("Dear Brave/Lighting Test Window")]
    public static void ShowWindow()
    {
        var window = GetWindow<LightingTestWindow>("Lighting Test");
        window.minSize = new Vector2(300, 400);
    }

    void OnEnable()
    {
        FindSun();
        LoadCurrentSettings();
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("Lighting Test", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // === 빠른 프리셋 ===
        EditorGUILayout.LabelField("빠른 프리셋", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("아침")) ApplyPreset(TimeOfDay.Morning);
        if (GUILayout.Button("낮")) ApplyPreset(TimeOfDay.Afternoon);
        if (GUILayout.Button("석양")) ApplyPreset(TimeOfDay.Sunset);
        if (GUILayout.Button("밤")) ApplyPreset(TimeOfDay.Night);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // === 태양 (Directional Light) ===
        EditorGUILayout.LabelField("태양 (Directional Light)", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        sunIntensity = EditorGUILayout.Slider("밝기", sunIntensity, 0f, 5f);
        sunColor = EditorGUILayout.ColorField("색상", sunColor);
        sunRotX = EditorGUILayout.Slider("고도 (X)", sunRotX, -10f, 90f);
        sunRotY = EditorGUILayout.Slider("방위 (Y)", sunRotY, -180f, 180f);
        if (EditorGUI.EndChangeCheck()) ApplyToScene();

        EditorGUILayout.Space(10);

        // === 앰비언트 ===
        EditorGUILayout.LabelField("앰비언트", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        ambientColor = EditorGUILayout.ColorField("앰비언트 색상", ambientColor);
        ambientIntensity = EditorGUILayout.Slider("앰비언트 강도", ambientIntensity, 0f, 3f);
        if (EditorGUI.EndChangeCheck()) ApplyToScene();

        EditorGUILayout.Space(10);

        // === 안개 ===
        EditorGUILayout.LabelField("안개", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        fogEnabled = EditorGUILayout.Toggle("안개 사용", fogEnabled);
        if (fogEnabled)
        {
            fogColor = EditorGUILayout.ColorField("안개 색상", fogColor);
            fogDensity = EditorGUILayout.Slider("안개 밀도", fogDensity, 0f, 0.1f);
        }
        if (EditorGUI.EndChangeCheck()) ApplyToScene();

        EditorGUILayout.Space(15);

        // === 저장/로드 ===
        EditorGUILayout.LabelField("프리셋 저장", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("LightingPreset SO로 저장"))
        {
            SaveAsPreset();
        }
        if (GUILayout.Button("현재 씬 값 불러오기"))
        {
            LoadCurrentSettings();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    void ApplyPreset(TimeOfDay time)
    {
        selectedTime = time;
        switch (time)
        {
            case TimeOfDay.Morning:
                sunIntensity = 0.8f;
                sunColor = new Color(1f, 0.9f, 0.75f);
                sunRotX = 20f; sunRotY = -60f;
                ambientColor = new Color(0.4f, 0.4f, 0.5f);
                ambientIntensity = 0.8f;
                fogEnabled = true;
                fogColor = new Color(0.85f, 0.85f, 0.9f);
                fogDensity = 0.015f;
                break;

            case TimeOfDay.Afternoon:
                sunIntensity = 1.2f;
                sunColor = new Color(1f, 0.98f, 0.9f);
                sunRotX = 50f; sunRotY = -30f;
                ambientColor = new Color(0.35f, 0.38f, 0.45f);
                ambientIntensity = 1f;
                fogEnabled = false;
                break;

            case TimeOfDay.Sunset:
                sunIntensity = 0.9f;
                sunColor = new Color(1f, 0.6f, 0.3f);
                sunRotX = 10f; sunRotY = -120f;
                ambientColor = new Color(0.3f, 0.25f, 0.35f);
                ambientIntensity = 0.7f;
                fogEnabled = true;
                fogColor = new Color(0.9f, 0.6f, 0.4f);
                fogDensity = 0.01f;
                break;

            case TimeOfDay.Night:
                sunIntensity = 0.15f;
                sunColor = new Color(0.5f, 0.55f, 0.7f);
                sunRotX = 60f; sunRotY = -30f;
                ambientColor = new Color(0.08f, 0.08f, 0.15f);
                ambientIntensity = 0.4f;
                fogEnabled = true;
                fogColor = new Color(0.15f, 0.15f, 0.25f);
                fogDensity = 0.03f;
                break;
        }
        ApplyToScene();
        Repaint();
    }

    void ApplyToScene()
    {
        if (cachedSun == null) FindSun();

        // Directional Light
        if (cachedSun != null)
        {
            Undo.RecordObject(cachedSun, "Lighting Test Change");
            cachedSun.intensity = sunIntensity;
            cachedSun.color = sunColor;

            Undo.RecordObject(cachedSun.transform, "Lighting Test Rotation");
            cachedSun.transform.rotation = Quaternion.Euler(sunRotX, sunRotY, 0f);
        }

        // Ambient
        RenderSettings.ambientLight = ambientColor * ambientIntensity;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

        // Fog
        RenderSettings.fog = fogEnabled;
        if (fogEnabled)
        {
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogMode = FogMode.Exponential;
        }

        SceneView.RepaintAll();
    }

    void FindSun()
    {
        // 씬에서 Directional Light 찾기
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                cachedSun = light;
                return;
            }
        }
    }

    void LoadCurrentSettings()
    {
        if (cachedSun == null) FindSun();

        if (cachedSun != null)
        {
            sunIntensity = cachedSun.intensity;
            sunColor = cachedSun.color;
            sunRotX = cachedSun.transform.eulerAngles.x;
            sunRotY = cachedSun.transform.eulerAngles.y;
        }

        ambientColor = RenderSettings.ambientLight;
        fogEnabled = RenderSettings.fog;
        fogColor = RenderSettings.fogColor;
        fogDensity = RenderSettings.fogDensity;

        Repaint();
    }

    void SaveAsPreset()
    {
        var preset = ScriptableObject.CreateInstance<LightingPreset>();
        preset.ambientColor = ambientColor;
        preset.ambientIntensity = ambientIntensity;
        preset.directionalColor = sunColor;
        preset.directionalIntensity = sunIntensity;
        preset.lightRotation = Quaternion.Euler(sunRotX, sunRotY, 0f);
        preset.fogColor = fogColor;
        preset.fogDensity = fogDensity;

        string path = EditorUtility.SaveFilePanelInProject(
            "LightingPreset 저장",
            "LightingPreset_New",
            "asset",
            "저장할 위치를 선택하세요",
            "Assets/02_SDH/Lighting"
        );

        if (!string.IsNullOrEmpty(path))
        {
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LightingTest] 프리셋 저장: {path}");
        }
    }
}
