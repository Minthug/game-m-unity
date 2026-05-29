using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

    // ── 방 배치 저장 데이터 리셋 (테스트용) ─────────────────────
    [MenuItem("Game-M/Reset Room Save")]
    static void ResetRoomSave()
    {
        PlayerPrefs.DeleteKey("room_save");
        PlayerPrefs.Save();
        Debug.Log("[Game-M] 방 배치 저장 데이터 초기화 완료");
    }

    // ── Main 씬 세팅 (슬라임 월드) ───────────────────────────────
    [MenuItem("Game-M/Setup Scene")]
    static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Game-M] 플레이 모드 중에는 실행할 수 없어요. ■ 버튼으로 플레이를 멈춘 후 실행하세요.");
            return;
        }

        // 0. EventSystem 항상 먼저 교체 (StandaloneInputModule → InputSystemUIInputModule)
        var oldES = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (oldES != null) Object.DestroyImmediate(oldES.gameObject);
        var esGO2 = new GameObject("EventSystem");
        esGO2.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO2.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        Debug.Log("[Game-M] EventSystem 재생성 완료 (InputSystemUIInputModule)");

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

        // 6. EnvironmentManager 생성
        var existingEnv = Object.FindFirstObjectByType<EnvironmentManager>();
        var envGO       = existingEnv != null ? existingEnv.gameObject : new GameObject("EnvironmentManager");
        var envMgr      = envGO.GetComponent<EnvironmentManager>() ?? envGO.AddComponent<EnvironmentManager>();
        SetupMistItem(envMgr);

        // 6-1. BackgroundThemeManager 생성
        var existingBgTheme = Object.FindFirstObjectByType<BackgroundThemeManager>();
        var bgThemeGO = existingBgTheme != null ? existingBgTheme.gameObject : new GameObject("BackgroundThemeManager");
        if (bgThemeGO.GetComponent<BackgroundThemeManager>() == null)
            bgThemeGO.AddComponent<BackgroundThemeManager>();

        // 6-2. AdManager 생성
        var existingAd = Object.FindFirstObjectByType<AdManager>();
        var adGO = existingAd != null ? existingAd.gameObject : new GameObject("AdManager");
        if (adGO.GetComponent<AdManager>() == null)
            adGO.AddComponent<AdManager>();

        // 6-3. SettingsManager 생성
        var existingSettings = Object.FindFirstObjectByType<SettingsManager>();
        var settingsGO = existingSettings != null ? existingSettings.gameObject : new GameObject("SettingsManager");
        if (settingsGO.GetComponent<SettingsManager>() == null)
            settingsGO.AddComponent<SettingsManager>();

        // 6-4. StatsManager 생성
        var existingStats = Object.FindFirstObjectByType<StatsManager>();
        var statsGO = existingStats != null ? existingStats.gameObject : new GameObject("StatsManager");
        if (statsGO.GetComponent<StatsManager>() == null)
            statsGO.AddComponent<StatsManager>();

        // 6-5. SideMenuManager 생성
        var existingSideMenu = Object.FindFirstObjectByType<SideMenuManager>();
        var sideMenuGO = existingSideMenu != null ? existingSideMenu.gameObject : new GameObject("SideMenuManager");
        if (sideMenuGO.GetComponent<SideMenuManager>() == null)
            sideMenuGO.AddComponent<SideMenuManager>();

        // 6-6. MilestoneManager 생성
        var existingMilestone = Object.FindFirstObjectByType<MilestoneManager>();
        var milestoneGO = existingMilestone != null ? existingMilestone.gameObject : new GameObject("MilestoneManager");
        if (milestoneGO.GetComponent<MilestoneManager>() == null)
            milestoneGO.AddComponent<MilestoneManager>();

        // 6-7. AudioManager 생성
        var existingAudio = Object.FindFirstObjectByType<AudioManager>();
        var audioGO = existingAudio != null ? existingAudio.gameObject : new GameObject("AudioManager");
        if (audioGO.GetComponent<AudioManager>() == null)
            audioGO.AddComponent<AudioManager>();

        // 7. HeartRoom 폴더 + RoomManager + UI 생성
        SetupHeartRoom();
        SetupRoomUI();
        RefreshRoomCatalog(); // 아이템 자동 등록
        SetupDefaultBgThemes();
        RefreshBgThemeCatalog();

        // 7. 카메라 설정
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0f, 0f, -10f);
            Camera.main.backgroundColor    = new Color(0.086f, 0.086f, 0.094f);
            Camera.main.clearFlags         = CameraClearFlags.SolidColor;
            Camera.main.orthographic       = true;
            Camera.main.orthographicSize   = 5f;
            if (Camera.main.GetComponent<AudioListener>() == null)
                Camera.main.gameObject.AddComponent<AudioListener>();
        }

        AssetDatabase.SaveAssets();
        var activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        if (!string.IsNullOrEmpty(activeScene.path))
            EditorSceneManager.SaveScene(activeScene);
        Debug.Log("씬 세팅 완료! SlimeManager와 Slime Prefab이 준비됐어요.");
    }

    // ── Room 카탈로그 갱신 ───────────────────────────────────────
    [MenuItem("Game-M/Refresh Room Catalog")]
    static void RefreshRoomCatalog()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("[Game-M] 플레이 중에는 실행 불가"); return; }

        var roomMgr = Object.FindFirstObjectByType<RoomManager>();
        if (roomMgr == null) { Debug.LogError("[Game-M] RoomManager가 씬에 없음 — Setup Scene 먼저 실행"); return; }

        const string itemsDir = "Assets/HeartRoom/Items";
        System.IO.Directory.CreateDirectory(itemsDir);
        AssetDatabase.Refresh();

        // FBX / 프리팹 파일 발견 시 RoomItem ScriptableObject 자동 생성
        var modelExtensions = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { ".fbx", ".obj", ".prefab" };
        var spriteExtensions = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".psd", ".tga" };

        foreach (var file in System.IO.Directory.GetFiles(itemsDir, "*", System.IO.SearchOption.AllDirectories))
        {
            var ext = System.IO.Path.GetExtension(file);
            if (!modelExtensions.Contains(ext) && !spriteExtensions.Contains(ext)) continue;

            var filePath = file.Replace('\\', '/');
            var baseName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            var assetPath = $"{itemsDir}/{baseName}.asset";

            if (System.IO.File.Exists(assetPath)) continue; // 이미 있으면 스킵

            var roomItem = ScriptableObject.CreateInstance<RoomItem>();
            roomItem.itemId      = baseName.ToLower().Replace(" ", "_");
            roomItem.displayName = baseName;
            roomItem.defaultScale = 1f;
            roomItem.isUnlocked  = true;
            roomItem.price       = 0;

            if (modelExtensions.Contains(ext))
            {
                // FBX/프리팹 → prefab 필드에 할당
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(filePath);
                if (prefabAsset != null) roomItem.prefab = prefabAsset;
            }
            else
            {
                // 스프라이트로 임포트
                var importer = AssetImporter.GetAtPath(filePath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType      = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
                }
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
                if (sprite != null) roomItem.sprite = sprite;
            }

            AssetDatabase.CreateAsset(roomItem, assetPath);
            Debug.Log($"[Game-M] RoomItem 자동 생성: {assetPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // .asset 파일 전부 카탈로그에 등록
        roomMgr.itemCatalog.Clear();
        roomMgr.themeCatalog.Clear();

        LoadAssetsFromFolder(itemsDir, path => {
            var item = AssetDatabase.LoadAssetAtPath<RoomItem>(path);
            if (item != null) roomMgr.itemCatalog.Add(item);
        });

        LoadAssetsFromFolder("Assets/HeartRoom/Themes", path => {
            var theme = AssetDatabase.LoadAssetAtPath<RoomTheme>(path);
            if (theme != null) roomMgr.themeCatalog.Add(theme);
        });

        EditorUtility.SetDirty(roomMgr);
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!string.IsNullOrEmpty(scene.path))
            EditorSceneManager.SaveScene(scene);
        else
            Debug.LogWarning("[Game-M] 씬 경로 없음 — Game-M/1. Save As Main Scene 으로 저장 후 다시 실행하세요");

        Debug.Log($"[Game-M] Room 카탈로그 갱신 완료 — 아이템 {roomMgr.itemCatalog.Count}개, 테마 {roomMgr.themeCatalog.Count}개");
    }

    // ── 현재 씬을 Main으로 저장 ──────────────────────────────────
    [MenuItem("Game-M/1. Save As Main Scene")]
    static void SaveAsMain()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Game-M] 플레이 모드 중에는 씬을 저장할 수 없어요. 플레이를 멈춘 후 실행하세요.");
            return;
        }
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
        // AudioListener가 없으면 오디오 전체가 무음 — 반드시 필요
        if (camGO.GetComponent<AudioListener>() == null)
            camGO.AddComponent<AudioListener>();

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

    static void SetupHeartRoom()
    {
        System.IO.Directory.CreateDirectory("Assets/HeartRoom/Items");
        System.IO.Directory.CreateDirectory("Assets/HeartRoom/Themes");
        System.IO.Directory.CreateDirectory("Assets/HeartRoom/Sprites");
        System.IO.Directory.CreateDirectory("Assets/HeartRoom/Prefabs");
        AssetDatabase.Refresh();

        // RoomItemObject 프리팹 생성
        const string prefabPath = "Assets/HeartRoom/Prefabs/RoomItemObject.prefab";
        if (!System.IO.File.Exists(prefabPath))
        {
            var go  = new GameObject("RoomItemObject");
            go.AddComponent<SpriteRenderer>();
            var col  = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            go.AddComponent<RoomItemObject>();
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("[Game-M] RoomItemObject 프리팹 생성 완료");
        }

        // RoomManager 씬에 추가
        var existing = Object.FindFirstObjectByType<RoomManager>();
        var roomGO   = existing != null ? existing.gameObject : new GameObject("RoomManager");
        var roomMgr  = roomGO.GetComponent<RoomManager>() ?? roomGO.AddComponent<RoomManager>();
        if (roomMgr.roomItemPrefab == null)
            roomMgr.roomItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        EditorUtility.SetDirty(roomMgr);
        Debug.Log("[Game-M] HeartRoom 세팅 완료 — Assets/HeartRoom/ 폴더 생성됨");
    }

    [MenuItem("Game-M/Recreate Room UI")]
    static void RecreateRoomUI()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("[Game-M] 플레이 중에는 실행 불가"); return; }
        var uiMgrExisting = Object.FindFirstObjectByType<RoomUIManager>();
        if (uiMgrExisting != null)
        {
            var existingCanvas = uiMgrExisting.rootCanvas;
            if (existingCanvas != null) Object.DestroyImmediate(existingCanvas.gameObject);
            else Object.DestroyImmediate(uiMgrExisting.gameObject);
        }
        SetupRoomUI();
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);
        Debug.Log("[Game-M] Room UI 재생성 완료");
    }

    static void SetupRoomUI()
    {
        // 이미 있으면 스킵 (강제 재생성은 Game-M/Recreate Room UI 사용)
        var uiMgrExisting = Object.FindFirstObjectByType<RoomUIManager>();
        if (uiMgrExisting != null) { Debug.Log("[Game-M] RoomUI 이미 존재 — 스킵 (재생성: Game-M/Recreate Room UI)"); return; }

        // Canvas
        var canvasGO = new GameObject("RoomCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // EventSystem은 Setup() 최상단에서 처리

        // ── 상점 열기 버튼 (우하단) ──────────────────────────────
        // Safe Area 컨테이너 — 런타임에 SafeAreaFitter가 조정
        var btnSafeGO = new GameObject("BtnSafeArea");
        btnSafeGO.transform.SetParent(canvasGO.transform, false);
        btnSafeGO.AddComponent<SafeAreaFitter>();

        var openBtnGO  = MakeButton(btnSafeGO.transform, "OpenShopBtn", "방꾸미기");
        var openRect   = openBtnGO.GetComponent<RectTransform>();
        openRect.anchorMin = openRect.anchorMax = new Vector2(1f, 0f);
        openRect.pivot     = new Vector2(1f, 0f);
        openRect.anchoredPosition = new Vector2(-20f, 521f);
        openRect.sizeDelta        = new Vector2(110f, 110f);
        openBtnGO.GetComponentInChildren<TextMeshProUGUI>().fontSize = 15f;

        // ── 상점 패널 (하단에서 슬라이드) ───────────────────────
        var panelGO  = new GameObject("ShopPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImg  = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.10f, 0.96f);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot     = new Vector2(0.5f, 0f);
        panelRect.sizeDelta        = new Vector2(0f, 480f); // 탭바 추가로 60px 증가
        panelRect.anchoredPosition = new Vector2(0f, 0f);

        // 닫기 버튼
        var closeBtnGO = MakeButton(panelGO.transform, "CloseBtn", "닫기");
        var closeRect  = closeBtnGO.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot     = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-16f, -12f);
        closeRect.sizeDelta        = new Vector2(100f, 40f);

        // ── 탭 바 ─────────────────────────────────────────────────
        var tabBarGO = new GameObject("TabBar");
        tabBarGO.transform.SetParent(panelGO.transform, false);
        var tabBarRt = tabBarGO.AddComponent<RectTransform>();
        tabBarRt.anchorMin        = new Vector2(0f, 1f);
        tabBarRt.anchorMax        = new Vector2(1f, 1f);
        tabBarRt.pivot            = new Vector2(0.5f, 1f);
        tabBarRt.anchoredPosition = new Vector2(0f, -56f);
        tabBarRt.sizeDelta        = new Vector2(-32f, 44f);
        var tabHLG = tabBarGO.AddComponent<HorizontalLayoutGroup>();
        tabHLG.spacing              = 8f;
        tabHLG.childForceExpandWidth  = true;
        tabHLG.childForceExpandHeight = true;
        tabHLG.padding = new RectOffset(0, 0, 0, 0);

        var tabDecorGO  = MakeButton(tabBarGO.transform, "Tab_Decor",   "방 꾸미기");
        var tabBgGO     = MakeButton(tabBarGO.transform, "Tab_BgTheme", "배경 테마");
        // 비활성 탭은 어두운 색으로
        tabBgGO.GetComponent<Image>().color = new Color(0.20f, 0.20f, 0.26f, 1f);

        // ── 스크롤뷰 (방 꾸미기) ─────────────────────────────────
        var scrollGO   = new GameObject("ScrollView_Decor");
        scrollGO.transform.SetParent(panelGO.transform, false);
        var scrollRect2 = scrollGO.AddComponent<ScrollRect>();
        var scrollRt   = scrollGO.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(16f, 16f);
        scrollRt.offsetMax = new Vector2(-16f, -108f); // 닫기(52) + 탭바(44) + 여백(12)

        var vpGO  = new GameObject("Viewport");
        vpGO.transform.SetParent(scrollGO.transform, false);
        vpGO.AddComponent<RectMask2D>();
        var vpRt  = vpGO.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;

        var contentGO  = new GameObject("Content");
        contentGO.transform.SetParent(vpGO.transform, false);
        var contentRt  = contentGO.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot     = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = new Vector2(0f, 300f);

        var grid = contentGO.AddComponent<GridLayoutGroup>();
        grid.cellSize   = new Vector2(160f, 180f);
        grid.spacing    = new Vector2(12f, 12f);
        grid.padding    = new RectOffset(12, 12, 12, 12);
        grid.constraint = GridLayoutGroup.Constraint.Flexible;
        contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect2.content    = contentRt;
        scrollRect2.viewport   = vpRt;
        scrollRect2.horizontal = false;

        // ── 스크롤뷰 (배경 테마) ─────────────────────────────────
        var bgScrollGO  = new GameObject("ScrollView_BgTheme");
        bgScrollGO.transform.SetParent(panelGO.transform, false);
        var bgScrollRect = bgScrollGO.AddComponent<ScrollRect>();
        var bgScrollRt   = bgScrollGO.GetComponent<RectTransform>();
        bgScrollRt.anchorMin = new Vector2(0f, 0f);
        bgScrollRt.anchorMax = new Vector2(1f, 1f);
        bgScrollRt.offsetMin = new Vector2(16f, 16f);
        bgScrollRt.offsetMax = new Vector2(-16f, -108f);

        var bgVpGO = new GameObject("Viewport");
        bgVpGO.transform.SetParent(bgScrollGO.transform, false);
        bgVpGO.AddComponent<RectMask2D>();
        var bgVpRt = bgVpGO.GetComponent<RectTransform>();
        bgVpRt.anchorMin = Vector2.zero; bgVpRt.anchorMax = Vector2.one;
        bgVpRt.offsetMin = bgVpRt.offsetMax = Vector2.zero;

        var bgContentGO = new GameObject("Content");
        bgContentGO.transform.SetParent(bgVpGO.transform, false);
        var bgContentRt = bgContentGO.AddComponent<RectTransform>();
        bgContentRt.anchorMin = new Vector2(0f, 1f);
        bgContentRt.anchorMax = new Vector2(1f, 1f);
        bgContentRt.pivot     = new Vector2(0.5f, 1f);
        bgContentRt.sizeDelta = new Vector2(0f, 300f);

        var bgGrid = bgContentGO.AddComponent<GridLayoutGroup>();
        bgGrid.cellSize   = new Vector2(160f, 180f);
        bgGrid.spacing    = new Vector2(12f, 12f);
        bgGrid.padding    = new RectOffset(12, 12, 12, 12);
        bgGrid.constraint = GridLayoutGroup.Constraint.Flexible;
        bgContentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        bgScrollRect.content    = bgContentRt;
        bgScrollRect.viewport   = bgVpRt;
        bgScrollRect.horizontal = false;
        bgScrollGO.SetActive(false); // 기본적으로 숨김

        // ── 아이템 버튼 프리팹 (한글 폰트 변경 시 강제 재생성) ──────
        const string itemBtnPath = "Assets/HeartRoom/Prefabs/ItemButton.prefab";
        if (System.IO.File.Exists(itemBtnPath)) AssetDatabase.DeleteAsset(itemBtnPath);
        if (true) // 항상 재생성
        {
            var ibGO   = new GameObject("ItemButton");
            var ibImg  = ibGO.AddComponent<Image>();
            ibImg.color = new Color(0.15f, 0.15f, 0.18f, 1f);
            ibGO.AddComponent<Button>();
            var ibRt   = ibGO.GetComponent<RectTransform>();
            ibRt.sizeDelta = new Vector2(160f, 180f);

            // 아이콘
            var iconGO  = new GameObject("Icon");
            iconGO.transform.SetParent(ibGO.transform, false);
            var iconImg = iconGO.AddComponent<Image>();
            iconImg.color = Color.white;
            var iconRt  = iconGO.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.1f, 0.35f);
            iconRt.anchorMax = new Vector2(0.9f, 0.95f);
            iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;

            // 이름
            var labelGO = MakeTMP(ibGO.transform, "Label", "아이템", 13f);
            var labelRt = labelGO.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0.18f);
            labelRt.anchorMax = new Vector2(1f, 0.38f);
            labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;

            // 가격
            var priceGO = MakeTMP(ibGO.transform, "Price", "무료", 11f);
            var priceRt = priceGO.GetComponent<RectTransform>();
            priceRt.anchorMin = new Vector2(0f, 0f);
            priceRt.anchorMax = new Vector2(1f, 0.2f);
            priceRt.offsetMin = priceRt.offsetMax = Vector2.zero;
            priceGO.GetComponent<TextMeshProUGUI>().color = new Color(0.6f, 0.6f, 0.7f);

            // 잠금 오버레이
            var lockGO  = new GameObject("Lock");
            lockGO.transform.SetParent(ibGO.transform, false);
            var lockImg = lockGO.AddComponent<Image>();
            lockImg.color = new Color(0f, 0f, 0f, 0.6f);
            var lockRt  = lockGO.GetComponent<RectTransform>();
            lockRt.anchorMin = Vector2.zero; lockRt.anchorMax = Vector2.one;
            lockRt.offsetMin = lockRt.offsetMax = Vector2.zero;
            var lockLabel = MakeTMP(lockGO.transform, "LockIcon", "잠금", 16f);
            var llRt = lockLabel.GetComponent<RectTransform>();
            llRt.anchorMin = Vector2.zero; llRt.anchorMax = Vector2.one;
            llRt.offsetMin = llRt.offsetMax = Vector2.zero;

            PrefabUtility.SaveAsPrefabAsset(ibGO, itemBtnPath);
            Object.DestroyImmediate(ibGO);
        }

        // ── 배경 테마 버튼 프리팹 ─────────────────────────────────
        System.IO.Directory.CreateDirectory("Assets/Backgrounds/Prefabs");
        AssetDatabase.Refresh();
        const string bgBtnPath = "Assets/Backgrounds/Prefabs/BgThemeButton.prefab";
        if (System.IO.File.Exists(bgBtnPath)) AssetDatabase.DeleteAsset(bgBtnPath);
        if (true) // 항상 재생성
        {
            var bgBtnGO  = new GameObject("BgThemeButton");
            var bgBtnImg = bgBtnGO.AddComponent<Image>(); // 카드 전체가 테마 색상 프리뷰
            bgBtnImg.color = new Color(0.10f, 0.10f, 0.14f, 1f);
            bgBtnGO.AddComponent<Button>();
            bgBtnGO.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 180f);

            // 이름 (상단)
            var bgLabelGO = MakeTMP(bgBtnGO.transform, "Label", "테마", 13f);
            var bgLabelRt = bgLabelGO.GetComponent<RectTransform>();
            bgLabelRt.anchorMin = new Vector2(0f, 0.65f);
            bgLabelRt.anchorMax = new Vector2(1f, 0.90f);
            bgLabelRt.offsetMin = bgLabelRt.offsetMax = Vector2.zero;

            // 액션 버튼 (하단)
            var actionGO  = new GameObject("ActionBtn");
            actionGO.transform.SetParent(bgBtnGO.transform, false);
            var actionImg = actionGO.AddComponent<Image>();
            actionImg.color = new Color(0.45f, 0.22f, 0.93f, 0.9f);
            actionGO.AddComponent<Button>();
            var actionRt  = actionGO.GetComponent<RectTransform>();
            actionRt.anchorMin = new Vector2(0.08f, 0.04f);
            actionRt.anchorMax = new Vector2(0.92f, 0.32f);
            actionRt.offsetMin = actionRt.offsetMax = Vector2.zero;

            var actionTxtGO = MakeTMP(actionGO.transform, "Text", "광고 보기", 11f);
            var actionTxtRt = actionTxtGO.GetComponent<RectTransform>();
            actionTxtRt.anchorMin = Vector2.zero; actionTxtRt.anchorMax = Vector2.one;
            actionTxtRt.offsetMin = actionTxtRt.offsetMax = Vector2.zero;

            // 잠금 오버레이
            var bgLockGO  = new GameObject("Lock");
            bgLockGO.transform.SetParent(bgBtnGO.transform, false);
            var bgLockImg = bgLockGO.AddComponent<Image>();
            bgLockImg.color = new Color(0f, 0f, 0f, 0.5f);
            var bgLockRt  = bgLockGO.GetComponent<RectTransform>();
            bgLockRt.anchorMin = Vector2.zero; bgLockRt.anchorMax = Vector2.one;
            bgLockRt.offsetMin = bgLockRt.offsetMax = Vector2.zero;
            var bgLockIcon = MakeTMP(bgLockGO.transform, "LockIcon", "잠금", 14f);
            var bliRt = bgLockIcon.GetComponent<RectTransform>();
            bliRt.anchorMin = new Vector2(0f, 0.35f); bliRt.anchorMax = new Vector2(1f, 0.65f);
            bliRt.offsetMin = bliRt.offsetMax = Vector2.zero;

            PrefabUtility.SaveAsPrefabAsset(bgBtnGO, bgBtnPath);
            Object.DestroyImmediate(bgBtnGO);
        }

        // ── RoomUIManager 연결 ────────────────────────────────────
        var uiMgrGO = new GameObject("RoomUIManager");
        uiMgrGO.transform.SetParent(canvasGO.transform, false);
        var uiMgr = uiMgrGO.AddComponent<RoomUIManager>();

        uiMgr.rootCanvas         = canvas;
        uiMgr.shopPanel          = panelGO;
        uiMgr.itemGrid           = contentRt;
        uiMgr.itemButtonPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>(itemBtnPath);
        uiMgr.tabDecorBtn        = tabDecorGO.GetComponent<Button>();
        uiMgr.tabBgBtn           = tabBgGO.GetComponent<Button>();
        uiMgr.scrollViewDecor    = scrollGO;
        uiMgr.scrollViewBg       = bgScrollGO;
        uiMgr.bgItemGrid         = bgContentRt;
        uiMgr.bgThemeButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bgBtnPath);

        var openBtn  = openBtnGO.GetComponent<Button>();
        var closeBtn = closeBtnGO.GetComponent<Button>();
        uiMgr.openShopBtn  = openBtn;
        uiMgr.closeShopBtn = closeBtn;
        // onClick은 RoomUIManager.Start()에서 런타임에 연결

        EditorUtility.SetDirty(uiMgrGO);
        Debug.Log("[Game-M] RoomUI 생성 완료 (탭 포함)");
    }

    static GameObject MakeButton(Transform parent, string name, string label)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img  = go.AddComponent<Image>();
        img.color = new Color(0.45f, 0.22f, 0.93f, 1f); // 보라
        go.AddComponent<Button>();

        var tmp = MakeTMP(go.transform, "Text", label, 14f);
        var rt  = tmp.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        return go;
    }

    static TMP_FontAsset _editorKorFont;
    static TMP_FontAsset EditorKorFont =>
        _editorKorFont != null ? _editorKorFont
            : (_editorKorFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Resources/KoreanFont.asset"));

    static GameObject MakeTMP(Transform parent, string name, string text, float size)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (EditorKorFont != null) tmp.font = EditorKorFont; // 프리팹에 한글 폰트 구워넣기
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return go;
    }

    // ── 배경 테마 기본 에셋 생성 ─────────────────────────────────
    static void SetupDefaultBgThemes()
    {
        // Resources 폴더에 저장 → 런타임 자동 로드 가능
        const string dir = "Assets/Resources/BackgroundThemes";
        System.IO.Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();

        //                id             displayName   top        bottom     adUnlock  defaultUnlocked
        CreateBgTheme(dir, "default",     "기본 배경",  "#0A0A0F", "#0A0A0F", false,    true);
        CreateBgTheme(dir, "dawn_purple", "새벽 보라",  "#3D1080", "#06000F", true,     false);
        CreateBgTheme(dir, "deep_sea",    "심해",       "#005080", "#000615", true,     false);
        CreateBgTheme(dir, "sunset",      "석양 노을",  "#A03800", "#1E0028", true,     false);
        CreateBgTheme(dir, "forest",      "달빛 숲",    "#0E4020", "#000A03", true,     false);
        CreateBgTheme(dir, "rose_night",  "장미빛 밤",  "#6B1040", "#100008", true,     false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Game-M] 기본 배경 테마 에셋 생성 완료 (Assets/Resources/BackgroundThemes/)");
    }

    static void CreateBgTheme(string dir, string id, string name,
                               string hexTop, string hexBottom,
                               bool adUnlock, bool defaultUnlocked)
    {
        var path = $"{dir}/{id}.asset";
        ColorUtility.TryParseHtmlString(hexTop,    out var colorTop);
        ColorUtility.TryParseHtmlString(hexBottom, out var colorBottom);

        var t = AssetDatabase.LoadAssetAtPath<BackgroundTheme>(path);
        if (t == null)
        {
            t = ScriptableObject.CreateInstance<BackgroundTheme>();
            AssetDatabase.CreateAsset(t, path);
        }

        t.themeId         = id;
        t.displayName     = name;
        t.bgColor         = colorTop;
        t.bgColorBottom   = colorBottom;
        t.isAdUnlock      = adUnlock;
        t.defaultUnlocked = defaultUnlocked;
        EditorUtility.SetDirty(t);
    }

    [MenuItem("Game-M/Refresh Background Theme Catalog")]
    static void RefreshBgThemeCatalog()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("[Game-M] 플레이 중에는 실행 불가"); return; }

        var mgr = Object.FindFirstObjectByType<BackgroundThemeManager>();
        if (mgr == null) { Debug.LogError("[Game-M] BackgroundThemeManager 없음 — Setup Scene 먼저 실행"); return; }

        const string dir = "Assets/Resources/BackgroundThemes";
        if (!System.IO.Directory.Exists(dir))
        {
            Debug.LogWarning($"[Game-M] {dir} 폴더 없음 — Setup Scene 실행 후 다시 시도");
            return;
        }

        mgr.catalog.Clear();
        LoadAssetsFromFolder(dir, path => {
            var theme = AssetDatabase.LoadAssetAtPath<BackgroundTheme>(path);
            if (theme != null) mgr.catalog.Add(theme);
        });

        // "default" 테마를 맨 앞으로 정렬
        mgr.catalog.Sort((a, b) => {
            if (a.themeId == "default") return -1;
            if (b.themeId == "default") return 1;
            return string.Compare(a.themeId, b.themeId, System.StringComparison.Ordinal);
        });

        EditorUtility.SetDirty(mgr);
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!string.IsNullOrEmpty(scene.path))
            EditorSceneManager.SaveScene(scene);

        Debug.Log($"[Game-M] 배경 테마 카탈로그 갱신 완료 — {mgr.catalog.Count}개");
    }

    [MenuItem("Game-M/Reset Background Theme Saves")]
    static void ResetBgThemeSaves()
    {
        PlayerPrefs.DeleteKey("bg_unlocked_v1");
        PlayerPrefs.DeleteKey("bg_active");
        PlayerPrefs.Save();
        Debug.Log("[Game-M] 배경 테마 저장 데이터 초기화 완료");
    }

    static void LoadAssetsFromFolder(string folder, System.Action<string> onLoad)
    {
        if (!System.IO.Directory.Exists(folder))
        {
            Debug.LogWarning($"[Game-M] 폴더 없음: {folder}");
            return;
        }
        var files = System.IO.Directory.GetFiles(folder, "*.asset", System.IO.SearchOption.AllDirectories);
        Debug.Log($"[Game-M] {folder}: .asset {files.Length}개 발견");
        foreach (var file in files)
        {
            var path = file.Replace('\\', '/');
            onLoad(path);
        }
    }

    static void SetupMistItem(EnvironmentManager envMgr)
    {
        const string envDir    = "Assets/Environment";
        const string mistAsset = "Assets/Environment/Mist.asset";

        System.IO.Directory.CreateDirectory(envDir);
        AssetDatabase.Refresh();

        // 대소문자 무관하게 "mist" 포함 PNG 파일 검색
        var pngs = System.IO.Directory.GetFiles(envDir, "*.png", System.IO.SearchOption.TopDirectoryOnly);
        string mistPng = System.Array.Find(pngs, p =>
            System.IO.Path.GetFileNameWithoutExtension(p).ToLower().Contains("mist"));

        if (mistPng == null)
        {
            Debug.Log($"[Game-M] {envDir}/ 안에 mist.png 없음 — 폴더는 생성됨, 파일 넣고 Setup Scene 재실행");
            Debug.Log($"[Game-M] 현재 {envDir}/ 에 있는 PNG: {string.Join(", ", System.Array.ConvertAll(pngs, System.IO.Path.GetFileName))}");
            return;
        }

        // 경로 구분자 통일
        mistPng = mistPng.Replace('\\', '/');

        // PNG → Sprite 임포트
        var importer = AssetImporter.GetAtPath(mistPng) as TextureImporter;
        if (importer != null)
        {
            importer.textureType      = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.wrapMode         = TextureWrapMode.Repeat;
            importer.filterMode       = FilterMode.Bilinear;
            AssetDatabase.ImportAsset(mistPng, ImportAssetOptions.ForceUpdate);
        }

        // EnvironmentItem ScriptableObject 생성 or 로드
        var mistItem = AssetDatabase.LoadAssetAtPath<EnvironmentItem>(mistAsset);
        if (mistItem == null)
        {
            mistItem             = ScriptableObject.CreateInstance<EnvironmentItem>();
            mistItem.itemId      = "mist";
            mistItem.displayName = "안개";
            mistItem.alpha       = 0.25f;
            mistItem.scale       = 1.2f;
            mistItem.scrollSpeedX = 0.15f;
            mistItem.sortingOrder = 5;
            mistItem.isUnlocked  = true;
            mistItem.price       = 0;
            AssetDatabase.CreateAsset(mistItem, mistAsset);
        }

        mistItem.sprite = LoadSprite(mistPng);
        EditorUtility.SetDirty(mistItem);

        if (!envMgr.catalog.Contains(mistItem))
            envMgr.catalog.Add(mistItem);

        EditorUtility.SetDirty(envMgr);
        AssetDatabase.SaveAssets();
        Debug.Log("[Game-M] Mist 환경 아이템 세팅 완료");
    }
}
