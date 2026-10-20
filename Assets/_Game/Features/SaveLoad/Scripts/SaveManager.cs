using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public const string TownScene = "Assets/_Game/Scenes/Town/_Town.unity";
    public static SaveManager Instance { get; private set; }
    public PlaythroughData ActivePlaythrough { get; private set; }
    public bool IsLoading { get; private set; }
    public string LastError { get; private set; }
    public event Action SavesChanged;

    private SaveStore store;
    private ItemCatalog catalog;
    private SaveData pendingSave;
    private double playSeconds;
    private PlayerModeController sessionMode;
    private readonly Dictionary<UnityEngine.Object, string> saveBlocks = new();
    private readonly Dictionary<string, SavedWorldObject> world = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
            new GameObject("SaveManager").AddComponent<SaveManager>();
    }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        store = new SaveStore(Path.Combine(Application.persistentDataPath, "Saves"));
        catalog = Resources.Load<ItemCatalog>("ItemCatalog");
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!IsLoading && ActivePlaythrough != null && sessionMode != null && sessionMode.Mode != PlayerMode.Dead && Time.timeScale > 0f)
            playSeconds += Time.unscaledDeltaTime;
    }

    // Owners block independently. Destroying an owner also releases its restriction.
    public void BlockSaving(UnityEngine.Object owner, string reason)
    {
        if (owner == null || string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A save restriction needs an owner and a reason.");

        saveBlocks[owner] = reason;
    }

    public void AllowSaving(UnityEngine.Object owner) => saveBlocks.Remove(owner);
    public bool CanSave => SaveBlockedReason == null;

    public string SaveBlockedReason
    {
        get
        {
            if (IsLoading || FindAnyObjectByType<LoadLevel>()?.IsLoading == true)
                return "Saving is unavailable while loading.";

            if (!IsSaveScene(SceneManager.GetActiveScene().path))
                return "Saving is only available in Town.";

            var player = FindAnyObjectByType<PlayerController>();
            if (player == null || ActivePlaythrough == null)
                return "No active playthrough.";

            if (PlayerModeController.ForPlayer(player).Mode == PlayerMode.Dead)
                return "Saving is unavailable after death.";

            if (FindAnyObjectByType<Inventory>()?.selectedItem != null)
                return "Finish moving the held item before saving.";

            if (player.GetComponent<PlayerStatsManager>().HasTemporaryEffects)
                return "Wait for temporary stat effects to finish before saving.";

            foreach (var block in saveBlocks)
                if (block.Key != null)
                    return block.Value;

            return null;
        }
    }

    public static bool IsSaveScene(string scenePath) => scenePath == TownScene;

    public List<SaveData> ListSaves() => store.List();

    public bool Save()
    {
        LastError = SaveBlockedReason;
        if (LastError != null)
            return false;

        try
        {
            CaptureWorld();
            var player = FindAnyObjectByType<PlayerController>();
            var inventory = FindAnyObjectByType<InventoryManager>();
            var save = new SaveData
            {
                id = Guid.NewGuid().ToString("N"),
                playthrough = ActivePlaythrough,
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                playSeconds = playSeconds,
                scenePath = TownScene,
                position = player.transform.position,
                rotation = player.transform.rotation,
                stats = player.GetComponent<PlayerStatsManager>().GetPlayerStats(),
                gold = player.goldAmount,
                world = world.Values.ToList()
            };

            for (int slot = 0; slot <= 7; slot++)
                save.inventory.Add(new SavedContainer { slot = slot, items = catalog.Capture(inventory.GetItems(slot).list) });

            store.Write(save);
            SavesChanged?.Invoke();
            return true;
        }
        catch (Exception exception)
        {
            LastError = "Could not save: " + exception.Message;
            Debug.LogError(LastError);
            return false;
        }
    }

    public bool Load(SaveData entry)
    {
        if (IsLoading)
            return false;

        try
        {
            // Read and resolve everything before replacing the current session.
            var save = store.Read(entry.playthrough.id, entry.id);
            if (save.scenePath != TownScene || save.stats.currentHealth <= 0 || save.inventory.Count != 8 ||
                save.inventory.Select(container => container.slot).Distinct().Count() != 8 ||
                save.inventory.Any(container => container.slot < 0 || container.slot > 7))
                throw new InvalidDataException("This save has an invalid player state.");

            foreach (var container in save.inventory)
                catalog.ValidateContainer(container, save.stats.inventoryType);

            foreach (var state in save.world)
                catalog.Restore(state.items);

            pendingSave = save;
            ActivePlaythrough = save.playthrough;
            LastError = null;
            StartCoroutine(ReplaceSession(1));
            return true;
        }
        catch (Exception exception)
        {
            LastError = "Could not load: " + exception.Message;
            Debug.LogError(LastError);
            return false;
        }
    }

    public void NewGame()
    {
        if (IsLoading)
            return;

        try
        {
            ActivePlaythrough = store.CreatePlaythrough();
            pendingSave = null;
            playSeconds = 0;
            world.Clear();
            StartCoroutine(ReplaceSession(1));
        }
        catch (Exception exception)
        {
            LastError = "Could not create a playthrough: " + exception.Message;
            Debug.LogError(LastError);
        }
    }

    public void MainMenu()
    {
        if (!IsLoading)
            StartCoroutine(ReplaceSession(0));
    }

    public bool Respawn()
    {
        var latest = ListSaves().Where(save => save.playthrough.id == ActivePlaythrough?.id)
            .OrderByDescending(save => save.savedAtUtc).FirstOrDefault();

        if (latest == null)
        {
            MainMenu();
            return true;
        }

        return Load(latest);
    }

    public void DeleteConfirmed(SaveData save)
    {
        store.Delete(save.playthrough.id, save.id);
        SavesChanged?.Invoke();
    }

    public void RecordCollected(WorldSaveObject target)
    {
        if (target != null && SceneManager.GetActiveScene().path == TownScene)
            world[target.SaveId] = new SavedWorldObject { id = target.SaveId, collected = true };
    }

    public void CaptureWorld()
    {
        if (SceneManager.GetActiveScene().path != TownScene)
            return;

        var identifiers = new HashSet<string>();
        foreach (var target in FindObjectsByType<WorldSaveObject>())
        {
            if (string.IsNullOrEmpty(target.SaveId) || !identifiers.Add(target.SaveId))
                throw new InvalidOperationException("Town contains a missing or duplicate world save identifier.");

            world[target.SaveId] = target.Capture(catalog);
        }
    }

    private IEnumerator ReplaceSession(int scene)
    {
        IsLoading = true;
        saveBlocks.Clear();
        foreach (var player in FindObjectsByType<PlayerController>())
        {
            player.gameObject.SetActive(false);
            Destroy(player.gameObject);
        }

        foreach (var inventory in FindObjectsByType<InventoryManager>())
            Destroy(inventory.gameObject);

        foreach (var loader in FindObjectsByType<LoadLevel>())
            Destroy(loader.gameObject);

        Time.timeScale = 1f;
        yield return null;
        yield return SceneManager.LoadSceneAsync(scene);
    }

    private void SceneLoaded(Scene scene, LoadSceneMode mode) => StartCoroutine(InitializeScene(scene));

    private IEnumerator InitializeScene(Scene scene)
    {
        // Container Start methods and HUD bindings must run before restoration.
        yield return null;
        sessionMode = FindAnyObjectByType<PlayerModeController>();
        if (scene.buildIndex == 0)
        {
            ActivePlaythrough = null;
            world.Clear();
            IsLoading = false;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            yield break;
        }

        if (scene.path != TownScene)
        {
            IsLoading = false;
            yield break;
        }

        var player = FindAnyObjectByType<PlayerController>();
        var inventory = FindAnyObjectByType<InventoryManager>();
        if (pendingSave != null)
        {
            var save = pendingSave;
            pendingSave = null;
            playSeconds = save.playSeconds;
            world.Clear();
            foreach (var state in save.world)
                world.Add(state.id, state);

            player.GetComponent<PlayerStatsManager>().RestoreStats(save.stats);
            player.goldAmount = save.gold;
            player.RestoreLocation(save.position, save.rotation);
            inventory.SetPlayerInventoryType(save.stats.inventoryType);
            foreach (var container in save.inventory)
                inventory.ReplaceItems(container.slot, catalog.Restore(container.items));

            inventory.RebuildEquipment();
        }

        foreach (var target in FindObjectsByType<WorldSaveObject>())
            if (world.TryGetValue(target.SaveId, out var state))
                target.Restore(state, catalog);

        bool initialSave = ActivePlaythrough == null || !ListSaves().Any(save => save.playthrough.id == ActivePlaythrough.id);
        if (ActivePlaythrough == null)
            ActivePlaythrough = store.CreatePlaythrough();

        IsLoading = false;
        PlayerModeController.ForPlayer(player).ResetToGameplay();
        if (initialSave)
            Save();
    }
}
