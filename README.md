
<p align="center">
<img src="github/logo_transparent_cropped.png" height="150"/>
</p>

<h3 align="center">Run and deploy LLM models in Unity!</h3>

LLMUnity allows to run and distribute LLM models in the Unity engine.
<br>
LLMUnity is built on top of the awesome [llama.cpp](https://github.com/ggerganov/llama.cpp) and [llamafile](https://github.com/Mozilla-Ocho/llamafile) libraries.

## Features
<img width="300" src="github/demo.gif" align="right" >


- :computer: Cross-platform! Supports Windows, Linux and macOS ([supported versions](https://github.com/Mozilla-Ocho/llamafile?tab=readme-ov-file#supported-oses-and-cpus))
- :house: Runs locally without need of internet access but also supports remote servers
- :zap: Real-time inference on CPU or Nvidia GPUs
- :hugs: Support of the major LLM models ([supported models](https://github.com/ggerganov/llama.cpp?tab=readme-ov-file#description))!
- :wrench: Easy to setup, call with a single line code
- :moneybag: Free to use for both personal and commercial purposes!


<br clear="right"/>

## Setup

- Install the asset in Unity
- Create an empty GameObject. In the GameObject Inspector press "Add Component" and select the LLM script ("Scripts">"LLM").
- Download the default model (Mistral 7B Instruct) with the "Download Model" button.<br>You can also load your own model in .gguf format with the "Load model" button (see [Use your own model](#use-your-own-model)).
- Define the role of your AI in the "Prompt". You can optionally specify the player and the AI name.
- (Optional) Adjust the server or model settings to your preference (see [Options](#options)).

<br>

- In your script you can then use it as follows:


``` c#
public class MyScript {
  LLM llm;
  
  void GetReply(string reply){
    // do something with the reply from the model
    Debug.Log(reply);
  }
  
  void Game(){
    // your game function
    ...
    string message = "Hello bot!"
    Task chatTask = llmClient.Chat(message, GetReply);
    ...
  }
}
```
- In the Inspector of the GameObject of your script, select the LLM GameObject created above as the llm property.

<br>

That's all :sparkles:!


## Options

<div>
<img width="300" src="github/GameObject.png" align="right" />
</div>

<br clear="right"/>

## Use your own model

A vast amount of models are available from [TheBloke](https://huggingface.co/TheBloke). Make sure to check the license before using any model!

## Remote server setup
## License
## Author
## Disclaimers
