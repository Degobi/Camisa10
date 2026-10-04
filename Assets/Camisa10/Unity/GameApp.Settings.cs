using System;
using Camisa10.Core;
using UnityEngine;

namespace Camisa10.UI
{
    /// <summary>Aba Ajustes: qualidade gráfica, som, vibração e dados do perfil.</summary>
    public partial class GameApp
    {
        void BuildSettings()
        {
            var cols = UIKit.Cols(content);

            var left = UIKit.Stack(cols, 1.2f);
            var gfx = UIKit.Tile(left, "Gráficos", Theme.Cyan);
            var cur = GameSettings.Quality;
            UIKit.Muted(gfx, cur == GameSettings.Level.Auto
                ? $"No automático, este aparelho usa a qualidade {GameSettings.Label(GameSettings.Effective).ToLowerInvariant()}."
                : "Qualidade mais alta deixa o lance 3D mais bonito, mas gasta mais bateria.", 24);
            var row = UIKit.Row(gfx, 14);
            foreach (GameSettings.Level lv in Enum.GetValues(typeof(GameSettings.Level)))
            {
                var level = lv;
                bool on = cur == lv;
                var b = UIKit.Btn(row, GameSettings.Label(lv), on ? Theme.Turf : Theme.Chip, on ? Theme.TurfInk : Theme.Ink,
                    () => { GameSettings.Quality = level; Render(); }, 76, 26);
                UIKit.LE(b, flexW: 1, prefW: 0, minW: 0);
            }
            UIKit.Muted(gfx, "Leve: sem sombras e resolução reduzida no lance. Equilibrada: sombras e antisserrilhado. Máxima: tudo ligado.", 22);

            var snd = UIKit.Tile(left, "Som e vibração", Theme.Purple);
            Toggle(snd, "Som (torcida, chute, apito)", GameSettings.Sound, v => GameSettings.Sound = v);
            Toggle(snd, "Vibrar no gol e na trave", GameSettings.Vibration, v => GameSettings.Vibration = v);

            var right = UIKit.Stack(cols, 1f);
            var prof = UIKit.Tile(right, "Perfil", Theme.Gold);
            var s = game.S;
            UIKit.KV(prof, "Jogador", s.player.name);
            UIKit.KV(prof, "Código do perfil", string.IsNullOrEmpty(s.profileId) ? "-" : s.profileId.Substring(0, 8).ToUpperInvariant());
            var saved = SaveSystem.LastSaved;
            UIKit.KV(prof, "Último salvamento", saved.HasValue && saved.Value.Year > 2000 ? saved.Value.ToLocalTime().ToString("dd/MM HH:mm") : "-");
            UIKit.KV(prof, "Versão do jogo", Version);
            UIKit.Muted(prof, "A carreira é salva sozinha a cada ação, com cópia de segurança. O modo online vai usar este perfil.", 22);

            var danger = UIKit.Tile(right, "Carreira", Theme.Red);
            UIKit.Muted(danger, "Apaga tudo e começa um jogador novo.", 22);
            UIKit.Ghost(danger, "Recomeçar do zero", () => ShowModal("Recomeçar do zero?",
                "Sua carreira atual será apagada e não poderá ser recuperada.",
                ("Apagar e recomeçar", (Action)ResetCareer, true), ("Cancelar", (Action)CloseModal, false)));
        }

        void Toggle(Transform parent, string label, bool value, Action<bool> set)
        {
            var r = UIKit.Row(parent, 16);
            var t = UIKit.Txt(r, label, 28, Theme.Ink);
            UIKit.LE(t, flexW: 1, minW: 0);
            var b = UIKit.Btn(r, value ? "Ligado" : "Desligado", value ? Theme.Turf : Theme.Chip, value ? Theme.TurfInk : Theme.Muted,
                () => { set(!value); Render(); }, 64, 24);
            UIKit.LE(b, 64, 64, prefW: 210, minW: 180);
        }
    }
}
