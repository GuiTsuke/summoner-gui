# Plano da expansão confirmada

1. Preservar cenas/fluxos; criar PokemonDefinition e EnemyDefinition como recursos de configuração. Separar estado da unidade e apresentação animada.
2. Estender GameSession para unicidade, evolução/melhoria e modo de movimento; WaveDirector para composições, chefe e dez ondas; manter guardas de resolução e limpeza de efeitos.
3. Inspecionar os sete PNGs e gerar SpriteFrames/AtlasTextures por regiões individuais, sem escrever imagens. Apoio consistente e escala única por folha; documentar variações reais/halos/limites.
4. Adaptar tabuleiro e UI com Controls inspecionáveis e retratos. Instanciar as mesmas cenas reutilizáveis.
5. Qualidade implementa testes de regra nos arquivos de testes existentes. Orquestrador atualiza runner/simulações, mede balanceamento, corrige regressões e inspeciona renderização e animações.

A autorização do usuário cobre a expansão completa do anexo. Sem novos frameworks ou plugins; nenhum commit. O teste de partida usa somente ações legais, sem alterar moedas/vida/configuração da produção.

## Entrega e validação

- Recursos de três Pokémon, quatro adversários e dez ondas explícitas; UI real com retratos, níveis, dano atual/próximo, custos, próxima evolução e modo de movimento.
- AtlasTextures gerados por regiões/apoio, originais preservados; validação dos 136 quadros e galeria renderizada. Limitações de halos/efeitos nas bordas em `docs/assets.md`.
- Qualidade revisou regras e implementou testes. A última morte passou a preservar sua animação até concluir; reinício limpa efeitos. Testes de movimento usam unidade evoluída e comparam a mesma referência/estado.
- Build sem erros/avisos. Testes reais na Godot: regras, UI, combate, FPS de animação, chefe e reinício. Runner estrito atualizado para a cobertura nova.
- Plano programado com três Pokémon e 14 melhorias venceu dez ondas/um chefe, cinco vidas e 282 moedas, em 245,8 s simulados. Sem melhorias perde na onda9; sem defesa perde na onda1. Ver `docs/validation-report.md` para evidências atuais e distinção entre teste automático/visual/manual.
