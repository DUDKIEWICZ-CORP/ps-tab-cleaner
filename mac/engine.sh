#!/bin/bash
# PS Tab Cleaner engine (macOS). Usage: engine.sh fix|restore "/Applications/Adobe Photoshop 2024"
set -e
MODE="$1"; DIR="$2"
[ -d "$DIR" ] || { echo "no such app dir"; exit 1; }
FOUND=0
while IFS= read -r -d '' f; do
  FOUND=1
  if [ "$MODE" = "restore" ]; then
    [ -e "$f.bak" ] && cp "$f.bak" "$f" && echo "restored: $f"
    continue
  fi
  [ -e "$f.bak" ] || cp "$f" "$f.bak"
  /usr/bin/python3 - "$f" <<'PYEOF'
import re, sys
p = sys.argv[1]
raw = open(p, 'rb').read()
enc = 'utf-16-le'
if raw[:2] == b'\xff\xfe':
    t = raw[2:].decode(enc); bom = b'\xff\xfe'
elif raw[:3] == b'\xef\xbb\xbf':
    t = raw[3:].decode('utf-8'); enc = 'utf-8'; bom = b'\xef\xbb\xbf'
else:
    t = raw.decode(enc); bom = b''
n = re.sub(r'("\$\$\$/ImageWindow/TitleTemplate[^=]*)=[^"]*"', r'\1=^0"', t)
open(p, 'wb').write(bom + n.encode(enc))
print(('patched: ' if n != t else 'already patched: ') + p)
PYEOF
done < <(find "$DIR/Locales" -name 'tw10428_*.dat' -print0 2>/dev/null)
[ $FOUND = 1 ] || { echo "no language files found"; exit 1; }
