# PS Tab Cleaner — macOS build

Native SwiftUI app. Detects Photoshop installs in /Applications, patches the
`tw10428_*.dat` tab-title templates (asks for the admin password via the
system prompt), keeps `.bak` backups, Restore undoes everything.

## Build, sign & notarize (on a Mac)
One-time setup is described at the top of `build.sh`. Then:
```
./build.sh
```
Output: `PhotoshopTabCleaner-MAC.zip` — signed, notarized and stapled, ready for
GitHub Releases.
