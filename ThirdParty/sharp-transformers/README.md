# sharp-transformers
## Description
Sharp Transformers is a unity plugin developed by Hugging Face.
The original code can be found here: <br>
https://github.com/huggingface/sharp-transformers/tree/577bfe23c286a605f58d328ea991aa4d4d06f4c8

## License
Sharp Transformers is distributed under Apache-2.0 license, see the [LICENSE file](LICENSE).

## Local Modifications
- adapt README.md to include the changelog from the original repo
- fix "hides inherited" warnings in Unity
- add missing LICENSE.meta

## Original Readme
The original readme is appended here:

-----

# Sharp Transformers 
A Unity plugin of utilities to **run Transformer 🤗 models in Unity games**.

Sharp transformers is designed to be **functionally equivalent to Hugging Face’s [transformers python library]**(https://github.com/huggingface/transformers/tree/main).

If you like this library, **don't hesitate to ⭐ star this repository**. This helps us for discoverability 🤗.

## Install with Unity

In your Unity Project: 
1. Go to "Window" > "Package Manager" to open the Package Manager.
2. Click the "+" in the upper left-hand corner and select "Add package from git URL".
3. Enter the URL of this repository and click "Add": https://github.com/huggingface/sharp-transformers.git

## Quick Tour

We published a tutorial where you're to build this smart robot that can understand player orders and perform them using a Sentence Similarity Model.

<img src="https://substackcdn.com/image/fetch/w_1456,c_limit,f_webp,q_auto:good,fl_progressive:steep/https%3A%2F%2Fsubstack-post-media.s3.amazonaws.com%2Fpublic%2Fimages%2F8e023e81-1644-40c3-972d-c1ccd7100bc8_640x360.gif" alt="Jammo the Robot"/>


The Tutorial 👉 https://open.substack.com/pub/thomassimonini/p/create-an-ai-robot-npc-using-hugging?r=dq5fg&utm_campaign=post&utm_medium=web

The Demo (Windows Executable) 👉 https://singularite.itch.io/jammo-the-robot-with-unity-sentis

## Supported tasks

- For now **only BERT models for embedding are supported** but we plan to support more models in the future see roadmap

## Roadmap 🗺️

- [X] Bert Tokenizer
- [ ] Llama 2 Tokenizers (BPE)
- [ ] Whisper Tokenizers and PostProcessing
- [ ] STT Tokenizers
