### 🚀 Features

- Cache LlamaLib to prevent re-downloads (PR: #386)
- Implement strategies for context overflow (chat truncation, chat summarization) (PR: #384)
- Upgrade LlamaLib to v2.0.4 (PR: #384)
- Re-introduce UI dropdown for level of debug messages (PR: #384)

### 🐛 Fixes

- Fix context overflow with caching and overflow strategies (PR: #384)
- Ensure macOS build includes the required runtime library (PR: #382)
- Fix inference for AMD GPUs using Vulkan (PR: #384)

