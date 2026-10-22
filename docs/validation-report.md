# Validação da expansão — 19/10/2026

## 1. Verificações executadas e resultados

Expansão implementada após confirmação explícita do usuário: três linhagens únicas, formas nos níveis 4/7, dez ondas e Rei Ogro no último surgimento. O [relatório anterior de cinco ondas](validation-five-waves.md) foi preservado como histórico; seus bloqueios NOT_IMPLEMENTED não descrevem mais o jogo atual.

Execução completa final: `TestResults/validation-20261019-213735/`.

- [validation.json](../TestResults/validation-20261019-213735/validation.json): ferramentas descobertas, comandos, saídas, marcadores e regras cobertas.
- [simulation.json](../TestResults/validation-20261019-213735/simulation.json): cada ação, saldo, resultado por onda, unidades e surgimentos.
- [simulation.stdout.log](../TestResults/validation-20261019-213735/simulation.stdout.log) e respectivos `*.stderr.log`: execução real na Godot, não apenas compilação.

Ambiente descoberto pelos processos/PATH, confirmado por execução:

- Godot `4.7.2.stable.mono.official.ed1daf0bf`, instalação em `E:\Download\Godot_v4.7.2-stable_mono_win64`.
- Executável console descoberto nessa pasta: `Godot_v4.7.2-stable_mono_win64_console.exe`.
- .NET em `C:\Program Files\dotnet\dotnet.exe`, SDK `10.0.400`; alvo do projeto `net8.0`, runtime .NET 8 instalado.
- PowerShell `5.1.19041.6456`.

Passaram nove etapas: descoberta da versão, dotnet-info, build, importação, Main, ArtChecks, CombatChecks, MatchChecks e simulação. Todos os processos retornaram 0; logs sem erros ou avisos. Build com zero erros/avisos. `-RequireCompleteCoverage` agora retorna 0: as regras futuras foram implementadas e exercitadas. Isso não certifica diversão ou perfeição visual.

### Cobertura de regras

| Regra | Verificação executada |
| --- | --- |
| Pokémon repetido | Compra da mesma linhagem recusada, inclusive depois de evoluir; saldo/ocupação não alterados. |
| Compras/melhorias | Recusa sem saldo, preço 25×nível, máximo 10, isolamento da unidade e nome/atributos após evolução. |
| Evolução | Níveis 1–10 e fórmula independente nos testes; mesma referência, linhagem, base e atributos fixos. Formas nos níveis 4 e 7. |
| Movimento/troca | Unidade já evoluída no nível 4 movida para vazio e trocada com outra; preserva referência/forma/nome/posição/alcance/intervalo. Cancelar não move; iniciar onda limpa fonte; durante combate e estados finais é bloqueado. |
| Combate | Alvo mais avançado, alcance, intervalo, dano único, readquisição após remoção. Animação a 0,5 e 120 FPS produz o mesmo número de ataques/dano. |
| Morte/recompensa | Resultado/recompensa únicos com sinais repetidos; chegada não paga. Morte deixa de ser ameaça imediatamente, mesmo enquanto o sprite desaparece. |
| Dez ondas/chefe | Configuração validada; exatamente um chefe, último surgimento da onda 10. Configurações inválidas recusadas. Matar chefe antes de outro inimigo não vence ainda. |
| Vitória/derrota | Onda 10 completa, chefe morto e vidas positivas; chefe chegando causa derrota imediata; derrota prevalece sobre vitória. |
| Encerramento/reinício | Cancela surgimentos/ameaças, bloqueia compras/ataques; efeito da última morte pode concluir. Reinício limpa também efeitos, seleção/modo de movimento, ownership e eventos antigos, restaurando 150 moedas/5 vidas. |
| Interface | Cliques simulados nos controles reais para selecionar, invocar, melhorar, iniciar e reiniciar; UI fora das bases. |
| Recursos | 136 quadros em AtlasTexture: regiões dentro das folhas, canvas constante e isolamento da amostragem. |

### Simulações com jogo real

Sem alterar saldos, dano, vida de inimigos ou ondas da produção. Só ações públicas de seleção, invocação, melhoria e início de onda; planos em `Tests/Scenarios/simulation.json`. Os resultados esperados foram fixados depois da primeira medição, para detectar regressões. O tempo é simulado a 60 FPS, sem espera humana entre ondas.

| Plano | Resultado | Onda | Moedas | Vidas | Melhorias aceitas | Mortes / chegadas | Tempo |
| --- | --- | --- | ---: | ---: | ---: | --- | --- |
| Três Pokémon com melhorias | Vitória | 10/10 | 282 | 5 | 14 | 98 / 0 | 245,80 s |
| Três Pokémon sem melhorias | Derrota | 9/10 | 902 | 0 | 0 | 74 / 5 | 251,55 s |
| Sem defesas | Derrota | 1/10 | 150 | 0 | 0 | 0 / 5 | 30,33 s |

Plano vencedor: Bulbasaur na base 2, Charmander na 5 e Squirtle na 8; melhorias antes das ondas e em instantes programados do combate. Terminou com Venusaur Nv.7 (66 dano), Charmeleon Nv.6 (25) e Wartortle Nv.4 (30). Das 16 tentativas de melhoria, duas foram recusadas por saldo insuficiente, sem débito; 14 aceitas. Contabilidade: `150 − 1200 + 1332 = 282`.

Houve exatamente um Rei Ogro: onda 10, `pending=0` no surgimento, vida 1045 (550×1,9), velocidade 55, recompensa 80. Ele foi derrotado; não chegou ao baú. A vitória aguardou todos os 98 inimigos da partida. No plano sem melhorias, seis ameaças ainda ativas foram canceladas na derrota, sem recompensa adicional.

### Balanceamento: leitura dos dados

- A estratégia legal com melhorias vence em cerca de quatro minutos, sem vazamentos. Há margem de segurança para essa agenda/posicionamento; isso não prova facilidade para todo jogador.
- Sem investir, o jogador acumula 902 moedas e perde na nona onda: a dificuldade exige progressão, não apenas invocar os três. Esse contraste é um sinal útil para orientar tutoriais/mensagens em uma entrega futura, sem acrescentá-los agora.
- Uma evolução final e duas intermediárias foram suficientes para vencer. Não foi necessário levar todos ao nível 10 nem alterar os atributos pedidos.
- Duas compras programadas falharam no instante escolhido apesar do saldo final positivo. Não há repetição automática oculta: o log mostra as recusas e permite ajustar a agenda.
- As ondas/vida base foram configuradas para três defensores e medidas; o balanceamento para nove genéricos não foi transportado. Os cenários são determinísticos e não substituem testes com jogadores.

## 2. Inspeção visual realmente realizada

Li as sete folhas originais e a referência visual antes de organizar recursos. Inspecionei também capturas renderizadas da nova Main: início, combate, evolução, chefe e resultado, além de prévias dos sprites em repouso, preparação/impacto do ataque, reação ao dano, movimento por trás e morte.

- [Main inicial](../TestResults/validation-20261019-213735/tres-pokemon-com-melhorias-initial.png): nove bases vazias, retratos reais, painéis claros, verde/dourado, textos e botões Godot.
- [Chefe em combate](../TestResults/validation-20261019-213735/tres-pokemon-com-melhorias-boss.png): tamanho maior, nome/vida destacados, inimigos e Pokémon visíveis no tabuleiro.
- [Todas as formas](../TestResults/validation-20261019-213735/art-idle.png): nove aparências com linha de apoio nos pés.
- [Ataque/impacto](../TestResults/validation-20261019-213735/art-attack-impact.png) e [morte](../TestResults/validation-20261019-213735/art-death.png): quadros efetivamente disponíveis nos originais.

Nas prévias não vi cortes relevantes no corpo principal das sequências usadas, e o apoio foi estabilizado. A escala é constante por folha; não há redimensionamento a cada quadro nem giro do sprite no caminho. Há variações de desenho/tamanho inerentes às poses e halos coloridos. Pontas de asas, armas, água, fogo e partículas podem atingir limites já presentes nos originais ou se misturar à coluna vizinha: não certifico que todos os apêndices/efeitos estejam completos. Ver [assets.md](assets.md).

O título apareceu íntegro nas capturas da interface nova, inclusive com o chefe. Isso não demonstra que toda oscilação visual seja impossível. Capturas são momentos isolados; ainda não representam uma revisão humana de cada transição em movimento.

## 3. Testes que ainda dependem do desenvolvedor

- F5 no editor aberto, mouse físico, cancelar movimento, troca de bases e redimensionamento no seu monitor.
- Jogar sem seguir a agenda automatizada e avaliar dificuldade, clareza das melhorias e ritmo.
- Observar animações em movimento, especialmente efeitos que chegam às bordas das folhas, mudanças de orientação e morte do chefe.
- Conferir se o tamanho dos sprites, níveis e painéis está confortável no tamanho final da janela.

Sem downloads de assets, plugins instalados ou commits. Imagens originais preservadas.
