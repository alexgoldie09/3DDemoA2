# GPR202 - A2 OpenGL Project
## DISCLAIMER
**This project is for educational purposes only. 
The code and assets used in this project are either created by myself or sourced from free and open resources in the exception of the instance highlighted in the AI Usage section.**  

### Important note
*The majority of the project uses knowledge built upon in other classes such as:*
- *Vector, quaternion, and matrix manipulation from MAT101 and MAT102.*
- *Use of multiple different class / interface types regarding OOP techniques from PYF200, ADS103, and ICG202.* 
- *Graphics pipeline and lighting formulas such as Blinn-Phong and Lambertian Reflectance from ICG202.*

*The assessment builds on previous code from A1 and code provided in lectures.*

*The island and cannon models were built by myself. The alien model and its textures were provided by a university friend, Andre Lamb, strictly for testing complex meshes. e metal textures came from AmbientCG (2018), the skybox textures from Freestylized (2026), and the debug text font from Cile (n.d.).*

### TO RUN THE APPLICATION
Run bin/Debug/net10.0/3DDemoA2.exe from within the extracted project folder (requires .NET 10).

## Controls
| Key | Action |
|---|---|
| W / A / S / D | Move the camera forward / left / back / right |
| Q / E | Move the camera up / down |
| Mouse | Look around |
| Up / Down arrows | Roll the cannon forward / back (the wheels spin to match) |
| Left / Right arrows | Turn the cannon |
| Shift + Up / Down | Raise / lower the barrel |
| V | Cycle the debug view (Texture, Normals, UVs, Wireframe, Metallic, Roughness) |
| Tab | Toggle lighting |
| C | Toggle backface culling |
| Esc | Quit |

## AI Usage
I have used ChatGPT to assist in the development of this project. The AI was used to provide guidance on how to code the objloader and lighting as the current resource material needed further simplification (OpenAI, 2026). Both these principles were taken from sources used and the AI helped understand how to implement them in my project (de Vries, 2019, 2020a; Groff, 2010; Jansson, 2026; opengl-tutorial, 2022, 2026; Sas, 2025). 

All code written and implemented in this project is my own work.

## Project Overview
The scene is a small floating island with two models on it: a cannon that can be driven around, and an alien used to test a complex mesh. A skybox surrounds the scene, and the metal on the cannon reflects it. Every model is loaded from an .obj file, drawn and rendered efficiently using buffers and my own shaders, and lit by a single sun with normal, metallic and roughness maps.

### Where each task is covered
| Brief | How it is covered | Main classes |
|---|---|---|
| Task 1: textured model with a transform hierarchy | The cannon is one .obj file with three parts (Mount, Barrel and Wheels), loaded as separate meshes and joined in a parent / child hierarchy | ObjLoader, MeshData, Mesh, ModelPart, Cannon, Material |
| Task 2: keyboard-controlled parts | The barrel elevates between -20° and 20°, the cannon turns, and it rolls (with the wheels spinning to match) but stops before the island's edge. Esc closes the program | Cannon, Game |
| Task 3: first-person camera | WASD and mouse-look, with Q / E for up and down. Movement uses the frame time, so its speed doesn't depend on the frame rate, and the pitch is clamped to ±89° so the view can't flip (Coding with Sphere, 2023) | Camera |
| Task 4: skybox | A cube map in its own class, drawn on an inward-facing cube with its own shaders | CubeMap, Skybox, Skybox.vert, Skybox.frag |

## Resources
* AmbientCG. (2018). Metal 001 [Graphic]. https://ambientcg.com/view?id=Metal001
* Barry Evans. (2019, November 12). Basic Shading Concepts: Lambert and Phong Models [Video]. YouTube. https://www.youtube.com/watch?v=z0-MsGvKOhg
* Basler, R. (2013). Three normal mapping techniques explained for the mathematically uninclined. Gamedeveloper.Com. https://www.gamedeveloper.com/programming/three-normal-mapping-techniques-explained-for-the-mathematically-uninclined
* Cile. (n.d.). Bitpap font [Graphic]. In dafont.com. Retrieved https://www.dafont.com/search.php?q=bit
* Coding with Sphere. (2023, May 22). First person camera | how to code minecraft without a game engine (OpenTK/OpenGL C#) [Video]. YouTube. https://www.youtube.com/watch?v=S6KdFTaX8Ww
* de Vries, J. (2018). LearnOpenGL - model. Learnopengl.Com. https://learnopengl.com/Model-Loading/Model
* de Vries, J. (2019). LearnOpenGL - basic lighting. Learnopengl.Com. https://learnopengl.com/Lighting/Basic-Lighting
* de Vries, J. (2020a). LearnOpenGL - advanced lighting. Learnopengl.Com. https://learnopengl.com/Advanced-Lighting/Advanced-Lighting
* de Vries, J. (2020b). LearnOpenGL - cubemaps. Learnopengl.Com. https://learnopengl.com/Advanced-OpenGL/Cubemaps
* DragoniteSpam. (2024, January 8). Tangents and bitangents - 3D games in GameMaker [Video]. YouTube. https://www.youtube.com/watch?v=yBEi9l-n_O4
* EREMEX JSC. (2026). Skybox. EMX Controls Documentation. https://eremexcontrols.net/controls/graphics3dcontrol/skybox/
* Freestylized. (2026). sky_126 [Graphic]. Freestylized.com. https://freestylized.com/skybox/sky_126/
* Gordan, V. (2021). YoutubeOpenGL 27 - Normal Maps (Version master) [Source Code]. GitHub. https://github.com/VictorGordan/opengl-tutorials/tree/main/YoutubeOpenGL%2027%20-%20Normal%20Maps
* Groff, J. (2010, July 15). An intro to modern OpenGL. Chapter 4: Rendering a dynamic 3D scene with Phong shading. Durian Software. https://duriansoftware.com/joe/an-intro-to-modern-opengl.-chapter-4:-rendering-a-dynamic-3d-scene-with-phong-shading
* Jansson, C. (2026). ObjLoader (Version master) [Source Code]. GitHub. https://github.com/chrisjansson/objloader
* lordofdankness. (2022). GLTF2.0-reference-guide. Scribd. https://www.scribd.com/document/619514490/gltf20-reference-guide
* OpenAI. (2026, October 16). Implement OBJ loader c# [Generative AI chat]. ChatGPT. https://chatgpt.com/share/6ae1d45d-d504-83ec-b25e-71b1f10d5cf3
* opengl-tutorial. (2022). Tutorial 7 : Model loading. Www.Opengl-Tutorial.Org. https://www.opengl-tutorial.org/beginners-tutorials/tutorial-7-model-loading/
* opengl-tutorial. (2024). Tutorial 13 : Normal mapping. Www.Opengl-Tutorial.Org. https://www.opengl-tutorial.org/intermediate-tutorials/tutorial-13-normal-mapping/
* opengl-tutorial. (2026). Ogl: tutorial07_model_loading (Version master) [Source Code]. GitHub. https://github.com/opengl-tutorials/ogl/tree/master/tutorial07_model_loading
* Sas. (2025, October 18). Phong vs Lambert: Visualizing the phong illumination model in OpenGL 🎨. Medium. https://medium.com/@chamudi_sashanka/phong-vs-lambert-visualizing-the-phong-illumination-model-in-opengl-3b447cb07c0e
* Victor Gordan. (2021, August 3). OpenGL tutorial 27 - normal maps [Video]. YouTube. https://www.youtube.com/watch?v=JNj1A1bl7gg