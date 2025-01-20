# LLaVA Vision Integration for Unity with LLMforUnity

This project integrates LLaVA (LLaMA-based Vision Language Model) into Unity using LLMforUnity. The integration allows you to test text and image input using Ollama as the backend server.

1. Prerequisites

1.1 Install Ollama

Ollama provides a local inference server for LLaMA-based models.

    Download and install Ollama from the official website. [Download Ollama](https://ollama.com/download)

Verify the installation:

    ollama --version

Link: [Ollama Download](https://ollama.com/download)

1.2 Download LLaVA Model

The model used here is LLaVA-LLaMA-3-8B-v1.1.

    Visit the model page and download the model weights in gguf format. [LLaVA Model](https://huggingface.co/xtuner/llava-llama-3-8b-v1_1)

Link: [LLaVA Model](https://huggingface.co/xtuner/llava-llama-3-8b-v1_1)

1.3 Create and Register the Model in Ollama

To make the model usable with Ollama, follow these steps:

    Move the downloaded .gguf model file into the correct Ollama directory:

    Home/<user>/.ollama/models

    Replace <user> with your system username.

    Open your terminal and use the following commands to create the model in Ollama:

    ollama create llava-llama3-int4 -f llava-llama3-int4.gguf

    Note: Replace llava-llama3-int4.gguf with the correct file name for your downloaded .gguf model.

    Verify the model is registered:

    ollama list

You should see the model name (llava-llama3-int4) in the list.

2. Test the Ollama Server

Before integrating the model with Unity, test it using a basic terminal command:

    ollama run llava-llama3-int4 "Hello, world!"

If successful, the model should respond with text output.

3. Unity Integration

3.1 LLMforUnity Asset Setup

    Install the LLMforUnity asset into your Unity project.
        This is the core asset for managing LLaMA models within Unity.

Link: [LLMforUnity GitHub Repository](https://github.com/undream-ai/llm-for-unity)

    Add the OllamaTest.cs script to your Unity project.
    This script tests the API connection to the local Ollama server.

3.2 API Endpoint Configuration

Ensure Ollama is running on the default endpoint:

    Host: http://127.0.0.1
    Port: 11434

3.3 Run the Test Script

    Attach the OllamaTest script to a GameObject in your scene.
    Start Play mode in Unity.

You should see logs confirming the connection to the Ollama API and the response from the model.

4. LLaVA Scene

If the Ollama server is set up correctly and no additional ports need to be configured, the LLaVA Scene should already work.

    It uses the integrated camera feed to capture images.
    The images are converted to Base64 and sent to the LLaVA model for processing.

5. Troubleshooting

    Port Issues: Ensure that port 11434 is not blocked or occupied by other processes.
    Model Not Found:
        Verify the model name during creation and listing. Use ollama list to double-check.
        Ensure the model file is located in the correct directory:

        Home/<user>/.ollama/models

    Image Conversion: The Main.cs script currently relies on Unity's WebcamTexture for image input. Ensure the webcam is accessible.

Next Steps

    Test the integration with different LLaVA models.
    Provide feedback or report issues to collaborate further on improving the asset.

Contributors:

    Special thanks to UndreamAI for the LLMforUnity asset.

Links:

    [Ollama Download](https://ollama.com/download)
    [LLaVA Model](https://huggingface.co/xtuner/llava-llama-3-8b-v1_1)
    [LLMforUnity GitHub Repository](https://github.com/undream-ai/llm-for-unity)

