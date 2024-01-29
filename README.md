
<p align="center">
<img src=".github/logo_transparent_cropped.png" height="150"/>
</p>

<h3 align="center">Integrate LLM models in Unity!</h3>

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
<a href="https://discord.gg/RwXKQb6zdv"><img src="https://discordapp.com/api/guilds/1194779009284841552/widget.png?style=shield"/></a>
[![Reddit](https://img.shields.io/badge/Reddit-%23FF4500.svg?style=flat&logo=Reddit&logoColor=white)](https://www.reddit.com/user/UndreamAI)
[![X (formerly Twitter) URL](https://img.shields.io/twitter/url?url=https%3A%2F%2Ftwitter.com%2FUndreamAI&style=social)](https://twitter.com/UndreamAI)


LLMUnity allows to integrate, run and deploy LLMs (Large Language Models) in the Unity engine.<br>
LLMUnity is built on top of the awesome [llama.cpp](https://github.com/ggerganov/llama.cpp) and [llamafile](https://github.com/Mozilla-Ocho/llamafile) libraries.

  
<sub>
<a href="#at-a-glance" style="color: black">At a glance</a>&nbsp;&nbsp;•&nbsp;
<a href="#how-to-help" style=color: black>How to help</a>&nbsp;&nbsp;•&nbsp;
<a href="#setup" style=color: black>Setup</a>&nbsp;&nbsp;•&nbsp;
<a href="#how-to-use" style=color: black>How to use</a>&nbsp;&nbsp;•&nbsp;
<a href="#examples" style=color: black>Examples</a>&nbsp;&nbsp;•&nbsp;
<a href="#use-your-own-model" style=color: black>Use your own model</a>&nbsp;&nbsp;•&nbsp;
<a href="#multiple-ai--remote-server-setup" style=color: black>Multiple AI / Remote server setup</a>&nbsp;&nbsp;•&nbsp;
<a href="#options" style=color: black>Options</a>&nbsp;&nbsp;•&nbsp;
<a href="#license" style=color: black>License</a>
</sub>


## At a glance
- :computer: Cross-platform! Supports Windows, Linux and macOS ([supported versions](https://github.com/Mozilla-Ocho/llamafile?tab=readme-ov-file#supported-oses-and-cpus))
- :house: Runs locally without internet access but also supports remote servers
- :zap: Fast inference on CPU and GPU
- :hugs: Support of the major LLM models ([supported models](https://github.com/ggerganov/llama.cpp?tab=readme-ov-file#description))
- :wrench: Easy to setup, call with a single line code
- :moneybag: Free to use for both personal and commercial purposes

🧪 Tested on Unity: 2021 LTS, 2022 LTS, 2023<br>
:vertical_traffic_light: [Upcoming Releases](https://github.com/orgs/undreamai/projects/2/views/10)

## How to help
- Join us at [Discord](https://discord.gg/RwXKQb6zdv) and say hi!
- ⭐ Star the repo and spread the word about the project!
- Submit feature requests or bugs as [issues](https://github.com/undreamai/LLMUnity/issues) or even submit a PR and become a collaborator!

## Setup
To install the package you can follow the typical asset / package process in Unity:<br>

_Method 1: Install the asset using the asset store_
- Open the [LLMUnity](https://assetstore.unity.com/packages/slug/273604) asset page and click `Add to My Assets`
- Open the Package Manager: `Window > Package Manager`
- Select the `Packages: My Assets` option from the drop-down
- Select the `LLMUnity` package, click `Download` and then `Import`

_Method 2: Install the asset using the GitHub repo:_
- Open the Package Manager: `Window > Package Manager`
- Click the `+` button and select `Add package from git URL`
- Use the repository URL `https://github.com/undreamai/LLMUnity.git` and click `Add`

## How to use
For a step-by-step tutorial you can have a look at our guide: 

<a href="https://towardsdatascience.com/how-to-use-llms-in-unity-308c9c0f637c">
<img width="400" src=".github/life_is_strange_dialogue.png"/>
</a>

[How to Use LLMs in Unity](https://towardsdatascience.com/how-to-use-llms-in-unity-308c9c0f637c)

Create a GameObject for the LLM :chess_pawn::
- Create an empty GameObject. In the GameObject Inspector click `Add Component` and select the LLM script (`Scripts>LLM`).
- Download the default model with the `Download Model` button (this will take a while as it is ~4GB).<br>You can also load your own model in .gguf format with the `Load model` button (see [Use your own model](#use-your-own-model)).
- Define the role of your AI in the `Prompt`. You can also define the name of the AI (`AI Name`) and the player (`Player Name`).
- (Optional) By default the LLM script is set up to receive the reply from the model as is it is produced in real-time (recommended). If you prefer to receive the full reply in one go, you can deselect the `Stream` option.
- (Optional) Adjust the server or model settings to your preference (see [Options](#options)).
<br>

In your script you can then use it as follows :unicorn::
``` c#
using LLMUnity;

public class MyScript {
  public LLM llm;
  
  void HandleReply(string reply){
    // do something with the reply from the model
    Debug.Log(reply);
  }
  
  void Game(){
    // your game function
    ...
    string message = "Hello bot!"
    _ = llm.Chat(message, HandleReply);
    ...
  }
}
```
You can also specify a function to call when the model reply has been completed.<br>
This is useful if the `Stream` option is selected for continuous output from the model (default behaviour):
``` c#
  void ReplyCompleted(){
    // do something when the reply from the model is complete
    Debug.Log("The AI replied");
  }
  
  void Game(){
    // your game function
    ...
    string message = "Hello bot!"
    _ = llm.Chat(message, HandleReply, ReplyCompleted);
    ...
  }
```

- Finally, in the Inspector of the GameObject of your script, select the LLM GameObject created above as the llm property.

That's all :sparkles:!
<br><br>
You can also:


<details>
<summary>Add or not the message to the chat/prompt history</summary>

  The last argument of the `Chat` function is a boolean that specifies whether to add the message to the history (default: true):
``` c#
  void Game(){
    // your game function
    ...
    string message = "Hello bot!"
    _ = llm.Chat(message, HandleReply, ReplyCompleted, false);
    ...
  }
```

</details>
<details>
<summary>Wait for the reply before proceeding to the next lines of code</summary>

  For this you can use the `async`/`await` functionality:
``` c#
  async void Game(){
    // your game function
    ...
    string message = "Hello bot!"
    await llm.Chat(message, HandleReply, ReplyCompleted);
    ...
  }
```

</details>
<details>
<summary>Process the prompt at the beginning of your app for faster initial processing time</summary>

``` c#
  void WarmupCompleted(){
    // do something when the warmup is complete
    Debug.Log("The AI is warm");
  }

  void Game(){
    // your game function
    ...
    _ = llm.Warmup(WarmupCompleted);
    ...
  }
```

</details>
<details>
<summary>Add a LLM / LLMClient component dynamically</summary>

``` c#
using UnityEngine;
using LLMUnity;

public class MyScript : MonoBehaviour
{
    LLM llm;
    LLMClient llmclient;

    async void Start()
    {
        // Add and setup a LLM object
        gameObject.SetActive(false);
        llm = gameObject.AddComponent<LLM>();
        await llm.SetModel("mistral-7b-instruct-v0.1.Q4_K_M.gguf");
        llm.prompt = "A chat between a curious human and an artificial intelligence assistant.";
        gameObject.SetActive(true);
        // or a LLMClient object
        gameObject.SetActive(false);
        llmclient = gameObject.AddComponent<LLMClient>();
        llmclient.prompt = "A chat between a curious human and an artificial intelligence assistant.";
        gameObject.SetActive(true);
    }
}
```

</details>


## Examples
The [Samples~](Samples~) folder contains several examples of interaction :robot::
- [SimpleInteraction](Samples~/SimpleInteraction): Demonstrates simple interaction between a player and a AI
- [ServerClient](Samples~/ServerClient): Demonstrates simple interaction between a player and multiple AIs using a `LLM` and a `LLMClient`
- [ChatBot](Samples~/ChatBot): Demonstrates interaction between a player and a AI with a UI similar to a messaging app (see image below)
  
<img width="400" src=".github/demo.gif">

If you install the package as an asset, the samples will already be in the `Assets/Samples` folder.<br>
Otherwise if you install it with the GitHub URL, to install a sample:
- Open the Package Manager: `Window > Package Manager`
- Select the `LLMUnity` Package. From the `Samples` Tab, click `Import` next to the sample you want to install.

The samples can be run with the `Scene.unity` scene they contain inside their folder.<br>
In the scene, select the `LLM` GameObject and click the `Download Model` button to download the default model.<br>
You can also load your own model in .gguf format with the `Load model` button (see [Use your own model](#use-your-own-model)).<br>
Save the scene, run and enjoy!


## Use your own model
Alternative models can be downloaded from [HuggingFace](https://huggingface.co/models).<br>
The required model format is .gguf as defined by the llama.cpp.<br>
The easiest way is to download gguf models directly by [TheBloke](https://huggingface.co/TheBloke) who has converted an astonishing number of models :rainbow:!<br>
Otherwise other model formats can be converted to gguf with the `convert.py` script of the llama.cpp as described [here](https://github.com/ggerganov/llama.cpp/tree/master?tab=readme-ov-file#prepare-data--run).

:grey_exclamation: Before using any model make sure you **check their license** :grey_exclamation:

## Multiple AI / Remote server setup
LLMUnity allows to have multiple AI characters efficiently.<br>
Each character can be implemented with a different client with its own prompt (and other parameters), and all of the clients send their requests to a single server.<br>
This is essential as multiple server instances would require additional compute resources.<br>

In addition to the `LLM` server functionality, we define the `LLMClient` class that handles the client functionality.<br>
The `LLMClient` contains a subset of options of the `LLM` class described in the [Options](#options).<br>
To use multiple instances, you can define one `LLM` GameObject (as described in [How to use](#how-to-use)) and then multiple `LLMClient` objects.
See the [ServerClient](Samples~/ServerClient) sample for a server-client example.

The `LLMClient` can be configured to connect to a remote instance by providing the IP address of the server in the `host` property.<br>
The server can be either a LLMUnity server or a standard [llama.cpp server](https://github.com/ggerganov/llama.cpp/blob/master/examples/server).

## Options

- `Show/Hide Advanced Options` Toggle to show/hide advanced options from below

#### :computer: Server Settings

<div>
<img width="300" src=".github/GameObject.png" align="right"/>
</div>

- `Num Threads` number of threads to use (default: -1 = all)
- `Num GPU Layers` number of model layers to offload to the GPU.
If set to 0 the GPU is not used. Use a large number i.e. >30 to utilise the GPU as much as possible.<br>
If the user's GPU is not supported, the LLM will fall back to the CPU
- `Stream` select to receive the reply from the model as it is produced (recommended!).<br>
If it is not selected, the full reply from the model is received in one go
- Advanced options:
  - `Parallel Prompts` number of prompts that can happen in parallel (default: -1 = number of LLM/LLMClient objects)
  - `Debug` select to log the output of the model in the Unity Editor
  - `Port` port to run the server

#### :hugs: Model Settings
- `Download model` click to download the default model (Mistral 7B Instruct)
- `Load model` click to load your own model in .gguf format
- `Load lora` click to load a LORA model in .bin format
- `Model` the model being used (inside the Assets/StreamingAssets folder)
- `Lora` the LORA model being used (inside the Assets/StreamingAssets folder)
- Advanced options:
  - <code>Context Size</code> Size of the prompt context (0 = context size of the model)
  - <code>Batch Size</code> Batch size for prompt processing (default: 512)
  - <code>Seed</code> seed for reproducibility. For random results every time select -1
  - <details><summary><code>Temperature</code> LLM temperature, lower values give more deterministic answers</summary>The temperature setting adjusts how random the generated responses are. Turning it up makes the generated choices more varied and unpredictable. Turning it down  makes the generated responses more predictable and focused on the most likely options.</details>
  - <details><summary><code>Top K</code> top-k sampling (default: 40, 0 = disabled)</summary>The top k value controls the top k most probable tokens at each step of generation. This value can help fine tune the output and make this adhere to specific patterns or constraints.</details>
  - <details><summary><code>Top P</code> top-p sampling (default: 0.9, 1.0 = disabled)</summary>The top p value controls the cumulative probability of generated tokens. The model will generate tokens until this theshold (p) is reached. By lowering this value you can shorten output & encourage / discourage more diverse output.</details>
  - <details><summary><code>Num Predict</code> number of tokens to predict (default: 256, -1 = infinity, -2 = until context filled)</summary>This is the amount of tokens the model will maximum predict. When N predict is reached the model will stop generating. This means words / sentences might not get finished if this is too low.  </details>

#### :left_speech_bubble: Chat Settings
- `Player Name` the name of the player
- `AI Name` the name of the AI
- `Prompt` a description of the AI role

## License
The license of LLMUnity is MIT ([LICENSE.md](LICENSE.md)) and uses third-party software with MIT and Apache licenses ([Third Party Notices.md](<Third Party Notices.md>)).
