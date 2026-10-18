# Movement / aim / dash regression

Run: `dotnet run --project OtherworldMotionTests.csproj -c Release`

The executable compiles the production `OtherworldMotion` policy directly. It
checks render/physics frame gaps at 30/60/144/240 Hz, opposing aim, sector
hysteresis, distance-based gait, teleport exclusion, movement-only dash with
last-movement fallback, and uninterrupted attack clocks across slow/rapid fire.
These are deterministic code tests, not Unity or visual gameplay tests.

Runtime wiring reviewed against the installed game's actual assemblies:
- `模块_玩家_移动.移动实现`: target x is negated input x; target y is input y.
  A dash suppresses this native root translation only while `Dashing` is true.
- Native smoothed movement axes determine local gait through release/turn deceleration; remote displacement gets an
  80 ms hold between snapshots. Travel distance advances gait below a 3-cycle/second readability cap. The cap preserves elapsed physics/snapshot spacing across render gaps. At native high movement speeds this is a stylized run cue; exact foot-ground contact is not claimed.
- Only the input owner sends a hold-qualified action 4. Its X/Y payload is now
  a movement direction; both peers must run this version. Start, release and
  focus-loss continue to use actions 1 and 2 with monotonic sequence checks.
- Dash owns facing and a fixed lunge frame; the existing phase-band renderer
  remains active and draws the body throughout the dash.

Known verification limits: no game was launched, no user computer was accessed,
and network/visual playback still needs in-game verification. Production assets use 16 gait phases, independent 4-phase recoil, and movement relative to aim; the separate OtherworldAssetSelection suite checks the actual baked atlas selector. Artwork quality and remaining stylized foot slip require offline visual inspection and user gameplay feedback.
