# Resultado da validação — 19/10/2026

## 1. Verificações executadas

Ambiente localizado nesta execução, sem presumir os caminhos anteriores:

- Editor nos processos: `E:\Download\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe`.
- Executável console descoberto na mesma instalação: `Godot_v4.7.2-stable_mono_win64_console.exe`.
- `--version`: `4.7.2.stable.mono.official.ed1daf0bf`.
- `dotnet` localizado no PATH: `C:\Program Files\dotnet\dotnet.exe`.
- SDK efetivo: `10.0.400`; runtimes Microsoft.NETCore.App 8.0.11, 8.0.15 e 10.0.11.
- Projeto: `Godot.NET.Sdk/4.7.2`, alvo `net8.0`. Build efetivamente executado com zero erros e zero avisos.

Diretório da execução final: `TestResults/validation-20261019-204600/`. Consulte [relatório JSON](../TestResults/validation-20261019-204600/validation.json), [simulações](../TestResults/validation-20261019-204600/simulation.json) e [log da simulação](../TestResults/validation-20261019-204600/simulation.stdout.log). Esses arquivos são evidências locais, não fontes versionadas.

Todas as oito etapas executadas passaram: descoberta da versão da Godot, dotnet-info, build, importação, execução da Main, CombatChecks, MatchChecks e simulação com renderização OpenGL. Os processos da Godot retornaram zero, sem `ERROR`, `SCRIPT ERROR` ou `WARNING` nos logs. Os três executores de testes emitiram seus marcadores finais. O modo estrito reprova a cobertura do pedido porque há regras não implementadas; isso é separado do sucesso da execução do código atual.

Após medir os quatro planos, fixei seus resultados esperados e a exigência de alcançar todas as ações. A execução final passou nessas expectativas. Também executei um teste negativo separado: plano sem defesas exigindo vitória. O jogo realmente perdeu; a simulação preservou o JSON, marcou `baselineMatches=false`, registrou erro e saiu com código 1. A [evidência desse teste negativo](../TestResults/validation-20261019-204600/negative-baseline/probe.json) confirma que divergência de resultado não é silenciosamente aprovada. O erro desse teste controlado é esperado e não pertence aos logs da execução normal.

| Requisito pedido | Resultado | Evidência/limite |
| --- | --- | --- |
| Bloqueio de Pokémon repetido, inclusive após evolução | NOT_IMPLEMENTED | Só existe uma cena genérica, sem identidade de linhagem. Criaturas genéricas repetidas são permitidas. Não confundir com bloqueio de espaço ocupado, que passou. |
| Compra e melhoria sem saldo | PASS no jogo atual | MatchChecks verifica recusa, mensagem, ausência de débito e atributos inalterados. A simulação também registrou uma melhoria recusada sem débito. |
| Evolução preserva nível, posição, alcance e intervalo | NOT_IMPLEMENTED | Não há formas nem evolução. Testar melhoria genérica não prova preservação durante evolução. |
| Reposicionar/trocar só entre ondas | NOT_IMPLEMENTED | Não há ação de movimento/troca na implementação. |
| Recompensa única | PASS | Dano/sinais repetidos não repetem recompensa ou descontam vida novamente; chegadas não pagam. |
| Dez ondas e exatamente um chefe na última | NOT_IMPLEMENTED | Configuração efetiva tem cinco ondas e nenhum tipo de chefe. Não simulei dez ondas artificiais para contornar isso. |
| Vitória, derrota e reinício | PASS no jogo atual | Testes de regra e partidas reais; reinício restaura 100 moedas, cinco vidas e tabuleiro vazio, conforme a implementação atual. Ainda não valida o estado inicial de 150 moedas do anexo. |

Os testes também cobrem limite/isolamento de melhoria, início duplicado, pendentes + vivos, derrota prevalecendo sobre vitória, cancelamento com surgimentos pendentes e inimigos vivos, ataques interrompidos, sinais antigos após reinício e cliques simulados nos controles reais.

### Partidas medidas

Configuração de produção intacta; 60 FPS fixos; tempo abaixo é tempo simulado, não duração da execução na máquina. Os planos e todas as tentativas estão no JSON de simulação.

| Plano | Resultado | Onda | Moedas | Vidas | Invocações | Melhorias aceitas | Mortes / chegadas |
| --- | --- | --- | ---: | ---: | ---: | ---: | --- |
| Invocação e melhorias | Vitória | 5/5 | 150 | 5 | 4 | 6 | 45 / 0 |
| Três unidades com melhorias | Vitória | 5/5 | 150 | 5 | 3 | 6 | 45 / 0 |
| Duas unidades sem melhorias | Derrota | 5/5 | 380 | 0 | 2 | 0 | 38 / 5 |
| Sem defesas | Derrota | 1/5 | 100 | 0 | 0 | 0 | 0 / 5 |

Na derrota da estratégia de duas unidades, 45 inimigos surgiram, 38 morreram, cinco chegaram e dois foram cancelados ao terminar: cancelamento não concedeu recompensa. Em ambos os planos vencedores: 100 iniciais − 400 gastos + 450 ganhos = 150 finais.

O plano de três unidades tentou uma sétima melhoria no segundo 6 da onda 5: havia 40 moedas para um custo de 50. A tentativa foi recusada sem débito; não houve crédito artificial nem repetição automática escondida. A partida ainda venceu com seis melhorias aceitas.

### Leitura de balanceamento

- Há uma estratégia vencedora medida com três criaturas **genéricas atuais**, seis melhorias, nenhuma perda de vida e 150 moedas sobrando. Isso sugere margem de segurança no balanceamento de cinco ondas para essa posição/agenda; não prova que todas as estratégias são fáceis.
- Guardar moedas sem investir não compensa poder de fogo insuficiente: duas unidades perderam com 380 moedas. Esse contraste pode orientar mensagens de incentivo a melhorias, se desejado; nenhum ajuste foi feito nesta entrega.
- Comprar em um instante fixo pode falhar por fluxo de caixa mesmo quando o saldo final é alto. A melhoria recusada mostra a importância de registrar o momento da ação e não só o resultado final.
- Nada disso valida a economia dos três Pokémon do anexo: dano, alcance, intervalo, evolução, limite de unidades, dez ondas e chefe serão diferentes. Esses dados exigem novas simulações após implementação. Não recomendo transportar o balanceamento atual sem medições.
- São cenários determinísticos, não um levantamento de dificuldade para jogadores nem evidência estatística de equilíbrio geral.

## 2. Verificações visuais realmente realizadas

Inspecionei quatro imagens produzidas pela renderização real desta execução:

- [Estado inicial](../TestResults/validation-20261019-204600/tres-unidades-com-melhorias-initial.png): tabuleiro vazio, cinco ondas, 100 moedas e cinco vidas; painéis fora das bases.
- [Combate](../TestResults/validation-20261019-204600/tres-unidades-com-melhorias-combat.png): inimigos na rota, barras de vida, duas defesas já invocadas naquele instante, seleção e alcance visíveis; botões indisponíveis distinguíveis.
- [Vitória](../TestResults/validation-20261019-204600/tres-unidades-com-melhorias-result.png): resultado e botão Jogar novamente.
- [Derrota](../TestResults/validation-20261019-204600/sem-defesas-result.png): vidas zeradas e resultado de derrota.

Achado visual: na captura do combate, o título SUMMONERGUI aparece parcialmente cortado no topo, enquanto está inteiro na imagem inicial e na final. A causa/intermitência ainda não foi investigada; não considero a apresentação visual inteiramente aprovada. Capturas de momentos isolados também não validam fluidez ou ausência de saltos de animação.

Não realizei inspeção visual dos sprites Pokémon/inimigos fornecidos nem validação de recortes/alinhamento: eles não estão integrados ao jogo atual. Não alterei os arquivos originais.

## 3. Testes que ainda dependem do desenvolvedor

- Mouse físico, F5 no editor aberto, redimensionamento e legibilidade no tamanho real de uso.
- Observar o título durante combate para reproduzir o corte da captura e avaliar a fluidez/clareza dos ataques.
- Avaliar diversão/dificuldade sem conhecer as estratégias automatizadas e ajustar balanceamento se necessário.
- As regras marcadas NOT_IMPLEMENTED dependem primeiro de implementação; não são apenas testes manuais pendentes. Depois disso, testar também evolução, troca de sprites, reposicionamento, chefe e suas animações.

Nenhum plugin instalado, nenhum commit criado e nenhuma regra de produção modificada nesta validação.
