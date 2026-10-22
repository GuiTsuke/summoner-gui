# Ataques e evolução — v5

Implementado conforme PROMPT-CODEX.txt, LEIA-ME.txt e ASSET-MANIFEST.json da entrada. O único novo PNG ativo é `Assets/Art/Effects/efeitos-elementais.png`, cópia do fornecido, com 16 recortes individuais em `Resources/Animations/elemental-effects.tres`. Pokémon ativos preservados: as folhas em Preserved não trazem novas poses. A inspeção confirmou repouso sem projétil nas primeiras colunas; ataques agora usam repouso com antecipação horizontal discreta, sem os efeitos antigos pintados nas poses de ataque. Não foi gerada caminhada ou vista traseira.

## Implementação e configuração

- `Resources/Attacks/{bulbasaur,charmander,squirtle}.tres`: elemento, velocidade em pixels/s, tempo de contato/recolhimento, limite de vida do efeito, emissores por forma e lado, chance/dano/duração/cadência da queimadura. Pontos em pixels locais relativos ao centro da base. Mouth no fogo/água; lados da planta nos cipós; dois emissores no Blastoise. Os pontos Left são ajustes de espelhamento, não novas vistas fornecidas.
- `ElementalAttack.cs`: um fluxo comum para alvo válido, valores capturados, contato e dano único. Fogo persegue alvo móvel; água avança até o alvo; cipós usam dois arcos, atlas e recolhimento, validando alcance no contato. Só efeitos giram. Alvo removido cancela sem dano/recompensa extra. Limite de 48 ataques simultâneos; sem pooling/framework. Linha genérica removida da cena.
- `Enemy.cs`: uma queimadura por inimigo. Reaplicação renova duração, preserva maior dano e não reinicia o relógio do próximo tick. Ticks chamam TakeDamage, sem sortear queimaduras. Visual termina ao expirar, morrer, chegar ou reiniciar.
- Fogo inicial: chance `min(0,60; 0,10 + 0,02 × (nível−1))`, tick `(1 + 0,5 × (nível−1)) × multiplicador da forma`, três segundos, um tick/s. Para testes, chance 100% usa BaseChance=1 e Cap=1; zero usa BaseChance=0 e ChancePerLevel=0.
- `Resources/Pokemon/*.tres`: alcance, intervalo, níveis/custos de evolução, multiplicadores e ligação para configuração do ataque. Bulbasaur agora 100 px; Charmander 120; Squirtle 155. Valores não crescem com nível/forma.
- Evolução comprada por GameSession.TryEvolve: nível 4/150 moedas e nível 7/400, multiplicadores absolutos 1/2/3,5. Preserva referência, nível, linhagem, posição e melhorias. FormAbilityNotes oferece apenas dados de descrição por forma; habilidades adicionais seguem indefinidas. Não há árvore nem habilidades inventadas.
- Main e Main.tscn: botão Evoluir com nome, nível mínimo e custo antes da compra; controles compactados no painel existente. Cenário, rota, menus e áudio não foram substituídos. Upgrade/evolve continuam emitindo os eventos sonoros existentes.

O atlas de água aponta originalmente para a esquerda: o efeito é espelhado antes de orientar o jato. Fogo aponta para a direita. Cipós têm dois ramos desenhados. `Tools/Build-ElementalFrames.ps1` regenera apenas recursos de texto, sem editar o PNG. Recortes ajustam fronteiras porque fases excedem a grade nominal; canvas 512×256 e pivôs em `effects-inspection.json`. Halos com alfa muito baixo podem ser omitidos pelos recortes. Nearest ativo.

## Verificação executada

```powershell
powershell -NoProfile -File .\Tests\Run-Validation.ps1 -ElementalOnly -Capture
```

Build, importação, Main, CombatChecks, MatchChecks e ElementalChecks passaram em `TestResults/validation-20261019-224053`, sem erros/avisos. A primeira importação revelou que a nova configuração de ataque precisava de `[Tool]` para ser acessada pelo recurso Pokémon usado no editor; corrigido e importação refeita.

Após separar as fixtures visuais para evitar unidades/efeitos de testes anteriores sobrepostos, executei novamente somente build e ElementalChecks: `TestResults/v5-focused-final/elemental.stdout.log`, código 0 e stderr vazio. Conferidos três ataques, dano diferido/único, alvo móvel, morto/removido, valores preservados ao melhorar/evoluir durante voo, perda de alcance dos cipós, queimadura 0%/100%, três ticks até expirar, reaplicação sem empilhar/atrasar cadência, maior dano, morte por queimadura, recompensa única e limpeza no reinício. MatchChecks cobre compras, saldo, requisitos e preservação/duplicidade após evolução. CombatChecks compara FPS visual baixo/alto, alvo e movimento.

Inspecionei capturas reais dos três efeitos avançando, emissores iniciais e chama de queimadura, além do painel com o novo botão e requisitos. Não houve inspeção visual contínua de todas as formas/direções. Quadros de ataque embutidos antigos continuam disponíveis no recurso para referência, mas não são usados pelo combate.

PresentationChecks também executado separadamente após as mudanças: passou, sem erros/avisos, incluindo som separado de melhoria/evolução, resultado e música única após reinícios. Logs em `TestResults/v5-focused-final/presentation.*`. Não houve audição humana nesta execução.

Economia: orçamento máximo teórico de 1.482 moedas (150 iniciais + 1.332 se todos os inimigos forem derrotados). Sequência real de compras em fixture com esse saldo: três invocações + uma unidade no nível 7 + ambas evoluções = 1.225, sobra 257. Custos propostos mantidos. Isso verifica custo e possibilidade no orçamento total, não quando o dinheiro fica disponível nem dificuldade real. Não foram repetidas partidas completas; planos/resultados antigos ainda descrevem evolução automática e precisam de nova agenda para uma comparação válida. As expectativas antigas da simulação não foram silenciosamente relaxadas.

## Pendências e teste manual

F5: invoque cada linhagem, observe contato e orientação dos efeitos, melhore até nível 4 e compre Evoluir. Confirme requisito/custo antes de pagar e ausência de evolução automática; no nível 7 compre forma final. Observe dois canhões no Blastoise, emissores das formas maiores, ataque à esquerda, queimadura e reinício. Ajustes finos de emissores e ritmo em movimento dependem desse teste; não afirmo ter revisado todos eles visualmente.

Novas folhas padronizadas, vistas e habilidades especiais adicionais continuam pendentes no pacote. Sem downloads, plugins, commits ou alterações globais de agentes.
