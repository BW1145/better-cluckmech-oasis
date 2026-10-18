# Otherworld protocol compatibility

Run with `dotnet run --project tests/OtherworldProtocol/OtherworldProtocolTests.csproj -p:GameDir=<game directory>` from the mod directory. This test uses the installed game's real Mirror assembly. It does not start Unity or the game.

The fixed samples were first checked against the v0.6.4 serializer before it was moved to `OtherworldProtocol`. They freeze all field positions, numeric widths, signed values, UTF-8 string encoding, and the distinction between null and empty strings. Round trips alone would not detect a writer and reader changing together.

`OtherworldMessage` retains its namespace, type name, integer kind field, and published kind values. Its payload has no version prefix. Keep this layout stable; an incompatible payload needs a new message type or an explicitly negotiated capability. Dash continues using its existing `Value` capability marker. Authentication, player ownership, sequence ordering and session tokens remain the network handler's responsibility.

Save schemas remain separate: the existing `CluckmechCoop.Characters.v1` ES3 key and JSON field names are unchanged. Missing optional fields retain model defaults, including `Initial=0`. Known talent ownership and numeric invariants are shared with network payloads, but disk loading deliberately does not apply the live network's six-talent cap or branch restriction. Incompatible future storage changes need a new versioned key and an explicit migration; malformed or unknown-field snapshots use the existing protected-save fallback and retain their original data.
