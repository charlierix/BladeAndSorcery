# PerfectlyNormalBaS
This is a copy of [PerfectlyNormalUnity](https://github.com/charlierix/PerfectlyNormalUnity)

These are helper classes for unity development, but pointing to the version of unity that blade and sorcery use

Also, anything specifically needed by B&S or the SDK will be built into this project (like the debug renderer knowing which shaders to use)

---------------------------------

PerfectlyNormalUnity has references to Genetic Sharp, but since PerfectlyNormalBaS will be included in mods, I don't want that included (if it's needed in the future, I may add it back, assuming that's allowed)

---------------------------------

If you want to compile the code, here are some comments about getting the solution working:

This project's references are to the unity install location.  If your install locaion is different, you'll need to repair those references before compiling (it's easiest to just modify the csproj file directly).  An easy way to find the folder is to go to definition of something like Vector3 from unity.  The top of the file has a comment saying where the dll is

C:\Program Files\Unity\Editor\Data\Managed\UnityEngine\UnityEditor.dll
C:\Program Files\Unity\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll
C:\Program Files\Unity\Editor\Data\Managed\UnityEngine\UnityEngine.InputLegacyModule.dll
C:\Program Files\Unity\Editor\Data\Managed\UnityEngine\UnityEngine.PhysicsModule.dll

There is a postbuild command that copies the dll into the bin folder.  So you can copy PerfectlyNormalBaS.dll from either bin, or bin\debug or bin\release

Any class in unity that you want to use this from needs a using statment at the top:
using PerfectlyNormalBaS;

---------------------------------

The easiest way to add new code to this project is to develop and test it in a unity project.  Then when you have it the way you want, copy into this solution

If there is a new reference needed, just select the type (from your unity project), hit F12 and the top of the generated file tells what dll that type came from

This page has more details:
https://docs.unity3d.com/Manual/UsingDLL.html
