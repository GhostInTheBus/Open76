#!/bin/bash
# Builds libsmacker (cutscene video decoder) as a universal macOS dylib
# for Unity: Apple Silicon (arm64) + Intel (x86_64).
# Needs Xcode Command Line Tools:  xcode-select --install
set -euo pipefail
cd "$(dirname "$0")"

OUT="../Assets/Plugins/macOS"
TMP="$(mktemp -d)"
mkdir -p "$OUT"

ARCHS=(-arch arm64 -arch x86_64)

for f in smacker.c smk_bitstream.c smk_hufftree.c; do
  clang -std=c99 -O2 -fPIC -fvisibility=hidden "${ARCHS[@]}" -c "$f" -o "$TMP/${f%.c}.o"
done
clang++ -std=c++11 -O2 -fPIC -fvisibility=hidden "${ARCHS[@]}" -c UnityInterface.cpp -o "$TMP/UnityInterface.o"

clang++ -dynamiclib "${ARCHS[@]}" -install_name @rpath/libsmacker.dylib \
  "$TMP"/*.o -o "$OUT/libsmacker.dylib"

# Apple Silicon refuses to load unsigned code; ad hoc sign it.
codesign --force --sign - "$OUT/libsmacker.dylib"

rm -rf "$TMP"
echo "Built $OUT/libsmacker.dylib"
lipo -info "$OUT/libsmacker.dylib"
nm -gU "$OUT/libsmacker.dylib" | grep -E "OpenFile|GetFrameData|AdvanceFrame|GetAudioData|DisposeFile|DisposeAudioData"
