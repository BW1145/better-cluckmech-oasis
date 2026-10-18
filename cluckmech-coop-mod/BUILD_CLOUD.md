# Cloud-only build fallback

`BuildCloud.py` builds the three plugins with Roslyn from an already installed
official .NET 8 SDK. It is useful when `dotnet build` cannot initialize its
NuGet/MSBuild IPC in a restricted Linux environment. It never launches the game,
installs anything into the game directory, downloads dependencies, or publishes.
Windows development can continue using the existing project and `Package.ps1`.

Provide these paths explicitly:

```bash
python BuildCloud.py --dotnet-root "$DOTNET_ROOT" --game-dir "$CLUCKMECH_GAME_DIR" --output "$BUILD_OUTPUT" --check
python BuildCloud.py --dotnet-root "$DOTNET_ROOT" --game-dir "$CLUCKMECH_GAME_DIR" --output "$BUILD_OUTPUT"
# Only after code/art review is complete; use a fresh output directory:
python BuildCloud.py --dotnet-root "$DOTNET_ROOT" --game-dir "$CLUCKMECH_GAME_DIR" --output "$RELEASE_OUTPUT" --package
```

Requirements: Python 3, an official .NET SDK, the repository's checked-in BepInEx
loader, and access to the game's Managed reference assemblies. All game files
are read-only references. The three project files supply assembly names,
versions, references and resource names. The fallback compiles against the
installed game's BCL/facades and embeds the same character, stratagem and UI
resources as the main project; it is not a replacement for an in-game test.

Checks include input completeness, all three production compilations, all ten
music OGG files, all four effect OGG files, ZIP integrity, plugin hashes and
required loader/configuration. Private game assemblies are excluded from the
package and explicitly checked against accidental inclusion. Assembly versions
and source-path mapping are generated; source paths are not embedded as local
machine paths. Full output consists of a staging directory, ZIP and SHA-256
manifest. A pre-existing package staging directory is refused to prevent stale
files entering a release.

The package contains default configuration, matching `Package.ps1`. Existing
user settings must be preserved by the normal installation workflow. This script
does not install the package or access a user's computer.
