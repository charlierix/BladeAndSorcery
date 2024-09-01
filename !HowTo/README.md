# Cloning

If you're new to git, cloning is basically downloading a copy - with an ability to push updates.  It's safe to make as my clones as you want, they're just folders on your computer, the server doesn't know about them.  So if you clone something, screw it up, but want to keep it around for reference, just clone another folder with a _2 at the end

You can either download the zip or run "git clone url" from command line.  Either way, click the root page of this repo, drop down the green button and make your selection

The folder can be anywhere.  Visual Studio defaults to something like users\name\repos\source.  Personally, I like to make a !dev_repos folder at the root of my hard drive and put all repos there.  It's easy to find and has a low risk of long folder names messing things up

If you're just going to download, there's no need for any special git tools.  But if you're going to make your own repo, create a github account.  [GitHub Desktop](https://desktop.github.com/download) is a good tool for managing repos.  You can clone directly from it, or if you've already cloned something, you can add a repo later.  VSCode and Visual Studio have git abilities as well, but I wouldn't trust visual studio when dealing with files outside of the solution

See [this](https://docs.github.com/en/get-started/getting-started-with-git/set-up-git) page for more

# Visual Studio

As I learn more, I might figure out how to do everything under the unity sdk.  But for now, the logic for a mod is in its own visual studio solution.  The sdk is just for packaging assets (sounds)

Before opening the solution, go into the .csproj file with something like notepad++.  Look for all the dll references and change the path to where your blade and sorcery is installed (simple replace all of the root folder)

```xml
<Reference Include="SteamVR, Version=0.0.0.0, Culture=neutral, processorArchitecture=MSIL">
    <SpecificVersion>False</SpecificVersion>
    <HintPath>D:\SteamLibrary\steamapps\common\Blade &amp; Sorcery\BladeAndSorcery_Data\Managed\SteamVR.dll</HintPath>
</Reference>
```

After opening the solution, do a rebuild solution.  Then copy the projectname.dll from bin\debug into the folder that will be distributed to the world

# B&S SDK

Download the sdk [here](https://github.com/KospY/BasSDK)

Open [this](https://kospy.github.io/BasSDK/Components/Guides/SDK-HowTo/UnitySDKSetup.html) page to help get unity set up

# Creating Audio

### stable audio tools
I use [stable-audio-tools](https://github.com/Stability-AI/stable-audio-tools) ([model](https://huggingface.co/stabilityai/stable-audio-open-1.0), [video](https://www.youtube.com/watch?v=zu1TypuTl3U)) to generate audio

### llm
This system prompt to an LLM helps come up the prompt to the audio generator (describe what you want, then have a conversation to refine it)

> You are an assistant that helps with sound generation prompts.  Your responses will be plugged into the text prompt of a sound effect generator.  Please try to keep the prompt to a single paragraph

If you've never done anything with LLMs, research [ollama](https://ollama.com) for the wrapper service to the llm.  After installing ollama, you'll need to download a [model](https://ollama.com/library).  But before doing that, you may want to [change](https://github.com/ollama/ollama/blob/main/docs/faq.md#where-are-models-stored) where they download to.  It can be talked to directly from command line, but is better through a ui.  I use [chatbox](https://chatboxai.app), it's simple and clean.  I also made a [tool](https://github.com/charlierix/HAL_NIN/tree/main/!playground/speech%20to%20textbox) that lets you record from microphone and paste into chatbox

### audacity
[Audacity](https://www.audacityteam.org) is a good tool for pulling apart and modifying the wav files that were generated