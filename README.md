VolumetricShading
=================

Overview
--------
Adds volumetric lighting, screen space reflections and other nice things to Vintage Story.

Installation
------------
Download a release and put it in your mods folder. Then, press `CTRL+C` ingame to open the configuration GUI. Make sure the release matches your Vintage Story version exactly!

Cloning & Building
------------------
In order to obtain the actual shader files, you need to have Vintage Story installed on your machine. It needs to be the exact version the mod is targeting!

You can then run `generate_shaders.sh` to generate the patched shader files used when playing the game. On Windows, you can run this file using Git Bash (or potentially WSL, but that is untested).

If you make any changes to the shader files, you need to run `generate_patches.sh` before committing. This will generate diff files based on your changes compared to the normal game files. These diff files can then be committed to the repository.