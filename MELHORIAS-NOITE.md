# Melhorias da noite (v0.15)

Tudo está no branch `melhorias-noite`, um commit por novidade. O `main` não foi tocado e nada foi enviado ao GitHub.
O primeiro commit (`v0.14: ...`) só guarda o trabalho da sessão anterior que estava sem commit.

## O que entrou

1. **Caixa de mensagens** (aba **Mensagens**, com selo vermelho de não lidas, e bloco na Central).
   Técnico, empresário, diretoria, patrocinadores, namorada, companheiros (nomes reais do elenco), imprensa,
   torcida organizada, departamento médico, Seleção e o rival escrevem para você. Várias pedem resposta
   (churrasco do elenco, cobrança do técnico, post pago do patrocinador, convite da torcida, braçadeira de capitão,
   propostas da janela...) e cada resposta mexe em moral, fama, dinheiro e reputação.
2. **Objetivos da temporada**: o técnico define metas individuais (gols, gols + assistências, nota média, jogos)
   de acordo com a posição e o seu tamanho no elenco; a diretoria cobra a posição na tabela ou a copa.
   Barras de progresso na Central, mensagem de meio de temporada e, no fim, bônus (com imposto), relação com o técnico,
   moral, fama e peso na renovação (cumprir tudo melhora a renovação; não cumprir nada pode tirá-la).
3. **Pós-jogo de verdade**: placar final, sua nota com cada ação e os pontos dela, lances da partida, **craque do jogo**,
   repercussão (moral, técnico, elenco, torcida, fama, dinheiro) e a **entrevista coletiva** opcional com três perguntas
   que dependem do jogo (clássico, rival, nota baixa, banco, rumores, capitania, namorada).
4. **Reputação em três frentes**: técnico, **elenco** e **torcida** (barras em Condição). O elenco pesa na escalação;
   com elenco, técnico e torcida altos o técnico oferece a **braçadeira de capitão** (selo no topo e na carta).
   Clássicos valem o dobro para a torcida. Torcida alta canta o seu nome na apresentação do lance 3D; baixa, vaia.
5. **Especialidades**: Finalizador, Driblador, Maestro, Velocista, Cabeceador, Muralha, Cobrador de falta, Frieza, Líder e Ídolo.
   Liberadas por atributo ou conquista, ficam para sempre e têm efeito real no lance 3D (precisão do chute, curva da falta,
   pênalti, janela do cabeceio, fôlego da arrancada, dribles, passes, disputas) e na simulação. Selos na carta do jogador.
6. **Calendário** no topo da Central: próximos jogos com escudos, casa/fora, **clássicos** (vermelho) e jogo contra o
   **rival** (roxo), fases da copa, datas da Seleção, janela de transferências e Copa do Mundo.
7. **Mercado vivo**: rumores na imprensa antes da janela; **janela no meio da temporada** (depois da rodada 18) com
   propostas pela caixa de mensagens e na aba Contrato (aceitar, o empresário negocia, recusar); clube grande pode
   **pagar a multa**; a diretoria pode segurar a venda; **empréstimo** para jovem sem minutos (volta no fim do ano);
   **pedir para ser negociado** na aba Contrato. Trocar de liga no meio do ano remonta a tabela e a artilharia.
   Arábia e MLS continuam só por convite/sondagem.
8. **Vida** com menos tabela: seu estilo de vida (casa, carro, estilo), vida pessoal com o rival, extrato,
   **programas com a galera do time** (games, pagode, futevôlei, aniversário, churrasco na sua casa, viagem: gastam
   um horário da agenda e unem o vestiário) e loja por categoria com **roupas e acessórios** baratos para o começo.
   O apartamento/garagem em 3D ficou de fora: feito com peças de código ficaria com cara de desenho, que você não quer.
9. **Replay do gol** em câmera lenta numa câmera alta de TV, com selo REPLAY e a rede estufando de novo, antes da
   comemoração (botão PULAR encerra).
10. **Gala de fim de temporada**: depois do último jogo, suas taças e prêmios do ano em 3D e o pódio da Bola de Ouro.

Também: a **fama cresce mais devagar** (o simulador mostrava fama 100 já na primeira temporada, o que fazia TV e
patrocínio pagarem demais). Agora fica em 20–50 no primeiro ano e chega perto de 90 em 3–5 temporadas.
O convite da TV foi recalibrado. Versão do jogo: **0.15**.

## Como testar no celular

- Nova carreira: abra **Mensagens** (boas-vindas do técnico com resposta, objetivos da diretoria, empresário).
- **Central**: calendário no topo, bloco de objetivos abaixo do próximo jogo, bloco Mensagens.
- Jogue uma rodada até o fim: depois do apito toque em **Pós-jogo**, veja a nota explicada e faça a **Entrevista coletiva**.
- Marque um gol no lance 3D: o replay passa em câmera lenta antes da comemoração (teste o PULAR).
- **Jogador**: barras de elenco e torcida, bloco Especialidades (como liberar cada uma) e selos na carta.
- **Contrato**: "Pedir para ser negociado" e, depois da rodada 18, a janela de transferências.
- **Vida**: programas com o elenco (usam horários da agenda), loja com abas Garagem / Imóveis / Roupas / Acessórios.
- Chegue ao fim da temporada: a gala aparece uma vez, depois o balanço com os objetivos cumpridos ou não.
- Saves antigos continuam abrindo: os campos novos são preenchidos na hora (objetivos, reputação, caixa de mensagens).

## O que foi validado

- Compilação contra as DLLs da Unity (scripts do jogo e do editor): sem erros.
- Simulador (`dotnet run -- 10 7`): "OK: nenhuma exceção" em 10 carreiras de 21 temporadas, com teste direto de troca
  de liga no meio do ano e de empréstimo (volta ao clube no fim). Responde mensagens, coletivas, faz programas com o elenco
  e pede para sair de vez em quando. Dinheiro no fim da 1ª temporada: R$ 48 mil a R$ 225 mil (antes: R$ 93 mil a R$ 305 mil);
  2ª temporada: R$ 106 mil a R$ 858 mil. Craque do jogo em 31% dos jogos (o simulador acerta muito mais que um jogador real).
- Teste automático dos lances em Play: todos os tipos + "gol" + o novo "replay": **0 erro(s)**
  (o replay rodou em câmera lenta e o tempo voltou ao normal).
- Capturas conferidas: `Capturas/menu-home.png` (calendário e objetivos), `menu-inbox.png`, `menu-posjogo.png`,
  `menu-coletiva.png`, `menu-player.png` (especialidades e capitão), `menu-contrato-janela.png`, `menu-life.png`,
  `menu-life-meio.png`, `menu-fim.png`, `menu-gala.png` e `replay.png` (quadro do replay tirado durante o teste).

## Limitações e ideias que ficaram para depois

- O simulador acerta muitos lances (nota média 7,4), então os objetivos saem fáceis nele; calibre jogando de verdade
  (as metas estão em `Core/Objectives.cs`, `MakeObjectives`).
- O replay mostra bola, goleiro e o batedor correndo (ainda sem animação de chute: entra junto com o jogador da Mixamo).
- A troca de clube no meio do ano soma os números da temporada nos dois clubes (o histórico mostra "Clube A / Clube B").
- Apartamento e garagem em 3D, partida de despedida, hall da fama e capitão com efeito no lance ficaram de fora.
- Mensagens antigas somem quando a caixa passa de 60 (as que esperam resposta ficam).
- Antes de publicar: continua valendo o aviso sobre licença de nomes reais e dos sons da Mixkit.
