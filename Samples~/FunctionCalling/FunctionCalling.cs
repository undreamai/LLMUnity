using UnityEngine;
using LLMUnity;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Reflection;

namespace LLMUnitySamples
{
    public static class Functions
    {
        static System.Random random = new System.Random();


        public static string Weather()
        {
            string[] weather = new string[]{"sunny", "rainy", "cloudy", "snowy"};
            return "The weather is " + weather[random.Next(weather.Length)];
        }

        public static string Time()
        {
            return "The time is " + random.Next(24).ToString("D2") + ":" + random.Next(60).ToString("D2");
        }

        public static string Emotion()
        {
            string[] emotion = new string[]{"happy", "sad", "exhilarated", "ok"};
            return "I am feeling " + emotion[random.Next(emotion.Length)];
        }
    }

    public class FunctionCalling : MonoBehaviour
    {
        public LLMCharacter llmCharacter;
        public InputField playerText;
        public Text AIText;

        void Start()
        {
            playerText.onSubmit.AddListener(onInputFieldSubmit);
            playerText.Select();
            llmCharacter.grammarString = MultipleChoiceGrammar();
        }

        string[] GetFunctionNames()
        {
            List<string> functionNames = new List<string>();
            foreach (var function in typeof(Functions).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)) functionNames.Add(function.Name);
            return functionNames.ToArray();
        }

        string MultipleChoiceGrammar()
        {
            return "root ::= (\"" + string.Join("\" | \"", GetFunctionNames()) + "\")";
        }

        string ConstructPrompt(string message)
        {
            string prompt = "Which of the following choices matches best the input?\n\n";
            prompt += "Input:" + message + "\n\n";
            prompt += "Choices:\n";
            foreach(string functionName in GetFunctionNames()) prompt += $"- {functionName}\n";
            prompt += "\nAnswer directly with the choice";
            return prompt;
        }

        string CallFunction(string functionName)
        {
            return (string) typeof(Functions).GetMethod(functionName).Invoke(null, null);
        }

        async void onInputFieldSubmit(string message)
        {
            playerText.interactable = false;
            string functionName = await llmCharacter.Chat(ConstructPrompt(message));
            string result = CallFunction(functionName);
            AIText.text = $"Calling {functionName}\n{result}";
            playerText.interactable = true;
        }

        public void CancelRequests()
        {
            llmCharacter.CancelRequests();
        }

        public void ExitGame()
        {
            Debug.Log("Exit button clicked");
            Application.Quit();
        }

        bool onValidateWarning = true;
        void OnValidate()
        {
            if (onValidateWarning && !llmCharacter.remote && llmCharacter.llm != null && llmCharacter.llm.model == "")
            {
                Debug.LogWarning($"Please select a model in the {llmCharacter.llm.gameObject.name} GameObject!");
                onValidateWarning = false;
            }
        }
    }
}
