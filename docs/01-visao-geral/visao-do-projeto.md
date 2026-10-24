# Visão do projeto — SummonerGui (nome provisório)

> Status: visão inicial aprovada em conversa; detalhes sujeitos a especificação.
> Última atualização: 24/10/2026 (horário de São Paulo).

## Proposta

SummonerGui é, provisoriamente, o nome técnico de um jogo **Tower Defense estratégico**, inspirado na estrutura de defesa de *Summoner's Greed*, que será a porta de entrada para um universo autoral habitado pelos **Selumins**.

O jogador conhece Selumins, cria vínculos, forma equipes, invoca aliados e defende diferentes objetivos contra ondas de inimigos. Colecionar, desenvolver e evoluir Selumins é tão importante quanto vencer as defesas.

## Pilares aprovados

1. **Defesa estratégica:** enfrentar ondas e proteger objetivos que podem variar de acordo com a fase e a história; não necessariamente um baú.
2. **Coleção permanente:** Selumins obtidos continuam na coleção entre fases e partidas. Níveis, evoluções e melhorias permanentes são preservados.
3. **Obtenção por múltiplos caminhos:** invocações, progressão, missões, eventos e recompensas especiais.
4. **Campanha narrativa:** jornada principal em regiões, capítulos e fases, com diálogos, cutscenes, acontecimentos e momentos marcantes de defesa, invocação e evolução.
5. **Conteúdo complementar:** desafios independentes e eventos também contam histórias e ampliam a longevidade.
6. **Universo centrado nos Selumins:** seres com comunidades, famílias, habitats e vida própria, não meras unidades de combate.

## Premissas narrativas e mágicas aprovadas

- A **Essência** é a fonte fundamental da magia.
- Os **Selumins** possuem conexão natural e profunda com a Essência.
- Os **humanos** podem interagir com a Essência de maneira limitada.
- O conceito central de invocação envolve **vínculo** entre humano viajante e Selumin; seus mecanismos exatos ainda serão detalhados.
- A marca da Essência é um conceito inicial associado aos Selumins, a aprofundar na construção do universo.

## Filosofia da experiência — DEC-006

O jogo será **fácil de aprender, estratégico de dominar e emocionalmente rico para quem quiser se aprofundar**. A defesa deve ser divertida por si só, mesmo para quem não acompanha toda a narrativa. Coleção e evolução constroem vínculos duradouros; a campanha introduz descoberta, aventura, amizade e drama sem prejudicar o ritmo das batalhas.

**Ideias ainda não aprovadas como funcionalidades:** pergaminhos desbloqueáveis com histórias detalhadas, biblioteca narrativa e reprodução de cutscenes. O núcleo narrativo deve permanecer compreensível mesmo para quem não consumir extras opcionais.

## Plataformas — DEC-007

- **Android:** plataforma principal, em orientação **vertical/retrato**.
- **PC:** plataforma complementar planejada, com interface adaptada à tela horizontal, mouse e teclado.
- Compartilhar sistemas, regras e conteúdo; adaptar interface, controles e apresentação.
- A ordem definitiva de disponibilização no mercado ainda será decidida. Sincronização de progresso e iOS não foram aprovados como compromissos de primeira versão.

## Primeiro lançamento — DEC-008

Entregar uma **campanha inicial pequena, porém completa**, com começo, desenvolvimento e conclusão, introduzindo o universo e integrando a história ao Tower Defense. O encerramento deve satisfazer e abrir espaço para novas aventuras.

**Direção narrativa aprovada (DEC-039 a DEC-048):** floresta de nascentes, vila antiga em transformação e comunidades diversas; exploração abusiva da Essência por organização externa com apoiadores locais; protagonista e Selumin incomum se conhecem em confronto por mal-entendido, colaboram sem pacto, enfrentam o chefe que revela ter um pacto, vencem e só então percebem a ressonância que torna possível escolher livremente o próprio pacto ou adiá-lo. O pacto adiado poderá ser oferecido futuramente sem punição irreversível. Ver `../02-universo/primeira-regiao-e-campanha.md`; capítulos, elenco e mecânicas permanecem abertos.

## Experiência pretendida

Defesas, alianças, melhorias e evoluções devem ter peso emocional quando a história pedir, sem impedir acesso rápido à jogabilidade.

**Direção aprovada (DEC-036 a DEC-038):** o mundo abriga outras formas de vida mágica além dos Selumins, com inimigos variados. Derrotas ordinárias não implicam morte automática. A fantasia deve permanecer divertida, animada e encantadora, mas preservar envelhecimento, mudança, esquecimento, perda e consequências reais para criaturas, pessoas e lugares.

## Possibilidade estratégica ainda não aprovada — PEN-027

O criador está explorando se o primeiro título deveria **continuar Tower Defense** e funcionar como introdução/prológo de um universo maior, com **jogos futuros em gêneros diferentes** (RPG, aventura), ou se valeria revisar o gênero já no primeiro jogo. **Nenhuma migração de gênero foi aprovada:** DEC-001, DEC-007 e DEC-008 continuam vigentes. Avaliar no chat 01, com apoio do 04/05, antes de mudar projeto, código, escopo, MVP ou calendário. Há um risco concreto a estudar: a história coloca o primeiro pacto do protagonista depois da vitória, enquanto invocação é pilar do Tower Defense; ver PEN-026.

## Limites desta definição

Este documento **não fixa** raridades, probabilidades, economia, atributos, níveis máximos, detalhes de combate, regras da invocação, história definitiva, orçamento, conteúdo exato do MVP ou calendário de lançamento. Esses temas serão trabalhados nos chats especializados e documentos correspondentes.

## Nomes

- **SummonerGui:** nome provisório do jogo/projeto técnico.
- **Selumins:** nome atual das criaturas e possível identidade do universo; não é necessariamente o título deste Tower Defense.
- **Título comercial do jogo:** pendente, com intenção de remeter à invocação e ao seu significado narrativo.

## Relação com o protótipo

As regras atuais do protótipo Godot/C# não se tornam regras definitivas por constarem nos documentos antigos. O código e os testes serão inspecionados antes de planejar a transição do estado atual (AS-IS) ao estado desejado (TO-BE).
