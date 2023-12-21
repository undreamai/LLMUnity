
<p align="center">
<img src="github/logo_transparent_cropped.png" height="150"/>
</p>

<h3 align="center">
Run and deploy LLM models in Unity!
</h3>

LLMUnity allows to run and distribute LLM models in the Unity engine.
<br>
LLMUnity is built on top of the awesome [llama.cpp](https://github.com/ggerganov/llama.cpp) and [llamafile](https://github.com/Mozilla-Ocho/llamafile) libraries.

<h3 align="center">Features</h3>

<div style="display: flex; justify-content: space-between;">
  <div>

- Cross-platform! Supports Windows, Linux and macOS ([supported versions](https://github.com/Mozilla-Ocho/llamafile?tab=readme-ov-file#supported-oses-and-cpus))
- Runs locally without need of internet access or supports remote servers
- Real-time inference on CPU or Nvidia GPUs
- Support of the major LLM models ([supported models](https://github.com/ggerganov/llama.cpp?tab=readme-ov-file#description))!
- Easy to setup, call with a single line code

- Free to use for both personal and commercial purposes!
  </div>
  <div>
<img width="300" src="github/demo.gif" align="right"/>
  </div>
</div>


<h3 align="center">Setup</h3>
<div style="display: flex; justify-content: space-between;">
<div>

- Install the asset in Unity
- Create an empty GameObject, press "Add Component" in the GameObject and select the LLM script (Scripts>LLM).
- Download the default model (Mistral 7B Instruct) with the "Download Model" button or load your own model in .gguf format with the "Load model" (see below)
- Define the role of the AI in the "Prompt". You can optionally specify the player and the AI name.
- (Optional) Adjust the server or model settings to your preference
    </div>
    <div>
<img width="300" src="github/GameObject.png" align="right"/>
    </div>
</div>

<h3 align="center">Options</h3>


<h3 align="center">Use your own model</h3>

A vast amount of models are available from [TheBloke](https://huggingface.co/TheBloke). Make sure to check the license before using any model!

<h3 align="center">Remote server setup</h3>
<h3 align="center">License</h3>
<h3 align="center">Author</h3>
<h3 align="center">Disclaimers</h3>