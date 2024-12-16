Video of gameplay: https://www.youtube.com/watch?v=NhXQpXGFaoE

# Celeste 64: FitnessGram Pacer Test
This is the source code for the Celeste 64 FitnessGram Pacer Test, a mod for *Celeste 64: Fragments of the Mountain*. This mod adds the FitnessGram Pacer Test as a fun challenge. This was my first time modding a game, enjoy!

A pre-built version will be available soon.

### Pacer Test Instructions
 - Go to the ending area. You will see a new path leading upwards.
 - Once you reach the top, interact with the sign. This will start the test.
 - Try to get as far as you can before making two mistakes.
 - Note: Your dash will not refill on the ground (hehe)
 - Dying counts as a mistake and will send you to the other side.
 - After reaching a score of 15, 25, and 50, you will be rewarded with a strawberry.

### Installation
 - You need [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
 - Clone this repo, make sure NuGet packages are found with `dotnet restore`
 - Run `Celeste64.csproj` with `dotnet run` or `dotnet build`

### Libraries Used
 - [Foster](https://github.com/FosterFramework/Foster) + [SDL2](https://github.com/libsdl-org/sdl): Input/Windowing/Rendering
 - [SledgeFormats](https://github.com/LogicAndTrick/sledge-formats): Parsing TrenchBroom level formats
 - [SharpGLTF](https://github.com/vpenades/SharpGLTF): Parsing and Animating glTF2 models
 - [FMOD](https://www.fmod.com): For Music and Sound Effects

### Tools Used
 - [TrenchBroom](https://trenchbroom.github.io/): For Level Editing
 - [Blender](https://www.blender.org/): For creating 3D Models
 - [Aseprite](https://www.aseprite.org/): For drawing Textures

### Resources Used
 - [khronos glTF Tutorials](https://github.khronos.org/glTF-Tutorials/gltfTutorial/gltfTutorial_020_Skins.html#the-joint-matrices): To figure out how Mesh Skins/Bones work
 - [LearnOpenGL](https://learnopengl.com/Advanced-OpenGL/Depth-testing): For general rendering concepts / normalizing Depth
 - [Kenny's Input Prompts](https://kenney.nl/assets/input-prompts): For UI Button Prompts
 - [Renogare](https://www.dafont.com/renogare.font): Main font

### Created By ...
 - [Maddy Thorson](http://maddymakesgames.com/)
 - [Noel Berry](https://noelberry.ca)
 - [Amora B.](https://amorabettany.com)
 - [Pedro "Saint11" Medeiros](http://saint11.org/)
 - [Power Up Audio](https://powerupaudio.com/)
 - [Lena Raine](https://lena.fyi/)
 - [Heidy Motta](https://www.heidy.page/).

### License
 - The Celeste IP and everything in the `Content` folder are owned by [Maddy Makes Games, Inc](https://www.maddymakesgames.com/).
 - The `Source` folder, with exceptions where noted, is [licensed under MIT](Source/License.txt).
 - The `Source/Audio/FMOD` folder contains bindings and binaries from FMOD.
 - We're fine with non-commercial Mods / Levels / Fan Games using assets from the `Content` folder as long as it's clear it is not made by the Celeste team or endorsed by us.
