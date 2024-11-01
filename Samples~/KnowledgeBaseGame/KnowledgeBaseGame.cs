using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEngine.UI;
using LLMUnity;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace LLMUnitySamples
{
    public class KnowledgeBaseGame : KnowledgeBaseGameUI
    {
        [Header("Models")]
        public LLMCharacter llmCharacter;
        public RAG rag;
        public int numRAGResults = 3;

        string ragPath = "KnowledgeBaseGame.zip";
        Dictionary<string, Dictionary<string, string>> botQuestionAnswers = new Dictionary<string, Dictionary<string, string>>();
        Dictionary<string, RawImage> botImages = new Dictionary<string, RawImage>();
        string currentBotName;

        new async void Start()
        {
            base.Start();
            CheckLLMs(false);
            InitElements();
            await InitRAG();
            InitLLM();
        }

        void InitElements()
        {
            PlayerText.interactable = false;
            botImages["Butler"] = ButlerImage;
            botImages["Maid"] = MaidImage;
            botImages["Chef"] = ChefImage;
            botQuestionAnswers["Butler"] = LoadQuestionAnswers(ButlerText.text);
            botQuestionAnswers["Maid"] = LoadQuestionAnswers(MaidText.text);
            botQuestionAnswers["Chef"] = LoadQuestionAnswers(ChefText.text);
        }

        async Task InitRAG()
        {
            // create the embeddings
            await CreateEmbeddings();
            DropdownChange(CharacterSelect.value);
        }

        void InitLLM()
        {
            // warm-up the LLM
            PlayerText.text += "Warming up the model...";
            _ = llmCharacter.Warmup(AIReplyComplete);
        }

        public Dictionary<string, string> LoadQuestionAnswers(string questionAnswersText)
        {
            Dictionary<string, string> questionAnswers = new Dictionary<string, string>();
            foreach (string line in questionAnswersText.Split("\n"))
            {
                if (line == "") continue;
                string[] lineParts = line.Split("|");
                questionAnswers[lineParts[0]] = lineParts[1];
            }
            return questionAnswers;
        }

        public async Task CreateEmbeddings()
        {
            bool loaded = await rag.Load(ragPath);
            if (!loaded)
            {
    #if UNITY_EDITOR
                Stopwatch stopwatch = new Stopwatch();
                // build the embeddings
                foreach ((string botName, Dictionary<string, string> botQuestionAnswers) in botQuestionAnswers)
                {
                    PlayerText.text += $"Creating Embeddings for {botName} (only once)...\n";
                    List<string> questions = botQuestionAnswers.Keys.ToList();
                    stopwatch.Start();
                    foreach (string question in questions) await rag.Add(question, botName);
                    stopwatch.Stop();
                    Debug.Log($"embedded {rag.Count()} phrases in {stopwatch.Elapsed.TotalMilliseconds / 1000f} secs");
                }
                // store the embeddings
                rag.Save(ragPath);
    #else
                // if in play mode throw an error
                throw new System.Exception("The embeddings could not be found!");
    #endif
            }
        }

        public async Task<List<string>> Retrieval(string question)
        {
            // find similar questions for the current bot using the RAG
            (string[] similarQuestions, _) = await rag.Search(question, numRAGResults, currentBotName);
            // get the answers of the similar questions
            List<string> similarAnswers = new List<string>();
            foreach (string similarQuestion in similarQuestions) similarAnswers.Add(botQuestionAnswers[currentBotName][similarQuestion]);
            return similarAnswers;
        }

        public async Task<string> ConstructPrompt(string question)
        {
            // get similar answers from the RAG
            List<string> similarAnswers = await Retrieval(question);
            // create the prompt using the user question and the similar answers
            string answers = "";
            foreach (string similarAnswer in similarAnswers) answers += $"\n- {similarAnswer}";
            // string prompt = $"Robot: {currentBotName}\n\n";
            string prompt = $"Question: {question}\n\n";
            prompt += $"Possible Answers: {answers}";
            return prompt;
        }

        protected async override void OnInputFieldSubmit(string question)
        {
            PlayerText.interactable = false;
            SetAIText("...");
            string prompt = await ConstructPrompt(question);
            _ = llmCharacter.Chat(prompt, SetAIText, AIReplyComplete);
        }

        protected override void DropdownChange(int selection)
        {
            // select another character
            if (!String.IsNullOrEmpty(currentBotName)) botImages[currentBotName].gameObject.SetActive(false);
            currentBotName = CharacterSelect.options[selection].text;
            botImages[currentBotName].gameObject.SetActive(true);
            Debug.Log($"{currentBotName}: {rag.Count(currentBotName)} phrases available");

            // set the LLMCharacter name
            llmCharacter.AIName = currentBotName;
        }

        void SetAIText(string text)
        {
            AIText.text = text;
        }

        void AIReplyComplete()
        {
            PlayerText.interactable = true;
            PlayerText.Select();
            PlayerText.text = "";
        }

        public void CancelRequests()
        {
            llmCharacter.CancelRequests();
            AIReplyComplete();
        }

        public void ExitGame()
        {
            Debug.Log("Exit button clicked");
            Application.Quit();
        }

        void CheckLLM(LLMCaller llmCaller, bool debug)
        {
            if (!llmCaller.remote && llmCaller.llm != null && llmCaller.llm.model == "")
            {
                string error = $"Please select a llm model in the {llmCaller.llm.gameObject.name} GameObject!";
                if (debug) Debug.LogWarning(error);
                else throw new System.Exception(error);
            }
        }

        void CheckLLMs(bool debug)
        {
            CheckLLM(rag.search.llmEmbedder, debug);
            CheckLLM(llmCharacter, debug);
        }

        bool onValidateWarning = true;
        void OnValidate()
        {
            if (onValidateWarning)
            {
                CheckLLMs(true);
                onValidateWarning = false;
            }
        }
    }

    public class KnowledgeBaseGameUI : MonoBehaviour
    {
        [Header("UI elements")]
        public Dropdown CharacterSelect;
        public InputField PlayerText;
        public Text AIText;

        [Header("Bot texts")]
        public TextAsset ButlerText;
        public TextAsset MaidText;
        public TextAsset ChefText;

        [Header("Bot images")]
        public RawImage ButlerImage;
        public RawImage MaidImage;
        public RawImage ChefImage;

        [Header("Buttons")]
        public Button NotesButton;
        public Button MapButton;
        public Button SolveButton;
        public Button HelpButton;
        public Button SubmitButton;

        [Header("Panels")]
        public RawImage NotebookImage;
        public GameObject NotesPanel;
        public GameObject SolvePanel;
        public GameObject HelpPanel;
        public RawImage MapImage;
        public RawImage SuccessImage;
        public Text FailText;
        public Dropdown Answer1;
        public Dropdown Answer2;
        public Dropdown Answer3;

        protected void Start()
        {
            AddListeners();
        }

        void OnValueChanged(string newText)
        {
            // Get rid of newline character added when we press enter
            if (Input.GetKey(KeyCode.Return))
            {
                if (PlayerText.text.Trim() == "")
                    PlayerText.text = "";
            }
        }

        protected virtual void AddListeners()
        {
            CharacterSelect.onValueChanged.AddListener(DropdownChange);
            NotesButton.onClick.AddListener(ShowNotes);
            MapButton.onClick.AddListener(ShowMap);
            SolveButton.onClick.AddListener(ShowSolve);
            HelpButton.onClick.AddListener(ShowHelp);
            SubmitButton.onClick.AddListener(SubmitAnswer);
            Answer1.onValueChanged.AddListener(HideFail);
            Answer2.onValueChanged.AddListener(HideFail);
            Answer3.onValueChanged.AddListener(HideFail);
            PlayerText.onSubmit.AddListener(OnInputFieldSubmit);
            PlayerText.onValueChanged.AddListener(OnValueChanged);
        }

        protected virtual void DropdownChange(int selection) {}
        protected virtual void OnInputFieldSubmit(string question) {}

        void ShowNotes()
        {
            NotesPanel.gameObject.SetActive(true);
            HelpPanel.gameObject.SetActive(false);
            SolvePanel.gameObject.SetActive(false);
            NotebookImage.gameObject.SetActive(true);
        }

        void ShowMap()
        {
            MapImage.gameObject.SetActive(true);
        }

        void HideFail(int selection)
        {
            FailText.gameObject.SetActive(false);
        }

        void ShowSolve()
        {
            HideFail(0);
            NotesPanel.gameObject.SetActive(false);
            HelpPanel.gameObject.SetActive(false);
            SolvePanel.gameObject.SetActive(true);
            NotebookImage.gameObject.SetActive(true);
        }

        void ShowHelp()
        {
            NotesPanel.gameObject.SetActive(false);
            HelpPanel.gameObject.SetActive(true);
            SolvePanel.gameObject.SetActive(false);
            NotebookImage.gameObject.SetActive(true);
        }

        void SubmitAnswer()
        {
            if (Answer1.options[Answer1.value].text == "Professor Pluot" && Answer2.options[Answer2.value].text == "Living Room" && Answer3.options[Answer3.value].text == "A Hollow Bible")
            {
                NotebookImage.gameObject.SetActive(false);
                SuccessImage.gameObject.SetActive(true);
            }
            else
            {
                FailText.gameObject.SetActive(true);
            }
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                foreach (RawImage image in new RawImage[] {NotebookImage, MapImage, SuccessImage})
                {
                    if (image.IsActive() && !RectTransformUtility.RectangleContainsScreenPoint(image.rectTransform, Input.mousePosition))
                    {
                        image.gameObject.SetActive(false);
                    }
                }
            }
        }
    }
}
