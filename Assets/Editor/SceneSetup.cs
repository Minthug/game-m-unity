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

    // ── Main 씬 세팅 (슬라임 월드) ───────────────────────────────
    [MenuItem("Game-M/Setup Scene")]
    static void Setup()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[Game-M] 플레이 모드 중에는 실행할 수 없어요. ■ 버튼으로 플레이를 멈춘 후 실행하세요.");
            return;
        }

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

        // 7. HeartRoom 폴더 + RoomManager + UI 생성
        SetupHeartRoom();
        SetupRoomUI();

        // 7. 카메라 설정
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0f, 0f, -10f);
            Camera.main.backgroundColor    = new Color(0.086f, 0.086f, 0.094f);
            Camera.main.clearFlags         = CameraClearFlags.SolidColor;
            Camera.main.orthographic       = true;
            Camera.main.orthographicSize   = 5f;
        }

        AssetDatabase.SaveAssets();
        var activeScene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(activeScene);
        if (!string.IsNullOrEmpty(activeScene.path))
            EditorSceneManager.SaveScene(activeScene);
        Debug.Log("씬 세팅 완료! SlimeManager와 Slime Prefab이 준비됐어요.");
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
        // 폴더 생성
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

    static void SetupRoomUI()
    {
        // Canvas 찾기 or 생성
        var uiMgrExisting = Object.FindFirstObjectByType<RoomUIManager>();
        if (uiMgrExisting != null) { Debug.Log("[Game-M] RoomUI 이미 존재 — 스킵"); return; }

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

        // EventSystem — 기존 것 제거 후 New Input System 모듈로 새로 생성
        var existingES = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (existingES != null) Object.DestroyImmediate(existingES.gameObject);
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        Debug.Log("[Game-M] EventSystem (InputSystemUIInputModule) 생성 완료");

        // ── 상점 열기 버튼 (우하단) ──────────────────────────────
        var openBtnGO  = MakeButton(canvasGO.transform, "OpenShopBtn", "🛋️ 방 꾸미기");
        var openRect   = openBtnGO.GetComponent<RectTransform>();
        openRect.anchorMin = openRect.anchorMax = new Vector2(1f, 0f);
        openRect.pivot     = new Vector2(1f, 0f);
        openRect.anchoredPosition = new Vector2(-24f, 24f);
        openRect.sizeDelta        = new Vector2(160f, 52f);

        // ── 상점 패널 (하단에서 슬라이드) ───────────────────────
        var panelGO  = new GameObject("ShopPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImg  = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.10f, 0.96f);
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot     = new Vector2(0.5f, 0f);
        panelRect.sizeDelta        = new Vector2(0f, 420f);
        panelRect.anchoredPosition = new Vector2(0f, 0f);

        // 닫기 버튼
        var closeBtnGO = MakeButton(panelGO.transform, "CloseBtn", "✕ 닫기");
        var closeRect  = closeBtnGO.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot     = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-16f, -12f);
        closeRect.sizeDelta        = new Vector2(100f, 40f);

        // 스크롤뷰
        var scrollGO   = new GameObject("ScrollView");
        scrollGO.transform.SetParent(panelGO.transform, false);
        var scrollRect2 = scrollGO.AddComponent<ScrollRect>();
        var scrollRt   = scrollGO.GetComponent<RectTransform>();
        scrollRt.anchorMin        = new Vector2(0f, 0f);
        scrollRt.anchorMax        = new Vector2(1f, 1f);
        scrollRt.offsetMin        = new Vector2(16f, 16f);
        scrollRt.offsetMax        = new Vector2(-16f, -60f);

        // Viewport
        var vpGO  = new GameObject("Viewport");
        vpGO.transform.SetParent(scrollGO.transform, false);
        vpGO.AddComponent<Image>().color = Color.clear;
        vpGO.AddComponent<Mask>().showMaskGraphic = false;
        var vpRt  = vpGO.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;

        // Content (아이템 Grid)
        var contentGO  = new GameObject("Content");
        contentGO.transform.SetParent(vpGO.transform, false);
        var contentRt  = contentGO.AddComponent<RectTransform>();
        contentRt.anchorMin        = new Vector2(0f, 1f);
        contentRt.anchorMax        = new Vector2(1f, 1f);
        contentRt.pivot            = new Vector2(0.5f, 1f);
        contentRt.sizeDelta        = new Vector2(0f, 300f);

        var grid = contentGO.AddComponent<GridLayoutGroup>();
        grid.cellSize    = new Vector2(160f, 180f);
        grid.spacing     = new Vector2(12f, 12f);
        grid.padding     = new RectOffset(12, 12, 12, 12);
        grid.constraint  = GridLayoutGroup.Constraint.Flexible;

        contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect2.content    = contentRt;
        scrollRect2.viewport   = vpRt;
        scrollRect2.horizontal = false;

        // ── 아이템 버튼 프리팹 ────────────────────────────────────
        const string itemBtnPath = "Assets/HeartRoom/Prefabs/ItemButton.prefab";
        if (!System.IO.File.Exists(itemBtnPath))
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
            var lockLabel = MakeTMP(lockGO.transform, "LockIcon", "🔒", 22f);
            var llRt = lockLabel.GetComponent<RectTransform>();
            llRt.anchorMin = Vector2.zero; llRt.anchorMax = Vector2.one;
            llRt.offsetMin = llRt.offsetMax = Vector2.zero;

            PrefabUtility.SaveAsPrefabAsset(ibGO, itemBtnPath);
            Object.DestroyImmediate(ibGO);
        }

        // ── RoomUIManager 연결 ────────────────────────────────────
        var uiMgrGO = new GameObject("RoomUIManager");
        uiMgrGO.transform.SetParent(canvasGO.transform, false);
        var uiMgr = uiMgrGO.AddComponent<RoomUIManager>();

        uiMgr.rootCanvas      = canvas;
        uiMgr.shopPanel       = panelGO;
        uiMgr.itemGrid        = contentGO.transform;
        uiMgr.itemButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(itemBtnPath);

        var openBtn  = openBtnGO.GetComponent<Button>();
        var closeBtn = closeBtnGO.GetComponent<Button>();
        uiMgr.openShopBtn  = openBtn;
        uiMgr.closeShopBtn = closeBtn;
        // onClick은 RoomUIManager.Start()에서 런타임에 연결

        EditorUtility.SetDirty(uiMgrGO);
        Debug.Log("[Game-M] RoomUI 생성 완료");
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

    static GameObject MakeTMP(Transform parent, string name, string text, float size)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        return go;
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
