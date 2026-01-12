# **LLMUnity Migration Guide: Upgrading to the New Architecture**

This guide helps you migrate to LLMUnity v3 from v2.

The LLM backend, [LlamaLib](https://github.com/undreamai/LlamaLib), has been completely rewritten and most of the LLM functionality in Unity has now been ported to the backend.<br>
At the same time, LlamaLib has been implemented as a clean, high-level library that can been used on its own by C++ and C# developers.<br>
Special care has been taken to provide better support for future llama.cpp versions, streamlining more the upgrade process.

## **Breaking Changes - Graphical Interface**
In terms of graphical interface in Unity, there have been almost no changes:
- The LLMCharacter class has been renamed to LLMAgent
- The chat templates have been removed from the LLM model management (LLM GameObjects)
- "Use extras" has been renamed to "Use cuBLAS" (LLM GameObjects)
- Grammars can be directly edited (LLMAgent GameObjects)

## **Breaking Changes - Scripting**

### **1\. LLM Server (LLM.cs)**

#### **API Changes**

**Setting Model:**

``` c#
// OLD
llm.SetModel("path/to/model.gguf");

// NEW
llm.model = "path/to/model.gguf";
```

**Setting Chat Template:**

Setting the chat template is not needed anymore, it is handled by LlamaLib and loads the LLM-supported template automatically.

``` c#
// OLD
llm.SetTemplate("chatml");

// NEW
// Template is auto-detected from model_
```

### **2\. Chat Agent (LLMCharacter → LLMAgent)**

LLMCharacter has been moved to a new class LLMAgent.<br>
**Note:** LLMCharacter still exists as a deprecated wrapper for backward compatibility, but will be removed in future versions.

#### **Property changes**

| **Old Property** | **New Property** | **Migration** |
| --- | --- | --- |
| prompt | systemPrompt | Direct rename |
| chat | chat | Now a property with getter/setter |
| playerName | Removed | set to "user" for broad model compatibility |
| AIName | Removed | set to "assistant" for broad model compatibility |
| saveCache | Removed | Cache management removed |


#### **API Changes**

**Clear History:**
``` c#
// OLD
character.ClearChat();

// NEW
await agent.ClearHistory();
```

**Setting / Changing the System Prompt:**
- Without clearing the history

``` c#
// OLD
character.SetPrompt("You are a helpful assistant", clearChat: false);

// NEW
agent.systemPrompt = "You are a helpful assistant";
```

- Clearing the history
``` c#
// OLD
character.SetPrompt("You are a helpful assistant", clearChat: true);

// NEW
agent.systemPrompt = "You are a helpful assistant";
await agent.ClearHistory();
```

**Managing History:**

``` c#
character.AddPlayerMessage("Hello");
character.AddAIMessage("Hi there");

// NEW
await agent.AddUserMessage("Hello");
await agent.AddAssistantMessage("Hi there");
```

**Saving/Loading:**

``` c#
// OLD
await character.Save("chat_history");
await character.Load("chat_history");

// NEW
await agent.SaveHistory();
await agent.LoadHistory();
```

The history path is directly the `character.save` property.

**Grammar:**

Setting the grammar directly:
``` c#
// OLD
llmClient.grammarString = "your grammar here";

// NEW
llmClient.grammar = "your grammar here";
```

Loading a grammar file:
``` c#
// OLD
await character.SetGrammar("path/to/grammar.gbnf");
await character.SetJSONGrammar("path/to/schema.json");

// NEW
llmClient.LoadGrammar("path/to/grammar.gbnf");
llmClient.LoadGrammar("path/to/schema.json");
```

### **Reasoning Mode (New Feature)**

``` c#
// NEW - Enable "thinking" mode of the LLM
llm.reasoning = true;
// Or
llm.SetReasoning(true);
```

## **Migration Steps**

### **Step 1: Update Class Inheritance**

``` c#
// Change LLMCharacter to LLMAgent
public class MyNPC : LLMAgent // was LLMCharacter
{
// Your code_
}
```

### **Step 2: Update Property Names**

``` c#
// In LLMAgent
// Remove: 
// agent.playerName = "Player";
// agent.AIName = "AI";
agent.systemPrompt = "..."; // was character.prompt
```

### **Step 3: Update Method Calls**

``` c#
// History management
await agent.ClearHistory(); // was character.ClearChat()
await agent.AddUserMessage("Hi"); // was character.AddPlayerMessage("Hi")
await agent.AddAssistantMessage("Hello"); // was character.AddAIMessage("Hello")
```

### **Step 4: Update Grammar**

``` c#
agent.grammar = "your grammar here"; // was character.grammarString = "your grammar here"
// or
agent.LoadGrammar("grammar.gbnf"); // was await character.SetGrammar("path/to/grammar.gbnf"); or await character.SetJSONGrammar("path/to/schema.json");
```

### **Step 5: Update Save/Load**

``` c#
agent.save = "history"; // Set once
await agent.SaveHistory(); // was await character.Save("history");
await agent.LoadHistory(); // was await character.Load("history");
```

### **Step 6: Remove Chat Template**

``` c#
// OLD
// Remove: 
// llm.SetTemplate("phi-3");
```
