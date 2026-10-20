using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Everything here is confined to CombatPlayground, including UI and round resets.
public class PlaygroundArena : MonoBehaviour
{
    public PlaygroundFighter player;
    public PlaygroundFighter enemy;
    public Vector3 playerSpawn = new Vector3(0f, 1.04f, -2f);
    public Vector3 enemySpawn = new Vector3(0f, 1.04f, 3f);
    public Quaternion playerRotation = Quaternion.identity;
    public Quaternion enemyRotation = Quaternion.Euler(0f, 180f, 0f);
    public LayerMask environmentMask = ~((1 << 9) | (1 << 10) | (1 << 5) | (1 << 2));

    private TMP_Text playerText;
    private TMP_Text enemyText;
    private TMP_Text message;
    private Image playerBar;
    private Image enemyBar;
    private string lastEvent = "Enemy approaches. Time your swing or back out of its reach.";
    private bool hadViewPreference;
    private int viewPreference;
    private Canvas hud;

    public bool RoundOver => player.IsDead || enemy.IsDead;
    public Canvas HUD => hud;

    private void Start()
    {
        // This prefab normally persists between game scenes. Keep its combat overrides here.
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(player.gameObject, gameObject.scene);
        hadViewPreference = PlayerPrefs.HasKey(PlayerCameraRig.PreferenceKey);
        viewPreference = PlayerPrefs.GetInt(PlayerCameraRig.PreferenceKey);
        player.GetComponent<PlayerCameraRig>().SetThirdPerson(true, true);
        foreach (var fighter in new[] { player, enemy })
        {
            if (fighter.weapon == null)
                continue;
            foreach (var child in fighter.weapon.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = 10;
        }
        BuildHUD();
        ResetRound();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            ResetRound();
        var mode = player.GetComponent<PlayerModeController>();
        // Inventory and saves are deliberately outside this isolated combat test.
        if (mode.Mode == PlayerMode.Inventory || mode.Mode == PlayerMode.Looting)
            mode.ResetToGameplay();

        playerBar.fillAmount = player.Health / player.MaxHealth;
        enemyBar.fillAmount = enemy.Health / enemy.MaxHealth;
        playerBar.rectTransform.localScale = new Vector3(playerBar.fillAmount, 1f, 1f);
        enemyBar.rectTransform.localScale = new Vector3(enemyBar.fillAmount, 1f, 1f);
        playerText.text = $"YOU   {player.Health:0}/{player.MaxHealth:0} HP   |   {player.Phase}   |   STAMINA {player.GetComponent<PlayerStatsManager>().GetStamina():0}";
        enemyText.text = $"SPARRING ENEMY   {enemy.Health:0}/{enemy.MaxHealth:0} HP   |   {enemy.GetComponent<PlaygroundEnemy>().State}";
        message.text = mode.Mode == PlayerMode.Paused ? "PAUSED — Escape to resume" : lastEvent;
    }

    public void ResetRound()
    {
        Time.timeScale = 1f;
        player.ResetFighter(playerSpawn, playerRotation);
        enemy.ResetFighter(enemySpawn, enemyRotation);
        enemy.GetComponent<PlaygroundEnemy>().ResetBrain();
        player.GetComponent<PlayerCameraRig>().ResetView();
        lastEvent = "Enemy approaches. Time your swing or back out of its reach.";
        Physics.SyncTransforms();
    }

    public void ReportHit(PlaygroundFighter attacker, PlaygroundFighter victim, float amount)
    {
        string who = attacker.isPlayer ? "You hit the enemy" : "Enemy hit you";
        lastEvent = RoundOver ? player.IsDead ? "YOU DIED — R to reset the fight" :
            "ENEMY DEFEATED — R for another round" : $"{who} for {amount:0}.";
    }

    public void ReportDeath(PlaygroundFighter victim)
    {
        lastEvent = victim.isPlayer ? "YOU DIED — R to reset the fight" : "ENEMY DEFEATED — R for another round";
    }

    private void BuildHUD()
    {
        var root = new GameObject("Combat playground HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        hud = root.GetComponent<Canvas>();
        hud.renderMode = RenderMode.ScreenSpaceCamera;
        hud.worldCamera = player.GetComponent<PlayerCameraRig>().ViewCamera;
        hud.planeDistance = 0.5f;
        hud.sortingOrder = 2000;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        Rect(root.transform, "Top backdrop", Vector2.zero, new Vector2(1280f, 162f))
            .gameObject.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 0.82f);
        Rect(root.transform, "Bottom backdrop", new Vector2(0f, -646f), new Vector2(1280f, 74f))
            .gameObject.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.05f, 0.82f);
        Text(root.transform, "COMBAT PLAYGROUND", new Vector2(20f, -16f), new Vector2(1240f, 36f), 27f);
        Text(root.transform, "WASD move  |  Mouse aim  |  Left click / gamepad attack  |  Shift run  |  Space jump  |  F5 view  |  R reset  |  Esc pause",
            new Vector2(20f, -54f), new Vector2(1240f, 34f), 18f);
        playerText = Text(root.transform, "", new Vector2(20f, -98f), new Vector2(590f, 30f), 20f);
        enemyText = Text(root.transform, "", new Vector2(670f, -98f), new Vector2(590f, 30f), 20f);
        playerBar = Bar(root.transform, new Vector2(20f, -133f), new Color(0.24f, 0.8f, 0.4f));
        enemyBar = Bar(root.transform, new Vector2(670f, -133f), new Color(0.9f, 0.25f, 0.2f));
        message = Text(root.transform, "", new Vector2(20f, -658f), new Vector2(1240f, 42f), 24f);
    }

    private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        result.SetParent(parent, false);
        result.anchorMin = result.anchorMax = result.pivot = new Vector2(0f, 1f);
        result.anchoredPosition = position;
        result.sizeDelta = size;
        return result;
    }

    private static TMP_Text Text(Transform parent, string text, Vector2 position, Vector2 size, float fontSize)
    {
        var label = Rect(parent, "Label", position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.raycastTarget = false;
        label.outlineWidth = 0.2f;
        return label;
    }

    private static Image Bar(Transform parent, Vector2 position, Color color)
    {
        var background = Rect(parent, "Health bar", position, new Vector2(590f, 12f));
        background.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
        var fill = Rect(background, "Health", Vector2.zero, background.sizeDelta).gameObject.AddComponent<Image>();
        fill.color = color;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.raycastTarget = false;
        return fill;
    }

    private void OnDestroy()
    {
        if (hud != null)
            Destroy(hud.gameObject);
        if (hadViewPreference)
            PlayerPrefs.SetInt(PlayerCameraRig.PreferenceKey, viewPreference);
        else
            PlayerPrefs.DeleteKey(PlayerCameraRig.PreferenceKey);
    }
}
