# SummonerGui

Prot?tipo de jogo 2D em C# com Godot. No c?digo atual, um inimigo percorre um Path2D, sinaliza a chegada ao ba?, atualiza uma mensagem na interface e ? removido da cena. Existe uma cena CreatureSlot; n?o se afirma que haja sistema completo de invoca??o, combate ou progress?o.

## Stack e estrutura

- Godot com suporte .NET; project.godot declara recursos da s?rie 4.7.
- SummonerGui.csproj declara Godot.NET.Sdk 4.7.2 e alvo .NET 8.
- Main.tscn: cena inicial.
- Scenes/: cenas auxiliares.
- Scripts/Main.cs: conex?o do sinal e atualiza??o da interface.
- Scripts/Enemy.cs: movimento e sinal de chegada.
- .godot/: cache do editor, fora do versionamento.

## Requisitos e execu??o

Use uma edi??o .NET do Godot compat?vel com o SDK declarado e SDK .NET 8. Importe project.godot no editor; execute o projeto com F5 ou a cena principal com F6. O editor pode precisar gerar os artefatos C#. Nenhuma dessas etapas foi executada nesta prepara??o.

## Estado e publica??o

Prot?tipo inicial, descrito a partir dos scripts, sem teste funcional nesta prepara??o. A checagem est?tica limitada n?o garante aus?ncia de segredos. Preservar cenas, scripts, assets, arquivos .uid e metadados .import; n?o versionar caches nem export_credentials.cfg. Publica??o exige autoriza??o espec?fica do propriet?rio.
