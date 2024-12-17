## v2.4.1
#### 🚀 Features

- Static library linking on mobile (fixes iOS signing) (PR: #289)

#### 🐛 Fixes

- Fix support for extras (flash attention, iQ quants)  (PR: #292)


## v2.4.0
#### 🚀 Features

- iOS deployment (PR: #267)
- Improve building process (PR: #282)
- Add structured output / function calling sample (PR: #281)
- Update LlamaLib to v1.2.0 (llama.cpp b4218) (PR: #283)

#### 🐛 Fixes

- Clear temp build directory before building (PR: #278)

#### 📦 General

- Remove support for extras (flash attention, iQ quants) (PR: #284)
- remove support for LLM base prompt (PR: #285)


## v2.3.0
#### 🚀 Features

- Implement Retrieval Augmented Generation (RAG) in LLMUnity (PR: #246)

#### 🐛 Fixes

- Fixed build conflict, endless import of resources. (PR: #266)


## v2.2.4
#### 🚀 Features

- Add Phi-3.5 and Llama 3.2 models (PR: #255)
- Speedup LLMCharacter warmup (PR: #257)

#### 🐛 Fixes

- Fix handling of incomplete requests (PR: #251)
- Fix Unity locking of DLLs during cross-platform build (PR: #252)
- Allow spaces in lora paths (PR: #254)

#### 📦 General

- Set default context size to 8192 and allow to adjust with a UI slider (PR: #258)


## v2.2.3
#### 🚀 Features

- LlamaLib v1.1.12: SSL certificate & API key for server, Support more AMD GPUs (PR: #241)
- Server security with API key and SSL (PR: #238)
- Show server command for easier deployment (PR #239)

#### 🐛 Fixes

- Fix multiple LLM crash on Windows (PR: #242)
- Exclude system prompt from saving of chat history (PR: #240)


## v2.2.2
#### 🚀 Features

- Allow to set the LLMCharacter slot (PR: #231)

#### 🐛 Fixes

- fix adding grammar from StreamingAssets (PR: #229)
- fix library setup restart when interrupted (PR: #232)
- Remove unnecessary Android linking in IL2CPP builds (PR: #233)


## v2.2.1
#### 🐛 Fixes

- Fix naming showing full path when loading model (PR: #224)
- Fix parallel prompts (PR: #226)


## v2.2.0
#### 🚀 Features

- Implement embedding and lora adapter functionality (PR: #210)
- Read context length and warn if it is very large (PR: #211)
- Setup allowing to use extra features: flash attention and IQ quants (PR: #216)
- Allow HTTP request retries for remote server (PR: #217)
- Allow to set lora weights at startup, add unit test (PR: #219)
- allow relative StreamingAssets paths for models (PR: #221)

#### 🐛 Fixes

- Fix set template for remote setup (PR: #208)
- fix crash when stopping scene before LLM creation (PR: #214)

#### 📦 General

- Documentation/point to gguf format for lora (PR: #215)


## v2.1.1
#### 🐛 Fixes

- Resolve build directory creation

## v2.1.0
#### 🚀 Features

- Android deployment (PR: #194)
- Allow to download models on startup with resumable download functionality (PR: #196)
- LLM model manager (PR: #196)
- Add Llama 3 7B and Qwen2 0.5B models (PR: #198)
- Start LLM always asynchronously (PR: #199)
- Add contributing guidelines (PR: #201)

## v2.0.3
#### 🚀 Features

- Add LLM selector in Inspector mode (PR: #182)
- Allow to save chat history at custom path (PR: #179)
- Use asynchronous startup by default (PR: #186)
- Assign LLM if not set according to the scene and hierarchy (PR: #187)
- Allow to set log level (PR: #189)
- Allow to add callback functions for error messages (PR: #190)
- Allow to set a LLM base prompt for all LLMCharacter objects (PR: #192)

#### 🐛 Fixes

- set higher priority for mac build with Accelerate than without (PR: #180)
- Fix duplicate bos warning


## v2.0.2
#### 🐛 Fixes

- Fix bugs in chat completion (PR: #176)
- Call DontDestroyOnLoad on root to remove warning (PR: #174)


## v2.0.1
#### 🚀 Features

- Implement backend with DLLs (PR: #163)
- Separate LLM from LLMClient functionality (PR: #163)
- Add sample with RAG and LLM integration (PR: #170)


## v1.2.9
#### 🐛 Fixes

- disable GPU compilation when running on CPU (PR: #159)


## v1.2.8
#### 🚀 Features

- Switch to llamafile v0.8.6 (PR: #155)
- Add phi-3 support (PR: #156)


## v1.2.7
#### 🚀 Features

- Add Llama 3 and Vicuna chat templates (PR: #145)

#### 📦 General

- Use the context size of the model by default for longer history (PR: #147)


## v1.2.6
#### 🚀 Features

- Add documentation (PR: #135)

#### 🐛 Fixes

- Add server security for interceptions from external llamafile servers (PR: #132)
- Adapt server security for macOS (PR: #137)

#### 📦 General

- Add sample to demonstrates the async functionality (PR: #136)


## v1.2.5
#### 🐛 Fixes

- Add to chat history only if the response is not null (PR: #123)
- Allow SetTemplate function in Runtime (PR: #129)


## v1.2.4
#### 🚀 Features

- Use llamafile v0.6.2 (PR: #111)
- Pure text completion functionality (PR: #115)
- Allow change of roles after starting the interaction (PR: #120)

#### 🐛 Fixes

- use Debug.LogError instead of Exception for more verbosity (PR: #113)
- Trim chat responses (PR: #118)
- Fallback to CPU for macOS with unsupported GPU (PR: #119)
- Removed duplicate EditorGUI.EndChangeCheck() (PR: #110)

#### 📦 General

- Provide access to LLMUnity version (PR: #117)
- Rename to "LLM for Unity" (PR: #121)


## v1.2.3
#### 🐛 Fixes

- Fix async server 2 (PR: #108)


## v1.2.2
#### 🐛 Fixes

- use namespaces in all classes (PR: #104)
- await separately in StartServer (PR: #107)


## v1.2.1
#### 🐛 Fixes

- Kill server after Unity crash (PR: #101)
- Persist chat template on remote servers (PR: #103)


## v1.2.0
#### 🚀 Features

- LLM server unit tests (PR: #90)
- Implement chat templates (PR: #92)
- Stop chat functionality (PR: #95)
- Keep only the llamafile binary (PR: #97)

#### 🐛 Fixes

- Fix remote server functionality (PR: #96)
- Fix Max issue needing to run llamafile manually the first time (PR: #98)

#### 📦 General

- Async startup support (PR: #89)


## v1.1.1
#### 📦 General

- Refactoring and small enhancements (PR: #80)


## v1.0.6
#### 🐛 Fixes

- Fix Mac command spaces (PR: #71)


## v1.0.5
#### 🚀 Features

- Expose new llama.cpp arguments (PR: #60)
- Allow to change prompt (PR: #64)
- Feature/variable sliders (PR: #65)
- Feature/show expert options (PR: #66)
- Improve package loading (PR: #67)

#### 🐛 Fixes

- Fail if port is already in use (PR: #62)
- Run server without mmap on mmap crash (PR: #63)


## v1.0.4
#### 🐛 Fixes

- Fix download function (PR: #51)

#### 📦 General

- Added how settings impact generation to the readme (PR: #49)


## v1.0.3
#### 🐛 Fixes

- fix slash in windows paths (PR: #42)
- Fix chmod when deploying from windows (PR: #43)


## v1.0.2
#### 🚀 Features

- Code auto-formatting (PR: #26)
- Setup auto-formatting precommit (PR: #31)
- Start server on Awake instead of OnEnable (PR: #28)
- AMD support, switch to llamafile 0.6 (PR: #33)
- Release workflows (PR: #35)

#### 🐛 Fixes

- Support Unity 2021 LTS (PR: #32)
- Fix macOS command (PR: #34)
- Release fixes and readme (PR: #36)


## v1.0.1
- Fix running commands for projects with space in path
  -  closes #8
  -  closes #9
- Fix sample scenes for different screen resolutions
  -  closes #10
- Allow parallel prompts
