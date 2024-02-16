using LLMUnity;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Demonstrates how to use the LLM component to chat with an AI and provides a simple UI using UI Toolkit.
/// On enable the prompt and response templates are loaded and the LLM component is warmed up before displaying the 1st prompt.
/// On enter key press, the user prompt is sent to the LLM component and the streaming response is displayed.
/// On completion of the response, the prompt is displayed again.
/// In this example, the chat added to LLM history.
/// </summary>
public class ChatWithAI : MonoBehaviour
{
    [Tooltip("The UIDocument which holds the reference to the MainPage uxml file")]
    public UIDocument uiDocument;

    [Tooltip("The LLM component to use for chat")]
    public LLM llm;

    VisualElement root;
    ScrollView chatScrollView;
    VisualTreeAsset promptTextFieldTemplate;
    VisualTreeAsset responseTextFieldTemplate;
    TextField currentResponse;
    bool triggerUpdateScrollView;

    async void OnEnable()
    {
        promptTextFieldTemplate = Resources.Load<VisualTreeAsset>("PromptTemplate");
        responseTextFieldTemplate = Resources.Load<VisualTreeAsset>("ResponseTemplate");
        
        root = uiDocument.rootVisualElement;
        chatScrollView = root.Q<ScrollView>(name: "ChatScrollView");
        chatScrollView.Clear();

        await llm.Warmup(); // Processes the prompt from the LLM component
        DisplayPrompt();
    }

    void Update()
    {
        if (triggerUpdateScrollView)
        {
            UpdateScrollView();
            triggerUpdateScrollView = false;
        }
    }

    /// <summary>
    /// Display the next prompt for the user to enter.
    /// As this example uses TextField elements for user and response prompts any previous prompt and response is made read-only.
    /// </summary>
    void DisplayPrompt()
    {
        if (currentResponse != null)
        {
            currentResponse.isReadOnly = true;
        }
        var promptInstance = promptTextFieldTemplate.Instantiate();
        var prompt = promptInstance.Q<TextField>(name: "Prompt");
        prompt.RegisterCallback<KeyDownEvent>(ev => TriggerResponse(ev, prompt));
        chatScrollView.Add(prompt);
        triggerUpdateScrollView = true;
        prompt.Focus();
    }

    /// <summary>
    /// When the user presses the enter key, the prompt is made read-only and the user prompt is sent to the LLM component.
    /// </summary>
    /// <param name="evt">The KeyDownEvent</param>
    /// <param name="prompt">The TextField of the user prompt which contains the text</param>
    void TriggerResponse(KeyDownEvent evt, TextField prompt)
    {
        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            prompt.isReadOnly = true;
            string userPrompt = prompt.text.Replace("\v", "\n");
            var responseInstance = responseTextFieldTemplate.Instantiate();
            currentResponse = responseInstance.Q<TextField>(name: "Response");
            chatScrollView.Add(currentResponse);
            triggerUpdateScrollView = true;
            _ = llm.Chat(userPrompt, ResponseCallback, DisplayPrompt, true);
        }
    }

    /// <summary>
    /// The streaming response from the LLM component is displayed in the response TextField.
    /// </summary>
    /// <param name="message"></param>
    void ResponseCallback(string message)
    {
        currentResponse.SetValueWithoutNotify(message);
        triggerUpdateScrollView = true;
    }

    /// <summary>
    /// Updates the scroll view to show the latest response.
    /// </summary>
    void UpdateScrollView()
    {
        chatScrollView.scrollOffset = new Vector2(0, chatScrollView.contentContainer.layout.size.y - chatScrollView.contentViewport.layout.size.y);
    }
}
