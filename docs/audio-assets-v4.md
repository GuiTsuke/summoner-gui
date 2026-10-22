# Integração visual e áudio — 19/10/2026

Entrega restrita a assets, cenário e áudio. Sem menus, obstáculos, atalhos, novas regras de navegação ou preferências salvas. Economia, ondas, combate, evolução e reposicionamento foram preservados.

## Organização e ajustes

- Personagens: `Assets/Art/Source/` e `Resources/Animations/`; uma folha ativa por personagem, conforme manifesto. Escala em `VisualScale` dos recursos Pokémon/inimigos. Detalhes em [assets.md](assets.md).
- Cenário: `Assets/Art/Environment/`, `Resources/Environment/`, `Scenes/CreatureSlot.tscn` e Main. Terreno recortado sem deformação; nove bases maiores e baú separado; caminho preservado.
- Áudio: `Assets/Audio/music/` e `sfx/`, com treze efeitos/vinhetas e uma música. Créditos, manifesto e quatro licenças copiados para `Assets/Audio/` e `Licenses/`. Efeitos Kenney e música RandomMind: CC0, fontes indicadas nesses arquivos.
- `Scenes/AudioController.tscn` é o único autoload GameAudio, configurado em `project.godot`. Um AudioStreamPlayer Music persiste entre cenas; seis vozes SFX e uma voz exclusiva para resultado. `default_bus_layout.tres` separa Master, Music e SFX.
- Volumes centrais: `Resources/Audio/settings.tres`, valores lineares 0–1: Master 0,80, Music 0,22, Sfx 0,65. Zero silencia. Alterações no recurso em execução aplicadas no próximo frame; `ApplyVolumes()` disponível para menu futuro. Sem menu/persistência nesta entrega.
- GameSession emite eventos de seleção, compra, melhoria/evolução, recompensa, recusa, início, resultado e reinício. Main conecta áudio e acompanha chefe, impacto e derrota do inimigo. Cliques dos botões têm efeito próprio. Gameplay nunca espera um som terminar.
- Música em loop; efeitos/vinhetas explicitamente sem loop em cópias privadas dos streams. Resultado reduz a música e interrompe efeitos; vinheta toca uma vez. Reinício interrompe vinheta/efeitos, restaura volume e mantém música já tocando. Intervalos mínimos: impacto 0,09 s; moedas/derrotas 0,12 s; demais 0,05 s por tipo. Com seis vozes ocupadas, novos efeitos são descartados; resultado usa voz reservada.

## Verificações executadas

Comando focado, sem simular partidas completas:

```powershell
powershell -NoProfile -File .\Tests\Run-Validation.ps1 -PresentationOnly -Capture
```

Execução final: [validation-20261019-220905](../TestResults/validation-20261019-220905/validation.json). Nove etapas passaram, todas com código 0 e sem erros/avisos: versão Godot, SDK, build, importação, Main por 120 frames, galeria, combate, regras locais e integração de apresentação. Godot .NET 4.7.2 e SDK .NET 10.0.400 descobertos pelos processos/PATH.

PresentationChecks usa Main, ações e sinais reais com ondas privadas curtas para eventos de compra, melhoria, evolução, recusa, impacto, recompensa, chefe, vitória/derrota e reinício. Confere reprodução iniciada, streams/loop, buses independentes, silêncio, limite de vozes/repetição e o mesmo tocador após três reinícios e recriação da cena. Essas fixtures não representam partida completa nem nova medição de balanceamento. CombatChecks mantém a checagem de que FPS visual não altera dano/ataques; MatchChecks verifica as regras existentes.

Uma cópia independente foi montada no diretório temporário do sistema contendo recursos definitivos, cenas, scripts, testes e configuração, **sem TempAssets e sem cache de importação**. Compilou, importou e executou Main por 120 frames, incluindo música e encerramento, sem erros/avisos. Caminho em `TestResults/standalone-copy-path.txt`; logs `standalone-build.*`, `standalone-import.*` e `standalone-main.*`. Dependências de recursos e caminhos de runtime examinados: não apontam para a entrada. Os originais não foram movidos.

A primeira execução encontrou referências OGG não liberadas ao encerrar com áudio ativo, correspondentes ao [problema conhecido da Godot](https://github.com/godotengine/godot/issues/76745). O controlador intercepta o fechamento da janela, para os tocadores e aguarda 200 ms reais antes de sair, permitindo liberação assíncrona pelo AudioServer. Testes usam o mesmo encerramento; o smoke test envia a notificação de fechar janela. Essa espera só se aplica à saída da aplicação. Execuções finais sem o erro; logs anteriores preservados.

## Inspeção visual realizada

Examinados originais e capturas renderizadas da Main inicial, evolução e chefe, além da galeria em repouso, direita, esquerda, ataque/dano e morte. Bases, baú e corpos principais inteiros nas capturas; rota/seleção correspondentes, chefe abaixo do painel de vida. Capturas de resultado também produzidas. Não houve avaliação auditiva humana nem revisão contínua de todos os quadros em movimento.

## Teste manual e recursos não usados

Recarregue arquivos externos no editor e pressione F5. Selecione base, invoque, melhore/evolua, inicie onda e ouça seleção/clique, compra, impactos, moedas e recusa. Confira vinhetas e reinicie várias vezes, observando se música continua única. Ajuste os três volumes no recurso central. Avalie mistura, adequação sonora, transições, conforto visual e redimensionamento.

Pedras, escombros, stone_hit.ogg e stone_break.ogg ficaram somente na entrada: não existe essa mecânica. As imagens completas de cenário/menu são referências, não interfaces do jogo. MeadowFlower preservada, sem uso na Main. Pokémon continuam v3 com padronização pendente; contatos de partículas entre poses exigem revisão da arte original. Nada baixado, nenhum plugin instalado ou commit criado.
