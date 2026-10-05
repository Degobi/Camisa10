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
                Opt("Criticar o técnico", x => { var p = x.S.player; x.AddSquad(-3); x.AddFans(2); p.coach -= 15; p.fame += 3; p.moral += 2; return "A declaração virou manchete. O clima com o técnico pesou."; }),
                Opt("Responder com diplomacia", x => { x.S.player.coach += 5; return "\"Vou continuar trabalhando.\" O técnico gostou da resposta."; }))),

            (g => true, 1, g => Ev("Visita ao hospital infantil", "O clube organiza uma ação social no seu dia de folga.",
                Opt("Participar", x => { var p = x.S.player; x.AddFans(3); p.fame += 2; p.moral += 5; p.energy -= 5; return "As crianças adoraram. Você saiu de lá renovado."; }),
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
                    Opt("Aceitar", x => { var p = x.S.player; x.AddSquad(3); p.xp[(int)a] += 9; p.energy -= 10; p.coach += 2; return $"Treino puxado. Você vai evoluir em {l}."; }),
                    Opt("Recusar e descansar", x => { x.S.player.energy += 5; return "Você foi para casa descansar."; })); }),

            (g => g.S.player.fame >= 8, .9, g => Ev("Provocação nas redes", "Um jogador do próximo adversário postou uma provocação marcando você.",
                Opt("Responder à altura", x => { var p = x.S.player;
                    if (Rng.Chance(.5)) { p.fame += 4; p.moral += 3; return "Sua resposta viralizou e a torcida adorou."; }
                    p.fame -= 2; p.coach -= 4; return "A resposta pegou mal e o clube pediu silêncio."; }),
                Opt("Ignorar", x => { x.S.player.coach += 1; return "Você vai responder dentro de campo."; }))),

            (g => true, 1, g => Ev("Churrasco do elenco", "O grupo marcou um churrasco no fim de semana.",
                Opt("Ir", x => { var p = x.S.player; x.AddSquad(6); p.moral += 5; p.coach += 3; p.energy -= 5; return "Resenha boa. O elenco está mais unido."; }),
                Opt("Não ir", x => { x.AddSquad(-3); x.S.player.coach -= 2; return "Sentiram sua falta."; }))),

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
                long v = Game.R1000(6000 * Math.Exp(g.S.player.fame / 26.0));
                return Ev("Convite da TV", $"Um programa de domingo quer uma entrevista exclusiva. Cachê de {Fmt.Money(v)}.",
                    Opt("Aceitar", x => { var p = x.S.player; p.money += v; p.fame += 3; p.energy -= 8; return "A entrevista foi sucesso de audiência."; }),
                    Opt("Recusar", x => "Você preferiu se preservar.")); }),

            // ---------- mercado ----------
            (g => g.S.player.fame >= 28 && g.MyClub.league != "sa", .5, g => {
                var club = Rng.Pick(new[] { "Al-Hilal", "Al-Nassr", "Al-Ittihad", "Al-Ahli" });
                return Ev($"Ligação da Arábia Saudita", $"O {club} quer você e fala em salário quatro vezes maior. Eles pedem uma resposta discreta.",
                    Opt("Ouvir a proposta", x => { x.AddFans(-4); x.S.interestLeague = "sa"; x.S.player.coach -= 6; x.S.player.moral += 3;
                        return $"Você deixou a porta aberta. No fim da temporada o {club} vai fazer uma proposta oficial."; }),
                    Opt("Não tenho interesse", x => { x.AddFans(5); x.S.player.coach += 4; x.S.player.fame += 1; return "A diretoria e a torcida gostaram da sua lealdade."; })); }),

            (g => g.S.player.fame >= 40 && g.Ovr >= 74 && g.MyClub.league != "en" && g.MyClub.league != "ib", .55, g => {
                var club = Rng.Pick(new[] { "Real Madrid", "Barcelona", "Manchester City", "Liverpool", "Arsenal", "Paris Saint-Germain", "Inter de Milão" });
                var lg = Array.Find(GameData.Leagues, l => Array.Exists(l.Clubs, c => c.name == club))?.Id ?? "en";
                return Ev("Olheiro na arquibancada", $"A imprensa descobriu: o {club} mandou observar você no último jogo.",
                    Opt("Mandar um recado: \"é um sonho\"", x => { x.AddFans(-6); x.S.interestLeague = lg; x.S.player.coach -= 8; x.S.player.fame += 3;
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
                Opt("Cobrar a diretoria publicamente", x => { var p = x.S.player; x.AddSquad(7); p.coach -= 10; p.fame += 3; p.money += x.S.contract.salary * 4;
                    return "Pegou mal com a diretoria, mas os salários caíram na conta em dois dias."; }),
                Opt("Resolver no vestiário", x => { x.AddSquad(4); x.S.player.moral -= 4; x.S.player.coach += 4; return "Você segurou o grupo. O dinheiro vai demorar, mas o clima ficou bom."; }))),

            // ---------- vida pessoal ----------
            (g => g.S.player.fame >= 25, .5, g => {
                var who = Rng.Pick(new[] { "uma cantora famosa", "uma atriz de novela", "uma influenciadora com milhões de seguidores", "uma apresentadora de TV" });
                return Ev("Paparazzi", $"Você foi fotografado jantando com {who}. As fotos estão em todos os sites de fofoca.",
                    Opt("Assumir o namoro", x => { var p = x.S.player; p.fame += 6; p.moral += 8; p.energy -= 6; return "O casal mais comentado do país. Sua fama explodiu."; }),
                    Opt("Dizer que é só amizade", x => { x.S.player.fame += 2; return "A história esfriou em uma semana."; })); }),

            (g => true, .6, g => Ev("Briga no treino", "Um companheiro deu uma entrada dura em você no rachão e partiu para cima.",
                Opt("Revidar", x => { var p = x.S.player; x.AddSquad(-6); p.coach -= 8; p.moral -= 2;
                    if (Rng.Chance(.4)) { p.injury = 1; return "Os dois foram afastados. Você está fora da próxima rodada."; }
                    return "A turma do deixa-disso separou. O técnico anotou o seu nome."; }),
                Opt("Esfriar a cabeça", x => { x.AddSquad(4); x.S.player.coach += 4; return "Você deixou para lá e depois apertou a mão dele. O grupo te respeita."; }))),

            (g => true, .6, g => Ev("Comemoração viralizou", "A sua dancinha de comemoração do último gol virou trend nas redes, com milhões de visualizações.",
                Opt("Gravar com a torcida", x => { var p = x.S.player; x.AddFans(4); p.fame += 4; p.moral += 4; return "A torcida adorou. Você virou figurinha carimbada."; }),
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
                Opt("Ir conversar com eles", x => { var p = x.S.player; x.AddFans(7); p.moral += 4; p.fame += 2; return "Conversa franca. A torcida prometeu apoio até o fim."; }),
                Opt("Ficar no vestiário", x => { x.AddFans(-6); x.S.player.moral -= 5; return "Picharam o muro do CT com o seu nome. Clima pesado."; }))),

            (g => g.S.player.form.Count > 0 && g.FormAvg >= 7.2f, .8, g => Ev("Elogio de lenda", "Um ídolo histórico do clube disse na TV que você \"é o melhor que vê em campo há anos\".",
                Opt("Agradecer publicamente", x => { var p = x.S.player; x.AddFans(3); p.fame += 3; p.moral += 6; return "A torcida adorou a troca de carinho entre gerações."; }),
                Opt("Pedir conselho a ele", x => { var p = x.S.player; p.xp[(int)Game.MainAttr(p.pos)] += 12; p.moral += 3; return "Vocês treinaram juntos uma tarde inteira. Você vai evoluir."; }))),

            // ---------- seleção ----------
            (g => g.NationalScore >= 72 && g.NationalScore < 80, .7, g => Ev("O técnico da Seleção ligou", "Ele diz que está de olho em você e pede mais regularidade para a próxima convocação.",
                Opt("Fazer treinos extras", x => { var p = x.S.player; p.xp[(int)x.WeakestAttr()] += 10; p.energy -= 10; p.fame += 1; return "Você dobrou a carga. A comissão técnica vai notar."; }),
                Opt("Manter a rotina", x => { x.S.player.moral += 3; return "Você seguiu tranquilo. A chance vai chegar."; }))),

            // ---------- fama e mídia ----------
            (g => g.S.player.fame >= 50, .45, g => {
                long v = Game.R1000(150000 + g.S.player.fame * 4000);
                return Ev("Capa do videogame", $"Uma produtora de jogos de futebol quer você na capa da próxima edição. Cachê de {Fmt.Money(v)}.",
                    Opt("Aceitar", x => { var p = x.S.player; p.money += v; p.fame += 6; p.energy -= 6; return "Seu rosto vai estar em milhões de consoles. Que fase!"; }),
                    Opt("Recusar: \"foco no campo\"", x => { x.S.player.coach += 3; return "O técnico gostou do foco."; })); }),

            (g => g.S.player.fame >= 18, .5, g => Ev("Participação em clipe", "Um cantor de sucesso quer você no clipe da nova música. A gravação é na folga.",
                Opt("Gravar", x => { var p = x.S.player; p.fame += 4; p.moral += 5; p.energy -= 8; return "O clipe bombou e você virou meme (do bom)."; }),
                Opt("Ficar descansando", x => { x.S.player.energy += 6; return "Você recarregou as energias."; }))),

            (g => g.S.player.fame >= 12, .5, g => Ev("Live com streamer", "Um streamer famoso te desafia para uma partida de videogame ao vivo.",
                Opt("Topar o desafio", x => { var p = x.S.player;
                    if (Rng.Chance(.5)) { p.fame += 4; p.moral += 4; return "Você venceu ao vivo. Recorde de audiência!"; }
                    p.fame += 2; return "Perdeu feio, mas levou na esportiva e ganhou seguidores."; }),
                Opt("Agora não", x => "Fica para outra vez."))),

            // ---------- polêmicas ----------
            (g => g.S.season.week >= 2, .45, g => Ev("Acusação de simulação", "O comitê disciplinar abriu processo: dizem que você simulou um pênalti no último jogo.",
                Opt("Recorrer com advogado", x => { var p = x.S.player; long c = Game.R1000(Math.Max(10000, x.S.contract.salary * 2)); p.money -= c;
                    if (Rng.Chance(.6)) return $"Absolvido! O advogado custou {Fmt.Money(c)}, mas valeu.";
                    p.injury = Math.Max(p.injury, 1); return "Recurso negado: suspensão de 1 rodada."; }),
                Opt("Aceitar a punição", x => { x.S.player.injury = Math.Max(x.S.player.injury, 1); x.S.player.fame -= 2; return "Suspenso por 1 rodada. Vida que segue."; }))),

            (g => g.S.player.fame >= 20, .45, g => Ev("Fake news", "Um site publicou que você brigou com o técnico e pediu para sair. Nada disso aconteceu.",
                Opt("Processar o site", x => { var p = x.S.player; p.coach += 4; p.fame += 1; return "O site se retratou. O técnico agradeceu o posicionamento."; }),
                Opt("Gravar vídeo desmentindo", x => { var p = x.S.player; p.fame += 3; p.moral += 2; return "O vídeo viralizou e a mentira morreu."; }),
                Opt("Ignorar", x => { x.S.player.coach -= 5; return "A dúvida ficou no ar no vestiário."; }))),

            (g => g.S.player.fame >= 30, .4, g => Ev("Confusão no aeroporto", "Torcedores rivais cercaram você no desembarque, com xingamentos e empurrões.",
                Opt("Contratar seguranças", x => { var p = x.S.player; p.money -= Game.R1000(Math.Max(8000, x.S.contract.salary)); p.moral += 2; return "Agora você anda com escolta. Mais tranquilidade."; }),
                Opt("Responder aos gritos", x => { var p = x.S.player; p.fame += 2; p.coach -= 6; return "O vídeo correu as redes. O clube pediu calma."; }),
                Opt("Sorrir e acenar", x => { var p = x.S.player; p.fame += 3; p.moral += 1; return "Classe. Até torcedores rivais elogiaram."; }))),

            // ---------- vida pessoal ----------
            (g => g.S.love.stage == 2 && g.S.love.weeks >= 24 && g.S.love.affection >= 60, .8, g => Ev("Pedido de casamento", $"Você está pensando em pedir {g.S.love.name} em casamento no gramado, depois do jogo.",
                Opt("Pedir no gramado", x => { var p = x.S.player; p.moral += 15; p.fame += 4; p.energy -= 5; x.S.love.stage = 3; x.S.love.weeks = 0; x.S.love.affection += 15; return "Ela disse SIM! O estádio inteiro aplaudiu."; }),
                Opt("Pedir num jantar a dois", x => { x.S.player.moral += 12; x.S.love.stage = 3; x.S.love.weeks = 0; x.S.love.affection += 12; return "Ela disse sim, num momento só de vocês."; }),
                Opt("Ainda não é a hora", x => { x.S.love.affection -= 8; return "Você guardou o anel na gaveta."; }))),

            // ---------- namoro ----------
            (g => g.S.love.stage == 0 && g.S.player.fame >= 4, .7, g => {
                var (job, famous) = Rng.Pick(LifeData.PartnerJobs); string name = Rng.Pick(LifeData.PartnerNames);
                return Ev("Mensagem no direct", $"{name}, {job}, respondeu seu story e puxou conversa.",
                    Opt("Chamar para sair", x => { x.S.love = new Relationship { name = name, job = job, famous = famous, stage = 1, affection = 58 }; x.S.player.moral += 4;
                        return $"Vocês marcaram um café. Rolou química com {name}."; }),
                    Opt("Focar no futebol", x => { x.S.player.coach += 1; return "Agora a cabeça está no campeonato."; })); }),

            (g => g.S.love.stage == 1 && g.S.love.weeks >= 4 && g.S.love.affection >= 50, 1.2, g => Ev("Assumir o namoro?", $"{g.S.love.name} quer saber o que vocês são. Os paparazzi já fotografaram os dois juntos.",
                Opt("Assumir nas redes", x => { var l = x.S.love; l.stage = 2; l.weeks = 0; l.affection += 12; x.S.player.moral += 6; if (l.famous) x.S.player.fame += 3;
                    return l.famous ? "O post bombou: o casal virou assunto nacional." : "Namoro assumido. A torcida aprovou o casal."; }),
                Opt("Manter discreto", x => { x.S.love.affection -= 10; return "Ela aceitou, mas não gostou muito."; }),
                Opt("Terminar", x => { x.S.love = new Relationship(); x.S.player.moral -= 3; return "Você preferiu ficar solteiro."; }))),

            (g => g.S.love.stage >= 2, .7, g => Ev("Aniversário na véspera do jogo", $"O aniversário da {g.S.love.name} é hoje à noite. O jogo é amanhã cedo.",
                Opt("Ir à festa até tarde", x => { var p = x.S.player; x.S.love.affection += 15; p.energy -= 15;
                    if (Rng.Chance(.3)) { p.coach -= 6; return "Ela amou, mas o técnico soube que você chegou de madrugada."; }
                    return "Ela amou a surpresa e ninguém do clube ficou sabendo."; }),
                Opt("Passar rapidinho", x => { x.S.love.affection += 4; x.S.player.energy -= 4; return "Você deu um beijo, entregou o presente e foi dormir."; }),
                Opt("Ficar concentrado", x => { x.S.love.affection -= 14; x.S.player.coach += 2; return "Ela ficou chateada. Vai precisar compensar."; }))),

            (g => g.S.love.stage >= 2 && g.S.player.fame >= 15, .6, g => Ev("Ciúmes nas redes", $"Uma torcedora comentou num post seu e {g.S.love.name} viu.",
                Opt("Conversar com calma", x => { x.S.love.affection += 4; return "Papo franco. Ficou tudo bem."; }),
                Opt("Apagar o comentário", x => { x.S.love.affection += 2; x.S.player.fame -= .5f; return "Resolvido, mas virou print em página de fofoca."; }),
                Opt("Não dar importância", x => { x.S.love.affection -= 12; return "Ela achou que você não se importou. Clima pesado em casa."; }))),

            (g => g.S.love.stage == 2 && g.S.love.weeks >= 12 && g.S.player.money >= 30000, .6, g => {
                long v = Game.R1000(Math.Max(15000, g.S.contract.salary * 1.5));
                return Ev("Morar juntos", $"{g.S.love.name} propõe que vocês morem juntos. Mudança e móveis novos: {Fmt.Money(v)}.",
                    Opt("Bora", x => { x.S.player.money -= v; x.S.love.affection += 15; x.S.player.moral += 6; return "Casa nova, vida nova. Vocês estão felizes."; }),
                    Opt("Ainda é cedo", x => { x.S.love.affection -= 10; return "Ela entendeu, mas ficou pensativa."; })); }),

            (g => g.S.love.stage == 3 && g.S.love.weeks >= 10, 1.3, g => {
                long big = Game.R1000(Math.Max(120000, g.S.player.money * .12)), small = Game.R1000(Math.Max(25000, g.S.player.money * .03));
                return Ev("O casamento", $"Chegou a hora de casar com {g.S.love.name}. Como vai ser a festa?",
                    Opt($"Festança ({Fmt.Money(big)})", x => { var p = x.S.player; p.money -= big; p.fame += 5; p.moral += 15; x.S.love.stage = 4; x.S.love.weeks = 0; x.S.love.affection = 95;
                        return "Festa de três dias, revista de celebridades e o elenco inteiro na pista."; }),
                    Opt($"Cerimônia íntima ({Fmt.Money(small)})", x => { var p = x.S.player; p.money -= small; p.moral += 12; x.S.love.stage = 4; x.S.love.weeks = 0; x.S.love.affection = 92;
                        return "Só família e amigos de verdade. Inesquecível."; })); }),

            (g => g.S.love.stage >= 2 && g.S.love.famous && g.S.player.fame >= 25, .5, g => Ev("Paparazzi", $"Fotógrafos seguiram você e {g.S.love.name} na praia. As fotos saem amanhã.",
                Opt("Posar para as fotos", x => { x.S.player.fame += 2; x.S.love.affection += 3; return "Capa de revista. O casal mais comentado da semana."; }),
                Opt("Pedir privacidade", x => { x.S.love.affection += 6; x.S.player.coach += 1; return "Vocês foram embora. Ela gostou da atitude."; }))),

            (g => g.S.love.stage >= 2 && g.S.love.affection < 35, 1.1, g => Ev("Crise no relacionamento", $"{g.S.love.name} reclama que você só pensa em futebol.",
                Opt("Viagem de fim de semana", x => { long c = Game.R1000(Math.Max(8000, x.S.contract.salary * .6)); x.S.player.money -= c; x.S.player.energy += 6; x.S.love.affection += 25;
                    return $"Dois dias longe de tudo ({Fmt.Money(c)}). Vocês voltaram bem."; }),
                Opt("Prometer mudar", x => { x.S.love.affection += 8; return "Ela vai esperar para ver."; }),
                Opt("Dar um tempo", x => { x.S.love.affection = 5; return "Vocês decidiram dar um tempo."; }))),

            // ---------- rival ----------
            (g => !string.IsNullOrEmpty(g.S.rival.name) && g.RivalClub != null, .8, g => Ev("Provocação do rival", $"{g.S.rival.name}, do {g.RivalClub.name}, disse numa entrevista que você é \"jogador de rede social\".",
                Opt("Responder à altura", x => { var p = x.S.player; x.S.rival.heat += 12;
                    if (Rng.Chance(.55)) { p.fame += 3; p.moral += 3; return "Sua resposta viralizou. A torcida comprou a briga."; }
                    p.coach -= 4; return "A resposta pegou mal e o clube pediu silêncio."; }),
                Opt("Responder em campo", x => { x.S.player.coach += 2; x.S.player.moral += 2; return "\"Falo dentro de campo.\" A imprensa elogiou a postura."; }))),

            (g => !string.IsNullOrEmpty(g.S.rival.name) && g.S.season.week >= 6, .6, g => {
                var r = g.S.rival; int mine = g.S.season.stats.goals;
                return Ev("Quem é melhor?", $"Um programa de TV comparou você e {r.name}: {mine} gol(s) seus contra {r.goals} dele na temporada.",
                    Opt("\"Os números falam\"", x => { var p = x.S.player; x.S.rival.heat += 8;
                        if (mine >= r.goals) { p.fame += 3; p.moral += 4; return "Os números estão do seu lado. A frase virou meme."; }
                        p.fame -= 1; p.moral -= 3; return "Ele está na frente e as redes não perdoaram a frase."; }),
                    Opt("Elogiar o rival", x => { x.S.player.fame += 1; x.S.rival.heat -= 10; return "Classe. O público gostou da humildade."; })); }),

            (g => !string.IsNullOrEmpty(g.S.rival.name) && g.S.player.fame >= 15, .4, g => Ev("Encontro na premiação", $"Na festa de premiação você fica frente a frente com {g.S.rival.name}.",
                Opt("Cumprimentar", x => { x.S.rival.heat -= 15; x.S.player.fame += 1; return "Aperto de mão e foto juntos. A rivalidade esfriou um pouco."; }),
                Opt("Passar reto", x => { x.S.rival.heat += 10; x.S.player.fame += 1.5f; return "As câmeras pegaram o gelo. Virou assunto a semana inteira."; }))),

            (g => !string.IsNullOrEmpty(g.S.rival.name) && g.S.love.stage == 0 && g.S.rival.heat >= 50, .3, g => Ev("Rival na balada", $"{g.S.rival.name} postou foto numa festa com a sua ex. A internet não fala de outra coisa.",
                Opt("Rir da situação", x => { x.S.player.fame += 2; x.S.player.moral += 1; return "Seu meme em resposta foi o post mais curtido do mês."; }),
                Opt("Ignorar", x => { x.S.player.moral -= 2; x.S.rival.heat += 5; return "Você não comentou, mas ficou incomodado."; }))),

            (g => g.S.player.money >= 50000, .4, g => Ev("Irmão empresário", "Seu irmão quer largar o emprego para cuidar da sua carreira como empresário.",
                Opt("Dar a chance", x => { var p = x.S.player;
                    if (Rng.Chance(.5)) { p.fame += 3; p.moral += 6; x.S.interestLeague = null; return "Ele surpreendeu: fechou parcerias e a família ficou mais unida."; }
                    p.money -= Game.R1000(p.money * .1); p.moral -= 4; return "Ele fez negócios ruins e você perdeu dinheiro. Clima tenso no Natal."; }),
                Opt("Manter o empresário atual", x => { x.S.player.moral -= 3; return "Ele entendeu, mas ficou chateado."; }))),

            (g => true, .4, g => Ev("Adoção", "Um abrigo de animais pede ajuda e um vira-lata te seguiu na saída do treino.",
                Opt("Adotar o cachorro", x => { var p = x.S.player; x.AddFans(2); p.moral += 7; p.fame += 2; return "O Gol (é o nome dele) já virou xodó da torcida."; }),
                Opt("Fazer uma doação", x => { var p = x.S.player; p.money -= 5000; p.fame += 2; return "Sua doação reformou o abrigo."; }))),

            (g => g.S.season.week >= 6 && g.S.player.form.Count > 0 && g.FormAvg >= 6.8f, .4, g => Ev("Tatuagem do escudo", "Você está pensando em tatuar o escudo do clube no braço.",
                Opt("Tatuar", x => { var p = x.S.player; x.AddFans(9); p.fame += 3; p.coach += 3; p.moral += 4; return "A torcida enlouqueceu. Você virou ídolo de vez."; }),
                Opt("Melhor não", x => "Você preferiu não se comprometer."))),

            // ---------- campo ----------
            (g => g.S.season.week >= 3, .5, g => Ev("Improvisado", "Com desfalques, o técnico pergunta se você topa jogar improvisado em outra posição no próximo jogo.",
                Opt("Topar pelo time", x => { var p = x.S.player; x.AddSquad(4); p.coach += 8; p.xp[(int)x.WeakestAttr()] += 8; return "O técnico não esquece quem topa o sacrifício."; }),
                Opt("Pedir para jogar na sua posição", x => { x.S.player.coach -= 4; return "Ele aceitou, mas não gostou muito."; }))),

            (g => g.S.season.week >= 2, .4, g => Ev("Dividida dura", "Numa dividida no último jogo, um adversário se machucou feio. A imprensa diz que você exagerou.",
                Opt("Visitar ele no hospital", x => { var p = x.S.player; p.fame += 4; p.moral += 3; return "A visita emocionou o futebol. Fair play de verdade."; }),
                Opt("Dizer que foi lance de jogo", x => { x.S.player.fame -= 2; return "Foi lance de jogo mesmo, mas pegou mal com parte do público."; }))),

            (g => g.S.player.fame >= 15, .35, g => Ev("Despedida de um ídolo", "Uma lenda do futebol vai se despedir num jogo festivo e convidou você para o time dele.",
                Opt("Jogar a despedida", x => { var p = x.S.player; p.fame += 4; p.moral += 6; p.energy -= 12; return "Você deu a assistência para o último gol da lenda. Momento histórico."; }),
                Opt("Poupar o corpo", x => { x.S.player.energy += 5; return "Você mandou um vídeo de homenagem."; }))),

            (g => g.S.activeSponsors.Count > 0 || g.S.player.fame >= 25, .4, g => {
                var cores = Rng.Pick(new[] { ("#F2C230", "#111111"), ("#19E68C", "#0A0F1E"), ("#FF5468", "#FFFFFF"), ("#2BD9FE", "#111111"), ("#FFFFFF", "#D4AF37") });
                return Ev("Chuteira exclusiva", "A marca de material esportivo quer lançar uma chuteira com o seu nome, numa cor exclusiva.",
                    Opt("Aprovar o modelo", x => { var p = x.S.player; p.boot.c1 = cores.Item1; p.boot.c2 = cores.Item2; p.fame += 4; p.money += 30000;
                        return "Sua chuteira exclusiva esgotou em um dia. Ela já está nos seus pés."; }),
                    Opt("Manter a de sempre", x => "Você é fiel à chuteira de sempre.")); }),

            (g => g.S.season.week >= 8 && g.S.player.energy < 45, .6, g => Ev("Cansaço acumulado", "O preparador físico mostra que você está no limite. Ele sugere um tratamento intensivo.",
                Opt("Fazer o tratamento", x => { var p = x.S.player; p.energy += 25; p.money -= 8000; return "Câmara hiperbárica, crioterapia e sono. Novo em folha."; }),
                Opt("Seguir no sacrifício", x => { var p = x.S.player;
                    if (Rng.Chance(.35)) { p.injury = Rng.RangeInt(1, 3); return $"O corpo cobrou: lesão por fadiga, {p.injury} rodada(s) fora."; }
                    return "Você aguentou. Por enquanto."; }))),

            (g => g.S.career.Count >= 2, .35, g => {
                var old = g.S.career[Rng.RangeInt(0, g.S.career.Count - 2)].club;
                return Ev("Homenagem do ex-clube", $"O {old} vai fazer uma homenagem para você no próximo jogo deles.",
                    Opt("Ir ao estádio receber", x => { var p = x.S.player; p.moral += 8; p.fame += 2; p.energy -= 5; return $"Aplaudido de pé no estádio do {old}. Arrepiou."; }),
                    Opt("Mandar um vídeo", x => { x.S.player.moral += 3; return "Seu vídeo emocionou a antiga torcida."; })); }),
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
