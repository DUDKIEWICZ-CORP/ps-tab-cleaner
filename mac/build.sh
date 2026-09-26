#!/bin/bash
# Build, sign and notarize PS Tab Cleaner for macOS.
# One-time setup on the Mac:
#   1. xcode-select --install                      (compiler)
#   2. Developer ID Application certificate in Keychain (from developer.apple.com)
#   3. xcrun notarytool store-credentials dudkiewiczcorp-notary \
#        --apple-id YOUR_APPLE_ID --team-id YOUR_TEAM_ID --password APP_SPECIFIC_PASSWORD
# Then just:  ./build.sh
set -e
cd "$(dirname "$0")"

IDENTITY=$(security find-identity -v -p codesigning | grep 'Developer ID Application' | head -1 | sed 's/.*"\(.*\)"/\1/')
PROFILE="dudkiewiczcorp-notary"
APP="PS Tab Cleaner.app"

echo "== signing identity: $IDENTITY"
[ -n "$IDENTITY" ] || { echo "No Developer ID Application certificate in Keychain"; exit 1; }

echo "== compiling (universal)"
swiftc -O -parse-as-library -target x86_64-apple-macos12  -o PSTabCleaner_x86 PSTabCleaner.swift
swiftc -O -parse-as-library -target arm64-apple-macos12   -o PSTabCleaner_arm PSTabCleaner.swift
lipo -create -output PSTabCleaner PSTabCleaner_x86 PSTabCleaner_arm

echo "== bundling"
rm -rf "$APP"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp PSTabCleaner "$APP/Contents/MacOS/"
cp engine.sh "$APP/Contents/Resources/"
cp Info.plist "$APP/Contents/"
iconutil -c icns icon.iconset -o "$APP/Contents/Resources/icon.icns"

echo "== codesign (hardened runtime)"
codesign --force --options runtime --timestamp --sign "$IDENTITY" "$APP/Contents/Resources/engine.sh"
codesign --force --options runtime --timestamp --sign "$IDENTITY" "$APP"

echo "== notarize"
ditto -c -k --keepParent "$APP" notarize.zip
xcrun notarytool submit notarize.zip --keychain-profile "$PROFILE" --wait
xcrun stapler staple "$APP"
rm notarize.zip

echo "== final archive"
ditto -c -k --keepParent "$APP" PhotoshopTabCleaner-MAC.zip
rm -f PSTabCleaner PSTabCleaner_x86 PSTabCleaner_arm
echo "DONE -> PhotoshopTabCleaner-MAC.zip (upload to GitHub Releases)"
