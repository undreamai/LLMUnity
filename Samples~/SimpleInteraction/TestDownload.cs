using UnityEngine;
using LLMUnity;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.IO;

namespace LLMUnitySamples
{
    public class TestDownload : MonoBehaviour
    {
        public InputField playerText;
        public Scrollbar progressBar;
        public Text progressText;
        public Toggle overwriteToggle;

        void Start()
        {
            playerText.onSubmit.AddListener(onInputFieldSubmit);
        }

        void SetProgress(float progress)
        {
            // Debug.Log(progress);
            progressText.text = ((int)(progress * 100)).ToString() + "%";
            progressBar.size = progress;
        }

        string path;
        void onInputFieldSubmit(string message)
        {
            string url = message.Trim();
            path = "/tmp/" + Path.GetFileName(url).Split("?")[0];
            playerText.interactable = false;
            Debug.Log(overwriteToggle.isOn);
            _ = LLMUnitySetup.DownloadFile(
                url, path, overwriteToggle.isOn,
                CompleteCallback, SetProgress
            );
        }

        public void CompleteCallback(string path)
        {
            Complete();
        }

        public void Complete()
        {
            playerText.interactable = true;
            playerText.Select();
            playerText.text = "";
        }

        public void CancelRequests()
        {
            LLMUnitySetup.CancelDownload(path);
            Complete();
        }
    }
}
