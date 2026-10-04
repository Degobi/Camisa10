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

            // ---------- mercado ----------
            (g => g.S.player.fame >= 28 && g.MyClub.league != "sa", .5, g => {
                var club = Rng.Pick(new[] { "Al-Hilal", "Al-Nassr", "Al-Ittihad", "Al-Ahli" });
                return Ev($"Ligação da Arábia Saudita", $"O {club} quer você e fala em salário quatro vezes maior. Eles pedem uma resposta discreta.",
                    Opt("Ouvir a proposta", x => { x.S.interestLeague = "sa"; x.S.player.coach -= 6; x.S.player.moral += 3;
                        return $"Você deixou a porta aberta. No fim da temporada o {club} vai fazer uma proposta oficial."; }),
                    Opt("Não tenho interesse", x => { x.S.player.coach += 4; x.S.player.fame += 1; return "A diretoria e a torcida gostaram da sua lealdade."; })); }),

            (g => g.S.player.fame >= 40 && g.Ovr >= 74 && g.MyClub.league != "en" && g.MyClub.league != "ib", .55, g => {
                var club = Rng.Pick(new[] { "Real Madrid", "Barcelona", "Manchester City", "Liverpool", "Arsenal", "Paris Saint-Germain", "Inter de Milão" });
                var lg = Array.Find(GameData.Leagues, l => Array.Exists(l.Clubs, c => c.name == club))?.Id ?? "en";
                return Ev("Olheiro na arquibancada", $"A imprensa descobriu: o {club} mandou observar você no último jogo.",
                    Opt("Mandar um recado: \"é um sonho\"", x => { x.S.interestLeague = lg; x.S.player.coach -= 8; x.S.player.fame += 3;
                        return $"Manchete no mundo todo. O {club} deve fazer proposta no fim da temporada; seu clube não gostou."; }),
                    Opt("Desconversar", x => { x.S.player.coach += 3; return "\"Estou focado aqui.\" A diretoria respirou aliviada."; })); }),

            (g => g.S.player.age >= 29 && g.S.player.fame >= 22 && g.MyClub.league != "us", .45, g => Ev("Convite para jogar nos EUA",
                "Um clube da MLS oferece contrato, casa em Miami e um projeto de imagem para você e sua família.",
                Opt("Topar conversar", x => { x.S.interestLeague = "us"; x.S.player.moral += 4; return "A MLS vai mandar proposta oficial no fim da temporada."; }),
                Opt("Ainda quero jogar na elite", x => { x.S.player.coach += 2; return "Você quer provar que ainda tem lenha para queimar."; }))),

            // ---------- dilemas ----------
            (g => true, .35, g => {
                long v = Game.R1000(Math.Max(30000, g.S.contract.salary * 6));
                return Ev("Proposta indecente", $"Um homem de uma casa de apostas oferece {Fmt.Money(v)} para você tomar um cartão amarelo no próximo jogo.",
                    Opt("Denunciar à polícia", x => { var p = x.S.player; p.fame += 5; p.coach += 6; p.moral += 4;
                        x.AddNews("Você denunciou um esquema de apostas e virou exemplo no futebol."); return "Sua denúncia derrubou o esquema. Você virou exemplo no futebol."; }),
                    Opt("Recusar e esquecer", x => "Você recusou e seguiu a vida."),
                    Opt("Aceitar o dinheiro", x => { var p = x.S.player;
                        if (Rng.Chance(.55)) { p.injury = 6; p.fame -= 15; p.coach -= 30; p.moral -= 20;
                            return "A investigação te pegou. Suspensão de 6 rodadas, multa e a imagem destruída."; }
                        p.money += v; p.moral -= 10; return "Ninguém descobriu... por enquanto. Mas você não dorme direito."; })); }),

            (g => g.S.player.energy < 70, .5, g => Ev("Remédio para gripe", "Você acordou gripado. O farmacêutico indica um remédio forte, mas não sabe se tem substância proibida.",
                Opt("Tomar e jogar", x => { var p = x.S.player; p.energy += 15;
                    if (Rng.Chance(.25)) { p.injury = 4; p.fame -= 6; return "Exame antidoping positivo! Suspensão de 4 rodadas até provar que foi engano."; }
                    return "Melhorou rápido e o exame deu negativo. Sorte."; }),
                Opt("Falar com o médico do clube", x => { x.S.player.energy += 5; x.S.player.coach += 3; return "O médico indicou um remédio liberado. Profissionalismo."; }))),

            (g => g.S.season.week >= 4, .4, g => Ev("Salários atrasados", "O clube atrasou dois meses de salário. O elenco quer uma posição sua, que é uma das lideranças.",
                Opt("Cobrar a diretoria publicamente", x => { var p = x.S.player; p.coach -= 10; p.fame += 3; p.money += x.S.contract.salary * 4;
                    return "Pegou mal com a diretoria, mas os salários caíram na conta em dois dias."; }),
                Opt("Resolver no vestiário", x => { x.S.player.moral -= 4; x.S.player.coach += 4; return "Você segurou o grupo. O dinheiro vai demorar, mas o clima ficou bom."; }))),

            // ---------- vida pessoal ----------
            (g => g.S.player.fame >= 25, .5, g => {
                var who = Rng.Pick(new[] { "uma cantora famosa", "uma atriz de novela", "uma influenciadora com milhões de seguidores", "uma apresentadora de TV" });
                return Ev("Paparazzi", $"Você foi fotografado jantando com {who}. As fotos estão em todos os sites de fofoca.",
                    Opt("Assumir o namoro", x => { var p = x.S.player; p.fame += 6; p.moral += 8; p.energy -= 6; return "O casal mais comentado do país. Sua fama explodiu."; }),
                    Opt("Dizer que é só amizade", x => { x.S.player.fame += 2; return "A história esfriou em uma semana."; })); }),

            (g => true, .6, g => Ev("Briga no treino", "Um companheiro deu uma entrada dura em você no rachão e partiu para cima.",
                Opt("Revidar", x => { var p = x.S.player; p.coach -= 8; p.moral -= 2;
                    if (Rng.Chance(.4)) { p.injury = 1; return "Os dois foram afastados. Você está fora da próxima rodada."; }
                    return "A turma do deixa-disso separou. O técnico anotou o seu nome."; }),
                Opt("Esfriar a cabeça", x => { x.S.player.coach += 4; return "Você deixou para lá e depois apertou a mão dele. O grupo te respeita."; }))),

            (g => true, .6, g => Ev("Comemoração viralizou", "A sua dancinha de comemoração do último gol virou trend nas redes, com milhões de visualizações.",
                Opt("Gravar com a torcida", x => { var p = x.S.player; p.fame += 4; p.moral += 4; return "A torcida adorou. Você virou figurinha carimbada."; }),
                Opt("Deixar quieto", x => { x.S.player.fame += 1; return "A trend passou, mas o nome ficou."; }))),

            (g => g.S.player.money >= 150000, .5, g => {
                long v = Game.R1000(Math.Max(60000, g.S.player.money * .12));
                return Ev("Presente para a mãe", $"Sua mãe sempre sonhou com uma casa nova. Custaria {Fmt.Money(v)}.",
                    Opt("Dar a casa de presente", x => { x.S.player.money -= v; x.S.player.moral += 15; x.S.player.fame += 2; return "Ela chorou ao receber as chaves. Você também."; }),
                    Opt("Fica para o ano que vem", x => { x.S.player.moral -= 2; return "Ela entendeu. Mas você prometeu."; })); }),

            // ---------- clube e torcida ----------
            (g => g.S.season.week >= 5, .45, g => Ev("Técnico novo", "A diretoria demitiu o treinador. O novo técnico chega com ideias diferentes e quer conversar com você.",
                Opt("Se colocar à disposição", x => { x.S.player.coach = 55; x.S.player.moral += 3; return "Começo com o pé direito com o novo técnico."; }),
                Opt("Cobrar a posição de titular", x => { var p = x.S.player; p.coach = Rng.Chance(.5) ? 65 : 35; return p.coach > 50 ? "Ele gostou da personalidade." : "Ele não gostou da cobrança logo no primeiro dia."; }))),

            (g => g.S.player.form.Count > 0 && g.FormAvg < 6.3f, 1, g => Ev("Protesto da organizada", "Depois de maus resultados, a torcida organizada foi ao CT cobrar o elenco. Eles chamam você.",
                Opt("Ir conversar com eles", x => { var p = x.S.player; p.moral += 4; p.fame += 2; return "Conversa franca. A torcida prometeu apoio até o fim."; }),
                Opt("Ficar no vestiário", x => { x.S.player.moral -= 5; return "Picharam o muro do CT com o seu nome. Clima pesado."; }))),

            (g => g.S.player.form.Count > 0 && g.FormAvg >= 7.2f, .8, g => Ev("Elogio de lenda", "Um ídolo histórico do clube disse na TV que você \"é o melhor que vê em campo há anos\".",
                Opt("Agradecer publicamente", x => { var p = x.S.player; p.fame += 3; p.moral += 6; return "A torcida adorou a troca de carinho entre gerações."; }),
                Opt("Pedir conselho a ele", x => { var p = x.S.player; p.xp[(int)Game.MainAttr(p.pos)] += 12; p.moral += 3; return "Vocês treinaram juntos uma tarde inteira. Você vai evoluir."; }))),

            // ---------- seleção ----------
            (g => g.NationalScore >= 72 && g.NationalScore < 80, .7, g => Ev("O técnico da Seleção ligou", "Ele diz que está de olho em você e pede mais regularidade para a próxima convocação.",
                Opt("Fazer treinos extras", x => { var p = x.S.player; p.xp[(int)x.WeakestAttr()] += 10; p.energy -= 10; p.fame += 1; return "Você dobrou a carga. A comissão técnica vai notar."; }),
                Opt("Manter a rotina", x => { x.S.player.moral += 3; return "Você seguiu tranquilo. A chance vai chegar."; }))),
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
