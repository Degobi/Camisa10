# Camisa 10: contexto para o Claude Code

## O que é o jogo
Jogo mobile de carreira de futebol **em primeira pessoa**, no estilo do antigo **I Am Playr** (Facebook).
Não é gerenciador estilo Brasfoot: você controla um jogador, não um time.
- Sempre **paisagem** (celular deitado, nos dois sentidos). Pedido do dono em 04/10/2026; antes era retrato.
- Visual dos menus inspirado no **modo carreira dos jogos de futebol de console**: fundo azul-noite, blocos lado a lado,
  abas em caixa alta, botão "Continuar" no topo e destaque verde neon.
- Os lances decisivos da partida são jogados em **3D pelos olhos do jogador**, com chute por **swipe**
  (direção = alvo, velocidade = força, curva do traço = efeito). Goleiro reage, barreira pula.
- Fora de campo: carreira, contratos, patrocínios, chuteira personalizável, decisões e vida do jogador.
- **Agenda da semana** (3 ações por rodada: técnico, imprensa, redes, patrocinador, treino extra, jantar a dois,
  fisioterapeuta pago, podcast, alfinetar o rival...) e **Negócios** (bolsa fictícia com SAFs e empresas próprias): `Core/Business.cs`.
- Temporada de **36 rodadas** (quatro turnos entre 10 clubes). Use `Game.SeasonRounds` (saves antigos podem ter 18), nunca
  o número fixo. Copa, seleção e prêmio do mês (a cada 4 rodadas) ficam em `Core/Glory.cs`.
- **Vida fora de campo** em `Core/Life.cs`: extrato semanal (imposto de renda por país em `LeagueDef.Tax`, 8% do empresário,
  custo de vida, manutenção dos bens, vida a dois), dívida, namorada (ficando → namorando → noivos → casados, com afeto
  que esfria) e rival fictício da mesma posição (gols, duelos diretos, provocações). Eventos ligados a eles em `GameEvents.cs`.
  Calibre o dinheiro com o simulador (ele imprime o dinheiro, a fama e o saldo de cada temporada).
  Fama: ganhos amortecidos em `Game.Normalize` (`FameGainFactor`); some `p.fame +=` normalmente e chame `Normalize`.
- **Caixa de mensagens** (`Core/Inbox.cs`, aba Mensagens com selo de não lidas): o canal da história. Mande com
  `Game.Mail(papel, de, assunto, texto, kind, data, opções...)`; respostas com efeito ficam em `Inbox.Replies` pelo `kind`
  (o save guarda só o tipo). Chegam na virada da rodada (`InboxWeek`), no começo da temporada e nos acontecimentos.
- **Objetivos da temporada** (`Core/Objectives.cs`): técnico (gols/participações/nota/jogos) e diretoria (tabela ou copa),
  barras na Central; no fim valem bônus, relação com o técnico e renovação (`Season.objMet`).
- **Reputação** (`Core/Reputation.cs`): técnico (`coach`), elenco (`squad`) e torcida (`fans`); use `AddSquad`/`AddFans`
  (retorno decrescente). Elenco pesa na escalação; capitania oferecida pela caixa de mensagens; clássicos em `Game.Derbies`.
  Torcida alta canta o nome no lance (`FansChant`), baixa vaia.
- **Especialidades** (`Core/Traits.cs`): liberadas por atributo/conquista, efeito real no 3D (`Chance3D.Has("finalizador")`)
  e em `MatchEngine.Prob` (`TraitBonus`). Selos na carta do jogador.
- **Pós-jogo** (`GameApp.PostMatch`): placar, nota com a origem dos pontos, lances, craque do jogo (`MatchEngine.Motm`),
  repercussão e a coletiva opcional (`Core/Press.cs`). **Gala** de fim de temporada (`GameApp.Gala`).
- **Calendário** (`Core/Calendar.cs`, faixa no topo da Central) e **mercado** (`Core/Market.cs`): rumores, janela depois da
  rodada `WindowRound` (metade) com propostas pela caixa, multa paga, empréstimo de jovem sem minutos (`S.loanFrom`, volta
  no fim do ano), pedido para sair. Troca de liga no meio do ano remonta tabela e artilharia (`SwitchLeague`).
- Vida (aba Vida): estilo de vida, programas com o elenco (`LifeData.Social`, gastam horário da agenda) e loja por categoria.
- Plataforma: Android primeiro, iOS depois (o dono tem Mac). Idioma da interface: português do Brasil.

## Stack e regras do projeto
- Unity 6 (pacotes em `Packages/manifest.json`), C#, Built-in Render Pipeline.
- **Toda a interface é criada por código** (`UIKit.cs`). Não há prefabs; a cena `Main.unity` fica vazia
  e o `Bootstrap.cs` sobe o jogo. Não crie dependência de objetos montados à mão no editor.
- `Assets/Camisa10/Core/` é **C# puro**, sem `using UnityEngine`. Regras, economia e simulação ficam aqui.
- 3D em `Assets/Camisa10/Unity/FirstPerson/`: `Arena` (estádio com dois anéis, cobertura, placas de LED, dia ou noite
  via `StadiumStyle`), `StadiumArt` (texturas do estádio), `MeshBuilder` (junta peças repetidas numa malha só: no celular
  cada objeto custa uma chamada de desenho), `PersonRig` (jogador), `Kits` (uniforme real de cada clube: padrão, número,
  patrocinador fictício; a malha ganha UVs em `HumanModel.KitUVs`), `Chance3D` (lance em primeira pessoa; a finalização
  fica em `Chance3D.Shooting`: segurar CHUTAR carrega a força, falta/pênalti têm mira arrastável e efeito),
  `Goalkeeper` (reação + mergulho físico; calibrado por simulação: goleiro médio defende ~50% de canto baixo com força a 18 m,
  ~40% de ângulo, quase tudo no meio; pênalti com canto certo ~50%: mexa nos números testando a taxa, não no olho),
  `SwipePad` e `TouchControls` (entrada), `ChanceHud` (HUD do lance, placar estilo TV e vinheta),
  `Chance3D.Celebration` (comemoração em terceira pessoa depois do gol, com gestos do `PersonRig.Gesture`, pulável),
  `Chance3D.Replay` (replay do gol em câmera lenta numa câmera de TV, pulável; sempre devolva `Time.timeScale = 1`).
  Chuteira: `Art/BootModel.cs` (malha própria com cano aberto, forro, lingueta, cadarço, solado e travas; textura nas cores
  do `BootStyle`; a mesma malha vira a foto do menu). Mudou forma do corpo ou da chuteira? Suba `ProceduralBody.ModelVersion`
  e gere `Resources/Modelos/corpo.bytes` de novo (menu Camisa 10 > Gerar corpo dos jogadores).
  Movimento em campo com aceleração e bote (`Chance3D.Steer`), e o `PersonRig` escolhe parado/andando/correndo pela
  velocidade medida: mova o Transform, não force a animação.
- Nota da partida por ações (`MatchEngine.Score` e a tabela `MatchEngine.Pts`): nota = 6,0 + pontos ÷ 10, entre 3 e 10.
  Toda ação nova do jogador (drible, firula, passe...) deve chamar `Score`, nunca mexer em `Rating` direto. No lance 3D use
  `Chance3D.AddPoints` (soma e mostra "+3 DRIBLE"); o resultado final do lance entra por `MatchEngine.OutcomePoints`.
- Firulas em `Chance3D.Skills` (pedalada, elástico, chapéu, caneta; o joystick escolhe). Rede em `GoalNet` (linhas finas,
  estufa no gol). Bola com desenho real (icosaedro truncado) em `Arena.BallTexture` + malha própria `Arena.BallMesh`.
- Jogadores modelados em código (`ProceduralBody`): campo de distâncias com anatomia, rosto, cabelo, mãos, chuteiras e
  uniforme com volume (camisa/calção mais largos, bainhas retas), extraído por surface nets e preso ao esqueleto do
  `jogador.bytes` (de lá vêm só ossos e animações). O editor salva a malha em `Resources/Modelos/corpo.bytes`
  (`Camisa10BuildPrep.BakeBody`); mudou a modelagem? suba `ProceduralBody.ModelVersion` que ele regera.
  Olhos e sobrancelhas são peças à parte em alta resolução (a grade de 1,1 cm é grossa demais para eles).
- Bola (`BallPhysics`): arrasto do ar + efeito Magnus; `Chance3D.Launch(alvo, velocidade, giro)` resolve a trajetória
  para terminar no alvo. Chute forte (giro por cima), COLOCADO (efeito lateral, mais preciso), falta com efeito escolhido.
- Goleiro (`Goalkeeper`): mergulho pelo quadril (o corpo voa de lado, não tomba pelos pés), passo extra quando sobra tempo;
  números calibrados por simulação (médio: canto baixo ~43% de gol, ângulo ~57%, colocado ~47%, pênalti certo ~45%).
- Sem pé/perna de primeira pessoa na tela (a câmera é o olho do jogador).
- Tipos de lance (`Chance3D` + `Chance3D.Plays`): chance, cara, contra, meio, falta, pênalti, cabeceio, cruzamento, rebote,
  corte (cabeceio defensivo) e defesa; posições sorteadas a cada lance; pesos por posição em `GameData.Positions[].Moments`.
  Correr: botão CORRER (não solta se o dedo escorregar) ou joystick na borda; fôlego ~6 s.
- Gol: `Chance3D.BallIntoNet` leva a bola até o fundo e a rede (`GoalNet.Hold/Release`) estica e balança; a física não decide.
- Torcida: `CrowdMotion` (um recorte por assento, malha única, fileiras de trás para a frente; pula no gol via `Excite`).
  Qualidade máxima = dois anéis, equilibrada = anel de baixo, leve = textura pintada.
- Ligas: Brasileirão, La Liga, Premier League, Serie A, Ligue 1, Süper Lig, Saudi Pro League e MLS (`GameData.Leagues`,
  com copa, exigência de mercado e prestígio). Arábia e MLS só mandam proposta por convite/sondagem (evento ou fim de temporada).
- Copa do Mundo (`Core/WorldCup.cs`): anos 2026, 2030...; convocado joga grupo + mata-mata no fim da temporada, partidas
  jogáveis no 3D (`MatchEngine` aceita times avulsos). Troféus em 3D de metal: `Unity/Art/TrophyStudio` (mapa título → taça).
- Som: `MatchAudio` é o som do lance (torcida, toque, chute, comemoração, lamento, apito); `Sfx` é o som global
  (rede, trave e o que tocar fora do lance). Não chame os dois para o mesmo evento.
- Paisagem é garantida por `Landscape.cs` + `SafeArea.cs` (faixa 16:9 em telas mais altas que 4:3), pelo `CanvasScaler`
  (referência 1920x1080) e pela câmera do lance (`Arena.FitCamera` + `Arena.PitchBias`). Respeite isso em qualquer tela nova.
- Telas fora de campo: monte linhas com `UIKit.Cols` e blocos com `UIKit.Tile` (ou `UIKit.Stack` para empilhar).
  Cores só pelo `Theme` (paleta escura).
- Use APIs da Unity 6.6: `Rigidbody.linearVelocity`, `linearDamping`, `FindAnyObjectByType`, `FindObjectsByType<T>()`
  (as versões com `FindFirst`/`FindObjectsSortMode` estão obsoletas).
- Entrada de toque só pelo EventSystem (interfaces `IPointer*`), para funcionar com qualquer Input System.
- Clubes, ligas e jogadores são **reais** por decisão do dono (04/10/2026), com elencos em `GameData.Squads`
  (primeiro nome = goleiro). Marcas de patrocínio e empresas da bolsa seguem fictícias, exceto as SAFs de clubes.
  Não use o nome "FIFA". Publicar na loja com nomes reais exige licença: avise o dono antes de um build público.
- Materiais 3D são criados a partir do material padrão de primitiva (`Arena.Mat`), para não sumir no build.
  Coisas que brilham (LED, refletores, céu) usam `Arena.Unlit` (shader de sprites, sempre incluído), nunca `_EMISSION`.
- Sons reais da Mixkit em `Resources/Sons` (torcida, gol, aplauso, lamento, vaia, chute, toque, apito; ver o LEIA-ME de lá).
  Antes de publicar na loja, confira a licença da Mixkit, como já é preciso fazer com os nomes reais.
- **Arquivo real tem prioridade sobre o gerado por código** (o dono não quer arte desenhada por código):
  `Resources/Estadio/grama` e `torcida`, `Resources/Sons/<nome>` (crowd, roar, ooh, kick, post, net, whistle...),
  `Resources/Vida/<id do item>` (fotos da aba Vida), `Resources/Escudos`. O gerado só cobre a falta do arquivo.
- Preferências do aparelho (qualidade gráfica, som, vibração) ficam em `GameSettings` (PlayerPrefs), fora do save.
- Save (`SaveSystem`): gravação atômica com cópia `.bak`; `GameState` tem `profileId`, `rev` e datas para o futuro
  modo online. Mudou o formato de forma incompatível? Suba `GameState.CurrentVersion` e migre em `Game.EnsureProfile`.

## Como validar mudanças
1. Regras (Core): `cd Tools/Simulador && dotnet run -- 10 [semente]` joga carreiras inteiras (treino, eventos, partidas, Copas,
   transferências) e mostra notas, ligas e títulos. Rode depois de mexer em regra/economia e compare com antes.
2. Unity: com o MCP for Unity conectado, leia o Console, entre em Play e confira se não há erros vermelhos.
   Sem o editor aberto, fotografe o lance (dia e noite), os menus e exporte os sons em `Capturas/`:
   `Unity -batchmode -projectPath . -executeMethod Camisa10.EditorTools.Camisa10Preview.CaptureBatch -logFile -`
   (no editor: menu **Camisa 10 > Capturar imagens do lance**).
   As capturas de menu jogam rodadas de uma carreira de exemplo (`Camisa10Preview.PlayRounds`) e fotografam também caixa de
   mensagens, pós-jogo, coletiva, janela de transferências, fim de temporada e gala (`menu-*.png`).
   Lances jogados de verdade (todos os tipos, em Play, inclusive o replay): `-executeMethod Camisa10.EditorTools.Camisa10SmokeTest.Run`
   (menu **Camisa 10 > Testar lances automaticamente**). Com o editor aberto, rode numa cópia do projeto.
3. Builds: menu **Camisa 10 > Gerar APK de teste** (gera `Builds/Camisa10.apk`). `Camisa10BuildPrep` roda antes de todo build:
   sem ele o celular corta colisores e o shader (tela rosa), porque a cena é vazia e tudo nasce por código.

## Prioridades atuais (feedback do dono)
- Visual realista (o dono rejeita arte desenhada por código). Jogadores: `PersonRig` usa o corpo com captura de movimento
  de `HumanModel` (Resources/Modelos/jogador.bytes, gerado por `Tools/Modelos/converter.py`); próximo passo é um
  jogador da Mixamo com chute e defesa de goleiro. Escudos oficiais em Resources/Escudos; fontes Barlow em Resources/Fonts.
- Telas fora de campo com cara de I Am Playr (apartamento, carros, roupas, vida social), menos tabelas.
- Sensação do chute: ajustar força, curva e goleiro jogando no aparelho.
