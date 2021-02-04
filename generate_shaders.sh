#!/bin/bash

GIT_PATH="$(dirname "$0")"
PATCHES_PATH="$GIT_PATH/patches"
SURVIVAL_SHADERS_SUBPATH="assets/survival/shaders"
SHADERS_SUBPATH="assets/game/shaders"
SHADERINCLUDES_SUBPATH="assets/game/shaderincludes"

if [ -z "$VS_PATH" ] && [ -z "$APPDATA" ]; then
    echo "Cannot determine Vintagestory installation path! Please set \$VS_PATH manually!"
    exit 1
fi

: "${VS_PATH:=$APPDATA/Vintagestory}"
echo "Using Vintagestory installation directory $VS_PATH"

if [ ! -d "$VS_PATH" ]; then
    echo "Vintagestory not found at assumed path. Please set a correct installation path using \$VS_PATH!"
    exit 1
fi

# https://stackoverflow.com/a/38595160
# https://stackoverflow.com/a/800644
if sed --version >/dev/null 2>&1; then
  strip_cr() {
    sed -i -- "s/\r//" "$@"
  }
else
  strip_cr() {
    sed -i "" "s/$(printf '\r')//" "$@"
  }
fi

# $1: relative path of files
rebuild_files() {
    rm -rf "$GIT_PATH/$1"
    mkdir -p "$GIT_PATH/$1"
    
    for file in $(/bin/ls "$PATCHES_PATH/$1"); do
        patchFile="$PATCHES_PATH/$1/$file"
        file="${file%.*}"

        echo "Patching $file < $patchFile"
        strip_cr "$VS_PATH/$1/$file" > /dev/null
        cp "$VS_PATH/$1/$file" "$GIT_PATH/$1/$file"
        patch -d "$GIT_PATH" "$1/$file" < "$patchFile"
    done
}

rebuild_files "$SHADERS_SUBPATH"
rebuild_files "$SHADERINCLUDES_SUBPATH"
rebuild_files "$SURVIVAL_SHADERS_SUBPATH"