using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class CombatPlaygroundChecks
{
    private const string Running = "CombatPlaygroundChecks.Running";
    private const string Batch = "CombatPlaygroundChecks.Batch";
    private const string Prefix = "CombatPlaygroundChecks.";

    private static Stack<IEnumerator> routines;
    private static int previousFrame;
    private static int checks;
    private static double deadline;
    private static Keyboard keyboard;
    private static InputSettings.BackgroundBehavior previousBackgroundBehavior;
    private static InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;

    static CombatPlaygroundChecks() => EditorApplication.playModeStateChanged += Changed;

    [MenuItem("Tools/Validation/Combat Playground (Play Mode)")]
    public static void Run() => RunBatch();

    public static void RunBatch()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        SessionState.SetString(Prefix + "PreviousScene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        SaveEnvironment();
        CombatPlaygroundSetup.Run();
        PlayerFeatureChecks.RunAll();
        SaveFeatureChecks.RunAll();
        SessionState.SetBool(Batch, true);
        SessionState.SetBool(Running, true);
        EditorApplication.isPlaying = true;
    }

    private static void SaveEnvironment()
    {
        SessionState.SetBool(Prefix + "HadPreference", PlayerPrefs.HasKey(PlayerCameraRig.PreferenceKey));
        SessionState.SetInt(Prefix + "Preference", PlayerPrefs.GetInt(PlayerCameraRig.PreferenceKey));
        foreach (string key in new[] { PlayerCameraRig.FirstPersonFovKey, PlayerCameraRig.ThirdPersonFovKey })
        {
            SessionState.SetBool(Prefix + "Has." + key, PlayerPrefs.HasKey(key));
            SessionState.SetFloat(Prefix + "Value." + key, PlayerPrefs.GetFloat(key));
        }
        SessionState.SetInt(Prefix + "Background", (int)InputSystem.settings.backgroundBehavior);
        SessionState.SetInt(Prefix + "EditorInput", (int)InputSystem.settings.editorInputBehaviorInPlayMode);
        SessionState.SetInt(Prefix + "CursorLock", (int)Cursor.lockState);
        SessionState.SetBool(Prefix + "CursorVisible", Cursor.visible);
        SessionState.SetFloat(Prefix + "TimeScale", Time.timeScale);
    }

    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false))
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            previousBackgroundBehavior = (InputSettings.BackgroundBehavior)SessionState.GetInt(Prefix + "Background", 0);
            previousEditorInput = (InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(Prefix + "EditorInput", 0);
            previousFrame = -1;
            checks = 0;
            deadline = EditorApplication.timeSinceStartup + 120;
            routines = new Stack<IEnumerator>();
            routines.Push(Exercise());
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.update -= Tick;
            RestoreEnvironment();
            SessionState.SetBool(Running, false);
            if (SessionState.GetBool(Batch, false))
            {
                SessionState.SetBool(Batch, false);
                EditorApplication.Exit(SessionState.GetBool(Prefix + "Passed", false) ? 0 : 1);
            }
            else
            {
                string previousScene = SessionState.GetString(Prefix + "PreviousScene", string.Empty);
                if (!string.IsNullOrEmpty(previousScene))
                    EditorSceneManager.OpenScene(previousScene);
            }
        }
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
                throw new TimeoutException("Combat playground checks exceeded 120 seconds.");
            if (previousFrame == Time.frameCount)
                return;
            previousFrame = Time.frameCount;

            while (routines.Count > 0)
            {
                IEnumerator current = routines.Peek();
                if (!current.MoveNext())
                {
                    routines.Pop();
                    continue;
                }
                if (current.Current is IEnumerator nested)
                {
                    routines.Push(nested);
                    continue;
                }
                return;
            }

            Debug.Log($"[Combat Playground Checks] PASS: {checks} checks.");
            Finish(true);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Finish(false);
        }
    }

    private static void Finish(bool passed)
    {
        if (keyboard != null)
            InputSystem.RemoveDevice(keyboard);
        keyboard = null;
        InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        Time.timeScale = 1f;
        SessionState.SetBool(Prefix + "Passed", passed);
        EditorApplication.update -= Tick;
        EditorApplication.isPlaying = false;
    }

    private static void RestoreEnvironment()
    {
        if (SessionState.GetBool(Prefix + "HadPreference", false))
            PlayerPrefs.SetInt(PlayerCameraRig.PreferenceKey, SessionState.GetInt(Prefix + "Preference", 0));
        else
            PlayerPrefs.DeleteKey(PlayerCameraRig.PreferenceKey);
        foreach (string key in new[] { PlayerCameraRig.FirstPersonFovKey, PlayerCameraRig.ThirdPersonFovKey })
        {
            if (SessionState.GetBool(Prefix + "Has." + key, false))
                PlayerPrefs.SetFloat(key, SessionState.GetFloat(Prefix + "Value." + key, 0f));
            else
                PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
        InputSystem.settings.backgroundBehavior = (InputSettings.BackgroundBehavior)SessionState.GetInt(Prefix + "Background", 0);
        InputSystem.settings.editorInputBehaviorInPlayMode =
            (InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt(Prefix + "EditorInput", 0);
        Cursor.lockState = (CursorLockMode)SessionState.GetInt(Prefix + "CursorLock", (int)CursorLockMode.None);
        Cursor.visible = SessionState.GetBool(Prefix + "CursorVisible", true);
        Time.timeScale = SessionState.GetFloat(Prefix + "TimeScale", 1f);
    }

    private static IEnumerator Exercise()
    {
        Time.timeScale = 1f;
        yield return null;
        yield return null;

        PlaygroundArena arena = Object.FindAnyObjectByType<PlaygroundArena>();
        Require(arena != null && arena.player != null && arena.enemy != null,
            "The copied scene starts with one configured player and enemy.");
        PlaygroundFighter player = arena.player;
        PlaygroundFighter enemy = arena.enemy;
        PlaygroundEnemy brain = enemy.GetComponent<PlaygroundEnemy>();
        PlayerStatsManager stats = player.GetComponent<PlayerStatsManager>();
        PlayerCameraRig cameraRig = player.GetComponent<PlayerCameraRig>();
        CharacterMotor playerMotor = player.motor;
        CharacterMotor enemyMotor = enemy.motor;
        Camera camera = cameraRig.ViewCamera;

        Require(player.gameObject.scene == arena.gameObject.scene,
            "The arena keeps the player in its own scene instead of a persistent scene.");
        Require(player.isPlayer && !enemy.isPlayer && playerMotor != null && enemyMotor != null,
            "Both fighters use their real character motors and the player keeps player identity.");
        Require(player.maximumHealth == stats.GetPlayerStats().maxHealth && enemy.maximumHealth == 60f &&
            Mathf.Approximately(enemy.damage, 12f), "Combat health and enemy damage use the configured player stats.");
        Require(player.weapon != null && enemy.weapon != null && player.weapon.IsChildOf(playerMotor.Animator.GetBoneTransform(HumanBodyBones.RightHand)) &&
            enemy.weapon.IsChildOf(enemyMotor.Animator.GetBoneTransform(HumanBodyBones.RightHand)),
            "Both fighters carry their visual sword on the animated right hand.");
        AnimatorController combatController = playerMotor.Animator.runtimeAnimatorController as AnimatorController;
        AvatarMask gripMask = combatController != null
            ? combatController.layers.Single(layer => layer.name == "Sword grip").avatarMask
            : null;
        Require(combatController != null && enemyMotor.Animator.runtimeAnimatorController == combatController &&
            gripMask != null && ContainsOnlyRightFingers(gripMask),
            "The shared sword-grip layer masks only the right fingers, with no arm or root transforms.");
        Require(Object.FindObjectsByType<Camera>().Count(value => value.enabled && value.targetTexture == null) == 1 &&
            Object.FindObjectsByType<AudioListener>().Count(value => value.enabled) == 1,
            "The playground runs with one display camera and one active AudioListener.");
        Require(cameraRig.IsThirdPerson && camera.enabled && arena.HUD != null,
            "The arena starts in third person and creates its own combat HUD.");
        yield return Wait(0.25f);
        Require(GripAligned(player) && GripAligned(enemy),
            "Both sword grip points stay at the palm across live idle animation frames.");
        Require(ClosedFingerPose(player) && ClosedFingerPose(enemy),
            "The right middle finger stays curled around each sword during idle.");

        brain.pursuing = false;
        Vector3 initialEnemyPosition = enemy.transform.position;
        yield return Wait(1.25f);
        Require(Vector3.Distance(enemy.transform.position, initialEnemyPosition) < 0.02f,
            "Pausing enemy pursuit leaves its motor stationary during manual combat setup.");
        Require(enemyMotor.Animator.runtimeAnimatorController == playerMotor.Animator.runtimeAnimatorController,
            "The enemy uses the copied combat controller shared with the player.");

        arena.ResetRound();
        yield return WaitForGrounded(playerMotor, enemyMotor);
        Require(playerMotor.IsGrounded && enemyMotor.IsGrounded,
            "Both reset fighters settle on the arena floor before attacking.");

        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode =
            InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard = InputSystem.AddDevice<Keyboard>();
        cameraRig.SetThirdPerson(true, true);
        yield return PressView();
        yield return Wait(0.3f);
        Require(!cameraRig.IsThirdPerson, "The real F5 binding switches the player to first person.");
        int enemyVisualLayer = enemy.GetComponentInChildren<Renderer>(true).gameObject.layer;
        Require(enemyVisualLayer == 10 && (camera.cullingMask & (1 << 10)) != 0,
            "The first-person camera retains the enemy on visible layer 10.");
        yield return PressView();
        yield return Wait(0.3f);
        Require(cameraRig.IsThirdPerson, "F5 switches back to third person after the NPC visibility check.");

        var mode = player.GetComponent<PlayerModeController>();
        mode.SetMode(PlayerMode.Paused);
        Require(Time.timeScale == 0f && !player.TryAttack(), "Paused gameplay blocks a manual sword attack.");
        mode.ResetToGameplay();
        Require(Time.timeScale > 0f, "Returning to gameplay releases the pause gate.");

        PlaceEnemy(enemy, player.transform.position + Vector3.forward * 1.15f, Quaternion.Euler(0f, 180f, 0f));
        yield return WaitForGrounded(playerMotor, enemyMotor);
        float staminaBefore = stats.GetStamina();
        float enemyHealthBefore = enemy.Health;
        Require(player.TryAttack(), "A grounded player can begin a sword attack.");
        Require(Mathf.Approximately(staminaBefore - stats.GetStamina(), player.staminaCost),
            "A successful attack spends stamina once at startup.");
        bool hitDuringWindow = false;
        bool gripAlignedThroughAttack = true;
        bool closedFingerDuringSwing = true;
        bool closedFingerDuringRecovery = true;
        bool handRotatedDuringSwing = false;
        int swingPoseFrames = 0;
        int recoveryPoseFrames = 0;
        Quaternion idleHandRotation = playerMotor.Animator.GetBoneTransform(HumanBodyBones.RightHand).rotation;
        yield return CompleteAttack(player, enemy, enemyHealthBefore, value => hitDuringWindow |= value, state =>
        {
            if (!state.IsName("SwordAttack"))
                return;

            gripAlignedThroughAttack &= GripAligned(player) && GripAligned(enemy);
            float normalizedTime = state.normalizedTime % 1f;
            if (player.IsActiveWindow)
            {
                swingPoseFrames++;
                closedFingerDuringSwing &= ClosedFingerPose(player);
                handRotatedDuringSwing |= Quaternion.Angle(idleHandRotation,
                    playerMotor.Animator.GetBoneTransform(HumanBodyBones.RightHand).rotation) > 5f;
            }
            else if (normalizedTime > player.activeEnd && normalizedTime < 0.94f)
            {
                recoveryPoseFrames++;
                closedFingerDuringRecovery &= ClosedFingerPose(player);
            }
        });
        Require(player.AttacksStarted == 1,
            "Repeated attack requests during the swing cannot restart or spam it.");
        Require(hitDuringWindow && player.HitsLanded == 1 &&
            Mathf.Approximately(enemyHealthBefore - enemy.Health, player.damage),
            "The active sword window hits once and applies exactly one player damage event.");
        Require(gripAlignedThroughAttack && swingPoseFrames > 0 && recoveryPoseFrames > 0,
            "Both sword grips remain palm-aligned across actual swing and recovery frames.");
        Require(closedFingerDuringSwing && closedFingerDuringRecovery && handRotatedDuringSwing,
            "Fingers stay curled through the real swing and recovery while the hand rotates.");
        yield return Wait(0.25f);
        Require(playerMotor.Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded") && !player.IsAttacking,
            "The completed sword action returns to Grounded.");

        arena.ResetRound();
        yield return WaitForGrounded(playerMotor, enemyMotor);
        PlaceEnemy(enemy, player.transform.position + Vector3.forward * (player.reach + 3f), Quaternion.Euler(0f, 180f, 0f));
        enemyHealthBefore = enemy.Health;
        Require(player.TryAttack(), "An out-of-range attempt still plays its committed swing.");
        yield return CompleteAttack(player, enemy, enemyHealthBefore, _ => { });
        Require(enemy.HitsLanded == 0 && Mathf.Approximately(enemy.Health, enemyHealthBefore),
            "A swing beyond reach does not damage the enemy.");

        arena.ResetRound();
        yield return WaitForGrounded(playerMotor, enemyMotor);
        PlaceEnemy(enemy, player.transform.position + Vector3.back * 1.1f, Quaternion.identity);
        enemyHealthBefore = enemy.Health;
        Require(player.TryAttack(), "A player can commit an attack while the target is behind.");
        yield return CompleteAttack(player, enemy, enemyHealthBefore, _ => { });
        Require(enemy.HitsLanded == 0 && Mathf.Approximately(enemy.Health, enemyHealthBefore),
            "A target behind the fighter is outside the sword arc.");

        arena.ResetRound();
        yield return WaitForGrounded(playerMotor, enemyMotor);
        PlaceEnemy(enemy, player.transform.position + Vector3.forward * 1.2f, Quaternion.Euler(0f, 180f, 0f));
        GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.layer = LayerMask.NameToLayer("Obstacle");
        blocker.transform.position = player.transform.position + Vector3.forward * 0.58f;
        blocker.transform.localScale = new Vector3(2f, 2f, 0.2f);
        Physics.SyncTransforms();
        enemyHealthBefore = enemy.Health;
        Require(player.TryAttack(), "A player can commit an attack with cover between the fighters.");
        yield return CompleteAttack(player, enemy, enemyHealthBefore, _ => { });
        Require(enemy.HitsLanded == 0 && Mathf.Approximately(enemy.Health, enemyHealthBefore),
            "Environment geometry blocks a sword hit.");
        Object.Destroy(blocker);

        arena.ResetRound();
        yield return WaitForGrounded(playerMotor, enemyMotor);
        brain.pursuing = true;
        Vector3 chaseStart = enemy.transform.position;
        float chaseDistance = Vector3.Distance(player.transform.position, chaseStart);
        yield return Wait(1.25f);
        Require(Vector3.Distance(player.transform.position, enemy.transform.position) < chaseDistance - 0.15f,
            "The re-enabled enemy approaches the player under root motion.");
        Require(enemyMotor.AnimationInput.y > 0.01f || enemy.AttacksStarted > 0,
            "The enemy AI drives its locomotion motor while approaching.");

        arena.ResetRound();
        PlaceEnemy(enemy, player.transform.position + Vector3.forward * 1.15f, Quaternion.Euler(0f, 180f, 0f));
        yield return WaitForGrounded(playerMotor, enemyMotor);
        float playerHealthBefore = player.Health;
        float enemyHitDeadline = Time.time + 9f;
        while (player.Health >= playerHealthBefore && Time.time < enemyHitDeadline)
            yield return null;
        Require(player.Health < playerHealthBefore && enemy.AttacksStarted > 0,
            "The live enemy attack damages the player through the combat window.");

        brain.pursuing = false;
        arena.ResetRound();
        yield return WaitForGrounded(playerMotor, enemyMotor);
        enemy.TakeDamage(enemy.Health);
        Require(enemy.IsDead && arena.RoundOver && !player.TryAttack() && !enemy.TryAttack(),
            "Defeat closes the round and prevents either fighter from starting another attack.");

        AnimationClip deathClip = LoadClip("Falling Back Death");
        bool deathHasRootTravel = new Vector2(deathClip.averageSpeed.x, deathClip.averageSpeed.z).sqrMagnitude > 0.0001f;
        yield return Wait(0.15f);
        Vector3 deathMotionStart = enemy.transform.position;
        yield return Wait(deathClip.length + 0.2f);
        AnimatorStateInfo deathState = enemyMotor.Animator.GetCurrentAnimatorStateInfo(0);
        Vector3 deathMotionEnd = enemy.transform.position;
        Debug.Log($"[Death Motion] imported average {deathClip.averageSpeed}, collider {deathMotionStart} -> {deathMotionEnd}, grounded {enemyMotor.IsGrounded}, normalized {deathState.normalizedTime}");
        if (deathHasRootTravel)
            Require(Vector3.Distance(deathMotionStart, deathMotionEnd) > 0.01f,
                "The defeated enemy's collider follows authored Falling Back Death root motion.");
        Require(deathState.IsName("Death") && deathState.normalizedTime >= 0.99f,
            "The defeated enemy remains in the completed death animation state.");
        yield return Wait(0.3f);
        Require(Vector3.Distance(enemy.transform.position, deathMotionEnd) < 0.01f,
            "The defeated enemy holds its final collider position after the death clip ends.");

        player.TakeDamage(player.Health);
        Require(player.IsDead && !player.TryAttack(), "Player death also prevents another attack.");
        arena.ResetRound();
        yield return WaitForGrounded(playerMotor, enemyMotor);
        Require(!player.IsDead && !enemy.IsDead && !arena.RoundOver &&
            Mathf.Approximately(player.Health, player.MaxHealth) && Mathf.Approximately(enemy.Health, enemy.MaxHealth),
            "Reset restores both fighters and their full health.");
        Require(Vector3.Distance(player.transform.position, arena.playerSpawn) < 0.02f &&
            Vector3.Distance(enemy.transform.position, arena.enemySpawn) < 0.02f,
            "Reset returns both actors to their saved spawn transforms.");
    }

    private static IEnumerator CompleteAttack(PlaygroundFighter attacker, PlaygroundFighter victim,
        float initialHealth, Action<bool> reportWindowHit, Action<AnimatorStateInfo> observePose = null)
    {
        float end = Time.time + 5f;
        bool observedHitInWindow = false;
        bool repeatedRequestRejected = true;
        while (attacker.IsAttacking && Time.time < end)
        {
            if (attacker.isPlayer && attacker.TryAttack())
                repeatedRequestRejected = false;
            observePose?.Invoke(attacker.motor.Animator.GetCurrentAnimatorStateInfo(0));
            if (attacker.IsActiveWindow && victim.Health < initialHealth)
                observedHitInWindow = true;
            yield return null;
        }
        Require(!attacker.IsAttacking, "An animation-driven sword action completes within five seconds.");
        Require(repeatedRequestRejected, "Repeated input requests are rejected until the swing recovers.");
        reportWindowHit(observedHitInWindow);
    }

    private static bool ContainsOnlyRightFingers(AvatarMask mask)
    {
        if (mask.transformCount != 0 || !mask.GetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers))
            return false;

        for (int index = 0; index < (int)AvatarMaskBodyPart.LastBodyPart; index++)
        {
            var bodyPart = (AvatarMaskBodyPart)index;
            if (bodyPart != AvatarMaskBodyPart.RightFingers && mask.GetHumanoidBodyPartActive(bodyPart))
                return false;
        }
        return true;
    }

    private static bool GripAligned(PlaygroundFighter fighter)
    {
        Transform hand = fighter.motor.Animator.GetBoneTransform(HumanBodyBones.RightHand);
        if (hand == null || fighter.weapon == null || fighter.weapon.parent == null ||
            fighter.weapon.parent.name != "Playground sword socket")
            return false;

        Vector3 expectedSocketPosition = new Vector3(0.005f, 0.12f, 0.025f);
        Vector3 actualGripPosition = hand.InverseTransformPoint(fighter.weapon.TransformPoint(0f, -0.035f, 0f));
        return Vector3.Distance(fighter.weapon.parent.localPosition, expectedSocketPosition) < 0.001f &&
            Vector3.Distance(actualGripPosition, expectedSocketPosition) < 0.003f;
    }

    private static bool ClosedFingerPose(PlaygroundFighter fighter)
    {
        Transform hand = fighter.motor.Animator.GetBoneTransform(HumanBodyBones.RightHand);
        Transform finger = fighter.motor.Animator.GetBoneTransform(HumanBodyBones.RightMiddleIntermediate);
        if (hand == null || finger == null)
            return false;

        Vector3 expected = new Vector3(0.0042f, 0.1384f, 0.0343f);
        return Vector3.Distance(hand.InverseTransformPoint(finger.position), expected) < 0.01f;
    }

    private static IEnumerator WaitForGrounded(CharacterMotor first, CharacterMotor second)
    {
        float end = Time.time + 2f;
        while ((!first.IsGrounded || !second.IsGrounded) && Time.time < end)
            yield return null;
    }

    private static void PlaceEnemy(PlaygroundFighter enemy, Vector3 position, Quaternion rotation)
    {
        enemy.motor.Teleport(position, rotation);
        Physics.SyncTransforms();
    }

    private static AnimationClip LoadClip(string clipName)
    {
        string path = AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets(clipName + " t:Model")
            .First(guid => System.IO.Path.GetFileNameWithoutExtension(
                AssetDatabase.GUIDToAssetPath(guid)) == clipName));
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .First(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal));
    }

    private static IEnumerator PressView()
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F5));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
        yield return null;
    }

    private static IEnumerator Wait(float seconds)
    {
        float end = Time.time + seconds;
        while (Time.time < end)
            yield return null;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        checks++;
        Debug.Log("[Combat Playground Check] " + message);
    }
}
