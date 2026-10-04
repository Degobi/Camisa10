using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Camisa10.Core;
using Camisa10.UI.Art;
using UnityEngine;

namespace Camisa10.UI
{
    public static partial class Procedural
    {
        static Sprite ToSprite(Raster r)
        {
            var tex = NewTex(r.W, r.H);
            tex.LoadRawTextureData(r.ToRgba32BottomUp());
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, r.W, r.H), new Vector2(.5f, .5f), 100);
        }

        /// <summary>Nome do arquivo do escudo oficial: "São Paulo" vira "sao-paulo".</summary>
        public static string CrestFile(string club)
        {
            var sb = new StringBuilder();
            foreach (var ch in (club ?? "").Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
                else if (sb.Length > 0 && sb[sb.Length - 1] != '-') sb.Append('-');
            }
            return sb.ToString().Trim('-');
        }

        static readonly Dictionary<string, bool> officialCrest = new Dictionary<string, bool>();

        /// <summary>Verdadeiro quando há imagem oficial em Resources/Escudos para o clube.</summary>
        public static bool HasOfficialCrest(Club c) { CrestSprite(c); return officialCrest.TryGetValue(c.name ?? "", out var b) && b; }

        /// <summary>
        /// Escudo do clube. Usa a imagem de Assets/Camisa10/Resources/Escudos/&lt;nome&gt;.png quando existir;
        /// senão desenha uma versão com as cores e símbolos do clube.
        /// </summary>
        public static Sprite CrestSprite(Club c)
        {
            string key = "crest:" + c.name + c.c1 + c.c2;
            if (cache.TryGetValue(key, out var s)) return s;
            var tex = Resources.Load<Texture2D>("Escudos/" + CrestFile(c.name));
            if (tex != null)
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 100);
            else
                s = ToSprite(CrestArt.Render(CrestArt.For(c.name, c.c1, c.c2), 200, 240));
            officialCrest[c.name ?? ""] = tex != null;
            cache[key] = s;
            return s;
        }

        public static Sprite TrophySprite(bool gold)
        {
            string key = gold ? "trophy:gold" : "trophy:silver";
            if (cache.TryGetValue(key, out var s)) return s;
            s = ToSprite(TrophyArt.Render(gold, 160));
            cache[key] = s;
            return s;
        }

        static Sprite bootSprite;

        public static Sprite BootSprite(BootStyle b)
        {
            if (bootSprite != null) { Object.Destroy(bootSprite.texture); Object.Destroy(bootSprite); }
            bootSprite = ToSprite(BootArt.Render(b.c1, b.c2, b.sole, 750));
            return bootSprite;
        }
    }
}
