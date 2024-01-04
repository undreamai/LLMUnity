
<p align="center">
<img src=".github/logo_transparent_cropped.png" height="150"/>
</p>

<h3 align="center">Run and deploy LLM models in Unity!</h3>
<br>
LLMUnity allows to run and distribute LLM models in the Unity engine.<br>

LLMUnity is built on top of the awesome [llama.cpp](https://github.com/ggerganov/llama.cpp) and [llamafile](https://github.com/Mozilla-Ocho/llamafile) libraries.

## Features
- :computer: Cross-platform! Supports Windows, Linux and macOS ([supported versions](https://github.com/Mozilla-Ocho/llamafile?tab=readme-ov-file#supported-oses-and-cpus))
- :house: Runs locally without internet access but also supports remote servers
- :zap: Blazing fast inference on CPU and Nvidia GPUs
- :hugs: Support of the major LLM models ([supported models](https://github.com/ggerganov/llama.cpp?tab=readme-ov-file#description))
- :wrench: Easy to setup, call with a single line code
- :moneybag: Free to use for both personal and commercial purposes

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
Create a GameObject for the LLM :chess_pawn::
- Create an empty GameObject. In the GameObject Inspector click `Add Component` and select the LLM script (`Scripts>LLM`).
- Download the default model with the `Download Model` button (this will take a while as it is ~4GB).<br>You can also load your own model in .gguf format with the `Load model` button (see [Use your own model](#use-your-own-model)).
- Define the role of your AI in the `Prompt`.
- (Optional) By default the LLM script is set up to receive the reply from the model as is it is produced in real-time (recommended).<br>If you prefer to receive the full reply in one go, you can deselect the option.
- (Optional) Adjust the server or model settings to your preference (see [Options](#options)).
<br>

In your script you can then use it as follows :unicorn::
``` c#
public class MyScript {
  LLM llm;
  
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
- Finally, in the Inspector of the GameObject of your script, select the LLM GameObject created above as the llm property.

That's all :sparkles:!

---

**(Optional)** You can also specify a function that is called when the model reply is completed. <br>This is useful if the Stream option is selected for continuous output from the model (default behaviour):
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

**(Optional)** If you want to wait for the reply before proceeding with your next lines of code you can use `await`:
``` c#
  async void Game(){
    // your game function
    ...
    string message = "Hello bot!"
    await llm.Chat(message, HandleReply, ReplyCompleted);
    ...
  }
```

## Examples
An example chatbot is provided in the `Samples~` :robot:.<br>
The chatbot takes input from the player and holds a conversation with an LLM model.

To install it:
- Open the Package Manager: `Window > Package Manager`
- Select the `LLMUnity` Package. From the `Samples` Tab, click `Import`  next to the `ChatBot` Sample.

The sample can be run with the `Assets/Samples/LLMUnity/VERSION/ChatBot/Scene.unity` scene.<br>
In the scene, select the `LLM` GameObject and click the `Download Model` button to download the default model.<br>
You can also load your own model in .gguf format with the `Load model` button (see [Use your own model](#use-your-own-model)).<br>
Save the scene, run and enjoy!

<img width="400" src=".github/demo.gif">

## Use your own model
Alternative models can be downloaded from [HuggingFace](https://huggingface.co/models).<br>
The required model format is .gguf as defined by the llama.cpp.<br>
The easiest way is to download gguf models directly by [TheBloke](https://huggingface.co/TheBloke) who has converted an astonishing number of models :rainbow:!<br>
Otherwise other model formats can be converted to gguf with the `convert.py` script of the llama.cpp as described [here](https://github.com/ggerganov/llama.cpp/tree/master?tab=readme-ov-file#prepare-data--run).

:grey_exclamation: Before using any model make sure you **check their license** :grey_exclamation:

## Multiple client / Remote server setup
In addition to the `LLM` server functionality, LLMUnity defines the `LLMClient` client class that handles the client functionality.<br>
The `LLMClient` contains a subset of options of the `LLM` class described in the [Options](#options).<br>
It can be used to have multiple clients with different options e.g. different prompts that use the same server.<br>
This is important as multiple server instances would require additional compute resources.<br>
To use multiple instances, you can define one `LLM` GameObject (as described in [How to use](#how-to-use)) and then multiple `LLMClient` objects.

The `LLMClient` can be configured to connect to a remote instance by providing the IP address of the server in the `host` property.<br>
The server can be either a LLMUnity server or a standard [llama.cpp server](https://github.com/ggerganov/llama.cpp/blob/master/examples/server).

## Options

#### :computer: Server Settings

<div>
<img width="300" src=".github/GameObject.png" align="right"/>
</div>

- `Num Threads` number of threads to use (default: -1 = all)
- `Num GPU Layers` number of model layers to offload to the GPU.
If set to 0 the GPU is not used. Use a large number i.e. >30 to utilise the GPU as much as possible.<br>
If no Nvidia GPU exists in the user, the LLM will fall back to the CPU
- `Debug` select to log the output of the model in the Unity Editor
- `Port` port to run the server
- `Stream` select to receive the reply from the model as it is produced (recommended!).<br>
If it is not selected, the full reply from the model is received in one go

#### :hugs: Model Settings
- `Download model` click to download the default model (Mistral 7B Instruct)
- `Load model` click to load your own model in .gguf format
- `Load lora` click to load a LORA model in .bin format
- `Model` the model being used (inside the Assets/StreamingAssets folder)
- `Lora` the LORA model being used (inside the Assets/StreamingAssets folder)
- `Context Size` Size of the prompt context (0 = context size of the model)
- `Batch Size` Batch size for prompt processing (default: 512)
- `Seed` seed for reproducibility. For random results every time select -1
- `Temperature` LLM temperature, lower values give more deterministic answers
- `Top K` top-k sampling (default: 40, 0 = disabled)
- `Top P` top-p sampling (default: 0.9, 1.0 = disabled)
- `Num Predict` number of tokens to predict (default: 256, -1 = infinity, -2 = until context filled)

#### :left_speech_bubble: Chat Settings
- `Player Name` the name of the player
- `AI Name` the name of the AI
- `Prompt` a description of the AI role

## License
The license of LLMUnity is MIT ([LICENSE.md](LICENSE.md)) and uses third-party software with MIT and Apache licenses ([Third Party Notices.md](<Third Party Notices.md>)).
