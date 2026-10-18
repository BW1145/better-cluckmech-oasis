# Actual faithful atlas selector regression

Run `python tests/OtherworldAssetSelection/run.py --dotnet-root <official-sdk-root> --output <temporary-output>` from any working directory after both faithful atlases have been generated.

This compiles the production `OtherworldAssets.cs` with minimal Unity data stubs and embeds the actual production PNG/JSON files. It exhaustively resolves both heroes, all 8 aim directions, all 8 movement directions, all 16 gait frames, and all 4 independent attack phases, plus idle/standing attack/dash poses. It verifies crop bounds, common pivot, pixel scale, positive visible bounds, separate per-hero textures and counts. PNG dimensions are read from IHDR; no Unity renderer or game is started.

This is a selector/resource contract test, not evidence of in-game visual quality or multiplayer correctness. Offline image inspection and user gameplay testing remain separate.
