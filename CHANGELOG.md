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