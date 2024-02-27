using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LLMUnity
{
    public abstract class ChatTemplate
    {
        public readonly List<string> stop;
        public abstract string ComputePrompt(List<ChatMessage> messages);

        public static Dictionary<string, ChatTemplate> templates = new Dictionary<string, ChatTemplate>()
        {
            {"chatml", new ChatMLTemplate()},
            {"mistral", new MistralInstructTemplate()},
            {"llama", new LLama2Template()},
            {"alpaca", new AlpacaTemplate()},
            {"phi", new Phi2Template()},
            {"zephyr", new ZephyrTemplate()},
        };

        public static Dictionary<string, string> aliasTemplates = new Dictionary<string, string>()
        {
            {"openhermes", "chatml"}
        };

        public static Dictionary<string, string> chatTemplates = new Dictionary<string, string>()
        {
            {"{% for message in messages %}{{'<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n'}}{% endfor %}{% if add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}", "chatml"},
            {"{{ bos_token }}{% for message in messages %}{% if (message['role'] == 'user') != (loop.index0 % 2 == 0) %}{{ raise_exception('Conversation roles must alternate user/assistant/user/assistant/...') }}{% endif %}{% if message['role'] == 'user' %}{{ '[INST] ' + message['content'] + ' [/INST]' }}{% elif message['role'] == 'assistant' %}{{ message['content'] + eos_token}}{% else %}{{ raise_exception('Only user and assistant roles are supported!') }}{% endif %}{% endfor %}", "mistral"},
            {"{% for message in messages %}\n{% if message['role'] == 'user' %}\n{{ '<|user|>\n' + message['content'] + eos_token }}\n{% elif message['role'] == 'system' %}\n{{ '<|system|>\n' + message['content'] + eos_token }}\n{% elif message['role'] == 'assistant' %}\n{{ '<|assistant|>\n'  + message['content'] + eos_token }}\n{% endif %}\n{% if loop.last and add_generation_prompt %}\n{{ '<|assistant|>' }}\n{% endif %}\n{% endfor %}", "zephyr"}
        };

        public static string FromName(string name)
        {
            string nameLower = name.ToLower();
            foreach (var pair in aliasTemplates)
            {
                if (nameLower.Contains(pair.Key)) return pair.Value;
            }
            foreach (var pair in templates)
            {
                if (nameLower.Contains(pair.Key)) return pair.Key;
            }
            return null;
        }

        public static string FromTemplate(string template)
        {
            string templateTrim = template.Trim();
            if (chatTemplates.TryGetValue(templateTrim, out string value))
                return value;
            return null;
        }

        public static ChatTemplate FromGGUF(string path, out string name)
        {
            GGUFReader reader = new GGUFReader(path, "r");
            ReaderField field;

            if (reader.fields.TryGetValue("tokenizer.chat_template", out field))
            {
                string template = System.Text.Encoding.UTF8.GetString((byte[])field.parts[field.parts.Count - 1]);
                name = FromTemplate(template);
                if (name != null) return templates[name];
            }
            if (reader.fields.TryGetValue("general.name", out field))
            {
                string modelName = System.Text.Encoding.UTF8.GetString((byte[])field.parts[field.parts.Count - 1]);
                name = FromName(modelName);
                if (name != null) return templates[name];
            }

            string filename = Path.GetFileNameWithoutExtension(path);
            name = FromName(filename);
            if (name != null) return templates[name];

            Debug.Log("No chat template could be matched, fallback to ChatML");
            name = "chatml";
            return templates[name];
        }
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
