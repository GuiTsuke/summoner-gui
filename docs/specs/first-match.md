# Primeira partida jogável

Documento histórico da entrega de cinco ondas; a versão atual segue [pokemon-match.md](pokemon-match.md).

Escopo solicitado: partida curta de cinco ondas, com seleção de nove espaços, um único tipo de criatura/inimigo, moedas, melhorias, vidas do baú, vitória, derrota e reinício. Reutilizar movimento e combate; sem salvamento, coleção, sorteio, novos tipos, venda, movimentação ou fusão.

## Comportamento aceito

- Começar com 100 moedas, cinco vidas, nove espaços vazios e nenhuma onda em andamento.
- Selecionar espaço com mouse; invocar nele por 50 moedas. Espaços ocupados não recebem outra criatura. Saldo insuficiente não altera estado e apresenta mensagem.
- Criaturas começam no nível 1. Melhoria custa `25 × nível atual`, acrescenta cinco de dano e limita nível a cinco. Demais atributos não mudam. Compras funcionam também durante combate.
- Cada morte concede dez moedas uma vez. Chegada retira uma vida e não concede moedas. Resultado de cada inimigo é exclusivo.
- Ondas: quantidades 5, 7, 9, 11, 13; multiplicadores de vida 1, 1,2, 1,4, 1,6, 1,8. Intervalo inicial 1,2 s. Vida base ajustável ao combate existente.
- Início de cada onda é manual. Bloquear novo início até zerar surgimentos pendentes e inimigos vivos; mostrar a soma como restantes.
- Zero vidas encerra em derrota. Última onda resolvida com vidas positivas encerra em vitória. Derrota tem precedência sobre conclusão da onda.
- Resultado interrompe surgimentos, ataques e compras. Jogar novamente limpa inimigos, criaturas, seleção, moedas, vidas e ondas.
- Interface em português, indicadores acima e ações abaixo do tabuleiro. Nível discreto nas criaturas; alcance ajustável no editor. Sem baixar assets/plugins ou criar commits.

## Validação

Compilação; testes reais na Godot para compras inválidas, melhorias/limite/isolamento, início duplicado, recompensas únicas, contagem de pendentes/vivos, estados finais e reinício. Simular uma estratégia válida com o balanceamento padrão até vitória e uma partida sem defesas até derrota. Inspecionar renderização; distinguir isso de testes manuais do mouse/editor.
