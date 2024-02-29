using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;

namespace LLMUnity
{
    public abstract class ChatTemplate
    {
        public string playerName;
        public string AIName;

        public ChatTemplate(string playerName = "user", string AIName = "assistant")
        {
            this.playerName = playerName;
            this.AIName = AIName;
        }

        public static Dictionary<string, Type> templates = new Dictionary<string, Type>()
        {
            {"chatml (best overall)", typeof(ChatMLTemplate)},
            {"mistral (modified for chat)", typeof(MistralChatTemplate)},
            {"mistral instruct", typeof(MistralInstructTemplate)},
            {"llama (modified for chat)", typeof(LLama2ChatTemplate)},
            {"llama", typeof(LLama2Template)},
            {"alpaca", typeof(AlpacaTemplate)},
            {"zephyr", typeof(ZephyrTemplate)},
        };

        public static Dictionary<string, string> modelTemplates = new Dictionary<string, string>()
        {
            {"chatml", "chatml (best overall)"},
            {"hermes", "chatml (best overall)"},
            {"phi", "chatml (best overall)"},
            {"mistral", "mistral (modified for chat)"},
            {"llama", "llama (modified for chat)"},
            {"alpaca", "alpaca"},
            {"zephyr", "zephyr"},
        };

        public static Dictionary<string, string> chatTemplates = new Dictionary<string, string>()
        {
            {"{% for message in messages %}{{'<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n'}}{% endfor %}{% if add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}", "chatml (best overall)"},
            {"{{ bos_token }}{% for message in messages %}{% if (message['role'] == 'user') != (loop.index0 % 2 == 0) %}{{ raise_exception('Conversation roles must alternate user/assistant/user/assistant/...') }}{% endif %}{% if message['role'] == 'user' %}{{ '[INST] ' + message['content'] + ' [/INST]' }}{% elif message['role'] == 'assistant' %}{{ message['content'] + eos_token}}{% else %}{{ raise_exception('Only user and assistant roles are supported!') }}{% endif %}{% endfor %}", "mistral (modified for chat)"},
            {"{% for message in messages %}\n{% if message['role'] == 'user' %}\n{{ '<|user|>\n' + message['content'] + eos_token }}\n{% elif message['role'] == 'system' %}\n{{ '<|system|>\n' + message['content'] + eos_token }}\n{% elif message['role'] == 'assistant' %}\n{{ '<|assistant|>\n'  + message['content'] + eos_token }}\n{% endif %}\n{% if loop.last and add_generation_prompt %}\n{{ '<|assistant|>' }}\n{% endif %}\n{% endfor %}", "zephyr"}
        };

        public static string FromName(string name)
        {
            string nameLower = name.ToLower();
            foreach (var pair in modelTemplates)
            {
                if (nameLower.Contains(pair.Key)) return pair.Value;
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

        public static string FromGGUF(string path)
        {
            GGUFReader reader = new GGUFReader(path, "r");
            ReaderField field;
            string name;

            if (reader.fields.TryGetValue("tokenizer.chat_template", out field))
            {
                string template = System.Text.Encoding.UTF8.GetString((byte[])field.parts[field.parts.Count - 1]);
                name = FromTemplate(template);
                if (name != null) return name;
            }
            if (reader.fields.TryGetValue("general.name", out field))
            {
                string modelName = System.Text.Encoding.UTF8.GetString((byte[])field.parts[field.parts.Count - 1]);
                name = FromName(modelName);
                if (name != null) return name;
            }

            string filename = Path.GetFileNameWithoutExtension(path);
            name = FromName(filename);
            if (name != null) return name;

            Debug.Log("No chat template could be matched, fallback to ChatML");
            return "chatml";
        }

        public static ChatTemplate GetTemplate(string template, string playerName, string AIName)
        {
            return (ChatTemplate)Activator.CreateInstance(templates[template], playerName, AIName);
        }

        public abstract List<string> GetStop();
        protected virtual string PromptPrefix() { return ""; }
        protected virtual string SystemPrefix() { return ""; }
        protected virtual string SystemSuffix() { return ""; }
        protected virtual string PlayerPrefix() { return ""; }
        protected virtual string AIPrefix() { return ""; }
        protected virtual string RequestPrefix() { return ""; }
        protected virtual string RequestSuffix() { return ""; }
        protected virtual string PairSuffix() { return ""; }

        public virtual string ComputePrompt(List<ChatMessage> messages)
        {
            string chatPrompt = PromptPrefix();
            string systemPrompt = "";
            int start = 0;
            if (messages[0].role == "system")
            {
                systemPrompt = SystemPrefix() + messages[0].content + SystemSuffix();
                start = 1;
            }
            for (int i = start; i < messages.Count; i += 2)
            {
                chatPrompt += RequestPrefix();
                if (i == 1 && systemPrompt != "") chatPrompt += systemPrompt;
                if (messages[i].role != playerName)
                {
                    Debug.Log($"Role was {messages[i].role}, was expecting {playerName}");
                }
                chatPrompt += PlayerPrefix() + messages[i].content + RequestSuffix();
                if (i < messages.Count - 1)
                {
                    if (messages[i + 1].role != AIName)
                    {
                        Debug.Log($"Role was {messages[i + 1].role}, was expecting {AIName}");
                    }
                    chatPrompt += AIPrefix() + messages[i + 1].content + PairSuffix();
                }
            }
            chatPrompt += AIPrefix();
            return chatPrompt;
        }

        public List<string> AddStopNewlines(List<string> stop)
        {
            List<string> stopWithNewLines = new List<string>();
            foreach (string stopword in stop)
            {
                stopWithNewLines.Add(stopword);
                stopWithNewLines.Add("\n" + stopword);
            }
            return stopWithNewLines;
        }
    }

    public class ChatMLTemplate : ChatTemplate
    {
        public ChatMLTemplate(string playerName = "user", string AIName = "assistant") : base(playerName, AIName) {}

        protected override string SystemPrefix() { return "<|im_start|>system\n"; }
        protected override string SystemSuffix() { return "<|im_end|>\n"; }
        protected override string PlayerPrefix() { return $"<|im_start|>{playerName}\n"; }
        protected override string AIPrefix() { return $"<|im_start|>{AIName}\n"; }
        protected override string RequestSuffix() { return "<|im_end|>\n"; }
        protected override string PairSuffix() { return "<|im_end|>\n"; }

        public override List<string> GetStop()
        {
            return AddStopNewlines(new List<string> { "<|im_start|>", "<|im_end|>" });
        }
    }

    public class LLama2Template : ChatTemplate
    {
        public LLama2Template(string playerName = "user", string AIName = "assistant") : base(playerName, AIName) {}

        protected override string SystemPrefix() { return "<<SYS>>\n"; }
        protected override string SystemSuffix() { return "\n<</SYS>> "; }
        protected override string RequestPrefix() { return "<s>[INST] "; }
        protected override string RequestSuffix() { return " [/INST]"; }
        protected override string PairSuffix() { return " </s>"; }

        public override List<string> GetStop()
        {
            return AddStopNewlines(new List<string> { "[INST]", "[/INST]" });
        }
    }

    public class LLama2ChatTemplate : LLama2Template
    {
        public LLama2ChatTemplate(string playerName = "user", string AIName = "assistant") : base(playerName, AIName) {}

        protected override string PlayerPrefix() { return "### " + playerName + ": "; }
        protected override string AIPrefix() { return "### " + AIName + ": "; }

        public override List<string> GetStop()
        {
            return AddStopNewlines(new List<string> { "[INST]", "[/INST]", "###" });
        }
    }

    public class MistralInstructTemplate : ChatTemplate
    {
        public MistralInstructTemplate(string playerName = "user", string AIName = "assistant") : base(playerName, AIName) {}

        protected override string PromptPrefix() { return "<s>"; }
        protected override string SystemPrefix() { return ""; }
        protected override string SystemSuffix() { return "\n\n"; }
        protected override string RequestPrefix() { return "[INST] "; }
        protected override string RequestSuffix() { return " [/INST]"; }
        protected override string PairSuffix() { return "</s>"; }

        public override List<string> GetStop()
        {
            return AddStopNewlines(new List<string> { "[INST]", "[/INST]" });
        }
    }

    public class MistralChatTemplate : MistralInstructTemplate
    {
        public MistralChatTemplate(string playerName = "user", string AIName = "assistant") : base(playerName, AIName) {}

        protected override string PlayerPrefix() { return "### " + playerName + ": "; }
        protected override string AIPrefix() { return "### " + AIName + ": "; }

        public override List<string> GetStop()
        {
            return AddStopNewlines(new List<string> { "[INST]", "[/INST]", "###" });
        }
    }

    public class AlpacaTemplate : ChatTemplate
    {
        public AlpacaTemplate(string playerName = "user", string AIName = "assistant") : base(playerName, AIName) {}

        protected override string SystemSuffix() { return "\n\n"; }
        protected override string RequestSuffix() { return "\n"; }
        protected override string PlayerPrefix() { return "### " + playerName + ": "; }
        protected override string AIPrefix() { return "### " + AIName + ": "; }
        protected override string PairSuffix() { return "\n"; }

        public override List<string> GetStop()
        {
            return AddStopNewlines(new List<string> { "###" });
        }
    }

    public class ZephyrTemplate : ChatTemplate
    {
        public ZephyrTemplate(string playerName = "user", string AIName = "assistant") : base(playerName, AIName) {}

        protected override string SystemPrefix() { return "<|system|>\n"; }
        protected override string SystemSuffix() { return "</s>\n"; }
        protected override string PlayerPrefix() { return $"<|user|>\n"; }
        protected override string AIPrefix() { return $"<|assistant|>\n"; }
        protected override string RequestSuffix() { return "</s>\n"; }
        protected override string PairSuffix() { return "</s>\n"; }

        public override List<string> GetStop()
        {
            return AddStopNewlines(new List<string> { $"<|user|>", $"<|assistant|>" });
        }
    }
}
