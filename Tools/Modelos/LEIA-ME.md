# Modelo 3D dos jogadores

`Assets/Camisa10/Resources/Modelos/jogador.bytes` é gerado por `converter.py` a partir de um personagem glTF (.glb)
com esqueleto da Mixamo e os clipes `idle`, `run` e `walk`.

O atual é o "X Bot" da Mixamo (Adobe), na cópia que vem nos exemplos do three.js
(`examples/models/gltf/Xbot.glb`). Personagens e animações da Mixamo podem ser usados em jogos, inclusive comerciais,
mas não podem ser redistribuídos soltos fora do jogo.

Para trocar por outro personagem da Mixamo: exporte em glTF/GLB com as animações `idle` e `run`, depois rode
`python3 converter.py personagem.glb ../../Assets/Camisa10/Resources/Modelos/jogador.bytes` (precisa de numpy).
O `PersonRig` usa o arquivo automaticamente; sem ele, volta para o boneco de formas simples.
