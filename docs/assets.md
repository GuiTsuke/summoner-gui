# Assets ativos — pacote v4

Atualização v5: os personagens desta página foram preservados. Foram acrescentados 16 recortes de efeitos separados; ver [ataques elementais e evolução paga](elemental-v5.md). As poses antigas de ataque com efeitos embutidos não são mais usadas pelo combate.

As sete folhas do manifesto foram copiadas para `Assets/Art/Source/`, preservando nomes e conferindo SHA-256 contra a entrada. Há uma folha ativa por personagem. Os PNGs de TempAssets permanecem intactos; o jogo usa somente os diretórios definitivos. Terreno e atlas de props ficam em `Assets/Art/Environment/`.

| Folha | Dimensões | Organização |
| --- | --- | --- |
| bulbasaur-evolucoes.png | 1774×887 | 8 colunas × 3 formas; v3 preservada, padronização pendente |
| charmander-evolucoes.png | 1774×887 | 8×3; v3 preservada |
| squirtle-evolucoes.png | 1774×887 | 8×3; v3 preservada |
| slime.png | 1448×1086 | 4×5, referência visual |
| goblin.png | 1306×1205 | 4×5, revisado |
| golem.png | 1536×1024 | 4×5, revisado |
| rei-ogro-boss.png | 1448×1086 | 4×5, revisado |

Todas têm alfa real. O aparente fundo escuro do golem contém RGB residual com alfa zero; não foi necessário apagar fundo. As sete folhas e as duas referências novas foram inspecionadas. O cenário completo não é carregado pelo jogo; o menu ilustrado não foi implementado nesta entrega.

## Recortes, animações e escala

`Resources/Animations/*.tres` contém 152 regiões únicas de AtlasTexture: 72 poses de Pokémon e 80 de inimigos. Regiões/margens são inspecionáveis no editor; `filter_clip=true` isola amostragem. As cenas e a galeria usam Nearest.

`Tools/Build-SpriteFrames.ps1` lê PNGs por System.Drawing, sem bibliotecas adicionais. Usa faixas verticais individuais, busca separadores por coluna e mede o componente opaco principal para alinhar os pés. Não divide uniformemente a altura: as lacunas reais variam. Canvas constante de 384×384 para Pokémon e 512×512 para inimigos. A escala permanece constante por folha, sem deformar cada quadro.

- Pokémon: `idle_0/1/2` usa colunas 1/2; `attack_0/1/2`, colunas 5/6. Poses adicionais permanecem em `orientation_*` e `attack_alt_*`, sem misturar direções incoerentes. Ataque é espelhado para alvo à esquerda. Não há oito direções completas.
- Inimigos: linhas 1/2/3 são `move_right`, `move_left` e `move_front`; linhas 4/5 são `hit` e `death`. `Enemy.MovementAnimation` escolhe pelo deslocamento real, com tolerância no eixo dominante. Não há pose para cima; a alternativa é frente. A rota atual só desce e serpenteia. A textura não gira nas curvas.
- `VisualScale` nos recursos Pokémon/inimigos controla o tamanho: Pokémon 0,24; slime/goblin 0,16; golem 0,24; chefe 0,28. Golem e chefe mantêm corpos maiores.
- Dano, intervalo, vida, nível, posição e evolução continuam nos scripts de jogo; animação/áudio não aplicam dano adicional.

`Resources/Animations/inspection.json` registra regiões, apoio e fronteiras. No goblin, estrelas da quarta reação ao dano encostam na pose frontal anterior: não há separador totalmente transparente. Partículas e extremidades também podem tocar limites fornecidos. Capturas não mostraram cortes relevantes no corpo principal das sequências usadas, mas não certificam que todo efeito esteja completo. Anatomia/volume dos Pokémon ainda varia entre poses.

Para regenerar, sobrescrevendo ajustes manuais dos recursos gerados:

```powershell
powershell -NoProfile -File .\Tools\Build-SpriteFrames.ps1
```

## Cenário

Main/Terrain usa recorte central de `terreno-limpo.png`, com escala uniforme, na área entre painéis. Rota e suas Line2D mantêm coordenadas; o contraste diferencia o caminho funcional da terra. As nove bases mantêm centros e seleção, com cerca de 97×91 pixels e área clicável de 96×96. Flores retiradas da Main.

`Resources/Environment/base.tres` usa `Rect2(74,93,485,455)` e `chest.tres`, `Rect2(1080,620,320,285)` do `props-atlas.png`. Recortes individuais incluem folhagem/baú sem objetos vizinhos; divisão uniforme 3×2 cortaria a base. Bases e baú são nós separados, visíveis no editor. Nenhuma pedra foi colocada sobre a rota e não foi criada mecânica de obstáculos.

## Validação

Galeria renderizada com nove formas, quatro inimigos, repouso, ataque/dano, direita, esquerda e morte. Main inspecionada em início, evolução, combate e chefe. Ver [relatório desta entrega](audio-assets-v4.md). Avaliação contínua das animações e ajuste auditivo dependem do desenvolvedor.
