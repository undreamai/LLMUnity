# About UI Toolkit ChatBot Sample

This sample shows how to use LLMUnity asset along with UI Toolkit to make a simple chat bot.

In the Unity Editor, open 'Window > UI Toolkit > UI Builder' to work with UXML documents like the MainPage.uxml file.
The other 2 uxml files are PromptTemplate and ResponseTemplate which act as prefabs that are instantiated at runtime
and added to the ChatScrollView visual element.

The MainTextField.uss is a style sheet that will apply styling to the UI elements as they match the element names or types.

IMPORTANT: The only dependency which is NOT included in this sample, is the UnityDefaultRuntimeTheme file.
This file is found under 'Assets > UI Toolkit > UnityThemes'
This file MUST BE ADDED to the UIPanelSettings asset in this sample.