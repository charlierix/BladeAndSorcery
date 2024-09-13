# PerfectlyNormalUnity
Helper classes for unity development

---------------------------------

This is meant to be helper classes that you can reference into your unity project

---------------------------------

If you don't want to compile the code, you can just copy the dlls from the bin folder into your unity project's assest folder (I like to make a lib sub folder)

---------------------------------

If you want to compile the code, here are some comments about getting the solution working:

This project's references are to the unity install location.  If your install locaion is different, you'll need to repair those references before compiling (it's easiest to just modify the csproj file directly).  An easy way to find the folder is to go to definition of something like Vector3 from unity.  The top of the file has a comment saysing where the dll is

C:\Program Files\Unity\Editor\Data\Managed\UnityEngine\UnityEditor.dll
C:\Program Files\Unity\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll
C:\Program Files\Unity\Editor\Data\Managed\UnityEngine\UnityEngine.InputLegacyModule.dll
C:\Program Files\Unity\Editor\Data\Managed\UnityEngine\UnityEngine.PhysicsModule.dll

There is a postbuild command that copies the dll into the bin folder.  So you can copy PerfectlyNormalUnity.dll from either bin, or bin\debug or bin\release

Any class in unity that you want to use this from needs a using statment at the top:
using PerfectlyNormalUnity;

Alternatively, you can probably just copy the classes into a folder under Assets and let everything live inside unity

---------------------------------

The easiest way to add new code to this project is to develop and test it in a unity project.  Then when you have it the way you want, copy into this solution

If there is a new reference needed, just select the type (from your unity project), hit F12 and the top of the generated file tells what dll that type came from

This page has more details:
https://docs.unity3d.com/Manual/UsingDLL.html
