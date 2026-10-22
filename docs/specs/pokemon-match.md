# Partida com Pokémon e dez ondas

Implementar integralmente o anexo confirmado em 19/10/2026, mantendo a validação real já preparada.

- 150 moedas, cinco vidas, nove bases vazias; apenas uma unidade permanente de cada linhagem Bulbasaur, Charmander e Squirtle (50 moedas cada). Invocação/melhoria também durante combate.
- Atributos iniciais: 20/1,3s/185px, 10/0,65s/120px, 15/1s/155px. Alcance/intervalo fixos por linhagem.
- Níveis 1–10, preço25×nível; formas nos níveis1/4/7. Dano=base×(1+0,20×(nível−1))×multiplicador1/1,25/1,5. Evolução preserva objeto, linhagem, nível, base e atributos fixos; efeito breve sem pausar.
- Modo Reposicionar antes/entre ondas: escolher fonte e destino vazio/ocupado para mover/trocar, gratuito, preservando estado; cancelar/iniciar onda limpa fonte. Bloqueado em combate e estados finais.
- Slime, goblin, golem e um Rei Ogro exclusivamente no último surgimento da onda10. Recompensas10/12/20; chefe configurável. Comuns retiram uma vida; chefe causa derrota imediata. Resultado único por inimigo, morte sai imediatamente da contagem/alvos, animação pode terminar depois.
- Dez composições explícitas com vida e intervalo configuráveis; fim exige pending+alive=0. Vitória exige onda10 inteira resolvida, chefe derrotado e vidas>0; derrota prevalece.
- Inspecionar folhas originais (não modificar), recortes e apoio nos pés; animações separadas do relógio de combate, sem girar personagens nas curvas. UI real inspirada na referência com painéis claros/verde/dourado, retratos e informações de progressão. Sem plugins, downloads, commits ou sistemas extras.
- Validar regras com Main real, logs, simulações programadas e capturas. Declarar limitações dos assets e distinguir inspeção visual de execução automática.
