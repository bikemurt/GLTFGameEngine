# glTF 2.0 Game Engine in C#
The main objective of this game engine is to seamlessly render scenes, lighting, cameras and animation that are created in Blender (or any 3D design software that conforms to glTF 2.0).

It's not intended to be highly optimized, as the data from the GLTF files are loaded directly into ram and VRAM as needed.

Main focuses:
- PBR textures and lighting
-   To do: rendering without textures. Utilize material properties in glTF data
- Animation
- Deferred rendering (future)
- Nanite-like algorithm for reducing triangle counts (future)
