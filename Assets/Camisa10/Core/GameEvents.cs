using System;
using System.Collections.Generic;

namespace Camisa10.Core
{
    public class EventOption { public string Label; public Func<Game, string> Apply; }
    public class GameEvent { public string Title, Text; public EventOption[] Options; }

    /// <summary>Decisões fora de campo que aparecem depois do treino.</summary>
    public static class GameEvents
    {
        static EventOption Opt(string label, Func<Game, string> apply) => new EventOption { Label = label, Apply = apply };
        static GameEvent Ev(string title, string text, params EventOption[] o) => new GameEvent { Title = title, Text = text, Options = o };

        static readonly (Func<Game, bool> cond, double weight, Func<Game, GameEvent> build)[] All =
        {
            (g => true, 1, g => Ev("Festa na véspera do jogo", "Um amigo famoso te chama para uma festa hoje à noite. O jogo é amanhã.",
                Opt("Ir à festa", x => { var p = x.S.player; p.moral += 8; p.energy -= 20;
                    if (Rng.Chance(.35)) { p.coach -= 12; p.fame += 2; return "Fotos suas na festa vazaram nas redes. O técnico não gostou nada."; }
                    return "A noite foi boa e ninguém ficou sabendo."; }),
                Opt("Ficar em casa", x => { x.S.player.coach += 3; x.S.player.energy += 5; return "Você descansou. O técnico reparou no seu profissionalismo."; }))),

            (g => g.S.season.lastRole == Role.Reserva, 1.3, g => Ev("Entrevista após o treino", "Um repórter pergunta se você está insatisfeito por começar no banco.",
                Opt("Criticar o técnico", x => { var p = x.S.player; p.coach -= 15; p.fame += 3; p.moral += 2; return "A declaração virou manchete. O clima com o técnico pesou."; }),
                Opt("Responder com diplomacia", x => { x.S.player.coach += 5; return "\"Vou continuar trabalhando.\" O técnico gostou da resposta."; }))),

            (g => true, 1, g => Ev("Visita ao hospital infantil", "O clube organiza uma ação social no seu dia de folga.",
                Opt("Participar", x => { var p = x.S.player; p.fame += 2; p.moral += 5; p.energy -= 5; return "As crianças adoraram. Você saiu de lá renovado."; }),
                Opt("Ficar de fora", x => { x.S.player.fame -= 1; return "Você aproveitou a folga. Alguns torcedores notaram sua ausência."; }))),

            (g => g.S.player.money >= 60000, .8, g => {
                long v = Game.R1000(g.S.player.money * .2);
                return Ev("Proposta de investimento", $"Um conhecido oferece sociedade em uma rede de restaurantes. Aporte de {Fmt.Money(v)}.",
                    Opt("Investir", x => { var p = x.S.player; p.money -= v;
                        if (Rng.Chance(.5)) { long gain = (long)(v * Rng.RangeF(1.6, 2.4)); p.money += gain; return $"O negócio deu certo. Você recebeu {Fmt.Money(gain)} de volta."; }
                        return "A rede fechou em poucos meses. O dinheiro foi perdido."; }),
                    Opt("Recusar", x => "Você preferiu não arriscar.")); }),

            (g => true, 1, g => {
                var a = g.WeakestAttr(); string l = GameData.Label(a).ToLowerInvariant();
                return Ev("Treino extra com o capitão", $"O capitão se oferece para treinar {l} com você depois do horário.",
                    Opt("Aceitar", x => { var p = x.S.player; p.xp[(int)a] += 9; p.energy -= 10; p.coach += 2; return $"Treino puxado. Você vai evoluir em {l}."; }),
                    Opt("Recusar e descansar", x => { x.S.player.energy += 5; return "Você foi para casa descansar."; })); }),

            (g => g.S.player.fame >= 8, .9, g => Ev("Provocação nas redes", "Um jogador do próximo adversário postou uma provocação marcando você.",
                Opt("Responder à altura", x => { var p = x.S.player;
                    if (Rng.Chance(.5)) { p.fame += 4; p.moral += 3; return "Sua resposta viralizou e a torcida adorou."; }
                    p.fame -= 2; p.coach -= 4; return "A resposta pegou mal e o clube pediu silêncio."; }),
                Opt("Ignorar", x => { x.S.player.coach += 1; return "Você vai responder dentro de campo."; }))),

            (g => true, 1, g => Ev("Churrasco do elenco", "O grupo marcou um churrasco no fim de semana.",
                Opt("Ir", x => { var p = x.S.player; p.moral += 5; p.coach += 3; p.energy -= 5; return "Resenha boa. O elenco está mais unido."; }),
                Opt("Não ir", x => { x.S.player.coach -= 2; return "Sentiram sua falta."; }))),

            (g => g.S.player.fame >= 20 && !g.S.season.forceExit, .7, g => Ev("Seu empresário quer agir",
                "Ele quer espalhar que você deseja jogar em um clube maior. Isso atrai propostas, mas irrita a diretoria.",
                Opt("Autorizar", x => { x.S.player.coach -= 10; x.S.season.forceExit = true; return "A notícia saiu. Mais clubes vão te observar no fim da temporada."; }),
                Opt("Não autorizar", x => { x.S.player.coach += 2; return "Você mantém o foco no clube atual."; }))),

            (g => g.S.player.money >= 20000, .7, g => {
                long v = Math.Max(5000, Game.R1000(g.S.player.money * .08));
                return Ev("Família pede ajuda", $"Seus pais precisam reformar a casa. Custo: {Fmt.Money(v)}.",
                    Opt("Ajudar", x => { x.S.player.money -= v; x.S.player.moral += 8; return "A reforma começou. Sua família está orgulhosa."; }),
                    Opt("Agora não", x => { x.S.player.moral -= 4; return "Você adiou a ajuda e ficou com isso na cabeça."; })); }),

            (g => g.S.player.energy < 50, 1.2, g => Ev("Incômodo na coxa", "Você sente a coxa pesada no fim do treino.",
                Opt("Jogar mesmo assim", x => {
                    if (Rng.Chance(.4)) { x.S.player.injury = Rng.RangeInt(2, 4); return $"Piorou. Lesão muscular: {x.S.player.injury} rodadas fora."; }
                    return "Deu para segurar. Você está à disposição."; }),
                Opt("Pedir para ser poupado", x => { x.S.season.rested = true; x.S.player.energy += 15; x.S.player.coach -= 2; return "Você fica fora desta rodada para se recuperar."; }))),

            (g => g.S.player.fame >= 35, .8, g => {
                long v = Game.R1000(8000 * Math.Exp(g.S.player.fame / 20.0));
                return Ev("Convite da TV", $"Um programa de domingo quer uma entrevista exclusiva. Cachê de {Fmt.Money(v)}.",
                    Opt("Aceitar", x => { var p = x.S.player; p.money += v; p.fame += 3; p.energy -= 8; return "A entrevista foi sucesso de audiência."; }),
                    Opt("Recusar", x => "Você preferiu se preservar.")); }),
        };

        public static GameEvent Roll(Game g)
        {
            var pool = new List<(Func<Game, GameEvent> build, double weight)>();
            foreach (var e in All) if (e.cond(g)) pool.Add((e.build, e.weight));
            return pool.Count == 0 ? null : Rng.Weighted(pool)(g);
        }

        /// <summary>Aplica a opção escolhida e devolve o texto do resultado.</summary>
        public static string Resolve(Game g, GameEvent ev, int option)
        {
            string msg = ev.Options[option].Apply(g);
            g.Normalize();
            if (g.S.season.phase == "match") g.S.season.role = g.ComputeRole();
            g.AddNews($"{ev.Title}: {msg}");
            return msg;
        }
    }
}
