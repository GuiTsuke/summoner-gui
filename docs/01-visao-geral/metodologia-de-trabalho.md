# Metodologia de trabalho — SummonerGui

> Status: padrão de colaboração aprovado pelo criador.
> Escopo: conversas 01–06, documentação, planejamento e interação com Codex.
> Fonte de verdade: documentos versionados neste repositório.

## Princípios

1. **Decidir conscientemente:** distinguir proposta, hipótese, pendência e decisão aprovada. Não promover ideias a regras oficiais sem confirmação explícita.
2. **Fonte única de verdade:** consultar os documentos atuais do GitHub antes de continuar uma área ou alterar um registro; memória de conversa auxilia, mas não substitui os arquivos.
3. **Separar AS-IS e TO-BE:** preservar o histórico do protótipo e conferir código/testes para descrever o estado implementado; documentar separadamente a visão desejada e a transição.
4. **Desenvolver incrementalmente:** primeiro alinhar visão e requisitos suficientes, depois especificar, implementar, testar e validar. Não é necessário encerrar todo o universo antes de desenvolver.
5. **Colaboração crítica:** avaliar alternativas, consequências e riscos; fazer recomendações fundamentadas, sem concordância automática ou expansão descontrolada do escopo.

## Áreas responsáveis

| Chat | Responsabilidade |
| --- | --- |
| 01 — Visão Geral e Decisões | Direção do produto, prioridades, coerência entre áreas e decisões estratégicas |
| 02 — História e Universo | Essência, Selumins, pactos, culturas, personagens e narrativa |
| 03 — Identidade Visual | Direção artística, sprites, animações, cenários e interfaces |
| 04 — Game Design | Mecânicas, combate, coleção, invocação, economia e progressão |
| 05 — Especificações e Marcos | Requisitos, MVP, critérios de aceite, versões e marcos |
| 06 — Engenharia e IA | Godot/C#, arquitetura, Git, Codex, agentes, testes e automação |

Decisões com impacto em outras áreas devem referenciar seus responsáveis sem duplicar definições divergentes.

## Padrão de comunicação

- Responder de forma amigável, direta, resumida e suficientemente fundamentada.
- Evitar textos excessivamente longos, repetições e muitas perguntas de uma vez.
- Explicar diferenças relevantes entre alternativas; recomendar caminhos e apontar riscos reais.
- Respeitar decisões aprovadas; explicitar divergências entre fontes em vez de resolvê-las silenciosamente.
- Ao concluir um conjunto de decisões, resumir aprovações, pendências e próximo passo.

## Início ou retomada de conversa

1. Recuperar o contexto anterior disponível, sem presumir que todos os chats foram integralmente recuperados.
2. Consultar a versão atual dos documentos pertinentes em `https://github.com/GuiTsuke/summoner-gui`, principalmente:
   - `docs/01-visao-geral/visao-do-projeto.md`
   - `docs/01-visao-geral/registro-de-decisoes.md`
   - `docs/01-visao-geral/questoes-pendentes.md`
   - índice da área especializada em `docs/0X-.../LEIA-ME.md`, quando existente.
3. Distinguir o que é **aprovado**, **proposto** e **pendente**.
4. Verificar se o assunto já foi resolvido ou tem dependência em outra área.
5. Retomar apenas as questões pertinentes ao objetivo da conversa.

Quando os arquivos não estiverem acessíveis, informar a limitação e solicitar acesso ou conteúdo, sem fingir consulta.

## Ciclo de decisão e documentação

1. Conversar e explorar alternativas.
2. Identificar impactos e dependências.
3. Confirmar explicitamente a decisão com o criador.
4. Preparar a atualização do documento temático, do índice de decisões e do registro de pendências (conforme aplicável).
5. **Somente após concordância com a atualização documental**, consultar novamente a versão mais recente e aplicar as alterações autorizadas.
6. Criar commit descritivo no GitHub, quando o acesso permitir, sem sobrescrever alterações concorrentes.
7. Informar caminhos alterados, commit/SHA e eventual necessidade de `git pull` no repositório local.

Não realizar commit automático para mera proposta, exemplo narrativo ou hipótese. Não executar operações destrutivas ou force push. Se houver conflito com alterações locais/remotas, parar e apresentar opções.

## Organização das informações

- `registro-de-decisoes.md`: índice de DEC com status e área responsável.
- `questoes-pendentes.md`: índice de PEN com alternativas, responsável, gatilho de retomada e status.
- Documentos de cada área: detalhamento das decisões; evitar duplicação desnecessária.
- Git: histórico das versões; não gerar arquivos `final-v2-final` como mecanismo de versionamento.
- As definições do jogo (TO-BE) não são automaticamente funcionalidades implementadas (AS-IS).

## Relação com Codex

Este documento orienta a **colaboração e a governança das decisões**. Um `AGENTS.md` específico poderá traduzir as decisões em regras operacionais para agentes que inspecionam código, editam arquivos, executam testes e entregam implementações. Não alterar o `AGENTS.md` existente sem avaliação no chat 06.

## Objetivo

Construir um Tower Defense autoral divertido e um universo sustentável dos Selumins, mantendo documentação confiável, escopo consciente e entregas verificáveis. O processo deve poder ser adaptado a outros projetos futuros, sem impor complexidade desnecessária.
