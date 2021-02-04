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


# $1: relative path of files to create patches for
create_patches() {
    for file in $(/bin/ls "$GIT_PATH/$1"); do
        echo "Creating patch for file \"$file\"..."
        strip_cr "$GIT_PATH/$1/$file" > /dev/null
        strip_cr "$VS_PATH/$1/$file" > /dev/null
        outName="$PATCHES_PATH/$1/$file.patch"
        patchNew=$(diff -u --label a/$1/$file "$VS_PATH/$1/$file" --label b/$1/$file "$GIT_PATH/$1/$file")
        mkdir -p "$(dirname $outName)"
        echo "$patchNew" > "$outName"
    done
}

create_patches "$SHADERS_SUBPATH"
create_patches "$SHADERINCLUDES_SUBPATH"
create_patches "$SURVIVAL_SHADERS_SUBPATH"