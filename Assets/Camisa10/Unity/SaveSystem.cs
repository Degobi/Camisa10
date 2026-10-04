using System;
using System.IO;
using Camisa10.Core;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>Salva a carreira em JSON na pasta de dados do aparelho.</summary>
    public static class SaveSystem
    {
        static string FilePath => Path.Combine(Application.persistentDataPath, "camisa10.json");

        public static void Save(GameState state)
        {
            try { File.WriteAllText(FilePath, JsonUtility.ToJson(state)); }
            catch (Exception e) { Debug.LogWarning("Falha ao salvar: " + e.Message); }
        }

        public static GameState Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                var s = JsonUtility.FromJson<GameState>(File.ReadAllText(FilePath));
                if (s == null || s.version != 1 || s.player == null || string.IsNullOrEmpty(s.player.name) || s.clubs.Count == 0) return null;
                return s;
            }
            catch (Exception e) { Debug.LogWarning("Save inválido: " + e.Message); return null; }
        }

        public static void Delete()
        {
            try { if (File.Exists(FilePath)) File.Delete(FilePath); } catch { }
        }
    }
}
