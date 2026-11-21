using System;
using System.IO;
using UnityEngine;

// Единственное место, которое знает, КАК и КУДА сохранять игру.
// Игровая логика (GameBootstrap) только собирает SaveData и отдаёт сюда.
public static class SaveManager
{
    // Увеличиваем, когда меняется структура SaveData.
    public const int CurrentVersion = 1;

    private const string FileName = "dormitory_save.json";

    // Путь к файлу сохранения.
    // Windows: C:\Users\<пользователь>\AppData\LocalLow\<компания>\<игра>\
    public static string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, FileName); }
    }

    public static bool HasSave()
    {
        try
        {
            return File.Exists(SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Не удалось проверить наличие сохранения: {e.Message}");
            return false;
        }
    }

    public static bool Save(SaveData data)
    {
        if (data == null)
        {
            Debug.LogError("[SaveManager] SaveData == null, сохранять нечего.");
            return false;
        }

        try
        {
            data.version = CurrentVersion;
            data.savedAtUtc = DateTime.UtcNow.ToString("o");

            string json = JsonUtility.ToJson(data, true);

            // Сначала пишем во временный файл: если игра вылетит во время записи,
            // старое сохранение останется целым.
            string tempPath = SavePath + ".tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(SavePath)) File.Delete(SavePath);
            File.Move(tempPath, SavePath);

            Debug.Log($"[SaveManager] Игра сохранена: {SavePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Ошибка сохранения: {e}");
            return false;
        }
    }

    public static bool TryLoad(out SaveData data)
    {
        data = null;

        if (!HasSave())
        {
            Debug.Log("[SaveManager] Файл сохранения не найден.");
            return false;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            data = JsonUtility.FromJson<SaveData>(json);

            if (data == null)
            {
                Debug.LogError("[SaveManager] Файл сохранения пустой или повреждён.");
                return false;
            }

            if (data.version > CurrentVersion)
            {
                Debug.LogWarning($"[SaveManager] Сохранение версии {data.version} новее игры ({CurrentVersion}). " +
                                 "Возможны ошибки — лучше начать новую игру.");
            }

            if (data.resources == null) data.resources = new ResourceData();
            if (data.rooms == null) data.rooms = new System.Collections.Generic.List<RoomData>();
            if (data.students == null) data.students = new System.Collections.Generic.List<StudentData>();
            if (data.delayed == null) data.delayed = new System.Collections.Generic.List<DelayedEventData>();
            if (data.queue == null) data.queue = new System.Collections.Generic.List<QueuedEventData>();

            Debug.Log($"[SaveManager] Сохранение загружено: день {data.day}, версия {data.version}.");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Ошибка загрузки сохранения: {e}");
            data = null;
            return false;
        }
    }

    public static void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            Debug.Log("[SaveManager] Сохранение удалено.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Не удалось удалить сохранение: {e}");
        }
    }
}
