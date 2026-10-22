# SummonerGui

Tower defense 2D em Godot .NET/C#, referência vertical 540 × 960 com adaptação proporcional.
Partida de dez ondas, três Pokémon únicos, melhorias/evolução, reposicionamento entre ondas, moedas, vidas do baú e Rei Ogro final.
Visuais provisórios com as folhas fornecidas; sem salvamento, sorteio, tipos extras, venda ou fusão.

## Jogar

Abra `project.godot` na edição .NET da Godot. Aceite recarregar arquivos externos e compile; pressione F5.
Começa com 150 moedas, cinco vidas e nove bases vazias. Selecione uma base e clique no retrato do Pokémon para invocar por 50 moedas. Cada linhagem só pode ser comprada uma vez, inclusive após evoluir.

Invocação e melhorias funcionam também em combate. Cada melhoria custa `25 × nível atual`, até nível 10.
Evolução é uma compra separada: segunda forma exige nível 4 e 150 moedas; final, nível 7 e 400. Subir nível não muda a forma automaticamente. Dano = `danoBase × (1 + crescimento × (nível − 1)) × multiplicadorDaForma`.
Crescimento 0,20; multiplicadores absolutos 1 / 2 / 3,5. Alcance e intervalo não mudam ao evoluir.

Clique em Reposicionar antes da primeira onda ou entre ondas, escolha a unidade e depois o destino: vazio move, ocupado troca. Cancelar não move; iniciar onda limpa a operação pendente. Não há movimento durante combate ou resultado.

Cada onda começa pelo botão. Pendentes inclui quem ainda não surgiu e quem está vivo. A última onda tem um Rei Ogro como último surgimento. Comuns retiram uma vida; o chefe chegando causa derrota imediata. Matar o chefe não vence antes de resolver os demais. Jogar novamente limpa tudo.

## Onde ajustar no Inspector

| Local | Configuração |
| --- | --- |
| Main/GameSession | Saldo/vidas iniciais, custo de invocação, custo-base de melhoria e nível máximo |
| Resources/Pokemon/*.tres | Identidade permanente, nomes/formas, níveis de evolução, multiplicadores, dano base, crescimento, alcance, intervalo e SpriteFrames |
| Resources/Enemies/*.tres | Vida base, velocidade, recompensa, chefe e SpriteFrames |
| Resources/Waves/wave01…10.tres | Composição em ordem, multiplicador de vida e intervalo entre surgimentos |
| Main/WaveDirector | Lista de dez recursos de ondas e referências ao caminho/cena de inimigo |
| Main/EnemyPath | Curva fixa, visível/editável no editor; atualizar também as duas Line2D visuais se alterar a geometria |
| Resources/Animations/*.tres | Regiões, margens e animações dos atlas, sem alterar os PNGs originais |
| Resources/MatchTheme.tres | Contraste e cores dos controles/painéis |

A configuração de ondas é validada: dez entradas, exatamente um chefe e ele como último surgimento da última onda.

Atributos v5: Bulbasaur 20 / 1,3 s / 100 px (menor alcance); Charmander 10 / 0,65 s / 120 px; Squirtle 15 / 1 s / 155 px.
Adversários iniciais: slime 45 vida/100 px/s/10 moedas, goblin 30/135/12, golem 100/65/20, chefe 550/55/80; o multiplicador da onda também se aplica ao chefe.

## Organização

GameSession valida economia, ownership, movimento e resultado. CreatureSlot guarda ocupação/seleção.
WaveDirector controla composição, surgimentos e ameaças ativas. Enemy resolve dano/chegada uma única vez e avisa o diretor.
Creature controla alvo/cooldown; PokemonDefinition calcula o dano a partir de nível e forma, sem acumular multiplicadores.
CharacterVisual cuida da apresentação. ElementalAttack captura os valores e aplica dano uma vez no contato; Enemy controla os ticks de queimadura pelo mesmo fluxo de vida/morte. Seu FPS visual não aplica dano nem controla ataques.
Main conecta Controls à sessão e mostra estado/vida do chefe. Configuração fica em Resources, estado nas unidades.
GameAudio mantém música e limita efeitos. Volumes em `Resources/Audio/settings.tres`; animações em `Resources/Animations/`, escalas em `VisualScale` dos recursos de personagem.
Morto sai imediatamente dos alvos/ameaças; animação de morte pode terminar depois. Reinício remove também os efeitos.

## Validar e reproduzir

Entrega v5: `powershell -NoProfile -File .\Tests\Run-Validation.ps1 -ElementalOnly -Capture`. Configurações e resultados em [efeitos e combate v5](docs/elemental-v5.md). Os resultados de partidas completas e a agenda antiga abaixo são históricos da evolução automática; não foram revalidados para a economia v5 e podem falhar como baseline. As verificações focadas foram atualizadas, sem enfraquecer essas expectativas históricas.

```powershell
powershell -NoProfile -File .\Tests\Run-Validation.ps1 -Capture -RequireCompleteCoverage
```

O runner localiza Godot .NET pelos processos/PATH e dotnet pelo PATH, registra versões, compila, importa, executa Main, valida recursos/regras e simula partidas reais. Se a Godot não estiver aberta/PATH, informe `-GodotPath '<executável instalado>'`. Sem `-Capture`, executa headless; com ela produz capturas para inspeção. Não instala ferramentas.

Logs, relatórios JSON e imagens ficam em um diretório novo de TestResults (ignorado).
Planos de ações em `Tests/Scenarios/simulation.json`; custos/combate/ondas vêm dos scripts reais do jogo.
Ver [instruções](docs/validation.md), [resultados medidos](docs/validation-report.md) e [inspeção/limitações dos sprites](docs/assets.md).

Para assets/áudio, use `powershell -NoProfile -File .\Tests\Run-Validation.ps1 -PresentationOnly -Capture`: verificações focadas sem simulações completas. Não combine com `-RequireCompleteCoverage`. Ver [integração, volumes, créditos e validação](docs/audio-assets-v4.md). O jogo usa recursos definitivos de Assets, sem depender de TempAssets.

## Roteiro curto

1. Confira 150 moedas/5 vidas/onda0/10. Invoque Bulbasaur na base2, Charmander na5 e Squirtle na8.
2. Selecione outra base e confira retratos indisponíveis para linhagens já invocadas.
3. Teste Reposicionar: mova a unidade para vazio, troque com outra e cancele uma operação; volte às posições para seguir o plano medido.
4. Inicie as ondas manualmente. Níveis 4/7 liberam compras pelo botão Evoluir; mantenha moedas para pagar e preserve base/atributos fixos.
5. Confira bloqueio de movimento em combate e botão de melhoria sem saldo. Os retratos/atributos atualizam imediatamente.
6. Resolva dez ondas e derrote o chefe; confira vitória/reinício. Inicie sem defesas para conferir derrota por cinco chegadas. A chegada do chefe é coberta por cenário automatizado.
7. Para repetir exatamente a estratégia vencedora, execute o plano JSON pelo runner; o relatório lista cada compra/recusa e resultado por onda.

Pendentes manuais: F5 no editor já aberto, mouse físico, redimensionamento, fluidez/recortes das animações e avaliação de dificuldade.
Preserve imagens originais, arquivos .uid e metadados .import; não versione .godot nem export_credentials.cfg.
