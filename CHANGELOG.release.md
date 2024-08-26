### 🚀 Features

- Update to latest llama.cpp (b3617) (PR: #210)
- Integrate Llama 3.1 and Gemma2 models in model dropdown
- Implement embedding and lora adapter functionality (PR: #210)
- Read context length and warn if it is very large (PR: #211)
- Setup to allow to use extra features: flash attention and IQ quants (PR: #216)
- Allow HTTP request retries for remote server (PR: #217)
- Allow to set lora weights at startup, add unit test (PR: #219)

### 🐛 Fixes

- Fix set template for remote setup (PR: #208)
- fix crash when stopping scene before LLM creation (PR: #214)

### 📦 General

- Documentation/point to gguf format for lora (PR: #215)

