using System.Collections.Generic;

namespace LLMUnity
{
    public abstract class ChatTemplate
    {
        public readonly List<string> stop;
        public abstract string ComputePrompt(List<ChatMessage> messages);
    }

    public class ChatMLTemplate : ChatTemplate
    {
        public new readonly List<string> stop = new List<string> { "\n<|im_start|>", "<|im_start|>", "\n<|im_end|>", "<|im_end|>"};

        public string RoleString(string role)
        {
            // role as a delimited string for the model
            return "<|im_start|>" + role + "\n";
        }

        public string RoleMessageString(string role, string message)
        {
            // role and the role message
            return RoleString(role) + message + "<|im_end|>\n";
        }

        public override string ComputePrompt(List<ChatMessage> messages)
        {
            string chatPrompt = "";
            for (int i = 0; i < messages.Count; i++)
            {
                chatPrompt += RoleMessageString(messages[i].role, messages[i].content);
            }
            chatPrompt += RoleString("assistant");
            return chatPrompt;
        }
    }

    public class MistralInstructTemplate : ChatTemplate
    {
        public new readonly List<string> stop = new List<string> { "\n[INST]", "[INST]", "\n[/INST]", "[/INST]" };

        public override string ComputePrompt(List<ChatMessage> messages)
        {
            string chatPrompt = "<s>";
            string systemPrompt = "";
            int start = 0;
            if (messages[0].role == "system")
            {
                systemPrompt = messages[0].content + "\n\n";
                start = 1;
            }
            for (int i = start; i < messages.Count; i += 2)
            {
                chatPrompt += "[INST] ";
                if (i == 1 && systemPrompt != "") chatPrompt += systemPrompt;
                chatPrompt += messages[i].content + " [/INST]";
                if (i < messages.Count - 1) chatPrompt += messages[i + 1].content + "</s>";
            }
            return chatPrompt;
        }
    }

    public class LLama2Template : ChatTemplate
    {
        public new readonly List<string> stop = new List<string> { "\n[INST]", "[INST]", "\n[/INST]", "[/INST]" };

        public override string ComputePrompt(List<ChatMessage> messages)
        {
            string chatPrompt = "";
            string systemPrompt = "";
            int start = 0;
            if (messages[0].role == "system")
            {
                systemPrompt = "<<SYS>>\n" + messages[0].content + "\n<</SYS>> ";
                start = 1;
            }
            for (int i = start; i < messages.Count; i += 2)
            {
                chatPrompt += "<s>[INST] ";
                if (i == 1 && systemPrompt != "") chatPrompt += systemPrompt;
                chatPrompt += messages[i].content + " [/INST]";
                if (i < messages.Count - 1) chatPrompt += messages[i + 1].content + " </s>";
            }
            return chatPrompt;
        }
    }

    public class AlpacaTemplate : ChatTemplate
    {
        public new readonly List<string> stop = new List<string> { "\n###", "###" };

        public override string ComputePrompt(List<ChatMessage> messages)
        {
            string chatPrompt = "";
            int start = 0;
            if (messages[0].role == "system")
            {
                chatPrompt += messages[0].content + "\n\n";
                start = 1;
            }
            for (int i = start; i < messages.Count; i += 2)
            {
                chatPrompt += "### Instruction: " + messages[i].content + "\n\n";
                if (i < messages.Count - 1) chatPrompt += "### Response: " + messages[i + 1].content + "</s>";
            }
            return chatPrompt;
        }
    }

    public class Phi2Template : ChatTemplate
    {
        public new readonly List<string> stop = new List<string> { "\nuser:", "user:", "\nassistant:", "assistant:"};

        public override string ComputePrompt(List<ChatMessage> messages)
        {
            string chatPrompt = "";
            int start = 0;
            if (messages[0].role == "system")
            {
                chatPrompt += messages[0].content + "\n\n";
                start = 1;
            }
            for (int i = start; i < messages.Count; i += 2)
            {
                chatPrompt += "user: " + messages[i].content + "\n";
                if (i < messages.Count - 1) chatPrompt += "assistant: " + messages[i + 1].content + "\n";
            }
            chatPrompt += "assistant: ";
            return chatPrompt;
        }
    }

    public class ZephyrTemplate : ChatTemplate
    {
        public new readonly List<string> stop = new List<string> { "\n<|user|>", "<|user|>", "\n<|assistant|>", "<|assistant|>"};

        public override string ComputePrompt(List<ChatMessage> messages)
        {
            string chatPrompt = "";
            for (int i = 0; i < messages.Count; i++)
            {
                chatPrompt += "<|" + messages[i].role + "|>\n" + messages[i].content + "</s>\n";
            }
            chatPrompt += "<|assistant|>\n";
            return chatPrompt;
        }
    }
}
