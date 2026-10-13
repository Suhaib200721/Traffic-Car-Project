using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Traffic Escape 3D – Editor auto-builder.
/// Builds materials, prefabs, and the TrafficEscape scene from scratch when
/// explicitly requested via the menu or on first import (editor-only, never
/// during Play Mode).
/// </summary>
[InitializeOnLoad]
public class GameSetupEditor
{
    private const string BuildFlagFile = "Library/TrafficEscapeBuilt.flag";

    // ── Static constructor ──────────────────────────────────────────────────
    // Use playModeStateChanged instead of delayCall so the callback is only
    // invoked when the editor is genuinely in a stable edit-mode state.
    static GameSetupEditor()
    {
        // Hook into the state-change event; fire ONLY when entering EditMode.
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        // Also fire once on editor startup via delayCall, but with a strict guard.
        EditorApplication.delayCall += CheckAutoBuild;
    }

    /// <summary>
    /// Invoked by Unity whenever the editor transitions between Edit / Play modes.
    /// We only auto-build when the editor returns to (or starts in) Edit Mode.
    /// </summary>
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // EnteredEditMode fires AFTER the editor has fully exited Play Mode –
        // this is the only safe moment to call EditorSceneManager APIs.
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            CheckAutoBuild();
        }
    }

    /// <summary>
    /// Auto-build check. Only runs outside Play Mode and outside compile cycles.
    /// If the scene file already exists we simply mark the flag so the check
    /// is skipped on all future startups.
    /// </summary>
    private static void CheckAutoBuild()
    {
        // Hard stop: never run editor scene operations during or just before Play Mode.
        if (EditorApplication.isPlaying ||
            EditorApplication.isPlayingOrWillChangePlaymode ||
            EditorApplication.isCompiling)
        {
            return;
        }

        // Only auto-build on a fresh import where no scene exists yet.
        if (!File.Exists(BuildFlagFile) && !File.Exists("Assets/Scenes/TrafficEscape.unity"))
        {
            BuildAllAssetsAndScene();
        }
        else if (!File.Exists(BuildFlagFile))
        {
            // Scene already existed from version control – just write the flag.
            File.WriteAllText(BuildFlagFile, "Scene already existed at " + System.DateTime.Now);
        }
    }

    // ── Menu item ───────────────────────────────────────────────────────────
    [MenuItem("Traffic Escape/Build Game Scene and Assets", false, 1)]
    public static void BuildAllAssetsAndScene()
    {
        // Safety guard – menu item should be greyed out automatically by
        // the validate method below, but keep an explicit runtime guard too.
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Traffic Escape] Auto-Builder cannot run during Play Mode. Please exit Play Mode first.");
            return;
        }

        Debug.Log("===> Starting Traffic Escape 3D Auto-Builder <===");

        EnsureDirectories();
        CreateMaterials(out Material[] mats, out Material[] trafficMats);
        CreatePrefabs(mats, trafficMats, out GameObject roadPrefab, out GameObject playerPrefab, out GameObject trafficPrefab, out GameObject coinPrefab, out GameObject shieldPrefab);
        CreateScene(mats, trafficMats, roadPrefab, playerPrefab, trafficPrefab, coinPrefab, shieldPrefab);

        File.WriteAllText(BuildFlagFile, "Built successfully on " + System.DateTime.Now);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("===> Traffic Escape 3D Game Built Successfully! <===");
    }

    /// <summary>
    /// Disables the menu item while Unity is in Play Mode.
    /// </summary>
    [MenuItem("Traffic Escape/Build Game Scene and Assets", true)]
    private static bool ValidateBuildAllAssetsAndScene()
    {
        return !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    [MenuItem("Traffic Escape/Force Rebuild (Delete Flag)", false, 20)]
    public static void ForceRebuild()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[Traffic Escape] Cannot rebuild during Play Mode.");
            return;
        }

        if (File.Exists(BuildFlagFile)) File.Delete(BuildFlagFile);
        BuildAllAssetsAndScene();
    }

    [MenuItem("Traffic Escape/Force Rebuild (Delete Flag)", true)]
    private static bool ValidateForceRebuild()
    {
        return !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    // ── Directory helpers ───────────────────────────────────────────────────
    private static void EnsureDirectories()
    {
        if (!Directory.Exists("Assets/Materials")) Directory.CreateDirectory("Assets/Materials");
        if (!Directory.Exists("Assets/Prefabs")) Directory.CreateDirectory("Assets/Prefabs");
        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
    }

    // ── Shader helper ───────────────────────────────────────────────────────
    private static Shader GetBestShader(bool transparent = false)
    {
        Shader s = Shader.Find("Standard");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Lit");
        if (s == null) s = Shader.Find("Mobile/Diffuse");
        if (s == null) s = Shader.Find("Diffuse");
        if (s == null) s = Shader.Find("Unlit/Color");
        return s;
    }

    private static Material CreateMaterial(string name, Color color, float smoothness = 0.5f, bool transparent = false)
    {
        string path = $"Assets/Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(GetBestShader(transparent));
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);

        if (transparent)
        {
            if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 3); // Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ── Materials ───────────────────────────────────────────────────────────
    private static void CreateMaterials(out Material[] allMats, out Material[] trafficMats)
    {
        Material matAsphalt    = CreateMaterial("Mat_Asphalt",        new Color(0.18f, 0.18f, 0.20f), 0.2f);
        Material matStripe     = CreateMaterial("Mat_RoadStripe",     new Color(0.95f, 0.95f, 0.95f), 0.3f);
        Material matYellowStripe = CreateMaterial("Mat_RoadYellowStripe", new Color(0.95f, 0.80f, 0.15f), 0.3f);
        Material matGrass      = CreateMaterial("Mat_Grass",          new Color(0.24f, 0.52f, 0.22f), 0.1f);
        Material matGuardRail  = CreateMaterial("Mat_GuardRail",      new Color(0.70f, 0.72f, 0.75f), 0.6f);
        Material matPlayer     = CreateMaterial("Mat_PlayerCar",      new Color(0.12f, 0.48f, 0.92f), 0.7f);
        Material matWheel      = CreateMaterial("Mat_Wheel",          new Color(0.08f, 0.08f, 0.08f), 0.2f);
        Material matRim        = CreateMaterial("Mat_WheelRim",       new Color(0.85f, 0.85f, 0.85f), 0.8f);
        Material matWindow     = CreateMaterial("Mat_Window",         new Color(0.12f, 0.18f, 0.25f), 0.9f);
        Material matHeadlight  = CreateMaterial("Mat_Headlight",      new Color(1.0f,  0.95f, 0.7f),  0.8f);
        Material matTaillight  = CreateMaterial("Mat_Taillight",      new Color(0.95f, 0.1f,  0.1f),  0.8f);
        Material matCoin       = CreateMaterial("Mat_Coin",           new Color(1.0f,  0.82f, 0.1f),  0.85f);
        Material matShield     = CreateMaterial("Mat_Shield",         new Color(0.2f,  0.8f,  1.0f, 0.35f), 0.9f, true);
        Material matTreeTrunk  = CreateMaterial("Mat_TreeTrunk",      new Color(0.42f, 0.28f, 0.16f), 0.1f);
        Material matTreeLeaves = CreateMaterial("Mat_TreeLeaves",     new Color(0.18f, 0.48f, 0.18f), 0.1f);

        // Traffic car colours
        Material tRed    = CreateMaterial("Mat_TrafficRed",    new Color(0.88f, 0.16f, 0.16f), 0.6f);
        Material tBlue   = CreateMaterial("Mat_TrafficBlue",   new Color(0.20f, 0.40f, 0.80f), 0.6f);
        Material tWhite  = CreateMaterial("Mat_TrafficWhite",  new Color(0.92f, 0.92f, 0.92f), 0.6f);
        Material tYellow = CreateMaterial("Mat_TrafficYellow", new Color(0.95f, 0.82f, 0.15f), 0.6f);
        Material tBlack  = CreateMaterial("Mat_TrafficBlack",  new Color(0.12f, 0.12f, 0.14f), 0.7f);
        Material tGreen  = CreateMaterial("Mat_TrafficGreen",  new Color(0.18f, 0.65f, 0.28f), 0.6f);

        // Sports car specialty materials
        Material matCaliper = CreateMaterial("Mat_BrakeCaliper", new Color(0.92f, 0.12f, 0.12f), 0.7f);
        Material matCarbon  = CreateMaterial("Mat_CarbonSplitter", new Color(0.12f, 0.12f, 0.14f), 0.4f);
        Material matExhaust = CreateMaterial("Mat_ChromeExhaust", new Color(0.85f, 0.85f, 0.90f), 0.85f);

        trafficMats = new Material[] { tRed, tBlue, tWhite, tYellow, tBlack, tGreen };
        allMats = new Material[]
        {
            matAsphalt, matStripe, matYellowStripe, matGrass, matGuardRail,
            matPlayer,  matWheel,  matRim,          matWindow, matHeadlight, matTaillight,
            matCoin,    matShield, matTreeTrunk,    matTreeLeaves,
            matCaliper, matCarbon, matExhaust
        };
    }

    // ── Prefabs ─────────────────────────────────────────────────────────────
    private static void CreatePrefabs(
        Material[] mats, Material[] trafficMats,
        out GameObject roadPrefab, out GameObject playerPrefab,
        out GameObject trafficPrefab, out GameObject coinPrefab, out GameObject shieldPrefab)
    {
        Material matAsphalt    = mats[0];
        Material matStripe     = mats[1];
        Material matYellowStripe = mats[2];
        Material matGrass      = mats[3];
        Material matGuardRail  = mats[4];
        Material matPlayer     = mats[5];
        Material matWheel      = mats[6];
        Material matRim        = mats[7];
        Material matWindow     = mats[8];
        Material matHeadlight  = mats[9];
        Material matTaillight  = mats[10];
        Material matCoin       = mats[11];
        Material matShield     = mats[12];
        Material matTreeTrunk  = mats[13];
        Material matTreeLeaves = mats[14];
        Material matCaliper    = mats[15];
        Material matCarbon     = mats[16];
        Material matExhaust    = mats[17];

        // ── 1. Road Segment Prefab ──────────────────────────────────────────
        GameObject roadRoot = new GameObject("RoadSegment");
        float roadLen   = 40f;
        float roadWidth = 12f;

        // Asphalt surface
        GameObject asphalt = GameObject.CreatePrimitive(PrimitiveType.Cube);
        asphalt.name = "Asphalt";
        asphalt.transform.SetParent(roadRoot.transform);
        asphalt.transform.localPosition = new Vector3(0f, -0.1f, roadLen * 0.5f);
        asphalt.transform.localScale    = new Vector3(roadWidth, 0.2f, roadLen);
        asphalt.GetComponent<Renderer>().material = matAsphalt;

        // Left & Right Grass
        GameObject leftGrass = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftGrass.name = "LeftGrass";
        leftGrass.transform.SetParent(roadRoot.transform);
        leftGrass.transform.localPosition = new Vector3(-11f, -0.09f, roadLen * 0.5f);
        leftGrass.transform.localScale    = new Vector3(10f, 0.2f, roadLen);
        leftGrass.GetComponent<Renderer>().material = matGrass;

        GameObject rightGrass = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightGrass.name = "RightGrass";
        rightGrass.transform.SetParent(roadRoot.transform);
        rightGrass.transform.localPosition = new Vector3(11f, -0.09f, roadLen * 0.5f);
        rightGrass.transform.localScale    = new Vector3(10f, 0.2f, roadLen);
        rightGrass.GetComponent<Renderer>().material = matGrass;

        // Outer Yellow Lines
        float[] yellowX = new float[] { -5.6f, 5.6f };
        foreach (float x in yellowX)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "YellowLine";
            line.transform.SetParent(roadRoot.transform);
            line.transform.localPosition = new Vector3(x, 0.01f, roadLen * 0.5f);
            line.transform.localScale    = new Vector3(0.2f, 0.02f, roadLen);
            line.GetComponent<Renderer>().material = matYellowStripe;
            Object.DestroyImmediate(line.GetComponent<Collider>());
        }

        // Guard rails
        float[] railX = new float[] { -6.1f, 6.1f };
        foreach (float rx in railX)
        {
            GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rail.name = "GuardRail";
            rail.transform.SetParent(roadRoot.transform);
            rail.transform.localPosition = new Vector3(rx, 0.35f, roadLen * 0.5f);
            rail.transform.localScale    = new Vector3(0.2f, 0.6f, roadLen);
            rail.GetComponent<Renderer>().material = matGuardRail;
        }

        // Dashed White Stripes
        float[] dividerX = new float[] { -1.75f, 1.75f };
        for (float z = 3f; z < roadLen; z += 5f)
        {
            foreach (float dx in dividerX)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "Stripe";
                stripe.transform.SetParent(roadRoot.transform);
                stripe.transform.localPosition = new Vector3(dx, 0.01f, z);
                stripe.transform.localScale    = new Vector3(0.18f, 0.02f, 2.5f);
                stripe.GetComponent<Renderer>().material = matStripe;
                Object.DestroyImmediate(stripe.GetComponent<Collider>());
            }
        }

        // Roadside low-poly trees (Part 5 – environment)
        float[] treeZ = new float[] { 10f, 30f };
        foreach (float tz in treeZ)
        {
            CreateTree(roadRoot.transform, new Vector3(-9f, 0f, tz),       matTreeTrunk, matTreeLeaves);
            CreateTree(roadRoot.transform, new Vector3(9f,  0f, tz + 10f), matTreeTrunk, matTreeLeaves);
        }

        // Roadside streetlights
        CreateStreetlight(roadRoot.transform, new Vector3(-6.8f, 0f, 12f), true,  matGuardRail, matHeadlight);
        CreateStreetlight(roadRoot.transform, new Vector3( 6.8f, 0f, 28f), false, matGuardRail, matHeadlight);

        // Background low-poly buildings
        CreateBuilding(roadRoot.transform, new Vector3(-13.5f, 0f, 8f),  new Vector3(4.2f, 6.5f, 5.5f), matAsphalt);
        CreateBuilding(roadRoot.transform, new Vector3( 13.5f, 0f, 26f), new Vector3(4.5f, 8.5f, 6.5f), matGuardRail);

        roadPrefab = SavePrefab(roadRoot, "Assets/Prefabs/RoadSegment.prefab");
        Object.DestroyImmediate(roadRoot);

        // ── 2. Player Car Prefab ────────────────────────────────────────────
        GameObject playerRoot = new GameObject("PlayerCar");
        playerRoot.tag = "Player";
        var carCtrl  = playerRoot.AddComponent<CarController>();
        var rbPlayer = playerRoot.AddComponent<Rigidbody>();
        rbPlayer.isKinematic = true;
        var colPlayer = playerRoot.AddComponent<BoxCollider>();
        colPlayer.center = new Vector3(0f, 0.6f, 0f);
        colPlayer.size   = new Vector3(1.85f, 1.0f, 3.9f);

        BuildSportsCarModel(playerRoot.transform, matPlayer, matWheel, matRim, matWindow, matHeadlight, matTaillight, matCaliper, matCarbon, matExhaust, matStripe, out Renderer bodyRend);

        // Shield bubble child object
        GameObject shieldObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldObj.name = "ShieldBubble";
        shieldObj.transform.SetParent(playerRoot.transform);
        shieldObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        shieldObj.transform.localScale    = new Vector3(2.6f, 1.8f, 4.4f);
        shieldObj.GetComponent<Renderer>().material = matShield;
        Object.DestroyImmediate(shieldObj.GetComponent<Collider>());
        shieldObj.SetActive(false);

        SerializedObject soPlayer = new SerializedObject(carCtrl);
        soPlayer.FindProperty("shieldVisual").objectReferenceValue = shieldObj;
        soPlayer.ApplyModifiedProperties();

        playerPrefab = SavePrefab(playerRoot, "Assets/Prefabs/PlayerCar.prefab");
        Object.DestroyImmediate(playerRoot);

        // ── 3. Traffic Car Prefab ───────────────────────────────────────────
        GameObject trafficRoot = new GameObject("TrafficCar");
        trafficRoot.tag = "Traffic";
        var tcComp     = trafficRoot.AddComponent<TrafficCar>();
        var rbTraffic  = trafficRoot.AddComponent<Rigidbody>();
        rbTraffic.isKinematic = true;
        var colTraffic = trafficRoot.AddComponent<BoxCollider>();
        colTraffic.isTrigger = true;
        colTraffic.center    = new Vector3(0f, 0.6f, 0f);
        colTraffic.size      = new Vector3(1.8f, 1.0f, 3.8f);

        BuildTrafficCarModel(trafficRoot.transform, trafficMats[0], matWheel, matRim, matWindow, matHeadlight, matTaillight, out Renderer trafficBodyRend);

        SerializedObject soTraffic = new SerializedObject(tcComp);
        soTraffic.FindProperty("bodyRenderer").objectReferenceValue = trafficBodyRend;
        soTraffic.ApplyModifiedProperties();

        trafficPrefab = SavePrefab(trafficRoot, "Assets/Prefabs/TrafficCar.prefab");
        Object.DestroyImmediate(trafficRoot);

        // ── 4. Coin Prefab ──────────────────────────────────────────────────
        GameObject coinRoot = new GameObject("Coin");
        coinRoot.AddComponent<Coin>();
        var colCoin = coinRoot.AddComponent<SphereCollider>();
        colCoin.isTrigger = true;
        colCoin.radius    = 0.5f;

        GameObject coinMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        coinMesh.name = "CoinMesh";
        coinMesh.transform.SetParent(coinRoot.transform);
        coinMesh.transform.localPosition = Vector3.zero;
        coinMesh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        coinMesh.transform.localScale    = new Vector3(0.7f, 0.1f, 0.7f);
        coinMesh.GetComponent<Renderer>().material = matCoin;
        Object.DestroyImmediate(coinMesh.GetComponent<Collider>());

        coinPrefab = SavePrefab(coinRoot, "Assets/Prefabs/Coin.prefab");
        Object.DestroyImmediate(coinRoot);

        // ── 5. Shield Power-up Prefab ───────────────────────────────────────
        GameObject shieldRoot = new GameObject("ShieldPowerUp");
        shieldRoot.AddComponent<ShieldPowerUp>();
        var colShield = shieldRoot.AddComponent<SphereCollider>();
        colShield.isTrigger = true;
        colShield.radius    = 0.6f;

        GameObject shieldMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldMesh.name = "ShieldMesh";
        shieldMesh.transform.SetParent(shieldRoot.transform);
        shieldMesh.transform.localPosition = Vector3.zero;
        shieldMesh.transform.localScale    = new Vector3(0.8f, 0.8f, 0.8f);
        shieldMesh.GetComponent<Renderer>().material = matShield;
        Object.DestroyImmediate(shieldMesh.GetComponent<Collider>());

        shieldPrefab = SavePrefab(shieldRoot, "Assets/Prefabs/ShieldPowerUp.prefab");
        Object.DestroyImmediate(shieldRoot);
    }

    // ── Car mesh builder ────────────────────────────────────────────────────
    // ── Sports Car Model (Player) ──────────────────────────────────────────
    private static void BuildSportsCarModel(
        Transform parent, Material bodyMat, Material wheelMat, Material rimMat,
        Material windowMat, Material headlightMat, Material taillightMat,
        Material caliperMat, Material carbonMat, Material exhaustMat, Material stripeMat,
        out Renderer bodyRenderer)
    {
        // 1. Lower Main Chassis
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Chassis";
        body.transform.SetParent(parent);
        body.transform.localPosition = new Vector3(0f, 0.40f, 0f);
        body.transform.localScale    = new Vector3(1.75f, 0.40f, 3.8f);
        bodyRenderer = body.GetComponent<Renderer>();
        bodyRenderer.material = bodyMat;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // 2. Front Sloped Hood / Nose
        GameObject hood = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hood.name = "Hood";
        hood.transform.SetParent(parent);
        hood.transform.localPosition = new Vector3(0f, 0.44f, 1.25f);
        hood.transform.localRotation = Quaternion.Euler(6.5f, 0f, 0f);
        hood.transform.localScale    = new Vector3(1.68f, 0.28f, 1.35f);
        hood.GetComponent<Renderer>().material = bodyMat;
        Object.DestroyImmediate(hood.GetComponent<Collider>());

        // 3. Front Aerodynamic Splitter
        GameObject splitter = GameObject.CreatePrimitive(PrimitiveType.Cube);
        splitter.name = "FrontSplitter";
        splitter.transform.SetParent(parent);
        splitter.transform.localPosition = new Vector3(0f, 0.16f, 1.95f);
        splitter.transform.localScale    = new Vector3(1.85f, 0.08f, 0.40f);
        splitter.GetComponent<Renderer>().material = carbonMat;
        Object.DestroyImmediate(splitter.GetComponent<Collider>());

        // 4. Front Air Intake Grille
        GameObject grille = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grille.name = "FrontGrille";
        grille.transform.SetParent(parent);
        grille.transform.localPosition = new Vector3(0f, 0.32f, 1.91f);
        grille.transform.localScale    = new Vector3(1.2f, 0.18f, 0.05f);
        grille.GetComponent<Renderer>().material = carbonMat;
        Object.DestroyImmediate(grille.GetComponent<Collider>());

        // 5. Aerodynamic Cockpit Canopy
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "CockpitCanopy";
        cabin.transform.SetParent(parent);
        cabin.transform.localPosition = new Vector3(0f, 0.82f, -0.25f);
        cabin.transform.localScale    = new Vector3(1.30f, 0.48f, 1.85f);
        cabin.GetComponent<Renderer>().material = windowMat;
        Object.DestroyImmediate(cabin.GetComponent<Collider>());

        // 6. Roof Cap (body colored)
        GameObject roofCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roofCap.name = "RoofCap";
        roofCap.transform.SetParent(parent);
        roofCap.transform.localPosition = new Vector3(0f, 1.07f, -0.32f);
        roofCap.transform.localScale    = new Vector3(1.18f, 0.06f, 1.15f);
        roofCap.GetComponent<Renderer>().material = bodyMat;
        Object.DestroyImmediate(roofCap.GetComponent<Collider>());

        // 7. Dual Racing Stripes over hood & roof
        float[] stripeOffsets = new float[] { -0.15f, 0.15f };
        foreach (float sx in stripeOffsets)
        {
            GameObject sHood = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sHood.name = "RacingStripeHood";
            sHood.transform.SetParent(parent);
            sHood.transform.localPosition = new Vector3(sx, 0.59f, 1.25f);
            sHood.transform.localRotation = Quaternion.Euler(6.5f, 0f, 0f);
            sHood.transform.localScale    = new Vector3(0.12f, 0.02f, 1.30f);
            sHood.GetComponent<Renderer>().material = stripeMat;
            Object.DestroyImmediate(sHood.GetComponent<Collider>());

            GameObject sRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sRoof.name = "RacingStripeRoof";
            sRoof.transform.SetParent(parent);
            sRoof.transform.localPosition = new Vector3(sx, 1.11f, -0.32f);
            sRoof.transform.localScale    = new Vector3(0.12f, 0.02f, 1.15f);
            sRoof.GetComponent<Renderer>().material = stripeMat;
            Object.DestroyImmediate(sRoof.GetComponent<Collider>());
        }

        // 8. Rear GT Wing / Spoiler
        GameObject spoilerWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spoilerWing.name = "GTSpoilerWing";
        spoilerWing.transform.SetParent(parent);
        spoilerWing.transform.localPosition = new Vector3(0f, 0.98f, -1.82f);
        spoilerWing.transform.localScale    = new Vector3(1.72f, 0.06f, 0.35f);
        spoilerWing.GetComponent<Renderer>().material = carbonMat;
        Object.DestroyImmediate(spoilerWing.GetComponent<Collider>());

        // Spoiler upright stanchions
        float[] pylonX = new float[] { -0.55f, 0.55f };
        foreach (float px in pylonX)
        {
            GameObject pylon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pylon.name = "SpoilerPylon";
            pylon.transform.SetParent(parent);
            pylon.transform.localPosition = new Vector3(px, 0.78f, -1.82f);
            pylon.transform.localScale    = new Vector3(0.06f, 0.36f, 0.20f);
            pylon.GetComponent<Renderer>().material = carbonMat;
            Object.DestroyImmediate(pylon.GetComponent<Collider>());
        }

        // 9. Rear Exhaust Tips & Diffuser
        float[] exX = new float[] { -0.38f, 0.38f };
        foreach (float ex in exX)
        {
            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tip.name = "ExhaustTip";
            tip.transform.SetParent(parent);
            tip.transform.localPosition = new Vector3(ex, 0.28f, -1.94f);
            tip.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tip.transform.localScale    = new Vector3(0.14f, 0.08f, 0.14f);
            tip.GetComponent<Renderer>().material = exhaustMat;
            Object.DestroyImmediate(tip.GetComponent<Collider>());
        }

        GameObject diffuser = GameObject.CreatePrimitive(PrimitiveType.Cube);
        diffuser.name = "RearDiffuser";
        diffuser.transform.SetParent(parent);
        diffuser.transform.localPosition = new Vector3(0f, 0.22f, -1.88f);
        diffuser.transform.localScale    = new Vector3(1.6f, 0.12f, 0.25f);
        diffuser.GetComponent<Renderer>().material = carbonMat;
        Object.DestroyImmediate(diffuser.GetComponent<Collider>());

        // 10. Sport Wheels with Rims & Red Brake Calipers
        float[] wheelX = new float[] { -0.92f, 0.92f };
        float[] wheelZ = new float[] { -1.15f, 1.15f };
        foreach (float wx in wheelX)
        {
            foreach (float wz in wheelZ)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "SportWheel";
                wheel.transform.SetParent(parent);
                wheel.transform.localPosition = new Vector3(wx, 0.32f, wz);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale    = new Vector3(0.64f, 0.20f, 0.64f);
                wheel.GetComponent<Renderer>().material = wheelMat;
                Object.DestroyImmediate(wheel.GetComponent<Collider>());

                GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rim.name = "Rim";
                rim.transform.SetParent(wheel.transform);
                rim.transform.localPosition = new Vector3(0f, wx > 0 ? 0.54f : -0.54f, 0f);
                rim.transform.localRotation = Quaternion.identity;
                rim.transform.localScale    = new Vector3(0.52f, 0.06f, 0.52f);
                rim.GetComponent<Renderer>().material = rimMat;
                Object.DestroyImmediate(rim.GetComponent<Collider>());

                GameObject caliper = GameObject.CreatePrimitive(PrimitiveType.Cube);
                caliper.name = "BrakeCaliper";
                caliper.transform.SetParent(wheel.transform);
                caliper.transform.localPosition = new Vector3(0.18f, wx > 0 ? 0.40f : -0.40f, 0f);
                caliper.transform.localScale    = new Vector3(0.20f, 0.14f, 0.26f);
                caliper.GetComponent<Renderer>().material = caliperMat;
                Object.DestroyImmediate(caliper.GetComponent<Collider>());
            }
        }

        // 11. Headlights (sleek twin LED angular styling)
        float[] hlX = new float[] { -0.62f, 0.62f };
        foreach (float hx in hlX)
        {
            GameObject hl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hl.name = "Headlight";
            hl.transform.SetParent(parent);
            hl.transform.localPosition = new Vector3(hx, 0.46f, 1.86f);
            hl.transform.localScale    = new Vector3(0.34f, 0.14f, 0.08f);
            hl.GetComponent<Renderer>().material = headlightMat;
            Object.DestroyImmediate(hl.GetComponent<Collider>());
        }

        // 12. Full-width Modern Taillight Strip (rear is -Z)
        GameObject tlStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tlStrip.name = "TaillightStrip";
        tlStrip.transform.SetParent(parent);
        tlStrip.transform.localPosition = new Vector3(0f, 0.54f, -1.91f);
        tlStrip.transform.localScale    = new Vector3(1.55f, 0.12f, 0.05f);
        tlStrip.GetComponent<Renderer>().material = taillightMat;
        Object.DestroyImmediate(tlStrip.GetComponent<Collider>());
    }

    // ── Traffic Car Model (Oncoming Vehicles) ──────────────────────────────
    private static void BuildTrafficCarModel(
        Transform parent, Material bodyMat, Material wheelMat, Material rimMat,
        Material windowMat, Material headlightMat, Material taillightMat,
        out Renderer bodyRenderer)
    {
        // Lower Chassis
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Chassis";
        body.transform.SetParent(parent);
        body.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        body.transform.localScale    = new Vector3(1.72f, 0.50f, 3.65f);
        bodyRenderer = body.GetComponent<Renderer>();
        bodyRenderer.material = bodyMat;
        Object.DestroyImmediate(body.GetComponent<Collider>());

        // Cabin / Roof
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "Cabin";
        cabin.transform.SetParent(parent);
        cabin.transform.localPosition = new Vector3(0f, 0.92f, -0.15f);
        cabin.transform.localScale    = new Vector3(1.36f, 0.48f, 2.0f);
        cabin.GetComponent<Renderer>().material = windowMat;
        Object.DestroyImmediate(cabin.GetComponent<Collider>());

        // Wheels
        float[] wheelX = new float[] { -0.9f, 0.9f };
        float[] wheelZ = new float[] { -1.1f, 1.1f };
        foreach (float wx in wheelX)
        {
            foreach (float wz in wheelZ)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "Wheel";
                wheel.transform.SetParent(parent);
                wheel.transform.localPosition = new Vector3(wx, 0.32f, wz);
                wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                wheel.transform.localScale    = new Vector3(0.6f, 0.18f, 0.6f);
                wheel.GetComponent<Renderer>().material = wheelMat;
                Object.DestroyImmediate(wheel.GetComponent<Collider>());

                GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                rim.name = "Rim";
                rim.transform.SetParent(wheel.transform);
                rim.transform.localPosition = new Vector3(0f, wx > 0 ? 0.55f : -0.55f, 0f);
                rim.transform.localRotation = Quaternion.identity;
                rim.transform.localScale    = new Vector3(0.5f, 0.05f, 0.5f);
                rim.GetComponent<Renderer>().material = rimMat;
                Object.DestroyImmediate(rim.GetComponent<Collider>());
            }
        }

        // Oncoming Headlights (Bright, facing front +Z in car coords)
        float[] hlX = new float[] { -0.6f, 0.6f };
        foreach (float hx in hlX)
        {
            GameObject hl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hl.name = "Headlight";
            hl.transform.SetParent(parent);
            hl.transform.localPosition = new Vector3(hx, 0.48f, 1.83f);
            hl.transform.localScale    = new Vector3(0.32f, 0.20f, 0.06f);
            hl.GetComponent<Renderer>().material = headlightMat;
            Object.DestroyImmediate(hl.GetComponent<Collider>());
        }

        // Taillights (rear is -Z)
        foreach (float hx in hlX)
        {
            GameObject tl = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tl.name = "Taillight";
            tl.transform.SetParent(parent);
            tl.transform.localPosition = new Vector3(hx, 0.48f, -1.83f);
            tl.transform.localScale    = new Vector3(0.32f, 0.20f, 0.06f);
            tl.GetComponent<Renderer>().material = taillightMat;
            Object.DestroyImmediate(tl.GetComponent<Collider>());
        }
    }

    // ── Environment helpers ─────────────────────────────────────────────────
    private static void CreateTree(Transform parent, Vector3 localPos, Material trunkMat, Material leavesMat)
    {
        GameObject treeRoot = new GameObject("Tree");
        treeRoot.transform.SetParent(parent);
        treeRoot.transform.localPosition = localPos;

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Trunk";
        trunk.transform.SetParent(treeRoot.transform);
        trunk.transform.localPosition = new Vector3(0f, 1.2f, 0f);
        trunk.transform.localScale    = new Vector3(0.4f, 1.2f, 0.4f);
        trunk.GetComponent<Renderer>().material = trunkMat;
        Object.DestroyImmediate(trunk.GetComponent<Collider>());

        GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foliage.name = "Foliage";
        foliage.transform.SetParent(treeRoot.transform);
        foliage.transform.localPosition = new Vector3(0f, 2.8f, 0f);
        foliage.transform.localScale    = new Vector3(1.8f, 2.2f, 1.8f);
        foliage.GetComponent<Renderer>().material = leavesMat;
        Object.DestroyImmediate(foliage.GetComponent<Collider>());
    }

    private static void CreateStreetlight(Transform parent, Vector3 localPos, bool onLeft, Material poleMat, Material lampMat)
    {
        GameObject lightObj = new GameObject("Streetlight");
        lightObj.transform.SetParent(parent, false);

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pole.name = "Pole";
        pole.transform.SetParent(lightObj.transform, false);
        pole.transform.localPosition = localPos + new Vector3(0f, 1.8f, 0f);
        pole.transform.localScale    = new Vector3(0.16f, 3.6f, 0.16f);
        pole.GetComponent<Renderer>().material = poleMat;
        Object.DestroyImmediate(pole.GetComponent<Collider>());

        float armOffsetX = onLeft ? 0.35f : -0.35f;
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.name = "Arm";
        arm.transform.SetParent(lightObj.transform, false);
        arm.transform.localPosition = localPos + new Vector3(armOffsetX, 3.6f, 0f);
        arm.transform.localScale    = new Vector3(0.7f, 0.12f, 0.16f);
        arm.GetComponent<Renderer>().material = poleMat;
        Object.DestroyImmediate(arm.GetComponent<Collider>());

        float headOffsetX = onLeft ? 0.7f : -0.7f;
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.name = "LampHead";
        head.transform.SetParent(lightObj.transform, false);
        head.transform.localPosition = localPos + new Vector3(headOffsetX, 3.52f, 0f);
        head.transform.localScale    = new Vector3(0.25f, 0.12f, 0.25f);
        head.GetComponent<Renderer>().material = lampMat;
        Object.DestroyImmediate(head.GetComponent<Collider>());
    }

    private static void CreateBuilding(Transform parent, Vector3 localPos, Vector3 size, Material bldMat)
    {
        GameObject bld = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bld.name = "Building";
        bld.transform.SetParent(parent, false);
        bld.transform.localPosition = localPos + new Vector3(0f, size.y * 0.5f, 0f);
        bld.transform.localScale    = size;
        bld.GetComponent<Renderer>().material = bldMat;
        Object.DestroyImmediate(bld.GetComponent<Collider>());
    }

    private static GameObject SavePrefab(GameObject go, string path)
    {
        return PrefabUtility.SaveAsPrefabAsset(go, path);
    }

    // ── Scene builder ───────────────────────────────────────────────────────
    private static void CreateScene(
        Material[] mats, Material[] trafficMats,
        GameObject roadPrefab, GameObject playerPrefab,
        GameObject trafficPrefab, GameObject coinPrefab, GameObject shieldPrefab)
    {
        // Double-check we are NOT in Play Mode before touching the scene.
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("[Traffic Escape] Cannot create editor scene during Play Mode.");
            return;
        }

        // If the scene already exists ask the user whether to overwrite it.
        if (File.Exists("Assets/Scenes/TrafficEscape.unity"))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Traffic Escape – Scene Exists",
                "Assets/Scenes/TrafficEscape.unity already exists.\n\nOverwrite it with a fresh scene?",
                "Overwrite", "Keep Existing");

            if (!overwrite)
            {
                Debug.Log("[Traffic Escape] Scene rebuild skipped – existing scene preserved.");
                return;
            }
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 1. Directional Light
        GameObject dirLightObj = new GameObject("Directional Light");
        Light light = dirLightObj.AddComponent<Light>();
        light.type      = LightType.Directional;
        light.color     = new Color(1.0f, 0.98f, 0.92f);
        light.intensity = 1.15f;
        light.shadows   = LightShadows.Soft;
        dirLightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // 2. Player Car
        GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        playerInstance.name = "PlayerCar";
        playerInstance.transform.position = new Vector3(0f, 0.35f, 0f);
        CarController carCtrl = playerInstance.GetComponent<CarController>();

        // 3. Main Camera
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags   = CameraClearFlags.Skybox;
        cam.fieldOfView  = 60f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane  = 300f;
        camObj.AddComponent<AudioListener>();
        CameraFollow camFollow = camObj.AddComponent<CameraFollow>();
        camFollow.SnapToTarget(playerInstance.transform);

        // 4. Managers hierarchy
        GameObject managers = new GameObject("--- MANAGERS ---");

        GameObject audioObj = new GameObject("AudioManager");
        audioObj.transform.SetParent(managers.transform);
        AudioManager audioMgr = audioObj.AddComponent<AudioManager>();

        GameObject roadObj = new GameObject("RoadManager");
        roadObj.transform.SetParent(managers.transform);
        RoadManager roadMgr = roadObj.AddComponent<RoadManager>();
        SerializedObject soRoad = new SerializedObject(roadMgr);
        soRoad.FindProperty("playerTransform").objectReferenceValue = playerInstance.transform;
        soRoad.FindProperty("roadSegmentPrefab").objectReferenceValue = roadPrefab;
        soRoad.FindProperty("initialSegments").intValue = 6;
        soRoad.FindProperty("segmentLength").floatValue = 40f;
        soRoad.ApplyModifiedProperties();

        GameObject spawnerObj = new GameObject("TrafficSpawner");
        spawnerObj.transform.SetParent(managers.transform);
        TrafficSpawner spawner = spawnerObj.AddComponent<TrafficSpawner>();
        SerializedObject soSpawner = new SerializedObject(spawner);
        soSpawner.FindProperty("playerTransform").objectReferenceValue = playerInstance.transform;
        soSpawner.FindProperty("trafficCarPrefab").objectReferenceValue = trafficPrefab;
        soSpawner.FindProperty("coinPrefab").objectReferenceValue = coinPrefab;
        soSpawner.FindProperty("shieldPrefab").objectReferenceValue = shieldPrefab;

        SerializedProperty spMats = soSpawner.FindProperty("carMaterials");
        spMats.arraySize = trafficMats.Length;
        for (int i = 0; i < trafficMats.Length; i++)
            spMats.GetArrayElementAtIndex(i).objectReferenceValue = trafficMats[i];
        soSpawner.ApplyModifiedProperties();

        // 5. UI Canvas
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Event System
        GameObject eventSys = new GameObject("EventSystem");
        eventSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSys.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Build UI panels (includes Nitro button, boost bar, mission text, etc.)
        BuildUIPanels(canvasObj.transform, carCtrl, out UIManager uiManager);

        // Game Manager
        GameObject gmObj = new GameObject("GameManager");
        gmObj.transform.SetParent(managers.transform);
        GameManager gm = gmObj.AddComponent<GameManager>();
        SerializedObject soGM = new SerializedObject(gm);
        soGM.FindProperty("playerCar").objectReferenceValue     = carCtrl;
        soGM.FindProperty("trafficSpawner").objectReferenceValue = spawner;
        soGM.FindProperty("roadManager").objectReferenceValue   = roadMgr;
        soGM.FindProperty("cameraFollow").objectReferenceValue  = camFollow;
        soGM.FindProperty("uiManager").objectReferenceValue     = uiManager;
        soGM.ApplyModifiedProperties();

        // Save scene
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/TrafficEscape.unity");

        // Register in Build Settings
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/TrafficEscape.unity", true)
        };
    }

    // ── UI panel builder ────────────────────────────────────────────────────
    private static void BuildUIPanels(Transform canvasTransform, CarController playerCar, out UIManager uiManager)
    {
        GameObject uiMgrObj = new GameObject("UIManager");
        uiMgrObj.transform.SetParent(canvasTransform);
        uiManager = uiMgrObj.AddComponent<UIManager>();

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // ── 1. HUD Panel ──────────────────────────────────────────────────
        GameObject hudPanel = CreatePanel(canvasTransform, "HUDPanel", new Color(0, 0, 0, 0));
        Text scoreText  = CreateText(hudPanel.transform, "ScoreText",  "SCORE: 0",       34, TextAnchor.UpperLeft,   new Vector2(40, -40),  new Vector2(300, 60), defaultFont);
        Text coinsText  = CreateText(hudPanel.transform, "CoinsText",  "COINS: 0",       28, TextAnchor.UpperLeft,   new Vector2(40, -95),  new Vector2(300, 50), defaultFont, new Color(1f, 0.85f, 0.2f));
        Text speedText  = CreateText(hudPanel.transform, "SpeedText",  "SPEED: 60 KM/H", 34, TextAnchor.UpperCenter, new Vector2(0, -40),   new Vector2(400, 60), defaultFont);
        Text livesText  = CreateText(hudPanel.transform, "LivesText",  "LIVES: 1",       34, TextAnchor.UpperRight,  new Vector2(-200, -40),new Vector2(200, 60), defaultFont);
        livesText.gameObject.SetActive(false); // 1-hit collision design

        Text shieldText = CreateText(hudPanel.transform, "ShieldStatusText", "SHIELD: ACTIVE", 28, TextAnchor.UpperCenter, new Vector2(0, -100), new Vector2(300, 50), defaultFont, new Color(0.3f, 0.9f, 1f));
        shieldText.gameObject.SetActive(false);

        // Mission display (top center, below speed)
        Text missionText = CreateText(hudPanel.transform, "MissionText", "MISSION: Travel 250 Meters", 22, TextAnchor.UpperCenter, new Vector2(0, -90), new Vector2(500, 45), defaultFont, new Color(0.35f, 0.95f, 1f));

        // Mission completion fanfare banner (center screen)
        Text missionBannerText = CreateText(hudPanel.transform, "MissionBannerText", "", 32, TextAnchor.MiddleCenter, new Vector2(0, 110), new Vector2(600, 80), defaultFont, new Color(1f, 0.88f, 0.2f));

        // Pause button (top right)
        Button btnPause = CreateButton(hudPanel.transform, "BtnPause", "PAUSE", Vector2.zero, new Vector2(130, 52), defaultFont);
        RectTransform rtPause = btnPause.GetComponent<RectTransform>();
        rtPause.anchorMin = new Vector2(1f, 1f);
        rtPause.anchorMax = new Vector2(1f, 1f);
        rtPause.pivot     = new Vector2(1f, 1f);
        rtPause.anchoredPosition = new Vector2(-40f, -40f);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnPause.onClick, uiManager.OnClickPause);

        // Sound toggle button (top right, left of Pause)
        Button btnSound = CreateButton(hudPanel.transform, "BtnSound", "SOUND: ON", Vector2.zero, new Vector2(130, 52), defaultFont);
        RectTransform rtSound = btnSound.GetComponent<RectTransform>();
        rtSound.anchorMin = new Vector2(1f, 1f);
        rtSound.anchorMax = new Vector2(1f, 1f);
        rtSound.pivot     = new Vector2(1f, 1f);
        rtSound.anchoredPosition = new Vector2(-185f, -40f);
        Text soundBtnLabel = btnSound.GetComponentInChildren<Text>();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnSound.onClick, uiManager.OnClickToggleSound);

        // Nitro button (bottom right – clearly visible, large)
        Button btnNitro = CreateButton(hudPanel.transform, "BtnNitro", "NITRO\n[SPACE]", Vector2.zero, new Vector2(160, 65), defaultFont);
        RectTransform rtNitro = btnNitro.GetComponent<RectTransform>();
        rtNitro.anchorMin = new Vector2(1f, 0f);
        rtNitro.anchorMax = new Vector2(1f, 0f);
        rtNitro.pivot     = new Vector2(1f, 0f);
        rtNitro.anchoredPosition = new Vector2(-40f, 40f);
        btnNitro.GetComponent<Image>().color = new Color(0.12f, 0.55f, 0.95f, 0.95f);
        ColorBlock nitroCB = btnNitro.colors;
        nitroCB.normalColor      = new Color(0.12f, 0.55f, 0.95f, 0.95f);
        nitroCB.highlightedColor = new Color(0.2f,  0.70f, 1.0f,  1f);
        nitroCB.pressedColor     = new Color(0.08f, 0.40f, 0.80f, 1f);
        nitroCB.disabledColor    = new Color(0.25f, 0.35f, 0.45f, 0.5f);
        btnNitro.colors = nitroCB;
        Text nitroLabel = btnNitro.GetComponentInChildren<Text>();
        if (nitroLabel != null) nitroLabel.fontStyle = FontStyle.Bold;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnNitro.onClick, uiManager.OnClickNitro);

        // Horn button (bottom right, left of Nitro)
        Button btnHorn = CreateButton(hudPanel.transform, "BtnHorn", "HORN\n[H]", Vector2.zero, new Vector2(145, 65), defaultFont);
        RectTransform rtHorn = btnHorn.GetComponent<RectTransform>();
        rtHorn.anchorMin = new Vector2(1f, 0f);
        rtHorn.anchorMax = new Vector2(1f, 0f);
        rtHorn.pivot     = new Vector2(1f, 0f);
        rtHorn.anchoredPosition = new Vector2(-215f, 40f);
        btnHorn.GetComponent<Image>().color = new Color(0.95f, 0.55f, 0.12f, 0.95f);
        ColorBlock hornCB = btnHorn.colors;
        hornCB.normalColor      = new Color(0.95f, 0.55f, 0.12f, 0.95f);
        hornCB.highlightedColor = new Color(1.0f,  0.68f, 0.22f, 1f);
        hornCB.pressedColor     = new Color(0.75f, 0.40f, 0.08f, 1f);
        hornCB.selectedColor    = hornCB.highlightedColor;
        btnHorn.colors = hornCB;
        Text hornLabel = btnHorn.GetComponentInChildren<Text>();
        if (hornLabel != null) hornLabel.fontStyle = FontStyle.Bold;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnHorn.onClick, uiManager.OnClickHorn);

        // Camera Switch button (top right, left of Sound)
        Button btnCamera = CreateButton(hudPanel.transform, "BtnCamera", "CAM\n[C]", Vector2.zero, new Vector2(120, 52), defaultFont);
        RectTransform rtCamera = btnCamera.GetComponent<RectTransform>();
        rtCamera.anchorMin = new Vector2(1f, 1f);
        rtCamera.anchorMax = new Vector2(1f, 1f);
        rtCamera.pivot     = new Vector2(1f, 1f);
        rtCamera.anchoredPosition = new Vector2(-325f, -40f);
        Text camLabel = btnCamera.GetComponentInChildren<Text>();
        if (camLabel != null) camLabel.fontStyle = FontStyle.Bold;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnCamera.onClick, uiManager.OnClickCamera);

        // Boost/Nitro bar (bottom center)
        GameObject boostBg = CreatePanel(hudPanel.transform, "BoostBg", new Color(0.1f, 0.1f, 0.1f, 0.7f));
        RectTransform rtBoostBg = boostBg.GetComponent<RectTransform>();
        rtBoostBg.anchorMin = new Vector2(0.5f, 0f);
        rtBoostBg.anchorMax = new Vector2(0.5f, 0f);
        rtBoostBg.pivot     = new Vector2(0.5f, 0f);
        rtBoostBg.anchoredPosition = new Vector2(0f, 40f);
        rtBoostBg.sizeDelta        = new Vector2(400f, 36f);

        GameObject boostFillObj = new GameObject("BoostFill");
        boostFillObj.transform.SetParent(boostBg.transform);
        Image boostFill = boostFillObj.AddComponent<Image>();
        boostFill.color      = new Color(0.1f, 0.7f, 1f, 0.9f);
        boostFill.type       = Image.Type.Filled;
        boostFill.fillMethod = Image.FillMethod.Horizontal;
        boostFill.fillOrigin = 0;
        boostFill.fillAmount = 1f;
        RectTransform rtBoostFill = boostFillObj.GetComponent<RectTransform>();
        rtBoostFill.anchorMin      = Vector2.zero;
        rtBoostFill.anchorMax      = Vector2.one;
        rtBoostFill.sizeDelta      = Vector2.zero;
        rtBoostFill.anchoredPosition = Vector2.zero;

        Text boostText = CreateText(boostBg.transform, "BoostText", "BOOST: READY [SPACE]", 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(380, 36), defaultFont);

        // ── 2. Main Menu Panel ────────────────────────────────────────────
        GameObject menuPanel = CreatePanel(canvasTransform, "MainMenuPanel", new Color(0.08f, 0.10f, 0.15f, 0.92f));
        CreateText(menuPanel.transform, "Title",    "TRAFFIC ESCAPE 3D",                           68, TextAnchor.MiddleCenter, new Vector2(0, 220), new Vector2(900, 100), defaultFont, new Color(1f, 0.85f, 0.2f));
        CreateText(menuPanel.transform, "Subtitle", "DODGE TRAFFIC  •  COLLECT COINS  •  SURVIVE", 24, TextAnchor.MiddleCenter, new Vector2(0, 150), new Vector2(800, 50),  defaultFont, new Color(0.8f, 0.85f, 0.9f));
        Text menuHighScore = CreateText(menuPanel.transform, "MenuHighScore", "HIGH SCORE: 0", 30, TextAnchor.MiddleCenter, new Vector2(0, 80), new Vector2(500, 50), defaultFont, Color.white);

        Button btnPlay = CreateButton(menuPanel.transform, "BtnPlay", "PLAY", new Vector2(0, -10), new Vector2(280, 65), defaultFont);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnPlay.onClick, uiManager.OnClickPlay);

        Button btnHighScore = CreateButton(menuPanel.transform, "BtnHighScore", "HIGH SCORE", new Vector2(0, -90), new Vector2(280, 65), defaultFont);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnHighScore.onClick, uiManager.OpenHighScoreDialog);

        Button btnQuit = CreateButton(menuPanel.transform, "BtnQuit", "QUIT", new Vector2(0, -170), new Vector2(280, 65), defaultFont);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnQuit.onClick, uiManager.OnClickQuit);

        CreateText(menuPanel.transform, "ControlsNote",
            "CONTROLS:  A/D or Arrows: Steer  |  W/S: Speed/Brake  |  SPACE: Boost  |  ESC/P: Pause",
            20, TextAnchor.MiddleCenter, new Vector2(0, -280), new Vector2(900, 40), defaultFont, new Color(0.7f, 0.7f, 0.75f));

        // High Score Dialog
        GameObject hsDialog = CreatePanel(canvasTransform, "HighScoreDialog", new Color(0.05f, 0.05f, 0.08f, 0.95f));
        RectTransform rtHs = hsDialog.GetComponent<RectTransform>();
        rtHs.anchorMin = new Vector2(0.5f, 0.5f);
        rtHs.anchorMax = new Vector2(0.5f, 0.5f);
        rtHs.sizeDelta = new Vector2(500, 350);
        rtHs.anchoredPosition = Vector2.zero;
        CreateText(hsDialog.transform, "HsTitle",   "HALL OF FAME",          40, TextAnchor.MiddleCenter, new Vector2(0, 90),  new Vector2(400, 60),  defaultFont, new Color(1f, 0.85f, 0.2f));
        CreateText(hsDialog.transform, "HsContent", "BEST RECORD\n\nHIGH SCORE: 0", 28, TextAnchor.MiddleCenter, new Vector2(0, 10),  new Vector2(400, 100), defaultFont, Color.white);
        Button btnCloseHs = CreateButton(hsDialog.transform, "BtnCloseHs", "BACK", new Vector2(0, -100), new Vector2(180, 50), defaultFont);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnCloseHs.onClick, uiManager.CloseHighScoreDialog);
        hsDialog.SetActive(false);

        // ── 3. Pause Panel ────────────────────────────────────────────────
        GameObject pausePanel = CreatePanel(canvasTransform, "PausePanel", new Color(0.05f, 0.07f, 0.1f, 0.85f));
        CreateText(pausePanel.transform, "PauseTitle", "PAUSED", 60, TextAnchor.MiddleCenter, new Vector2(0, 150), new Vector2(500, 90), defaultFont, Color.white);
        Button btnResume   = CreateButton(pausePanel.transform, "BtnResume",    "RESUME",    new Vector2(0, 30),   new Vector2(260, 60), defaultFont);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnResume.onClick, uiManager.OnClickResume);
        Button btnRestart  = CreateButton(pausePanel.transform, "BtnRestart",   "RESTART",   new Vector2(0, -45),  new Vector2(260, 60), defaultFont);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnRestart.onClick, uiManager.OnClickRestart);
        Button btnPauseMenu = CreateButton(pausePanel.transform, "BtnPauseMenu", "MAIN MENU", new Vector2(0, -120), new Vector2(260, 60), defaultFont);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnPauseMenu.onClick, uiManager.OnClickMainMenu);
        pausePanel.SetActive(false);

        // ── 4. Game Over Panel ────────────────────────────────────────────
        GameObject gameOverPanel = CreatePanel(canvasTransform, "GameOverPanel", new Color(0.12f, 0.04f, 0.04f, 0.94f));
        CreateText(gameOverPanel.transform, "GameOverTitle", "GAME OVER", 66, TextAnchor.MiddleCenter, new Vector2(0, 210), new Vector2(600, 90), defaultFont, new Color(1f, 0.25f, 0.25f));
        GameObject newHsBanner = CreateText(gameOverPanel.transform, "NewHsBanner", "★ NEW HIGH SCORE! ★", 30, TextAnchor.MiddleCenter, new Vector2(0, 135), new Vector2(500, 50), defaultFont, new Color(1f, 0.85f, 0.2f)).gameObject;
        newHsBanner.SetActive(false);

        Text goScore     = CreateText(gameOverPanel.transform, "GoScore",     "SCORE: 0",        36, TextAnchor.MiddleCenter, new Vector2(0,  75), new Vector2(400, 50), defaultFont, Color.white);
        Text goHighScore = CreateText(gameOverPanel.transform, "GoHighScore", "HIGH SCORE: 0",   30, TextAnchor.MiddleCenter, new Vector2(0,  25), new Vector2(400, 50), defaultFont, new Color(0.9f, 0.85f, 0.7f));
        Text goDist      = CreateText(gameOverPanel.transform, "GoDistance",  "DISTANCE: 0 M",   28, TextAnchor.MiddleCenter, new Vector2(0, -20), new Vector2(400, 50), defaultFont, new Color(0.75f, 0.8f, 0.85f));
        Text goCoins     = CreateText(gameOverPanel.transform, "GoCoins",     "COINS: 0",        28, TextAnchor.MiddleCenter, new Vector2(0, -65), new Vector2(400, 50), defaultFont, new Color(1f, 0.85f, 0.2f));

        Button btnPlayAgain  = CreateButton(gameOverPanel.transform, "BtnPlayAgain", "PLAY AGAIN", new Vector2(0, -130), new Vector2(260, 55), defaultFont);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnPlayAgain.onClick, uiManager.OnClickRestart);
        Button btnGoMainMenu = CreateButton(gameOverPanel.transform, "BtnGoMenu",   "MAIN MENU",  new Vector2(0, -195), new Vector2(260, 55), defaultFont);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(btnGoMainMenu.onClick, uiManager.OnClickMainMenu);
        gameOverPanel.SetActive(false);

        // ── Wire up UIManager serialized references ──────────────────────
        SerializedObject soUI = new SerializedObject(uiManager);
        soUI.FindProperty("mainMenuPanel").objectReferenceValue  = menuPanel;
        soUI.FindProperty("hudPanel").objectReferenceValue       = hudPanel;
        soUI.FindProperty("pausePanel").objectReferenceValue     = pausePanel;
        soUI.FindProperty("gameOverPanel").objectReferenceValue  = gameOverPanel;
        soUI.FindProperty("highScoreDialog").objectReferenceValue = hsDialog;

        soUI.FindProperty("playButton").objectReferenceValue            = btnPlay;
        soUI.FindProperty("pauseButton").objectReferenceValue           = btnPause;
        soUI.FindProperty("highScoreButton").objectReferenceValue       = btnHighScore;
        soUI.FindProperty("quitButton").objectReferenceValue            = btnQuit;
        soUI.FindProperty("resumeButton").objectReferenceValue          = btnResume;
        soUI.FindProperty("restartButton").objectReferenceValue         = btnRestart;
        soUI.FindProperty("pauseMainMenuButton").objectReferenceValue   = btnPauseMenu;
        soUI.FindProperty("playAgainButton").objectReferenceValue       = btnPlayAgain;
        soUI.FindProperty("gameOverMainMenuButton").objectReferenceValue = btnGoMainMenu;
        soUI.FindProperty("closeHighScoreButton").objectReferenceValue  = btnCloseHs;
        soUI.FindProperty("nitroButton").objectReferenceValue           = btnNitro;
        soUI.FindProperty("soundButton").objectReferenceValue           = btnSound;
        soUI.FindProperty("hornButton").objectReferenceValue            = btnHorn;
        soUI.FindProperty("cameraButton").objectReferenceValue          = btnCamera;

        soUI.FindProperty("scoreText").objectReferenceValue        = scoreText;
        soUI.FindProperty("coinsText").objectReferenceValue        = coinsText;
        soUI.FindProperty("speedText").objectReferenceValue        = speedText;
        soUI.FindProperty("livesText").objectReferenceValue        = livesText;
        soUI.FindProperty("boostText").objectReferenceValue        = boostText;
        soUI.FindProperty("boostFillBar").objectReferenceValue     = boostFill;
        soUI.FindProperty("shieldStatusText").objectReferenceValue = shieldText;
        soUI.FindProperty("missionText").objectReferenceValue      = missionText;
        soUI.FindProperty("missionBannerText").objectReferenceValue = missionBannerText;
        soUI.FindProperty("soundButtonText").objectReferenceValue  = soundBtnLabel;

        soUI.FindProperty("menuHighScoreText").objectReferenceValue = menuHighScore;

        soUI.FindProperty("gameOverScoreText").objectReferenceValue     = goScore;
        soUI.FindProperty("gameOverHighScoreText").objectReferenceValue = goHighScore;
        soUI.FindProperty("gameOverDistanceText").objectReferenceValue  = goDist;
        soUI.FindProperty("gameOverCoinsText").objectReferenceValue     = goCoins;
        soUI.FindProperty("newHighScoreBanner").objectReferenceValue    = newHsBanner;

        soUI.FindProperty("playerCar").objectReferenceValue = playerCar;
        soUI.ApplyModifiedProperties();
    }

    // ── Generic UI helpers ──────────────────────────────────────────────────
    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panelObj = new GameObject(name);
        panelObj.transform.SetParent(parent);
        Image img = panelObj.AddComponent<Image>();
        img.color = color;
        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin      = Vector2.zero;
        rt.anchorMax      = Vector2.one;
        rt.sizeDelta      = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        return panelObj;
    }

    private static Text CreateText(
        Transform parent, string name, string text, int fontSize,
        TextAnchor alignment, Vector2 anchoredPos, Vector2 sizeDelta,
        Font font, Color? color = null)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent);
        Text t = textObj.AddComponent<Text>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.alignment = alignment;
        t.font      = font;
        t.color     = color ?? Color.white;
        RectTransform rt = textObj.GetComponent<RectTransform>();

        if (alignment == TextAnchor.UpperLeft)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
        }
        else if (alignment == TextAnchor.UpperCenter)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot     = new Vector2(0.5f, 1f);
        }
        else if (alignment == TextAnchor.UpperRight)
        {
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
        }
        else
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
        }

        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = sizeDelta;
        return t;
    }

    private static Button CreateButton(
        Transform parent, string name, string label,
        Vector2 anchoredPos, Vector2 sizeDelta, Font font)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent);
        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.18f, 0.26f, 0.38f, 0.95f);
        Button btn = btnObj.AddComponent<Button>();

        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.18f, 0.26f, 0.38f, 0.95f);
        cb.highlightedColor = new Color(0.26f, 0.38f, 0.56f, 1f);
        cb.pressedColor     = new Color(0.12f, 0.18f, 0.28f, 1f);
        cb.selectedColor    = cb.highlightedColor;
        btn.colors = cb;

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin      = new Vector2(0.5f, 0.5f);
        rt.anchorMax      = new Vector2(0.5f, 0.5f);
        rt.pivot          = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta      = sizeDelta;

        CreateText(btnObj.transform, "Label", label, 26, TextAnchor.MiddleCenter, Vector2.zero, sizeDelta, font, Color.white);
        return btn;
    }
}
