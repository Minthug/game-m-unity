using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class SceneSetup
{
    // ── 한글 폰트 에셋 자동 생성 ─────────────────────────────────
    [MenuItem("Game-M/0. Setup Korean Font")]
    static void SetupKoreanFont()
    {
        const string srcFont = @"C:\Windows\Fonts\malgun.ttf";
        const string dstFont = "Assets/Fonts/malgun.ttf";
        const string assetPath = "Assets/Resources/KoreanFont.asset";

        if (!System.IO.File.Exists(srcFont))
        {
            Debug.LogError("맑은 고딕을 찾을 수 없습니다: " + srcFont);
            return;
        }

        System.IO.Directory.CreateDirectory("Assets/Fonts");
        System.IO.Directory.CreateDirectory("Assets/Resources");

        if (!System.IO.File.Exists(dstFont))
            System.IO.File.Copy(srcFont, dstFont);

        AssetDatabase.ImportAsset(dstFont);
        var font = AssetDatabase.LoadAssetAtPath<Font>(dstFont);
        if (font == null) { Debug.LogError("폰트 임포트 실패"); return; }

        // Dynamic 모드: 런타임에 한글 글리프를 atlas에 자동 추가
        var fontAsset = TMP_FontAsset.CreateFontAsset(font);
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        AssetDatabase.CreateAsset(fontAsset, assetPath);

        // atlas 텍스처와 material을 sub-asset으로 저장 (없으면 런타임 에러)
        if (fontAsset.atlasTextures != null)
        {
            foreach (var tex in fontAsset.atlasTextures)
            {
                if (tex != null)
                {
                    tex.name = "Atlas";
                    AssetDatabase.AddObjectToAsset(tex, fontAsset);
                }
            }
        }
        if (fontAsset.material != null)
        {
            fontAsset.material.name = "Atlas Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("한글 폰트 에셋 생성 완료 → " + assetPath);
    }

    // ── 온보딩 PlayerPrefs 리셋 (테스트용) ──────────────────────
    [MenuItem("Game-M/Reset Onboarding PlayerPrefs")]
    static void ResetOnboarding()
    {
        PlayerPrefs.DeleteKey("onboarding_done");
        PlayerPrefs.DeleteKey("first_slime_text");
        PlayerPrefs.DeleteKey("first_slime_expression");
        PlayerPrefs.Save();
        Debug.Log("온보딩 PlayerPrefs 리셋 완료");
    }

    // ── Main 씬 세팅 (슬라임 월드) ───────────────────────────────
    [MenuItem("Game-M/Setup Scene")]
    static void Setup()
    {
        // 1. 슬라임 PNG들을 Sprite 타입으로 변환
        string[] slimePaths = {
            "Assets/Slimes/slime-angry.png",
            "Assets/Slimes/slime-sad.png",
            "Assets/Slimes/slime-fear.png",
            "Assets/Slimes/slime-happy.png",
            "Assets/Slimes/slime-disgust.png",
            "Assets/Slimes/slime-surprised.png",
            "Assets/Slimes/slime-contempt.png",
            "Assets/Slimes/slime-blank.png",
        };

        foreach (var path in slimePaths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogWarning($"임포터 없음: {path}"); continue; }
            importer.textureType      = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode       = FilterMode.Bilinear;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        // 2. Slime Prefab 생성
        System.IO.Directory.CreateDirectory("Assets/Prefabs");
        var slimeGO = new GameObject("Slime");
        var sr      = slimeGO.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";

        var rb            = slimeGO.AddComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.linearDamping  = 0f;
        rb.angularDamping = 5f;
        rb.constraints    = RigidbodyConstraints2D.FreezeRotation;

        var col    = slimeGO.AddComponent<CircleCollider2D>();
        col.radius = 0.42f;

        slimeGO.AddComponent<SlimeController>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(slimeGO, "Assets/Prefabs/Slime.prefab");
        Object.DestroyImmediate(slimeGO);
        Debug.Log("Slime Prefab 생성 완료");

        // 3. SlimeManager 오브젝트 생성
        var existing  = Object.FindFirstObjectByType<SlimeManager>();
        var managerGO = existing != null ? existing.gameObject : new GameObject("SlimeManager");
        var manager   = managerGO.GetComponent<SlimeManager>() ?? managerGO.AddComponent<SlimeManager>();

        manager.slimePrefab     = prefab;
        manager.spriteAngry     = LoadSprite("Assets/Slimes/slime-angry.png");
        manager.spriteSad       = LoadSprite("Assets/Slimes/slime-sad.png");
        manager.spriteFear      = LoadSprite("Assets/Slimes/slime-fear.png");
        manager.spriteHappy     = LoadSprite("Assets/Slimes/slime-happy.png");
        manager.spriteDisgust   = LoadSprite("Assets/Slimes/slime-disgust.png");
        manager.spriteSurprised = LoadSprite("Assets/Slimes/slime-surprised.png");
        manager.spriteContempt  = LoadSprite("Assets/Slimes/slime-contempt.png");
        manager.spriteBlank     = LoadSprite("Assets/Slimes/slime-blank.png");

        // 4. TestSpawner 추가 (에디터/개발 빌드 전용)
        if (managerGO.GetComponent<TestSpawner>() == null)
            managerGO.AddComponent<TestSpawner>();

        // Main 씬에 잘못 포함된 OnboardingManager 제거 후 씬 저장
        var strayOnboarding = Object.FindFirstObjectByType<OnboardingManager>();
        if (strayOnboarding != null)
        {
            Object.DestroyImmediate(strayOnboarding.gameObject);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Main 씬에서 OnboardingManager 제거 및 저장 완료");
        }

        // 5. BackgroundManager 생성
        var existingBg = Object.FindFirstObjectByType<BackgroundManager>();
        var bgGO       = existingBg != null ? existingBg.gameObject : new GameObject("BackgroundManager");
        if (bgGO.GetComponent<BackgroundManager>() == null)
            bgGO.AddComponent<BackgroundManager>();

        // 5. 카메라 설정
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0f, 0f, -10f);
            Camera.main.backgroundColor    = new Color(0.086f, 0.086f, 0.094f);
            Camera.main.clearFlags         = CameraClearFlags.SolidColor;
            Camera.main.orthographic       = true;
            Camera.main.orthographicSize   = 5f;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("씬 세팅 완료! SlimeManager와 Slime Prefab이 준비됐어요.");
    }

    // ── 현재 씬을 Main으로 저장 ──────────────────────────────────
    [MenuItem("Game-M/1. Save As Main Scene")]
    static void SaveAsMain()
    {
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/Main.unity");
        UpdateBuildSettings();
        Debug.Log("Main 씬 저장 완료 (Assets/Scenes/Main.unity)");
    }

    // ── Onboarding 씬 생성 ───────────────────────────────────────
    [MenuItem("Game-M/2. Setup Onboarding Scene")]
    static void SetupOnboarding()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 카메라
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic     = true;
        cam.orthographicSize = 5f;
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.depth            = -1;
        ColorUtility.TryParseHtmlString("#0A0A0F", out var bgColor);
        cam.backgroundColor = bgColor;

        // OnboardingManager (UI·파티클은 런타임에 자체 생성)
        var managerGO = new GameObject("OnboardingManager");
        managerGO.AddComponent<OnboardingManager>();

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Onboarding.unity");
        UpdateBuildSettings();
        AssetDatabase.Refresh();
        Debug.Log("Onboarding 씬 생성 완료 (Assets/Scenes/Onboarding.unity)");
    }

    // ── Build Settings 업데이트 ──────────────────────────────────
    static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>();
        if (System.IO.File.Exists("Assets/Scenes/Onboarding.unity"))
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Onboarding.unity", true));
        if (System.IO.File.Exists("Assets/Scenes/Main.unity"))
            scenes.Add(new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static Sprite LoadSprite(string path) =>
        AssetDatabase.LoadAssetAtPath<Sprite>(path);
}
