using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void SavePlayer(PlayerController player)
    {
        PlayerSaveData data = player.GetSaveData();

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);

        Debug.Log("Game saved to: " + SavePath);
    }

    public static PlayerSaveData LoadPlayer()
    {
        if (!File.Exists(SavePath))
        {
            Debug.LogWarning("Save file not found: " + SavePath);
            return null;
        }

        string json = File.ReadAllText(SavePath);
        PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);

        Debug.Log("Game loaded from: " + SavePath);
        return data;
    }
}
