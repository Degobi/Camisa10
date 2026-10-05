using System;
using System.Collections.Generic;
using System.Linq;

namespace Camisa10.Core
{
    public class PressAnswer { public string Label; public Func<Game, string> Apply; }
    public class PressQuestion { public string Reporter, Text; public PressAnswer[] Answers; }

    /// <summary>
    /// Entrevista coletiva depois do jogo (opcional), no estilo I Am Playr: três perguntas sobre o jogo, a sua atuação
    /// e os bastidores. Cada resposta mexe em moral, fama e na reputação com técnico, elenco e torcida.
    /// </summary>
    public static class PressConference
    {
        static PressAnswer A(string label, Func<Game, string> apply) => new PressAnswer { Label = label, Apply = apply };
        static PressQuestion Q(string text, params PressAnswer[] a) => new PressQuestion { Reporter = Rng.Pick(InboxData.Press), Text = text, Answers = a };

        public static List<PressQuestion> Build(Game g, MatchEngine m)
        {
            var list = new List<PressQuestion>();
            var p = g.S.player;
            bool derby = m.My != null && m.Opp != null && Game.IsDerby(m.My.name, m.Opp.name);

            // 1) o resultado
            if (derby && m.Result == 'w')
                list.Add(Q($"Vitória no clássico contra o {m.Opp.name}! Qual é o recado para a torcida rival?",
                    A("\"O clássico é nosso. Podem ir se acostumando.\"", x => { x.AddFans(8); x.S.player.fame += 2; x.S.player.coach -= 2; return "A torcida enlouqueceu com a provocação. O técnico pediu mais cuidado."; }),
                    A("\"Respeito ao adversário. Hoje fomos melhores.\"", x => { x.AddFans(3); x.S.player.coach += 2; return "Resposta elegante. A diretoria aprovou."; })));
            else if (m.Result == 'w' && m.Goals > 0)
                list.Add(Q($"Você marcou {(m.Goals == 1 ? "o gol" : m.Goals + " gols")} na vitória. A quem você dedica?",
                    A("Aos companheiros: sem eles eu não faço nada", x => { x.AddSquad(5); x.S.player.fame += .8f; return "O vestiário adorou a humildade."; }),
                    A("À torcida, que empurrou o tempo todo", x => { x.AddFans(5); x.S.player.fame += .8f; return "A arquibancada cantou o seu nome na saída."; }),
                    A("A mim mesmo: trabalhei muito por isso", x => { x.S.player.fame += 2.5f; x.S.player.moral += 3; x.AddSquad(-3); return "A frase virou manchete. Alguns companheiros torceram o nariz."; })));
            else if (m.Result == 'w')
                list.Add(Q("Vitória importante. O que fez a diferença hoje?",
                    A("A entrega do grupo inteiro", x => { x.AddSquad(4); return "Discurso de time. O grupo gostou."; }),
                    A("O plano do técnico funcionou", x => { x.S.player.coach += 4; return "O técnico agradeceu publicamente a confiança."; }),
                    A("A torcida jogou junto", x => { x.AddFans(4); return "A torcida retribuiu o carinho nas redes."; })));
            else if (m.Result == 'l')
                list.Add(Q($"Derrota para o {m.Opp.name}. O que faltou?",
                    A("Eu assumo a minha parte da responsabilidade", x => { x.AddSquad(4); x.AddFans(3); x.S.player.coach += 2; return "Postura de líder. Torcida e elenco respeitaram."; }),
                    A("Faltou atitude de alguns jogadores", x => { x.AddSquad(-7); x.AddFans(3); x.S.player.fame += 2; return "A declaração rachou o vestiário, mas parte da torcida concordou."; }),
                    A("Vamos analisar com calma e corrigir", x => { x.S.player.coach += 2; return "Resposta protocolar. Ninguém se incomodou."; })));
            else
                list.Add(Q("Empate fora do que se esperava. O resultado foi justo?",
                    A("Merecíamos mais. Faltou capricho na frente", x => { x.S.player.moral += 2; x.AddFans(2); return "A torcida concordou com a análise."; }),
                    A("Um ponto também conta. Vamos somando", x => { x.S.player.coach += 2; return "O técnico gostou da tranquilidade."; }),
                    A("A arbitragem atrapalhou o jogo", x => { x.AddFans(3); x.S.player.fame += 1; x.S.player.coach -= 2; if (Rng.Chance(.35)) { x.S.player.money -= Game.R1000(Math.Max(3000, x.S.contract.salary * .5)); return "O comitê disciplinar te multou pela declaração."; } return "A reclamação repercutiu, mas ficou por isso mesmo."; })));

            // 2) a sua atuação
            var personal = new List<PressQuestion>();
            if (m.Plays && m.Rating <= 5.8f)
                personal.Add(Q($"Sua atuação foi muito criticada (nota {Fmt.Rating(m.Rating)}). O que aconteceu?",
                    A("Dia ruim. Vou trabalhar para melhorar", x => { x.S.player.coach += 3; x.AddFans(1); return "Humildade. A cobrança diminuiu."; }),
                    A("Não concordo com as críticas", x => { x.S.player.moral += 2; x.S.player.coach -= 3; x.AddFans(-3); return "A resposta não pegou bem na arquibancada."; }),
                    A("Estou jogando fora da minha posição", x => { x.S.player.coach -= 7; x.S.player.fame += 1; return "O técnico não gostou nada de ler isso."; })));
            if (m.Plays && m.Rating >= 8.2f)
                personal.Add(Q($"Nota {Fmt.Rating(m.Rating)}: é o seu melhor momento na carreira?",
                    A("Estou só começando", x => { x.S.player.fame += 2; x.S.player.moral += 3; return "Confiança lá em cima. As redes adoraram."; }),
                    A("O mérito é do trabalho no dia a dia", x => { x.S.player.coach += 3; x.AddSquad(2); return "O técnico destacou o seu profissionalismo."; })));
            if (!m.Plays && m.Role == Role.Reserva)
                personal.Add(Q("Você nem saiu do banco hoje. Está insatisfeito?",
                    A("Respeito a decisão. Vou continuar trabalhando", x => { x.S.player.coach += 5; return "\"É assim que se ganha espaço\", disse o técnico."; }),
                    A("Todo jogador quer jogar. Estou incomodado", x => { x.S.player.coach -= 6; x.AddFans(2); x.S.player.fame += 1.5f; return "A declaração esquentou o clima com o técnico."; })));
            if (!string.IsNullOrEmpty(g.S.rival.name) && m.Opp != null && m.Opp.id == g.S.rival.club)
                personal.Add(Q($"Duelo particular com {g.S.rival.name}. Quem levou a melhor?",
                    A("O placar responde", x => { x.S.rival.heat += 10; x.AddFans(4); x.S.player.fame += 1.5f; return "Provocação registrada. A rivalidade esquentou."; }),
                    A("Ele é um grande jogador. Respeito", x => { x.S.rival.heat -= 8; x.S.player.fame += 1; return "Classe. A imprensa elogiou o fair play."; })));
            if (personal.Count == 0)
                personal.Add(Q("Como você avalia o seu jogo hoje?",
                    A("Dá para melhorar. Sou exigente comigo", x => { x.S.player.coach += 2; return "Autocrítica sempre agrada o técnico."; }),
                    A("Fiz o que o técnico pediu", x => { x.S.player.coach += 1; x.AddSquad(1); return "Resposta segura."; }),
                    A("Joguei bem, mas ninguém fala disso", x => { x.S.player.fame += 1.5f; x.AddSquad(-2); return "Virou meme: \"ninguém fala disso\"."; })));
            list.Add(Rng.Pick(personal));

            // 3) bastidores
            var off = new List<PressQuestion>();
            if (p.fame >= 25)
                off.Add(Q("Há rumores de interesse de clubes de fora. Você fica?",
                    A($"Estou muito feliz no {g.MyClub.name}", x => { x.AddFans(6); x.S.player.coach += 2; return "A torcida estendeu faixa de agradecimento."; }),
                    A("No futebol, nunca se sabe", x => { x.AddFans(-5); x.S.player.fame += 2; x.S.season.forceExit = true; return "O mercado anotou. A torcida, também."; }),
                    A("Quem cuida disso é o meu empresário", x => { x.S.player.fame += .8f; return "Resposta neutra. O assunto morreu."; })));
            if (g.S.captain)
                off.Add(Q("Como capitão, qual é o recado para o torcedor?",
                    A("Confiem no grupo. Vamos dar a volta por cima", x => { x.AddFans(4); x.AddSquad(3); return "Discurso de capitão. Torcida e elenco aplaudiram."; }),
                    A("Cobrem a gente, mas não deixem de apoiar", x => { x.AddFans(3); return "A organizada respondeu com apoio."; })));
            if (p.coach < 40)
                off.Add(Q("Sua relação com o técnico está desgastada?",
                    A("Não. Temos uma relação profissional", x => { x.S.player.coach += 5; return "O técnico gostou de ver você apagando o incêndio."; }),
                    A("Prefiro não comentar", x => { x.S.player.coach -= 2; x.S.player.fame += 1; return "O \"prefiro não comentar\" virou manchete."; })));
            if (g.S.love.stage >= 2)
                off.Add(Q($"E {g.S.love.name}, estava na arquibancada hoje?",
                    A("Estava, e o gol (ou a vontade) foi para ela", x => { x.S.love.affection += 6; x.S.player.fame += .5f; return "Ela compartilhou o vídeo com um coração."; }),
                    A("Vida pessoal fica fora da coletiva", x => { x.S.player.coach += 1; return "Os repórteres mudaram de assunto."; })));
            off.Add(Q("Qual é a meta para o resto da temporada?",
                A("Título. Não jogo para ser vice", x => { x.AddFans(4); x.S.player.fame += 1; x.S.player.moral += 2; return "A ambição empolgou a torcida."; }),
                A("Pensar jogo a jogo", x => { x.S.player.coach += 2; return "Clássico \"jogo a jogo\". O técnico assina embaixo."; }),
                A("Bater os meus objetivos individuais", x => { x.S.player.fame += 1; x.AddSquad(-2); return "Alguns companheiros acharam a resposta egoísta."; })));
            list.Add(Rng.Pick(off));
            return list;
        }

        /// <summary>Aplica a resposta, normaliza e registra nas notícias.</summary>
        public static string Answer(Game g, PressQuestion q, int i)
        {
            string r = q.Answers[i].Apply(g);
            g.Normalize();
            return r;
        }
    }
}
