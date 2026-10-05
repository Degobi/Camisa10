using System;
using System.Collections.Generic;
using System.Linq;

namespace Camisa10.Core
{
    /// <summary>Mensagem da caixa de entrada (técnico, empresário, diretoria, patrocinador, namorada, elenco, imprensa...).</summary>
    [Serializable]
    public class InboxMsg
    {
        public string id, role, from, subject, body;
        public string club;              // clube de quem escreve (escudo no avatar), quando houver
        public int year, week;           // quando chegou (rodada contada a partir de 1)
        public bool read;
        public string kind, data;        // tipo da resposta e parâmetro (id da proposta, valor...) para o tratador
        public List<string> options = new List<string>();
        public string answer = "";       // resultado da resposta escolhida ("" = ainda sem resposta)
        public bool NeedsReply => options != null && options.Count > 0 && string.IsNullOrEmpty(answer);
    }

    /// <summary>Nomes fictícios de quem escreve para você (empresário, imprensa).</summary>
    public static class InboxData
    {
        public static readonly string[] Agents = { "Márcio Teixeira", "Renato Albuquerque", "Paulo Vilela", "Sérgio Cardoso", "Alexandre Lins" };
        public static readonly string[] Press = { "Jornal do Gol", "Bola TV", "Rádio Arquibancada", "Portal Camisa 10", "Revista Placar Final" };

        /// <summary>Título curto do remetente, mostrado embaixo do nome.</summary>
        public static string RoleName(string role)
        {
            switch (role)
            {
                case "tecnico": return "Técnico";
                case "empresario": return "Empresário";
                case "diretoria": return "Diretoria";
                case "patrocinador": return "Patrocinador";
                case "namorada": return "Vida pessoal";
                case "elenco": return "Companheiro de time";
                case "imprensa": return "Imprensa";
                case "torcida": return "Torcida";
                case "selecao": return "Seleção Brasileira";
                case "medico": return "Departamento médico";
                case "rival": return "Rival";
                default: return "Mensagem";
            }
        }
    }

    /// <summary>
    /// Caixa de mensagens: o canal da história da carreira. As mensagens chegam na virada da rodada e nos acontecimentos
    /// (propostas, metas de patrocínio, lesões, convocações, rival, namorada), e algumas pedem resposta com efeito.
    /// </summary>
    public partial class Game
    {
        const int InboxMax = 60;

        void EnsureInbox()
        {
            if (S.inbox == null) S.inbox = new List<InboxMsg>();
            if (string.IsNullOrEmpty(S.agent)) S.agent = Rng.Pick(InboxData.Agents);
        }

        public string CoachName => "Técnico · " + (MyClub?.name ?? "clube");
        public string BoardName => "Diretoria · " + (MyClub?.name ?? "clube");
        public string AgentName => S.agent + " · empresário";
        public int UnreadCount => S.inbox.Count(x => !x.read);
        public int PendingReplies => S.inbox.Count(x => x.NeedsReply);

        /// <summary>Entrega uma mensagem. Com opções, ela espera resposta (tratada em <see cref="Reply"/> pelo kind).</summary>
        public InboxMsg Mail(string role, string from, string subject, string body, string kind = null, string data = null, params string[] options)
        {
            var m = new InboxMsg
            {
                id = NewId(), role = role, from = from, subject = subject, body = body, kind = kind, data = data,
                year = S.year, week = Math.Min(SeasonRounds, S.season.week + 1), options = new List<string>(options ?? new string[0]),
                club = role == "rival" ? S.rival.club : role == "tecnico" || role == "diretoria" || role == "torcida" || role == "medico" ? S.contract.club : null,
            };
            S.inbox.Insert(0, m);
            S.mailsThisRound++;
            // caixa cheia: some primeiro o que já foi lido e não espera resposta
            while (S.inbox.Count > InboxMax)
            {
                int i = S.inbox.FindLastIndex(x => x.read && !x.NeedsReply);
                S.inbox.RemoveAt(i >= 0 ? i : S.inbox.Count - 1);
            }
            return m;
        }

        public void MarkRead(string id) { var m = S.inbox.Find(x => x.id == id); if (m != null) m.read = true; }
        public void MarkAllRead() { foreach (var m in S.inbox) m.read = true; }

        /// <summary>Responde uma mensagem e devolve o texto do que aconteceu.</summary>
        public string Reply(string id, int option)
        {
            var m = S.inbox.Find(x => x.id == id);
            if (m == null || !m.NeedsReply || option < 0 || option >= m.options.Count) return null;
            m.read = true;
            string res = Replies.TryGetValue(m.kind ?? "", out var h) ? h(this, m, option) : "Mensagem respondida.";
            m.answer = $"Você: \"{m.options[option]}\". {res}";
            Normalize();
            if (S.season.phase == "match") S.season.role = ComputeRole();
            return res;
        }

        /// <summary>Respostas com efeito, pelo tipo da mensagem (o save guarda só o tipo; a regra fica aqui).</summary>
        static readonly Dictionary<string, Func<Game, InboxMsg, int, string>> Replies = new Dictionary<string, Func<Game, InboxMsg, int, string>>
        {
            ["boas_vindas"] = (g, m, o) =>
            {
                var p = g.S.player;
                if (o == 0) { p.coach += 3; return "O técnico gostou da postura. Começo com o pé direito."; }
                if (Rng.Chance(.5)) { p.coach += 5; p.moral += 3; return "Ele gostou da ambição: \"É disso que eu preciso\"."; }
                p.coach -= 4; return "Ele respondeu seco: \"Lugar no time se ganha no treino\".";
            },
            ["cobranca"] = (g, m, o) =>
            {
                var p = g.S.player;
                if (o == 0) { p.coach += 4; p.moral += 1; return "O técnico confia na sua resposta. Agora é mostrar em campo."; }
                p.coach -= 6; p.moral += 2; g.AddSquad(-2); return "A conversa esquentou. O clima com o técnico pesou.";
            },
            ["churrasco"] = (g, m, o) =>
            {
                var p = g.S.player;
                if (o == 0) { g.AddSquad(7); p.moral += 4; p.energy -= 4; return "Resenha boa até tarde. O grupo está mais unido."; }
                g.AddSquad(-3); return "Sentiram sua falta. Teve gente comentando no grupo do time.";
            },
            ["conselho"] = (g, m, o) =>
            {
                if (o == 0) { g.AddSquad(6); g.S.player.moral += 2; g.S.player.energy -= 3; return "Vocês treinaram finalização juntos depois do horário. O garoto te vê como referência."; }
                g.AddSquad(-2); return "Ele entendeu, mas ficou chateado.";
            },
            ["torcida_visita"] = (g, m, o) =>
            {
                var p = g.S.player;
                if (o == 0) { g.AddFans(8); p.energy -= 6; p.moral += 3; return "Fotos, autógrafos e um bandeirão com o seu rosto. A torcida adorou."; }
                if (o == 1) { long c = R100(Math.Max(1500, g.S.contract.salary * .4)); p.money -= c; g.AddFans(4); return $"As camisas autografadas ({Fmt.Money(c)}) foram sorteadas entre os sócios."; }
                g.AddFans(-4); return "A organizada não gostou do silêncio.";
            },
            ["entrevista"] = (g, m, o) =>
            {
                var p = g.S.player;
                if (o == 0) { p.fame += (float)(2.5 * (1 - p.fame / 120.0)); p.energy -= 4; g.AddFans(2); return "A entrevista exclusiva rendeu manchete positiva."; }
                return "Você preferiu falar só dentro de campo.";
            },
            ["patrocinador_post"] = (g, m, o) =>
            {
                long fee; long.TryParse(m.data, out fee);
                if (o == 0) { g.S.player.money += fee; g.S.player.fame += .6f; return $"Post publicado. A marca pagou {Fmt.Money(fee)}."; }
                return "A marca vai procurar você de novo mais para frente.";
            },
            ["namorada"] = (g, m, o) =>
            {
                var l = g.S.love; var p = g.S.player;
                if (l.stage == 0) return "Ela já não está mais na sua vida.";
                if (o == 0) { long c = g.ActionCost("encontro"); p.money -= c; l.affection += 14; p.moral += 3; return $"Jantar a dois ({Fmt.Money(c)}). Ela ficou feliz com o convite."; }
                l.affection -= 7; return "Ela respondeu só com \"ok\". Melhor não deixar esfriar.";
            },
            ["rival"] = (g, m, o) =>
            {
                var p = g.S.player; var r = g.S.rival;
                if (o == 0)
                {
                    r.heat += 12; g.AddFans(3);
                    if (Rng.Chance(.6)) { p.fame += 2; p.moral += 2; return "Sua resposta viralizou. A torcida comprou a briga."; }
                    p.coach -= 4; return "A resposta pegou mal e o clube pediu silêncio.";
                }
                p.coach += 2; r.heat -= 4; return "\"Falo dentro de campo.\" A imprensa elogiou a postura.";
            },
            ["janela"] = (g, m, o) => g.WindowReply(m, o),
            ["rumor"] = (g, m, o) =>
            {
                if (o == 0) { g.AddFans(4); g.S.player.coach += 2; g.S.season.rumor = null; return "A torcida aplaudiu. O assunto esfriou."; }
                g.AddFans(-4); g.S.player.fame += 1.5f; g.S.player.coach -= 3; return "A frase rodou o mundo. O clube interessado vai se mexer na janela.";
            },
            ["capitao"] = (g, m, o) =>
            {
                var p = g.S.player;
                if (o == 0) { g.S.captain = true; p.moral += 10; p.fame += 2; g.AddSquad(4); g.AddFans(4); g.Unlock("capitao"); g.AddNews($"Você é o novo capitão do {g.MyClub.name}."); return "A braçadeira é sua. O grupo aplaudiu no vestiário."; }
                g.S.captainOffered = false; p.coach -= 2; return "O técnico respeitou, mas vai esperar você se sentir pronto.";
            },
        };

        // ---------- geração na virada da rodada ----------

        /// <summary>Mensagens da rodada: o técnico comenta o jogo e a vida chama (elenco, torcida, imprensa, marca, namorada, rival).</summary>
        void InboxWeek(MatchEngine m, bool wasInjured)
        {
            var p = S.player; var se = S.season;
            // lesão nova: o departamento médico avisa
            if (!wasInjured && p.injury > 0)
                Mail("medico", "Departamento médico · " + MyClub.name, "Resultado dos exames",
                    $"Os exames confirmaram a lesão. Previsão de {p.injury} rodada(s) fora. Siga o protocolo de fisioterapia e não force: voltar antes da hora pode piorar.");

            // o técnico comenta atuações que chamam atenção
            if (m != null && m.Plays)
            {
                if (m.Rating >= 8.3f && Rng.Chance(.45))
                    Mail("tecnico", CoachName, "Grande jogo", Rng.Pick(new[]
                    {
                        $"Que atuação contra o {m.Opp.name}! Nota {Fmt.Rating(m.Rating)}. Continue assim e o seu lugar no time está garantido.",
                        $"Revi o jogo contra o {m.Opp.name} duas vezes. Você foi decisivo. Parabéns, mas amanhã tem treino às 9h.",
                    }));
                else if (m.Rating <= 5.6f && Rng.Chance(.55))
                    Mail("tecnico", CoachName, "Precisamos conversar",
                        $"Não gostei do que vi contra o {m.Opp.name} (nota {Fmt.Rating(m.Rating)}). Espero muito mais de você. O que está acontecendo?",
                        "cobranca", null, "Vou dar a volta por cima, professor", "Estou jogando fora de posição");
            }
            else if (m != null && m.Role == Role.Reserva && se.lastRole == Role.Reserva && Rng.Chance(.3))
                Mail("tecnico", CoachName, "Banco de reservas",
                    "Sei que você quer jogar. Treine forte, cuide do físico e da forma: quando a chance vier, ela vai vir para quem estiver pronto.");

            if (S.mailsThisRound >= 2) return;
            // a vida chama: no máximo uma mensagem social por rodada
            var pool = new List<(Action send, double w)>();
            var mate = Teammate();
            pool.Add((() => Mail("elenco", mate, "Churrasco no sábado", $"Fala, {FirstName}! Vai ter churrasco lá em casa depois do jogo, a resenha está garantida. Cola?",
                "churrasco", null, "Tô dentro!", "Dessa vez não vai dar"), .9));
            if (p.age >= 24)
                pool.Add((() => Mail("elenco", Teammate() + " (base)", "Me dá umas dicas?", "Sou da base e subi agora para o profissional. Você topa me ajudar com finalização depois do treino? Seria uma honra.",
                    "conselho", null, "Bora treinar junto", "Agora não tenho tempo"), .5));
            pool.Add((() => Mail("torcida", "Torcida organizada · " + MyClub.name, "Convite da torcida",
                "A torcida quer homenagear os jogadores na sede no fim de semana. Seria muito importante a sua presença.",
                "torcida_visita", null, "Ir à sede", "Mandar camisas autografadas", "Não vou poder"), p.fans >= 40 ? .6 : .9));
            if (p.fame >= 10)
                pool.Add((() => Mail("imprensa", Rng.Pick(InboxData.Press), "Pedido de entrevista exclusiva",
                    $"Gostaríamos de uma entrevista exclusiva com você sobre a temporada do {MyClub.name}. Uns 20 minutos, depois do treino.",
                    "entrevista", null, "Aceitar", "Recusar"), .6));
            if (S.activeSponsors.Count > 0)
            {
                var d = S.activeSponsors[Rng.RangeInt(0, S.activeSponsors.Count - 1)];
                long fee = R100(300 + p.fame * 160);
                pool.Add((() => Mail("patrocinador", d.brand, "Campanha nas redes", $"Lançamos uma campanha nova e queremos um post seu. Cachê de {Fmt.Money(fee)}.",
                    "patrocinador_post", fee.ToString(), "Postar", "Agora não"), .6));
            }
            if (S.love.stage >= 1 && S.love.affection < 50)
                pool.Add((() => Mail("namorada", S.love.name, "Saudade", Rng.Pick(new[] { "Você anda sumido... Quando a gente vai se ver com calma?", "Vi o jogo. Você está bem? Quase não conversamos essa semana." }),
                    "namorada", null, "Marcar um jantar a dois", "Te ligo depois do jogo"), 1.4));
            if (!string.IsNullOrEmpty(S.rival.name) && RivalClub != null && S.rival.heat >= 35)
                pool.Add((() => Mail("rival", S.rival.name + " · " + RivalClub.name, "Marcou você numa publicação",
                    $"\"Tem muita gente falando de {FirstName}, mas números são números. {S.rival.goals} gols na temporada e contando.\"",
                    "rival", null, "Responder à altura", "Ignorar e responder em campo"), .5));
            if (Rng.Chance(.3)) Rng.Weighted(pool)();
        }

        string FirstName => (S.player.name ?? "craque").Split(' ')[0];

        /// <summary>Um companheiro de verdade do elenco (ou um nome genérico quando o clube não tem elenco cadastrado).</summary>
        string Teammate()
        {
            var c = MyClub;
            if (c != null && GameData.Squads.TryGetValue(c.name, out var s) && s.Length > 1) return s[Rng.RangeInt(1, s.Length - 1)];
            return Rng.Pick(new[] { "Lucas", "Matheus", "Diego", "Rafael" });
        }

        /// <summary>Boas-vindas de temporada nova: o técnico fala do seu papel e a diretoria lembra dos objetivos.</summary>
        void InboxSeasonStart(bool newCareer)
        {
            var c = MyClub;
            var role = ComputeRole();
            string expect = role == Role.Estrela ? "Você chega como peça-chave do time." : role == Role.Titular ? "Conto com você no time titular." : "Você começa como opção no banco, mas o espaço é de quem treinar mais.";
            Mail("tecnico", CoachName, newCareer ? $"Bem-vindo ao {c.name}" : $"Temporada {S.year}",
                $"{expect} Quero dedicação nos treinos, foco nos jogos e cabeça no lugar fora de campo. Qual é a sua meta para esta temporada?",
                "boas_vindas", null, "Dar o meu máximo pelo grupo", "Quero ser titular absoluto");
            if (S.season.objectives != null && S.season.objectives.Count > 0)
                Mail("diretoria", BoardName, $"Objetivos da temporada {S.year}", ObjectivesLetter());
        }
    }
}
