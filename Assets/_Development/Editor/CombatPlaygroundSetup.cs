using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Builds a combat-only copy of the locomotion test scene. The source scene and
// gameplay controller remain untouched so this can be run repeatedly.
public static class CombatPlaygroundSetup
{
    public const string ScenePath = "Assets/_Development/Scenes/CombatPlayground.unity";
    private const string SourceScenePath = "Assets/_Development/Scenes/PlayerLocomotionTest.unity";
    private const string SourceControllerPath = "Assets/_Game/Features/Player/Animations/Controllers/PlayerLocomotion.controller";
    private const string ControllerPath = "Assets/_Development/Prototypes/CombatPlayground.controller";
    private const string PlayerPrefabPath = "Assets/_Game/Features/Player/Prefabs/player.prefab";
    private const string SwordDataPath = "Assets/_Game/Features/Inventory/Data/Items/ShortSword.asset";
    private const string EnemyMaterialPath = "Assets/_Development/Prototypes/CombatPlaygroundEnemy.mat";

    [MenuItem("Tools/Player/Build Combat Playground")]
    public static void Run()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Exit Play Mode before building the combat playground.");

        CopyAsset(SourceControllerPath, ControllerPath);
        AnimatorController controller = ConfigureController();
        CopyAsset(SourceScenePath, ScenePath);
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ConfigureScene(scene, controller);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Combat Playground] Saved {ScenePath}.");
    }

    private static void CopyAsset(string sourcePath, string destinationPath)
    {
        if (AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
            throw new InvalidOperationException("Required source asset was not found: " + sourcePath);

        if (AssetDatabase.LoadMainAssetAtPath(destinationPath) != null && !AssetDatabase.DeleteAsset(destinationPath))
            throw new InvalidOperationException("Could not replace generated asset: " + destinationPath);

        if (!AssetDatabase.CopyAsset(sourcePath, destinationPath))
            throw new InvalidOperationException("Could not copy " + sourcePath + " to " + destinationPath);
    }

    private static AnimatorController ConfigureController()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            throw new InvalidOperationException("Combat controller copy could not be loaded: " + ControllerPath);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        BlendTree groundedTree = AssetDatabase.LoadAllAssetsAtPath(ControllerPath).OfType<BlendTree>()
            .Single(tree => tree.name == "Directional Root Motion");
        ChildMotion[] motions = groundedTree.children;
        for (int index = 0; index < motions.Length; index++)
        {
            if (motions[index].position == Vector2.zero)
                motions[index].motion = LoadClip("CombatIdle");
            else if (motions[index].position == new Vector2(0f, 2f))
                motions[index].motion = LoadClip("Run With Sword");
        }
        groundedTree.children = motions;

        AddActionState(machine, "SwordAttack", "Stable Sword Outward Slash", 1.25f);
        AddActionState(machine, "Death", "Falling Back Death", 1f);
        ConfigureSwordGrip(controller);
        EditorUtility.SetDirty(groundedTree);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void ConfigureSwordGrip(AnimatorController controller)
    {
        const string maskPath = "Assets/_Development/Prototypes/Combat/RightHandGrip.mask";
        var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
        if (mask == null)
        {
            mask = new AvatarMask();
            AssetDatabase.CreateAsset(mask, maskPath);
        }
        for (int index = 0; index < (int)AvatarMaskBodyPart.LastBodyPart; index++)
            mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)index, false);
        mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
        EditorUtility.SetDirty(mask);

        controller.AddLayer("Sword grip");
        var layers = controller.layers;
        var layer = layers[layers.Length - 1];
        layer.avatarMask = mask;
        layer.defaultWeight = 1f;
        var grip = layer.stateMachine.AddState("Closed right hand");
        grip.motion = LoadClip("Stable Sword Outward Slash");
        grip.speed = 0f;
        grip.cycleOffset = 0.35f;
        layer.stateMachine.defaultState = grip;
        controller.layers = layers;
    }

    private static void AddActionState(AnimatorStateMachine machine, string stateName, string clipName, float speed)
    {
        AnimatorState state = machine.AddState(stateName);
        state.motion = LoadClip(clipName);
        state.speed = speed;
        // These action states deliberately have no transitions. Runtime combat
        // returns the animator to Grounded with CrossFade after each action.
    }

    private static AnimationClip LoadClip(string clipName)
    {
        string path = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(clipName + " t:Model")
            .First(guid => System.IO.Path.GetFileNameWithoutExtension(
                AssetDatabase.GUIDToAssetPath(guid)) == clipName));
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .First(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal));
    }

    private static void ConfigureScene(Scene scene, AnimatorController controller)
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
            player = Object.FindAnyObjectByType<PlayerController>()?.gameObject;
        if (player == null)
            throw new InvalidOperationException("The copied scene does not contain its player instance.");

        RemoveLegacySceneObjects();
        DisableLegacyPlayerCombat(player);

        player.transform.SetPositionAndRotation(new Vector3(0f, 1.04f, -2f), Quaternion.identity);
        CharacterMotor playerMotor = player.GetComponent<CharacterMotor>();
        Animator playerAnimator = playerMotor != null ? playerMotor.Animator : null;
        if (playerMotor == null || playerAnimator == null)
            throw new InvalidOperationException("The player prefab needs its CharacterMotor and body Animator.");
        playerAnimator.runtimeAnimatorController = controller;
        playerAnimator.applyRootMotion = true;
        playerAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        PlayerCameraRig cameraRig = player.GetComponent<PlayerCameraRig>();
        if (cameraRig == null || cameraRig.ViewCamera == null)
            throw new InvalidOperationException("The player prefab needs its configured PlayerCameraRig.");
        Camera playerCamera = cameraRig.ViewCamera;
        playerCamera.enabled = true;
        AudioListener listener = playerCamera.GetComponent<AudioListener>() ?? playerCamera.gameObject.AddComponent<AudioListener>();
        listener.enabled = true;
        DisableLegacyHands(player.transform);
        foreach (Transform child in player.GetComponentsInChildren<Transform>(true))
            if (child.name == "Sword pose preview (no damage)")
                Object.DestroyImmediate(child.gameObject);

        Material enemyMaterial = LoadOrCreateEnemyMaterial();
        var enemyRoot = new GameObject("Combat Playground Enemy");
        enemyRoot.layer = 10;
        enemyRoot.tag = "Enemy";
        enemyRoot.transform.SetPositionAndRotation(new Vector3(0f, 1.04f, 3f), Quaternion.Euler(0f, 180f, 0f));
        CharacterController enemyController = enemyRoot.AddComponent<CharacterController>();
        enemyController.center = Vector3.zero;
        enemyController.height = 2f;
        enemyController.radius = 0.35f;
        enemyController.skinWidth = 0.035f;
        enemyController.minMoveDistance = 0f;
        CharacterMotor enemyMotor = enemyRoot.AddComponent<CharacterMotor>();
        SetSerializedMask(enemyMotor, "groundMask", ~((1 << 9) | (1 << 10) | (1 << 5)));

        Transform playerBody = player.transform.Find("BodyModel");
        if (playerBody == null)
            playerBody = player.GetComponentInChildren<Animator>(true)?.transform;
        if (playerBody == null)
            throw new InvalidOperationException("The player has no body model to clone for the enemy.");

        GameObject enemyBody = Object.Instantiate(playerBody.gameObject, enemyRoot.transform, false);
        enemyBody.name = "BodyModel";
        enemyBody.transform.localPosition = playerBody.localPosition;
        enemyBody.transform.localRotation = playerBody.localRotation;
        enemyBody.transform.localScale = playerBody.localScale;
        SetLayerRecursively(enemyBody.transform, enemyRoot.layer);

        Animator enemyAnimator = enemyBody.GetComponent<Animator>();
        if (enemyAnimator == null)
            throw new InvalidOperationException("The cloned enemy body has no Animator.");
        enemyAnimator.runtimeAnimatorController = controller;
        enemyAnimator.applyRootMotion = true;
        enemyAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        SetSerializedReference(enemyMotor, "animator", enemyAnimator);
        CharacterRootMotion enemyRootMotion = enemyBody.GetComponent<CharacterRootMotion>() ?? enemyBody.AddComponent<CharacterRootMotion>();
        SetSerializedReference(enemyRootMotion, "motor", enemyMotor);
        foreach (ShowcaseRootMotion showcaseMotion in enemyBody.GetComponentsInChildren<ShowcaseRootMotion>(true))
            Object.DestroyImmediate(showcaseMotion);
        foreach (SkinnedMeshRenderer renderer in enemyBody.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => enemyMaterial).ToArray();

        ItemData swordData = AssetDatabase.LoadAssetAtPath<ItemData>(SwordDataPath);
        if (swordData == null || swordData.itemStats == null)
            throw new InvalidOperationException("ShortSword item data could not be loaded: " + SwordDataPath);

        PlaygroundFighter playerFighter = player.GetComponent<PlaygroundFighter>() ?? player.AddComponent<PlaygroundFighter>();
        playerFighter.isPlayer = true;
        playerFighter.maximumHealth = player.GetComponent<PlayerStatsManager>()?.GetPlayerStats().maxHealth ?? playerFighter.maximumHealth;
        playerFighter.damage = swordData.itemStats.GetDamage();
        playerFighter.motor = playerMotor;
        playerFighter.arena = null;
        playerFighter.weapon = CreateSword(playerAnimator).transform;

        PlaygroundFighter enemyFighter = enemyRoot.AddComponent<PlaygroundFighter>();
        enemyFighter.isPlayer = false;
        enemyFighter.maximumHealth = 60f;
        enemyFighter.damage = 12f;
        enemyFighter.motor = enemyMotor;
        enemyFighter.weapon = CreateSword(enemyAnimator).transform;

        PlaygroundEnemy enemy = enemyRoot.AddComponent<PlaygroundEnemy>();
        enemy.target = playerFighter;
        enemy.fighter = enemyFighter;

        var arenaObject = new GameObject("Combat Playground Arena");
        PlaygroundArena arena = arenaObject.AddComponent<PlaygroundArena>();
        arena.player = playerFighter;
        arena.enemy = enemyFighter;
        arena.playerSpawn = new Vector3(0f, 1.04f, -2f);
        arena.enemySpawn = new Vector3(0f, 1.04f, 3f);
        arena.playerRotation = Quaternion.identity;
        arena.enemyRotation = Quaternion.Euler(0f, 180f, 0f);
        playerFighter.arena = arena;
        enemyFighter.arena = arena;
    }

    private static void RemoveLegacySceneObjects()
    {
        foreach (PlayerAnimationTest test in Object.FindObjectsByType<PlayerAnimationTest>())
            Object.DestroyImmediate(test.gameObject);
        foreach (PlayerAnimationShowcase showcase in Object.FindObjectsByType<PlayerAnimationShowcase>())
            if (showcase != null)
                Object.DestroyImmediate(showcase);

        GameObject externalCamera = GameObject.Find("Test scene camera");
        if (externalCamera != null)
            Object.DestroyImmediate(externalCamera);
        GameObject captions = GameObject.Find("Test captions");
        if (captions != null)
            Object.DestroyImmediate(captions);
    }

    private static void DisableLegacyPlayerCombat(GameObject player)
    {
        foreach (PlayerCombatHandler component in player.GetComponentsInChildren<PlayerCombatHandler>(true))
            component.enabled = false;
        foreach (WeaponsHandler component in player.GetComponentsInChildren<WeaponsHandler>(true))
            component.enabled = false;
        foreach (Sword component in player.GetComponentsInChildren<Sword>(true))
            component.enabled = false;
        foreach (WeaponCollisionCheck component in player.GetComponentsInChildren<WeaponCollisionCheck>(true))
            component.enabled = false;
    }

    private static void DisableLegacyHands(Transform player)
    {
        Transform hands = player.Find("ViewPivot/Hands") ?? player.Find("Hands");
        if (hands != null)
            hands.gameObject.SetActive(false);
    }

    private static Material LoadOrCreateEnemyMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(EnemyMaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, EnemyMaterialPath);
        }

        material.SetColor("_BaseColor", new Color(0.72f, 0.1f, 0.12f));
        material.SetFloat("_Smoothness", 0.25f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CreateSword(Animator animator)
    {
        GameObject sword = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ItemData>(SwordDataPath).itemStats.GetPrefab());
        sword.name = "Combat Playground Sword";
        foreach (MonoBehaviour script in sword.GetComponentsInChildren<MonoBehaviour>(true))
            Object.DestroyImmediate(script);
        foreach (Collider collider in sword.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(collider);
        foreach (Rigidbody body in sword.GetComponentsInChildren<Rigidbody>(true))
            Object.DestroyImmediate(body);

        Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (rightHand == null)
            throw new InvalidOperationException("The Humanoid Animator has no RightHand bone for its sword.");
        // Fitted to this yBot and ShortSword only. The mesh pivot is near the guard,
        // while its handle centre is 0.035 model units below that pivot.
        var socket = new GameObject("Playground sword socket").transform;
        socket.SetParent(rightHand, false);
        socket.localPosition = new Vector3(0.005f, 0.12f, 0.025f);
        socket.localRotation = Quaternion.Euler(0f, 0f, 80f);
        sword.transform.SetParent(socket, false);
        var mesh = sword.GetComponent<MeshFilter>().sharedMesh;
        float scale = 0.85f / mesh.bounds.size.y;
        sword.transform.localScale = Vector3.one * scale;
        sword.transform.localRotation = Quaternion.identity;
        sword.transform.localPosition = Vector3.up * (0.035f * scale);
        sword.SetActive(true);
        return sword;
    }

    private static void SetSerializedReference(Object target, string propertyName, Object value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"Serialized field '{propertyName}' was not found on {target.GetType().Name}.");
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetSerializedMask(Object target, string propertyName, int value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            throw new InvalidOperationException($"Serialized field '{propertyName}' was not found on {target.GetType().Name}.");
        property.intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root)
            SetLayerRecursively(child, layer);
    }
}
