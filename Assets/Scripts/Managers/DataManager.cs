using System;
using UnityEngine;
using System.IO;
using System.Text;

public class DataManager : MonoBehaviour
{
    public static DataManager instance;
    public PlayerData currentPlayer;
    private string savePath;

    void Awake()
    {
        if (instance == null)
        {
                instance = this;
                DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        savePath = Path.Combine(Application.persistentDataPath, "save.dat");
        LoadGame();
    }


    public void SaveGame()
    {
        string json = JsonUtility.ToJson(currentPlayer);
        byte[] bytes=Encoding.UTF8.GetBytes(json);
        string code = Convert.ToBase64String(bytes);
        
        File.WriteAllText(savePath, code);
    }

    public void LoadGame()
    {
        if (File.Exists(savePath))
        {
            string code = File.ReadAllText(savePath);
            byte[] bytes = Convert.FromBase64String(code);
            string json = Encoding.UTF8.GetString(bytes);
            currentPlayer = JsonUtility.FromJson<PlayerData>(json);
        }
        else
        {
            currentPlayer = new PlayerData();
            SaveGame();
        }
    }
  
}
