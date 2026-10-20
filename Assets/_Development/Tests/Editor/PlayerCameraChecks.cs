using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class PlayerCameraChecks
{
    private const string Running = "PlayerCameraChecks.Running";
    private static Stack<IEnumerator> routines;
    private static int previousFrame;
    private static int checks;
    private static double deadline;
    private static Keyboard keyboard;
    private static InputSettings.BackgroundBehavior previousBackgroundBehavior;
    private static InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
    private static bool hadViewPreference;
    private static int previousViewPreference;

    static PlayerCameraChecks() => EditorApplication.playModeStateChanged += Changed;

    [MenuItem("Tools/Validation/First and Third Person Cameras")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        SessionState.SetString("PlayerCameraChecks.PreviousScene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
        hadViewPreference = PlayerPrefs.HasKey(PlayerCameraRig.PreferenceKey);
        previousViewPreference = PlayerPrefs.GetInt(PlayerCameraRig.PreferenceKey);
        previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        SessionState.SetBool("PlayerCameraChecks.HadPreference", hadViewPreference);
        SessionState.SetInt("PlayerCameraChecks.Preference", previousViewPreference);
        SessionState.SetInt("PlayerCameraChecks.Background", (int)previousBackgroundBehavior);
        SessionState.SetInt("PlayerCameraChecks.EditorInput", (int)previousEditorInput);
        foreach (string key in new[] { PlayerCameraRig.FirstPersonFovKey, PlayerCameraRig.ThirdPersonFovKey })
        {
            SessionState.SetBool("PlayerCameraChecks.Has." + key, PlayerPrefs.HasKey(key));
            SessionState.SetFloat("PlayerCameraChecks.Value." + key, PlayerPrefs.GetFloat(key));
        }
        EditorSceneManager.OpenScene(PlayerAnimationSceneSetup.ScenePath);
        SessionState.SetBool(Running, true);
        EditorApplication.isPlaying = true;
    }

    public static void RunBatch()
    {
        PlayerCameraSetup.Run();
        PlayerLocomotionChecks.CheckAssets();
        PlayerFeatureChecks.RunAll();
        SaveFeatureChecks.RunAll();
        SessionState.SetBool("PlayerCameraChecks.Batch", true);
        Run();
    }

    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false))
            return;

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            hadViewPreference = SessionState.GetBool("PlayerCameraChecks.HadPreference", false);
            previousViewPreference = SessionState.GetInt("PlayerCameraChecks.Preference", 0);
            previousBackgroundBehavior = (InputSettings.BackgroundBehavior)SessionState.GetInt("PlayerCameraChecks.Background", 0);
            previousEditorInput = (InputSettings.EditorInputBehaviorInPlayMode)SessionState.GetInt("PlayerCameraChecks.EditorInput", 0);
            previousFrame = -1;
            checks = 0;
            deadline = EditorApplication.timeSinceStartup + 120;
            routines = new Stack<IEnumerator>();
            routines.Push(Exercise());
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Running, false);
            EditorApplication.update -= Tick;
            if (SessionState.GetBool("PlayerCameraChecks.Batch", false))
                EditorApplication.Exit(SessionState.GetBool("PlayerCameraChecks.Passed", false) ? 0 : 1);
            else
            {
                string previousScene = SessionState.GetString("PlayerCameraChecks.PreviousScene", "");
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
                throw new TimeoutException("Camera checks exceeded 120 seconds.");
            if (previousFrame == Time.frameCount)
                return;
            previousFrame = Time.frameCount;
            while (routines.Count > 0)
            {
                var current = routines.Peek();
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
            Debug.Log($"[Player Camera Checks] PASS: {checks} checks.");
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
        if (hadViewPreference)
            PlayerPrefs.SetInt(PlayerCameraRig.PreferenceKey, previousViewPreference);
        else
            PlayerPrefs.DeleteKey(PlayerCameraRig.PreferenceKey);
        foreach (string key in new[] { PlayerCameraRig.FirstPersonFovKey, PlayerCameraRig.ThirdPersonFovKey })
        {
            if (SessionState.GetBool("PlayerCameraChecks.Has." + key, false))
                PlayerPrefs.SetFloat(key, SessionState.GetFloat("PlayerCameraChecks.Value." + key, 0f));
            else
                PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
        SessionState.SetBool("PlayerCameraChecks.Passed", passed);
        EditorApplication.update -= Tick;
        Time.timeScale = 1f;
        EditorApplication.isPlaying = false;
    }

    private static IEnumerator Exercise()
    {
        yield return null;
        var demo = Object.FindAnyObjectByType<PlayerAnimationTest>();
        demo.LiveMovement();
        demo.SetPlayerView(true);
        var player = demo.player;
        var rig = player.GetComponent<PlayerCameraRig>();
        var mode = player.GetComponent<PlayerModeController>();
        rig.SetThirdPerson(false, true);
        player.RestoreLocation(new Vector3(10f, 1.04f, -10f), Quaternion.identity);
        yield return Wait(0.3f);
        var camera = rig.ViewCamera;
        int playerLayer = 1 << player.gameObject.layer;
        Require(camera.enabled && camera.targetTexture == null && Camera.allCameras.Count(value => value.enabled) == 1,
            "Player view uses exactly one display camera.");
        Require(Object.FindObjectsByType<AudioListener>().Count(value => value.enabled) == 1,
            "Player/overview switching leaves exactly one active AudioListener.");
        Require(!rig.IsThirdPerson && Vector3.Distance(camera.transform.position, rig.EyePosition) < 0.01f &&
            (camera.cullingMask & playerLayer) == 0, "First person starts at the eye pivot with the local body hidden.");
        var hands = player.transform.Find("ViewPivot/Hands");
        Require(hands != null && !hands.IsChildOf(camera.transform), "Legacy weapon rig is anchored to the player, not the moving camera.");
        Vector3 handsOffset = hands.localPosition;
        Screenshot(camera, "FirstPerson");

        rig.SetThirdPerson(true, true);
        yield return null;
        Require(Vector3.Distance(camera.transform.position, rig.EyePosition) > 3f &&
            (camera.cullingMask & playerLayer) != 0, "Third person pulls back and renders the body.");
        Require(camera == player.GetComponentInChildren<Camera>() && hands.localPosition == handsOffset,
            "Switching retains the gameplay camera reference and weapon reach.");
        rig.Look(new Vector2(45f, 20f));
        yield return null;
        Require(Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y, 45f)) < 0.1f && Mathf.Abs(rig.Pitch + 20f) < 0.1f,
            "Look turns player facing and pitches the independent view pivot.");
        player.RestoreLocation(new Vector3(10f, 1.04f, -10f), Quaternion.identity);
        yield return null;
        Require(rig.IsThirdPerson && rig.Pitch == 0f && Vector3.Distance(camera.transform.position, rig.EyePosition) > 3f,
            "Teleport resets pitch and camera placement while retaining the selected view.");
        Screenshot(camera, "ThirdPerson");

        var visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.transform.SetParent(hands, false);
        visual.GetComponent<Collider>().enabled = false;
        var weaponRenderer = visual.GetComponent<Renderer>();
        rig.RefreshFirstPersonRenderers();
        Require(weaponRenderer.forceRenderingOff, "A weapon equipped while already in third person is hidden on the legacy rig.");
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        keyboard = InputSystem.AddDevice<Keyboard>();
        PlayerCameraRig.SaveFieldOfView(false, 72f);
        PlayerCameraRig.SaveFieldOfView(true, 98f);
        rig.ApplyFieldOfView();
        Require(Mathf.Approximately(camera.fieldOfView, 98f), "Third person uses its independent saved FOV.");
        yield return PressView();
        yield return Wait(1f);
        Debug.Log($"[Camera Input] Third person {rig.IsThirdPerson}, distance {Vector3.Distance(camera.transform.position, rig.EyePosition):F4}, weapon hidden {weaponRenderer.forceRenderingOff}, action enabled {player.GetNewControls().BasicActionMap.ToggleView.enabled}, keyboard enabled {keyboard.enabled}.");
        Require(!rig.IsThirdPerson && Vector3.Distance(camera.transform.position, rig.EyePosition) < 0.02f &&
            !weaponRenderer.forceRenderingOff, "The real F5 input switches back and restores first-person weapon rendering.");
        Require(Mathf.Approximately(camera.fieldOfView, 72f), "F5 applies the first-person FOV independently.");
        foreach (bool thirdPerson in new[] { false, true })
        {
            rig.SetThirdPerson(thirdPerson, true);
            float fov = camera.fieldOfView;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.LeftShift));
            yield return Wait(0.7f);
            Require(player.GetComponent<CharacterMotor>().AnimationInput.y > 1.2f &&
                Mathf.Approximately(camera.fieldOfView, fov),
                "Actual sprint input preserves FOV in " + (thirdPerson ? "third" : "first") + " person.");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return Wait(0.3f);
            Require(Mathf.Approximately(camera.fieldOfView, fov), "Stopping sprint preserves the selected FOV.");
        }
        rig.SetThirdPerson(false, true);
        Require(player.GetNewControls().BasicActionMap.ToggleView.bindings.Any(binding => binding.path == "<Gamepad>/rightStickPress"),
            "View switching also has a gamepad binding.");
        Object.Destroy(visual);

        foreach (var blockedMode in new[] { PlayerMode.Inventory, PlayerMode.Paused, PlayerMode.Dead })
        {
            mode.SetMode(blockedMode);
            yield return PressView();
            Require(!rig.IsThirdPerson, "View input is blocked in " + blockedMode + ".");
            mode.ResetToGameplay();
        }
        rig.SetThirdPerson(true, true);
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.layer = LayerMask.NameToLayer("Obstacle");
        wall.transform.position = rig.EyePosition + Vector3.back * 1.4f;
        wall.transform.localScale = new Vector3(4f, 4f, 0.2f);
        yield return Wait(0.1f);
        float compressedDistance = Vector3.Distance(camera.transform.position, rig.EyePosition);
        Require(compressedDistance < 1.2f && compressedDistance > 0.7f,
            "A rear wall retracts the camera immediately.");
        Require(!Physics.CheckSphere(camera.transform.position, 0.19f, 1 << wall.layer, QueryTriggerInteraction.Ignore),
            "The camera probe remains outside the wall.");
        Screenshot(camera, "WallClearance");
        wall.transform.position = rig.EyePosition + Vector3.back * 0.6f;
        yield return Wait(0.1f);
        Require(Vector3.Distance(camera.transform.position, rig.EyePosition) < 0.7f &&
            (camera.cullingMask & playerLayer) == 0, "A tight obstruction hides the body before the camera enters it.");
        wall.GetComponent<Collider>().isTrigger = true;
        yield return Wait(0.1f);
        Require(Vector3.Distance(camera.transform.position, rig.EyePosition) < 3f,
            "Camera returns gradually after the obstruction clears.");
        yield return Wait(0.8f);
        Require(Vector3.Distance(camera.transform.position, rig.EyePosition) > 3f,
            "Trigger volumes do not obstruct the camera.");
        wall.GetComponent<Collider>().isTrigger = false;
        player.enabled = false;
        player.GetComponent<CharacterMotor>().enabled = false;
        wall.transform.position = rig.EyePosition;
        yield return Wait(0.1f);
        Require(Vector3.Distance(camera.transform.position, rig.EyePosition) < 0.01f,
            "An obstructed boom origin falls back to the eye position.");
        Object.Destroy(wall);
        player.GetComponent<CharacterMotor>().enabled = true;
        player.enabled = true;
        yield return Wait(0.8f);

        rig.Look(new Vector2(0f, 1000f));
        yield return Wait(0.1f);
        Require(rig.Pitch == -60f && camera.transform.position.y > 0.19f,
            "Third-person pitch clamps and ground obstruction keeps the camera above the floor.");
        rig.Look(new Vector2(0f, -2000f));
        Require(rig.Pitch == 70f, "Looking down respects the third-person pitch limit.");
        player.RestoreLocation(new Vector3(10f, 1.04f, -10f), Quaternion.identity);
        yield return null;

        var interaction = player.GetComponent<InteractionController>();
        var targetObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        targetObject.layer = LayerMask.NameToLayer("Interactable");
        targetObject.transform.position = rig.EyePosition + Vector3.forward * 2.5f;
        var target = targetObject.AddComponent<InteractableBase>();
        yield return null;
        Require(FindTarget(interaction) == target, "Third-person aiming can select an object within player reach.");
        rig.SetThirdPerson(false, true);
        yield return null;
        Require(FindTarget(interaction) == target, "First-person aiming selects the same nearby object.");
        rig.SetThirdPerson(true, true);
        targetObject.transform.position = rig.EyePosition + Vector3.forward * 6f;
        yield return null;
        Require(FindTarget(interaction) == null, "Third person does not extend interaction reach.");
        targetObject.transform.position = rig.EyePosition + Vector3.forward * 2.5f;
        var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.layer = LayerMask.NameToLayer("Obstacle");
        blocker.transform.position = rig.EyePosition + Vector3.forward * 1.2f;
        blocker.transform.localScale = new Vector3(3f, 3f, 0.2f);
        yield return null;
        Require(FindTarget(interaction) == null, "An obstacle between player and target blocks interaction.");
        Object.Destroy(blocker);
        Object.Destroy(targetObject);
        demo.SetPlayerView(false);
        Require(Object.FindObjectsByType<AudioListener>().Count(value => value.enabled) == 1,
            "Returning to the overview keeps a single AudioListener.");
    }

    private static InteractableBase FindTarget(InteractionController interaction)
    {
        // Test-created/moved colliders must reach the physics world before a query.
        Physics.SyncTransforms();
        return (InteractableBase)typeof(InteractionController).GetMethod("FindTarget", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(interaction, null);
    }

    private static IEnumerator PressView()
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F5));
        yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return null;
    }

    private static IEnumerator Wait(float seconds)
    {
        float until = Time.time + seconds;
        while (Time.time < until)
            yield return null;
    }

    private static void Screenshot(Camera camera, string name)
    {
        if (!SessionState.GetBool("PlayerCameraChecks.Batch", false))
            return;
        const string directory = "Logs/PlayerCameraReview";
        Directory.CreateDirectory(directory);
        var target = new RenderTexture(1280, 720, 24);
        var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
            texture.Apply();
            File.WriteAllBytes(directory + "/" + name + ".png", texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            target.Release();
            Object.Destroy(target);
            Object.Destroy(texture);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        checks++;
        Debug.Log("[Camera Check] " + message);
    }
}
