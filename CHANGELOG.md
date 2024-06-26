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
