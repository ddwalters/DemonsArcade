using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed class SaveStore
{
    private readonly string root;

    public SaveStore(string root) => this.root = root;

    public PlaythroughData CreatePlaythrough()
    {
        Directory.CreateDirectory(root);
        var names = Directory.GetFiles(root, "profile.json", SearchOption.AllDirectories)
            .Select(path => JsonUtility.FromJson<PlaythroughData>(File.ReadAllText(path)).characterName)
            .ToHashSet();

        int number = 0;
        while (names.Contains("user_" + number))
            number++;

        var profile = new PlaythroughData
        {
            id = Guid.NewGuid().ToString("N"),
            characterName = "user_" + number
        };

        string directory = Path.Combine(root, profile.id);
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "profile.json"), JsonUtility.ToJson(profile, true));
        return profile;
    }

    public List<SaveData> List()
    {
        var saves = new List<SaveData>();
        if (!Directory.Exists(root))
            return saves;

        foreach (string path in Directory.GetFiles(root, "*.save.json", SearchOption.AllDirectories))
        {
            try
            {
                saves.Add(ReadFile(path));
            }
            catch (Exception exception) when (exception is IOException || exception is ArgumentException || exception is InvalidDataException)
            {
                Debug.LogWarning("Cannot read save " + Path.GetFileName(path) + ": " + exception.Message);
            }
        }

        return saves.OrderBy(save => save.playthrough.characterName)
            .ThenByDescending(save => save.savedAtUtc).ToList();
    }

    public SaveData Read(string playthroughId, string saveId) => ReadFile(SavePath(playthroughId, saveId));

    public void Write(SaveData save)
    {
        string path = SavePath(save.playthrough.id, save.id);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonUtility.ToJson(save, true));
        ReadFile(temporary);

        if (File.Exists(path))
            File.Replace(temporary, path, path + ".bak");
        else
            File.Move(temporary, path);
    }

    public void Delete(string playthroughId, string saveId)
    {
        string path = SavePath(playthroughId, saveId);
        File.Delete(path);
        File.Delete(path + ".bak");
    }

    private string SavePath(string playthroughId, string saveId)
    {
        if (!Guid.TryParseExact(playthroughId, "N", out _) || !Guid.TryParseExact(saveId, "N", out _))
            throw new InvalidDataException("Invalid save identifier.");

        return Path.Combine(root, playthroughId, saveId + ".save.json");
    }

    private static SaveData ReadFile(string path)
    {
        var save = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
        if (save == null || save.version != SaveData.CurrentVersion || save.stats == null ||
            save.playthrough == null || !Guid.TryParseExact(save.id, "N", out _) ||
            !Guid.TryParseExact(save.playthrough.id, "N", out _) ||
            !DateTime.TryParse(save.savedAtUtc, out _) || save.inventory == null || save.world == null)
            throw new InvalidDataException("Unsupported or incomplete save file.");

        return save;
    }
}
