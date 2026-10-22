# Plano da primeira partida

Plano histórico da entrega de cinco ondas, substituído por [pokemon-match.md](pokemon-match.md).

1. Preservar cenas reutilizáveis e proteção de resolução única em Enemy. Adicionar recompensa configurável e cancelamento sem resultado para encerramento/reinício.
2. WaveDirector cuida somente de sequência/configuração de ondas, surgimentos, inimigos ativos e notificações de resolução. WaveDefinition é um Resource editável no Inspector.
3. GameSession guarda moedas, vidas, seleção e resultado; valida invocação/melhoria e reage aos eventos das ondas. CreatureSlot guarda ocupação e destaca seleção. Creature mantém combate e nível.
4. Main conecta botões, indicadores e mensagens à sessão. Reorganizar verticalmente a rota para reservar rodapé sem sobrepor espaços clicáveis. Recursos visuais continuam locais.
5. Atualizar testes de combate para encontros isolados e adicionar testes de partida sem dependências externas. Validar regras críticas, vitória viável e derrota; revisar com Qualidade e inspecionar capturas.

Riscos principais: reentrada dos eventos ao encerrar uma onda, remoções adiadas por QueueFree, compras duplicadas, UI desatualizada e callbacks de uma partida anterior após reinício. Usar estados explícitos, conjunto de inimigos ativos e cancelamento síncrono antes da remoção adiada.

## Implementação e verificação

- Responsabilidades implementadas conforme o plano, com referências aos nós e configurações no Inspector. Vida base inicial: 60; recompensa: dez; demais valores seguem a spec.
- Qualidade revisou o fluxo de resolução, encerramento, compras e reinício sem apontar falha concreta. A lacuna identificada (derrota com vivos + pendentes) foi coberta por teste adicional que também verifica ataques e sinais antigos após encerramento.
- Build sem erros/avisos. Testes de combate e partida executados na Godot .NET 4.7.2. A simulação usa os preços/atributos padrão e ganhou as cinco ondas com cinco vidas; sem defesas perdeu por cinco chegadas.
- Interface exercitada com cliques simulados nos controles reais e captura de renderização inspecionada. Mouse físico, F5 no editor já aberto, edição do balanceamento e redimensionamento ficam para validação manual do desenvolvedor.
