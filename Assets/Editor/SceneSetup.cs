using UnityEngine;
using UnityEditor;

public static class SceneSetup
{
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
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType      = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode       = FilterMode.Bilinear;
                AssetDatabase.ImportAsset(path);
            }
        }

        // 2. Slime Prefab 생성
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

        // 3. SlimeManager 오브젝트 생성 (씬에 없으면)
        var existing = Object.FindFirstObjectByType<SlimeManager>();
        var managerGO = existing != null
            ? existing.gameObject
            : new GameObject("SlimeManager");

        var manager = managerGO.GetComponent<SlimeManager>()
                      ?? managerGO.AddComponent<SlimeManager>();

        manager.slimePrefab      = prefab;
        manager.spriteAngry      = LoadSprite("Assets/Slimes/slime-angry.png");
        manager.spriteSad        = LoadSprite("Assets/Slimes/slime-sad.png");
        manager.spriteFear       = LoadSprite("Assets/Slimes/slime-fear.png");
        manager.spriteHappy      = LoadSprite("Assets/Slimes/slime-happy.png");
        manager.spriteDisgust    = LoadSprite("Assets/Slimes/slime-disgust.png");
        manager.spriteSurprised  = LoadSprite("Assets/Slimes/slime-surprised.png");
        manager.spriteContempt   = LoadSprite("Assets/Slimes/slime-contempt.png");
        manager.spriteBlank      = LoadSprite("Assets/Slimes/slime-blank.png");

        // 4. 카메라 배경 어둡게
        if (Camera.main != null)
        {
            Camera.main.backgroundColor = new Color(0.086f, 0.086f, 0.094f);
            Camera.main.clearFlags      = CameraClearFlags.SolidColor;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        AssetDatabase.SaveAssets();
        Debug.Log("씬 세팅 완료! SlimeManager와 Slime Prefab이 준비됐어요.");
    }

    static Sprite LoadSprite(string path) =>
        AssetDatabase.LoadAssetAtPath<Sprite>(path);
}
