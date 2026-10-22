# Validação repetível da expansão

Para apresentação/áudio: `powershell -NoProfile -File .\Tests\Run-Validation.ps1 -PresentationOnly -Capture`. Inclui Main, galeria, combate, regras locais e PresentationChecks, sem simulações completas. Não combine com `-RequireCompleteCoverage`. Nesse modo, `requestedCoverageComplete=false` indica que não foram medidas partidas completas. Ver [entrega de assets/áudio](audio-assets-v4.md).

O jogo atual implementa três linhagens únicas, evolução, movimento entre ondas, dez ondas e chefe.
O relatório histórico de cinco ondas está em [validation-five-waves.md](validation-five-waves.md); o atual em [validation-report.md](validation-report.md).

## Um comando

Na raiz, com a Godot .NET aberta ou no PATH:

```powershell
powershell -NoProfile -File .\Tests\Run-Validation.ps1 -Capture -RequireCompleteCoverage
```

Se não for encontrada, informe `-GodotPath '<executável .NET instalado>'`.
O script consulta processos e PATH, confirma GodotSharp e registra `--version`; dotnet vem de Get-Command e `--info`.
Não presume caminho/versão nem instala plugins/ferramentas.

Cada execução cria TestResults/validation-<data-hora> novo, com .gdignore, logs stdout/stderr, validation.json, simulation.json e capturas opcionais.
Sem -Capture, usa headless; com a opção, renderiza via Godot real. Não confunda produzir imagens com inspecioná-las: a declaração de inspeção humana é separada.

Nove etapas: descoberta da versão, dotnet-info, build, importação, execução da Main, ArtChecks, CombatChecks, MatchChecks e SimulationChecks.
Testes exigem saída0, ausência de erros no log e marcador final de sucesso. Timeout, erros ou marcador ausente reprovam. Avisos ficam contabilizados.
Timeout real por etapa120s (configurável); máximo180s simulados por onda para detectar travamento.

Saídas com powershell -File: 0 execução/regras aprovadas; 1 falha; 2 cobertura solicitada incompleta no modo estrito.
A expansão atual completa a cobertura automatizada e o modo estrito retorna0. Isso não certifica sprites perfeitos ou facilidade/diversão.

## Sem duplicar a implementação

SimulationChecks instancia Main e usa GameSession.SelectSlot, TrySummon(linhagem), TryUpgrade e TryStartWave.
Movimento usa BeginReposition/SelectSlot. Não calcula combate, spawn, preços ou evolução em um modelo paralelo, nem altera o saldo/vida/atributos para fabricar vitória.
O relógio real da Godot avança com delta fixo1/60. Cada nova onda é iniciada pela API do botão; o jogo continua exigindo início manual fora do driver.

O JSON define ações: wave, kind (summon/upgrade/move), slot, lineage na invocação, targetSlot no movimento.
Sem seconds, executa antes da onda; com seconds, no primeiro quadro a partir daquele instante durante combate.
Recusas são registradas, sem repetição automática. Agenda de movimento em combate é recusada pelas mesmas regras da UI.

Registra cada tentativa, custo observado, saldo, nível/dano, unidade final, snapshots por onda, tipo/vida/velocidade dos surgimentos, mortes, chegadas, cancelamentos e chefe.
Concilia moedas pelos débitos/recompensas observados e vidas pelas perdas reais (a chegada do chefe pode consumir todas).
Cenário de melhoria exige ao menos uma compra aceita. Não depende de mensagens do console para contar resultados.

Os planos possuem expectedOutcome, fixado depois da primeira medição, para detectar regressão de vitória/derrota; requireAllActionsReached evita uma partida interrompida deixar ações sem executar silenciosamente.
Para levantar outro balanceamento livremente, remova expectedOutcome e use requireAllActionsReached=false se quiser permitir agendas interrompidas por derrota.
Divergências são relatadas após preservar o JSON. O resultado continua sendo determinado pelo jogo, nunca pelo teste.

## Suítes

- ArtChecks: 136 quadros, regiões dentro dos PNGs, canvas constante e amostragem isolada; prévias reais de todas as formas, ataques, reações, verso e morte.
- CombatChecks: alvo, alcance, cooldown, dano único, readquisição, animação após resolução e comparação entre FPS visual0,5 e120.
- MatchChecks: configuração10ondas/chefe, unicidade após evolução, custos/saldo/cap, fórmula, preservação de unidade/atributos, movimento/troca/cancelamento, UI, sinais duplicados, vitória aguardando restantes, chegada fatal do chefe, cancelamento e reinício inclusive de efeitos.

Os testes isolados podem configurar saldo alto, ondas privadas ou dano letal para casos-limite. Eles não são a prova de viabilidade do combate padrão.
Essa prova vem das simulações, que mantêm a configuração de produção intacta.
O runner não marca inspeção humana como realizada automaticamente: confira as imagens e relate o que de fato observou.
