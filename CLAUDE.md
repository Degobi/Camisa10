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
- **Agenda da semana** (3 ações por rodada: técnico, imprensa, redes, patrocinador, treino extra...) e
  **Negócios** (bolsa fictícia do futebol com SAFs e empresas próprias que rendem por rodada): `Core/Business.cs`.
- Plataforma: Android primeiro, iOS depois (o dono tem Mac). Idioma da interface: português do Brasil.

## Stack e regras do projeto
- Unity 6 (pacotes em `Packages/manifest.json`), C#, Built-in Render Pipeline.
- **Toda a interface é criada por código** (`UIKit.cs`). Não há prefabs; a cena `Main.unity` fica vazia
  e o `Bootstrap.cs` sobe o jogo. Não crie dependência de objetos montados à mão no editor.
- `Assets/Camisa10/Core/` é **C# puro**, sem `using UnityEngine`. Regras, economia e simulação ficam aqui.
- 3D em `Assets/Camisa10/Unity/FirstPerson/`: `Arena` (estádio), `PersonRig` (jogador articulado com animação
  procedural), `Chance3D` (lance em primeira pessoa), `SwipePad` (entrada), `ChanceHud` (HUD do lance).
- Paisagem é garantida por `Landscape.cs` + `SafeArea.cs` (faixa 16:9 em telas mais altas que 4:3), pelo `CanvasScaler`
  (referência 1920x1080) e pela câmera do lance (`Arena.FitCamera` + `Arena.PitchBias`). Respeite isso em qualquer tela nova.
- Telas fora de campo: monte linhas com `UIKit.Cols` e blocos com `UIKit.Tile` (ou `UIKit.Stack` para empilhar).
  Cores só pelo `Theme` (paleta escura).
- Use APIs da Unity 6: `Rigidbody.linearVelocity`, `linearDamping`, `FindFirstObjectByType`.
- Entrada de toque só pelo EventSystem (interfaces `IPointer*`), para funcionar com qualquer Input System.
- Clubes, ligas e jogadores são **reais** por decisão do dono (04/10/2026), com elencos em `GameData.Squads`
  (primeiro nome = goleiro). Marcas de patrocínio e empresas da bolsa seguem fictícias, exceto as SAFs de clubes.
  Não use o nome "FIFA". Publicar na loja com nomes reais exige licença: avise o dono antes de um build público.
- Materiais 3D são criados a partir do material padrão de primitiva (`Arena.Mat`), para não sumir no build.

## Como validar mudanças
1. Regras (Core): `cd Tools/Simulador && dotnet run -- 3` simula carreiras completas.
2. Unity: com o MCP for Unity conectado, leia o Console, entre em Play e confira se não há erros vermelhos.
3. Builds: menu **Camisa 10 > Configurar build Android** (paisagem, IL2CPP, ARM64).

## Prioridades atuais (feedback do dono)
- Visual realista (o dono rejeita arte desenhada por código). Jogadores: `PersonRig` usa o corpo com captura de movimento
  de `HumanModel` (Resources/Modelos/jogador.bytes, gerado por `Tools/Modelos/converter.py`); próximo passo é um
  jogador da Mixamo com chute e defesa de goleiro. Escudos oficiais em Resources/Escudos; fontes Barlow em Resources/Fonts.
- Telas fora de campo com cara de I Am Playr (apartamento, carros, roupas, vida social), menos tabelas.
- Sensação do chute: ajustar força, curva e goleiro jogando no aparelho.
