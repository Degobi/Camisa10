using System;
using System.IO;
using Camisa10.Core;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>
    /// Salva a carreira em JSON na pasta de dados do aparelho.
    /// A gravação é atômica (escreve num arquivo temporário e troca), e o save anterior vira cópia de segurança:
    /// se o celular desligar no meio, ou o arquivo corromper, a carreira volta do último save bom.
    /// </summary>
    public static class SaveSystem
    {
        static string Dir => Application.persistentDataPath;
        static string FilePath => Path.Combine(Dir, "camisa10.json");
        static string TempPath => FilePath + ".tmp";
        static string BackupPath => FilePath + ".bak";

        /// <summary>Data do último salvamento bem-sucedido (UTC), ou nulo.</summary>
        public static DateTime? LastSaved { get; private set; }

        public static void Save(GameState state)
        {
            try
            {
                state.savedUtc = DateTime.UtcNow.Ticks;
                state.rev++;
                File.WriteAllText(TempPath, JsonUtility.ToJson(state));
                if (File.Exists(FilePath))
                {
                    if (File.Exists(BackupPath)) File.Delete(BackupPath);
                    File.Move(FilePath, BackupPath);
                }
                File.Move(TempPath, FilePath);
                LastSaved = DateTime.UtcNow;
            }
            catch (Exception e) { Debug.LogWarning("Falha ao salvar: " + e.Message); }
        }

        public static GameState Load()
        {
            var s = Read(FilePath);
            if (s != null) return s;
            s = Read(BackupPath);
            if (s != null) Debug.LogWarning("[Camisa 10] Save principal ilegível; carreira recuperada da cópia de segurança.");
            return s;
        }

        static GameState Read(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                var s = JsonUtility.FromJson<GameState>(File.ReadAllText(path));
                if (s == null || s.player == null || string.IsNullOrEmpty(s.player.name) || s.clubs == null || s.clubs.Count == 0) return null;
                // saves de versões futuras do jogo não são abertos (evita perder dados que esta versão não conhece)
                if (s.version < 1 || s.version > GameState.CurrentVersion) return null;
                LastSaved = new DateTime(Math.Max(0, s.savedUtc), DateTimeKind.Utc);
                return s;
            }
            catch (Exception e) { Debug.LogWarning("Save inválido (" + Path.GetFileName(path) + "): " + e.Message); return null; }
        }

        public static void Delete()
        {
            foreach (var p in new[] { FilePath, TempPath, BackupPath })
                try { if (File.Exists(p)) File.Delete(p); } catch { }
            LastSaved = null;
        }
    }
}
