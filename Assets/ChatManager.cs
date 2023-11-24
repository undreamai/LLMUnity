using UnityEngine;
using TMPro;

public class ChatManager : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Text displayText;

    void Start()
    {
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(onInputFieldSubmit);
        }
    }

    void onInputFieldSubmit(string newText){
        inputField.ActivateInputField();
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)){
            inputField.text += "\n";
            inputField.caretPosition = inputField.text.Length;
            return;
        }
        displayText.text += "\n" + inputField.text;
        inputField.text = "";
    }
}
