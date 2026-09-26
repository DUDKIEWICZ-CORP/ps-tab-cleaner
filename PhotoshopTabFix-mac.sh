#!/bin/bash
# Photoshop Tab Fix (macOS) — show only the file name in Photoshop document tabs.
# Usage:  sudo ./PhotoshopTabFix-mac.sh [--restore]
set -e
MODE=patch
[ "$1" = "--restore" ] && MODE=restore
if pgrep -x "Adobe Photoshop" >/dev/null 2>&1; then
  echo "Close Photoshop first, then run this again."; exit 1
fi
FOUND=0
for f in /Applications/Adobe\ Photoshop*/Locales/*/Support\ Files/tw10428_*.dat; do
  [ -e "$f" ] || continue
  FOUND=1
  VER=$(echo "$f" | sed 's|/Applications/\([^/]*\)/.*|\1|')
  if [ "$MODE" = "restore" ]; then
    if [ -e "$f.bak" ]; then cp "$f.bak" "$f"; echo "[$VER] restored original tabs."
    else echo "[$VER] no backup — nothing to restore."; fi
    continue
  fi
  [ -e "$f.bak" ] || cp "$f" "$f.bak"
  # UTF-16LE aware replace via python3 (preinstalled on macOS 12.3+ via CLT; fallback: perl)
  python3 - "$f" <<'PYEOF'
import re, sys
p = sys.argv[1]
t = open(p, 'rb').read().decode('utf-16-le')
n = re.sub(r'("\$\$\$/ImageWindow/TitleTemplate[^=]*)=[^"]*"', r'\1=^0"', t)
open(p, 'wb').write(n.encode('utf-16-le'))
print('  patched' if n != t else '  already patched')
PYEOF
  echo "[$VER] done."
done
[ $FOUND = 1 ] || { echo "No Photoshop language files found in /Applications."; exit 1; }
echo "Start Photoshop to see the result."
