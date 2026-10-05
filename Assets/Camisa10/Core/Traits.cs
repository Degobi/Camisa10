using System;
using System.Collections.Generic;
using System.Linq;

namespace Camisa10.Core
{
    /// <summary>Especialidade do jogador: liberada por atributo ou conquista, vale para sempre e tem efeito de verdade no lance.</summary>
    public class TraitDef
    {
        public string Id, Name, Icon, Effect, How;
        public Func<Game, bool> Unlocks;
    }

    /// <summary>
    /// Especialidades (Finalizador, Driblador, Cobrador de falta...): pequenos ganhos reais no lance 3D (precisão, curva,
    /// janela do cabeceio, fôlego) e na simulação da partida. Aparecem como selos na carta do jogador.
    /// </summary>
    public partial class Game
    {
        public static readonly TraitDef[] Traits =
        {
            new TraitDef { Id = "finalizador", Name = "Finalizador", Icon = "FIN", Effect = "Chutes mais precisos no lance.", How = "Finalização 75 ou 60 gols na carreira.",
                Unlocks = g => g.S.player.Get(Attr.Fin) >= 75 || g.CareerGoals >= 60 },
            new TraitDef { Id = "driblador", Name = "Driblador", Icon = "DRI", Effect = "Dribles e firulas dão certo com mais frequência.", How = "Drible 75.",
                Unlocks = g => g.S.player.Get(Attr.Dri) >= 75 },
            new TraitDef { Id = "maestro", Name = "Maestro", Icon = "PAS", Effect = "Passes e cruzamentos saem na medida.", How = "Passe 76 ou 40 assistências na carreira.",
                Unlocks = g => g.S.player.Get(Attr.Pas) >= 76 || g.CareerAssists >= 40 },
            new TraitDef { Id = "velocista", Name = "Velocista", Icon = "VEL", Effect = "Fôlego de arrancada dura bem mais.", How = "Velocidade 78.",
                Unlocks = g => g.S.player.Get(Attr.Vel) >= 78 },
            new TraitDef { Id = "cabeceador", Name = "Cabeceador", Icon = "CAB", Effect = "Janela maior para cabecear e cabeçada mais precisa.", How = "Físico 74.",
                Unlocks = g => g.S.player.Get(Attr.Fis) >= 74 },
            new TraitDef { Id = "muralha", Name = "Muralha", Icon = "DEF", Effect = "Ganha mais botes e disputas pelo alto.", How = "Defesa 74.",
                Unlocks = g => g.S.player.Get(Attr.Def) >= 74 },
            new TraitDef { Id = "cobrador", Name = "Cobrador de falta", Icon = "FAL", Effect = "Mais curva e precisão nas faltas.", How = "3 gols de falta, ou finalização e passe 70.",
                Unlocks = g => g.S.fkGoals >= 3 || (g.S.player.Get(Attr.Fin) >= 70 && g.S.player.Get(Attr.Pas) >= 70) },
            new TraitDef { Id = "frieza", Name = "Frieza", Icon = "PEN", Effect = "Pênaltis mais precisos.", How = "5 gols de pênalti na carreira.",
                Unlocks = g => g.S.penGoals >= 5 },
            new TraitDef { Id = "lider", Name = "Líder", Icon = "CAP", Effect = "O time rende mais com você em campo.", How = "Seja capitão ou tenha o elenco na mão (82).",
                Unlocks = g => g.S.captain || g.S.player.squad >= 82 },
            new TraitDef { Id = "idolo", Name = "Ídolo", Icon = "TOR", Effect = "A torcida canta o seu nome e você ganha mais fama.", How = "Torcida 85.",
                Unlocks = g => g.S.player.fans >= 85 },
        };

        public static TraitDef Trait(string id) => Array.Find(Traits, t => t.Id == id);
        public bool HasTrait(string id) => S.traits != null && S.traits.Contains(id);
        public int CareerAssists => S.career.Sum(c => c.assists) + (SeasonRecorded ? 0 : S.season.stats.assists);

        void EnsureTraits() { if (S.traits == null) S.traits = new List<string>(); }

        /// <summary>Confere novas especialidades (fim de cada rodada e de cada temporada).</summary>
        void CheckTraits()
        {
            foreach (var t in Traits)
            {
                if (S.traits.Contains(t.Id) || !t.Unlocks(this)) continue;
                S.traits.Add(t.Id);
                S.player.moral += 4;
                AddNews($"Nova especialidade: {t.Name}.");
                Mail("tecnico", CoachName, $"Especialidade: {t.Name}", $"A comissão técnica reparou: você virou referência. Nova especialidade liberada: {t.Name}. {t.Effect}");
            }
        }

        /// <summary>Bônus da especialidade na chance de sucesso dos lances simulados (os do 3D usam os efeitos no próprio lance).</summary>
        public double TraitBonus(string momentType, string kind)
        {
            double b = 0;
            if (kind == "goal")
            {
                if (HasTrait("finalizador")) b += .04;
                if (momentType == "falta" && HasTrait("cobrador")) b += .05;
                if (momentType == "penalti" && HasTrait("frieza")) b += .05;
                if (momentType == "cabeceio" && HasTrait("cabeceador")) b += .05;
            }
            if (kind == "assist" && HasTrait("maestro")) b += .04;
            if (kind == "follow" && HasTrait("driblador")) b += .04;
            if ((kind == "tackle" || kind == "contain") && HasTrait("muralha")) b += .04;
            return b;
        }
    }
}
