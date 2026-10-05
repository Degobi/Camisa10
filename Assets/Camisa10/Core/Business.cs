using System;
using System.Collections.Generic;
using System.Linq;

namespace Camisa10.Core
{
    /// <summary>
    /// Conteúdo fixo da agenda da semana e dos negócios no futebol (bolsa e empreendimentos).
    /// Tudo fictício: empresas, marcas e clubes não existem.
    /// </summary>
    public static class BusinessData
    {
        public const int ActionsPerWeek = 3;
        public const int LotSize = 100;   // ações são negociadas em lotes
        public const int MaxVentureLevel = 3;

        // ---------- agenda ----------
        public class ActionDef
        {
            public string Id, Name, Hint, Icon;
            public float Energy; // custo (negativo) ou ganho de energia
            public Func<Game, bool> Available;
        }

        public static readonly ActionDef[] Actions =
        {
            new ActionDef { Id = "extra", Icon = "TRE", Name = "Treino extra", Energy = -10, Hint = "Evolui o seu ponto fraco. Cansa.", Available = g => g.S.player.injury == 0 },
            new ActionDef { Id = "ct", Icon = "REC", Name = "Recuperação no CT", Energy = 14, Hint = "Fisioterapia e gelo. Recupera energia." },
            new ActionDef { Id = "tecnico", Icon = "TEC", Name = "Conversa com o técnico", Energy = 0, Hint = "Melhora a relação com o técnico." },
            new ActionDef { Id = "video", Icon = "VID", Name = "Análise de vídeo", Energy = -2, Hint = "Estuda o adversário. Melhora a forma e agrada o técnico." },
            new ActionDef { Id = "imprensa", Icon = "IMP", Name = "Coletiva de imprensa", Energy = -3, Hint = "Ganha fama. Uma resposta atravessada pode virar polêmica." },
            new ActionDef { Id = "redes", Icon = "WEB", Name = "Post nas redes", Energy = 0, Hint = "Ganha seguidores. Com patrocinador, rende um extra." },
            new ActionDef { Id = "evento", Icon = "PAT", Name = "Evento do patrocinador", Energy = -6, Hint = "Cachê e fama. Precisa de um patrocínio ativo.", Available = g => g.S.activeSponsors.Count > 0 },
            new ActionDef { Id = "social", Icon = "SOC", Name = "Ação social", Energy = -4, Hint = "Aumenta moral e fama. A torcida repara." },
            new ActionDef { Id = "resenha", Icon = "ELE", Name = "Resenha com o elenco", Energy = -4, Hint = "Videogame, churrasco, pagode. Une o vestiário com você." },
            new ActionDef { Id = "torcida", Icon = "TOR", Name = "Atender torcedores", Energy = -3, Hint = "Autógrafos e fotos na saída do CT. A torcida adora." },
            new ActionDef { Id = "balada", Icon = "NOI", Name = "Noite com os amigos", Energy = -12, Hint = "Moral lá em cima, mas o técnico pode descobrir." },
            new ActionDef { Id = "empresario", Icon = "EMP", Name = "Reunião com o empresário", Energy = 0, Hint = "Divulga seu nome no mercado. Atrai propostas, irrita a diretoria.", Available = g => g.S.player.fame >= 15 && !g.S.season.forceExit },
            // vida pessoal
            new ActionDef { Id = "encontro", Icon = "AMO", Name = "Jantar a dois", Energy = -3, Hint = "Cuida do relacionamento. Custa um jantar caprichado.", Available = g => g.S.love.stage >= 1 },
            new ActionDef { Id = "paquera", Icon = "SAI", Name = "Sair para conhecer gente", Energy = -8, Hint = "Solteiro? Pode conhecer alguém. Pode render fofoca também.", Available = g => g.S.love.stage == 0 },
            new ActionDef { Id = "familia", Icon = "FAM", Name = "Visitar a família", Energy = 6, Hint = "Comida de mãe e descanso. Moral e energia." },
            new ActionDef { Id = "fisio", Icon = "FIS", Name = "Fisioterapeuta particular", Energy = 22, Hint = "Recupera bem mais que o CT, mas é pago do seu bolso." },
            new ActionDef { Id = "podcast", Icon = "POD", Name = "Podcast de futebol", Energy = -3, Hint = "Cachê e fama. Fale demais e vira polêmica.", Available = g => g.S.player.fame >= 12 },
            new ActionDef { Id = "rival", Icon = "RIV", Name = "Alfinetar o rival", Energy = 0, Hint = "Provoca seu rival nas redes. A torcida adora; o clube, nem tanto.", Available = g => !string.IsNullOrEmpty(g.S.rival.name) },
        };

        public static ActionDef Action(string id) => Array.Find(Actions, a => a.Id == id);

        // ---------- bolsa ----------
        public class StockDef
        {
            public string Id, Name, Sector, ClubName; // ClubName: SAF ligada ao desempenho de um clube
            public long BaseCents;
            public double Vol, Drift;
        }

        public static readonly StockDef[] Stocks =
        {
            new StockDef { Id = "BOTA3", Name = "Botafogo SAF", Sector = "Clube", ClubName = "Botafogo", BaseCents = 1850, Vol = .05, Drift = .002 },
            new StockDef { Id = "CRUZ3", Name = "Cruzeiro SAF", Sector = "Clube", ClubName = "Cruzeiro", BaseCents = 1620, Vol = .05, Drift = .002 },
            new StockDef { Id = "VASC3", Name = "Vasco da Gama SAF", Sector = "Clube", ClubName = "Vasco da Gama", BaseCents = 1180, Vol = .055, Drift = .002 },
            new StockDef { Id = "VOLT3", Name = "Volt Esportes", Sector = "Material esportivo", BaseCents = 3260, Vol = .035, Drift = .003 },
            new StockDef { Id = "ARNA3", Name = "Arena Brasil Eventos", Sector = "Estádios e eventos", BaseCents = 1190, Vol = .045, Drift = .002 },
            new StockDef { Id = "BOLA3", Name = "Bola TV Mídia", Sector = "Transmissão", BaseCents = 2140, Vol = .055, Drift = .001 },
            new StockDef { Id = "PXFG3", Name = "PixelForge Games", Sector = "Games de futebol", BaseCents = 4780, Vol = .09, Drift = .004 },
        };

        public static StockDef Stock(string id) => Array.Find(Stocks, s => s.Id == id);

        // ---------- empreendimentos ----------
        public class VentureDef
        {
            public string Id, Name, Cat, Hint;
            public long Price, Weekly;
            public int MinFame;
            public float Fame, Moral;
            public double Risk; // variação da receita semanal
        }

        public static readonly VentureDef[] Ventures =
        {
            new VentureDef { Id = "society", Cat = "Esporte", Name = "Quadra de futebol society", Price = 60000, Weekly = 420, Fame = 0, Moral = 2, Risk = .2,
                Hint = "Aluguel de quadra por hora. Receita estável." },
            new VentureDef { Id = "canal", Cat = "Mídia", Name = "Canal de futebol na internet", Price = 90000, Weekly = 300, Fame = 2, Moral = 2, Risk = .5,
                Hint = "Rende mais quanto maior a sua fama." },
            new VentureDef { Id = "loja", Cat = "Varejo", Name = "Loja de artigos esportivos", Price = 180000, Weekly = 1100, Fame = 1, Moral = 2, Risk = .3,
                Hint = "Camisas, chuteiras e bolas no centro da cidade." },
            new VentureDef { Id = "escolinha", Cat = "Base", Name = "Escolinha de futebol com seu nome", Price = 450000, Weekly = 2500, MinFame = 10, Fame = 6, Moral = 5, Risk = .15,
                Hint = "Forma talentos e melhora sua imagem." },
            new VentureDef { Id = "agencia", Cat = "Gestão", Name = "Agência de jogadores", Price = 1500000, Weekly = 9000, MinFame = 35, Fame = 3, Moral = 2, Risk = .6,
                Hint = "Comissão sobre transferências. Semanas boas e ruins." },
            new VentureDef { Id = "clube", Cat = "Clube", Name = "Clube da quarta divisão (SAF)", Price = 9000000, Weekly = 52000, MinFame = 55, Fame = 8, Moral = 6, Risk = .8,
                Hint = "Você vira dono de clube. Alto risco, alto retorno." },
        };

        public static VentureDef VentureById(string id) => Array.Find(Ventures, v => v.Id == id);
    }

    /// <summary>Agenda da semana e negócios do jogador (no espírito do I Am Playr).</summary>
    public partial class Game
    {
        // ---------- inicialização e saves antigos ----------
        public void EnsureBusiness()
        {
            if (S.quotes == null) S.quotes = new List<StockQuote>();
            if (S.holdings == null) S.holdings = new List<Holding>();
            if (S.ventures == null) S.ventures = new List<Venture>();
            if (S.season.doneActions == null) S.season.doneActions = new List<string>();
            // ações que saíram da bolsa (saves antigos): vende pelo último preço e devolve o dinheiro
            foreach (var h in S.holdings.ToList())
            {
                if (BusinessData.Stock(h.id) != null) continue;
                var old = S.quotes.Find(x => x.id == h.id);
                if (old != null) S.player.money += old.price * h.qty / 100;
                S.holdings.Remove(h);
            }
            S.quotes.RemoveAll(x => BusinessData.Stock(x.id) == null);
            foreach (var d in BusinessData.Stocks)
            {
                if (S.quotes.Exists(x => x.id == d.Id)) continue;
                long start = Math.Max(100, (long)Math.Round(d.BaseCents * Rng.RangeF(.9, 1.1)));
                var q = new StockQuote { id = d.Id, price = start };
                for (int i = 0; i < 8; i++) q.hist.Add(start);
                S.quotes.Add(q);
            }
        }

        // ---------- agenda ----------
        public int ActionsLeft => Math.Max(0, BusinessData.ActionsPerWeek - S.season.actionsUsed);

        public bool CanDoAction(string id, out string why)
        {
            var a = BusinessData.Action(id);
            why = null;
            if (a == null) { why = "Ação desconhecida."; return false; }
            if (S.season.phase == "end") { why = "Temporada encerrada."; return false; }
            if (ActionsLeft <= 0) { why = "Sem horários livres nesta rodada."; return false; }
            if (S.season.doneActions.Contains(id)) { why = "Já feito nesta rodada."; return false; }
            if (a.Available != null && !a.Available(this)) { why = "Indisponível agora."; return false; }
            if (a.Energy < 0 && S.player.energy + a.Energy < 5) { why = "Energia insuficiente."; return false; }
            if (ActionCost(id) > S.player.money) { why = "Sem dinheiro."; return false; }
            return true;
        }

        /// <summary>Quanto a ação custa do seu bolso (0 = de graça).</summary>
        public long ActionCost(string id) => id == "encontro" ? R100(350 + S.contract.salary * .03) : id == "fisio" ? R100(1800 * CostMult) : 0;

        public string DoAction(string id)
        {
            if (!CanDoAction(id, out string why)) return why;
            var a = BusinessData.Action(id);
            var p = S.player; var se = S.season;
            se.actionsUsed++;
            se.doneActions.Add(id);
            p.energy += a.Energy;
            string msg;
            switch (id)
            {
                case "extra":
                {
                    var w = WeakestAttr();
                    p.xp[(int)w] += 6;
                    LevelUp(w);
                    msg = $"Treino extra de {GameData.Label(w).ToLowerInvariant()} feito.";
                    break;
                }
                case "ct":
                    p.moral += 2;
                    msg = "Você recuperou o fôlego no CT.";
                    break;
                case "tecnico":
                    if (p.coach >= 75) { p.coach += 1; msg = "O técnico já confia em você. Conversa rápida."; }
                    else { p.coach += Rng.RangeInt(2, 4); msg = "Boa conversa. O técnico entendeu o que você quer."; }
                    break;
                case "video":
                    p.coach += 2;
                    p.form.Add(Math.Min(10f, FormAvg + .4f)); if (p.form.Count > 5) p.form.RemoveAt(0);
                    msg = "Você estudou o adversário. Está mais preparado para o jogo.";
                    break;
                case "imprensa":
                    p.fame += (float)(2 * (1 - p.fame / 120.0));
                    if (Rng.Chance(.22)) { p.coach -= 4; p.fame += 1; msg = "Uma frase sua virou manchete. A diretoria não gostou."; AddNews("Polêmica na coletiva de imprensa."); }
                    else msg = "Coletiva tranquila. Você ganhou espaço na mídia.";
                    break;
                case "redes":
                {
                    p.fame += (float)(1.2 * (1 - p.fame / 120.0));
                    long extra = S.activeSponsors.Count > 0 ? R100(250 + p.fame * 180) : 0;
                    p.money += extra;
                    msg = extra > 0 ? $"Post publicado. O patrocinador pagou {Fmt.Money(extra)}." : "Post publicado. Mais seguidores.";
                    break;
                }
                case "evento":
                {
                    var d = S.activeSponsors.OrderByDescending(x => x.perSeason).First();
                    long fee = R100(d.perSeason * .04);
                    p.money += fee; p.fame += .8f;
                    msg = $"Evento da {d.brand}: cachê de {Fmt.Money(fee)}.";
                    break;
                }
                case "resenha":
                    AddSquad(5); p.moral += 3;
                    msg = Rng.Chance(.5) ? $"Resenha boa com {Teammate()} e a turma. O vestiário está com você." : "Noite de videogame com o elenco. Você ganhou a final (e o respeito).";
                    break;
                case "torcida":
                    AddFans(4); p.fame += .4f;
                    msg = "Meia hora de autógrafos e selfies na saída do CT. Um garoto chorou ao te abraçar.";
                    break;
                case "social":
                    AddFans(2); p.moral += 4; p.fame += 1.2f;
                    msg = "A ação social emocionou a torcida.";
                    break;
                case "balada":
                    p.moral += 8; AddSquad(1.5f);
                    if (Rng.Chance(.3)) { p.coach -= 6; msg = "Alguém postou foto sua de madrugada. O técnico ficou sabendo."; AddNews("Fotos de madrugada repercutiram mal no clube."); }
                    else msg = "Noite boa e discreta.";
                    break;
                case "empresario":
                    p.coach -= 6; se.forceExit = true; AddFans(-3);
                    msg = "Seu nome está circulando. Mais clubes vão te observar no fim da temporada.";
                    break;
                case "encontro":
                {
                    long cost = ActionCost(id);
                    p.money -= cost; S.love.affection = (float)Clamp(S.love.affection + 14, 0, 100); p.moral += 3;
                    msg = $"Jantar com {S.love.name}. A noite foi ótima ({Fmt.Money(cost)}).";
                    break;
                }
                case "paquera":
                    p.moral += 3;
                    if (Rng.Chance(.4 + p.fame / 250.0))
                    {
                        StartDating();
                        msg = $"Você conheceu {S.love.name}, {S.love.job}. Vocês trocaram telefone.";
                        AddNews($"Você foi visto jantando com {S.love.name}. As redes já especulam.");
                    }
                    else if (Rng.Chance(.2)) { p.coach -= 3; msg = "A noite rendeu fofoca em site de celebridades. O técnico não curtiu."; }
                    else msg = "Noite divertida, mas ninguém especial.";
                    break;
                case "familia":
                    p.moral += 5;
                    msg = "Almoço de domingo com a família. Você voltou renovado.";
                    break;
                case "fisio":
                {
                    long cost = ActionCost(id);
                    p.money -= cost;
                    msg = $"Sessão com o fisioterapeuta particular: {Fmt.Money(cost)}. O corpo agradece.";
                    break;
                }
                case "podcast":
                {
                    long fee = R100(1200 + p.fame * 110);
                    p.money += fee; p.fame += (float)(1.2 * (1 - p.fame / 120.0));
                    if (Rng.Chance(.18)) { p.coach -= 4; msg = $"O corte do podcast viralizou pelo motivo errado. Cachê de {Fmt.Money(fee)}."; AddNews("Declaração em podcast gerou polêmica."); }
                    else msg = $"Papo bom no podcast. Cachê de {Fmt.Money(fee)}.";
                    break;
                }
                case "rival":
                {
                    var r = S.rival;
                    r.heat = (float)Clamp(r.heat + 12, 0, 100); p.fame += 1.2f; p.moral += 2; AddFans(2);
                    if (Rng.Chance(.25 + r.heat / 400.0)) { p.coach -= 3; msg = $"Sua provocação para {r.name} pegou mal no clube. Pediram calma."; }
                    else msg = $"A torcida foi à loucura com a alfinetada em {r.name}.";
                    break;
                }
                default:
                    msg = "Feito.";
                    break;
            }
            Normalize();
            if (se.phase == "match") se.role = ComputeRole();
            return msg;
        }

        void LevelUp(Attr attr)
        {
            var p = S.player; int i = (int)attr;
            for (int g = 0; g < 3; g++)
            {
                double need = 10 + Math.Max(0, p.attrs[i] - 55) * .9;
                if (p.xp[i] < need || p.attrs[i] >= 99) break;
                p.xp[i] -= (float)need; p.attrs[i]++;
            }
        }

        // ---------- bolsa ----------
        public StockQuote Quote(string id) => S.quotes.Find(q => q.id == id);
        public Holding HoldingOf(string id) => S.holdings.Find(h => h.id == id);
        public long PortfolioValue => S.holdings.Sum(h => { var q = Quote(h.id); return q == null ? 0 : q.price * h.qty / 100; });
        public long PortfolioCost => S.holdings.Sum(h => h.cost);
        public long LotPrice(string id) { var q = Quote(id); return q == null ? 0 : q.price * BusinessData.LotSize / 100; }

        /// <summary>Variação em % da última rodada.</summary>
        public double QuoteChange(string id)
        {
            var q = Quote(id);
            if (q == null || q.hist.Count < 2) return 0;
            long prev = q.hist[q.hist.Count - 2];
            return prev == 0 ? 0 : (q.price - prev) * 100.0 / prev;
        }

        public string BuyStock(string id, int lots)
        {
            var q = Quote(id);
            if (q == null || lots <= 0) return null;
            int qty = lots * BusinessData.LotSize;
            long total = q.price * qty / 100;
            if (S.player.money < total) return "Dinheiro insuficiente.";
            S.player.money -= total;
            var h = HoldingOf(id);
            if (h == null) { h = new Holding { id = id }; S.holdings.Add(h); }
            h.qty += qty; h.cost += total;
            return $"Você comprou {qty} ações de {BusinessData.Stock(id).Name} por {Fmt.Money(total)}.";
        }

        public string SellStock(string id, int lots)
        {
            var q = Quote(id); var h = HoldingOf(id);
            if (q == null || h == null || lots <= 0) return null;
            int qty = Math.Min(h.qty, lots * BusinessData.LotSize);
            long total = q.price * qty / 100;
            long costPart = h.qty == 0 ? 0 : h.cost * qty / h.qty;
            S.player.money += total;
            h.qty -= qty; h.cost -= costPart;
            if (h.qty <= 0) S.holdings.Remove(h);
            long result = total - costPart;
            return $"Venda de {qty} ações por {Fmt.Money(total)}. " + (result >= 0 ? $"Lucro de {Fmt.Money(result)}." : $"Prejuízo de {Fmt.Money(-result)}.");
        }

        void TickMarket()
        {
            foreach (var d in BusinessData.Stocks)
            {
                var q = Quote(d.Id);
                if (q == null) continue;
                double move = d.Drift * .5 + Rng.Gauss() * d.Vol * .75; // rodadas mais curtas: menos variação por rodada
                if (d.ClubName != null)
                {
                    // SAF: o preço persegue o desempenho do clube na tabela (quando ele está na sua liga)
                    var club = S.clubs.Find(c => c.name == d.ClubName);
                    var row = club == null ? null : S.season.table.Find(t => t.club == club.id);
                    if (row != null && row.p > 0)
                    {
                        double ppg = row.pts / (double)row.p;
                        double target = d.BaseCents * (.65 + ppg / 3.0 * .9);
                        move += (target - q.price) / q.price * .25;
                    }
                }
                if (d.Id == "VOLT3" && S.player.boot.brand == "Volt") move += S.player.fame / 4000.0; // seu sucesso vende chuteira
                long next = Math.Max(50, (long)Math.Round(q.price * (1 + Clamp(move, -.25, .25))));
                q.price = next;
                q.hist.Add(next);
                if (q.hist.Count > 24) q.hist.RemoveAt(0);
            }
        }

        // ---------- empreendimentos ----------
        public Venture VentureOf(string id) => S.ventures.Find(v => v.id == id);

        public long VentureCost(string id)
        {
            var d = BusinessData.VentureById(id); var v = VentureOf(id);
            if (d == null) return 0;
            return v == null ? d.Price : R1000(d.Price * (v.level + 1) * .75);
        }

        public long VentureWeekly(string id)
        {
            var d = BusinessData.VentureById(id); var v = VentureOf(id);
            if (d == null || v == null) return 0;
            double w = d.Weekly * v.level * (1 + .25 * (v.level - 1));
            if (id == "canal") w *= .5 + S.player.fame / 25.0;
            return R100(w);
        }

        public long VenturesWeekly => S.ventures.Sum(v => VentureWeekly(v.id));
        public long VenturesValue => S.ventures.Sum(v => { var d = BusinessData.VentureById(v.id); return d == null ? 0 : R1000(d.Price * v.level * .7); });

        public bool CanBuyVenture(string id, out string why)
        {
            var d = BusinessData.VentureById(id); var v = VentureOf(id);
            why = null;
            if (d == null) { why = "Negócio desconhecido."; return false; }
            if (v != null && v.level >= BusinessData.MaxVentureLevel) { why = "Nível máximo."; return false; }
            if (S.player.fame < d.MinFame) { why = $"Precisa de fama {d.MinFame}."; return false; }
            if (S.player.money < VentureCost(id)) { why = "Dinheiro insuficiente."; return false; }
            return true;
        }

        public string BuyVenture(string id)
        {
            if (!CanBuyVenture(id, out string why)) return why;
            var d = BusinessData.VentureById(id); var v = VentureOf(id);
            long cost = VentureCost(id);
            S.player.money -= cost;
            if (v == null)
            {
                S.ventures.Add(new Venture { id = id, level = 1 });
                S.player.fame += d.Fame; S.player.moral += d.Moral;
                AddNews($"Novo negócio: {d.Name.ToLowerInvariant()}.");
                Normalize();
                return $"{d.Name} é seu. Receita a partir da próxima rodada.";
            }
            v.level++;
            S.player.fame += d.Fame * .5f; S.player.moral += 2;
            Normalize();
            AddNews($"Você ampliou o negócio: {d.Name.ToLowerInvariant()} (nível {v.level}).");
            return $"{d.Name} ampliado para o nível {v.level}.";
        }

        // ---------- virada da rodada ----------
        void WeekBusiness()
        {
            TickMarket();
            long total = 0;
            foreach (var v in S.ventures)
            {
                var d = BusinessData.VentureById(v.id);
                if (d == null) continue;
                total += (long)Math.Round(VentureWeekly(v.id) * (1 + Rng.Gauss() * d.Risk * .5));
            }
            S.businessIncome = total;
            S.player.money += total;
            if (total < 0) AddNews($"Semana ruim nos negócios: prejuízo de {Fmt.Money(-total)}.");
            S.season.actionsUsed = 0;
            S.season.doneActions.Clear();
        }
    }
}
