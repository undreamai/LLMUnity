### 🚀 Features

- Add Phi-3.5 and Llama 3.2 models (PR: #255)
- Automatically set context size for models when it is too large (>32768) (PR: #256)
- Speedup LLMCharacter warmup (PR: #257)

### 🐛 Fixes

- fix handling of incomplete requests (PR: #251)
- fix Unity locking of DLLs during cross-platform build (PR: #252)
- allow spaces in lora paths (PR: #254)

### 📦 General

- Set default context size to 8192 and allow to adjust with a UI slider (PR: #258)

