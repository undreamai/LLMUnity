using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LLMUnity
{
    public abstract class ChatTemplate
    {
        public static string DefaultTemplate;
        public static Type[] templateClasses;
        public static Dictionary<string, Type> templates;
        public static Dictionary<string, string> templatesDescription;
        public static Dictionary<string, string> modelTemplates;
        public static Dictionary<string, string> chatTemplates;

        static ChatTemplate()
        {
            DefaultTemplate = "chatml";

            templateClasses = new Type[]
            {
                typeof(ChatMLTemplate),
                typeof(AlpacaTemplate),
                typeof(MistralChatTemplate),
                typeof(MistralInstructTemplate),
                typeof(LLama2ChatTemplate),
                typeof(LLama2Template),
                typeof(Phi2Template),
                typeof(ZephyrTemplate),
            };

            templates = new Dictionary<string, Type>();
            templatesDescription = new Dictionary<string, string>();
            modelTemplates = new Dictionary<string, string>();
            chatTemplates = new Dictionary<string, string>();
            foreach (Type templateClass in templateClasses)
            {
                ChatTemplate template = (ChatTemplate)Activator.CreateInstance(templateClass);
                if (templates.ContainsKey(template.GetName())) Debug.LogError($"{template.GetName()} already in templates");
                templates[template.GetName()] = templateClass;
                if (templatesDescription.ContainsKey(template.GetDescription())) Debug.LogError($"{template.GetDescription()} already in templatesDescription");
                templatesDescription[template.GetDescription()] = template.GetName();
                foreach (string match in template.GetNameMatches())
                {
                    if (modelTemplates.ContainsKey(match)) Debug.LogError($"{match} already in modelTemplates");
                    modelTemplates[match] = template.GetName();
                }
                foreach (string match in template.GetChatTemplateMatches())
                {
                    if (chatTemplates.ContainsKey(match)) Debug.LogError($"{match} already in chatTemplates");
                    chatTemplates[match] = template.GetName();
                }
            }
        }

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
            return DefaultTemplate;
        }

        public static ChatTemplate GetTemplate(string template)
        {
            return (ChatTemplate)Activator.CreateInstance(templates[template]);
        }

        public abstract string GetName();
        public abstract string GetDescription();
        public virtual string[] GetNameMatches() { return new string[] {}; }
        public virtual string[] GetChatTemplateMatches() { return new string[] {}; }
        public abstract string[] GetStop(string playerName, string AIName);

        protected virtual string PromptPrefix() { return ""; }
        protected virtual string SystemPrefix() { return ""; }
        protected virtual string SystemSuffix() { return ""; }
        protected virtual string PlayerPrefix(string playerName) { return ""; }
        protected virtual string AIPrefix(string AIName) { return ""; }
        protected virtual string PrefixMessageSeparator() { return ""; }
        protected virtual string RequestPrefix() { return ""; }
        protected virtual string RequestSuffix() { return ""; }
        protected virtual string PairSuffix() { return ""; }

        public virtual string ComputePrompt(List<ChatMessage> messages, string AIName)
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
                chatPrompt += PlayerPrefix(messages[i].role) + PrefixMessageSeparator() + messages[i].content + RequestSuffix();
                if (i < messages.Count - 1)
                {
                    chatPrompt += AIPrefix(messages[i + 1].role) + PrefixMessageSeparator() + messages[i + 1].content + PairSuffix();
                }
            }
            chatPrompt += AIPrefix(AIName);
            return chatPrompt;
        }

        public string[] AddStopNewlines(string[] stop)
        {
            List<string> stopWithNewLines = new List<string>();
            foreach (string stopword in stop)
            {
                stopWithNewLines.Add(stopword);
                stopWithNewLines.Add("\n" + stopword);
            }
            return stopWithNewLines.ToArray();
        }
    }

    public class ChatMLTemplate : ChatTemplate
    {
        public override string GetName() { return "chatml"; }
        public override string GetDescription() { return "chatml (best overall)"; }
        public override string[] GetNameMatches() { return new string[] {"chatml", "hermes"}; }
        public override string[] GetChatTemplateMatches() { return new string[] {"{% for message in messages %}{{'<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n'}}{% endfor %}{% if add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}"}; }

        protected override string SystemPrefix() { return "<|im_start|>system\n"; }
        protected override string SystemSuffix() { return "<|im_end|>\n"; }
        protected override string PlayerPrefix(string playerName) { return $"<|im_start|>{playerName}\n"; }
        protected override string AIPrefix(string AIName) { return $"<|im_start|>{AIName}\n"; }
        protected override string RequestSuffix() { return "<|im_end|>\n"; }
        protected override string PairSuffix() { return "<|im_end|>\n"; }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { "<|im_start|>", "<|im_end|>" });
        }
    }

    public class LLama2Template : ChatTemplate
    {
        public override string GetName() { return "llama"; }
        public override string GetDescription() { return "llama"; }

        protected override string SystemPrefix() { return "<<SYS>>\n"; }
        protected override string SystemSuffix() { return "\n<</SYS>> "; }
        protected override string RequestPrefix() { return "<s>[INST] "; }
        protected override string RequestSuffix() { return " [/INST]"; }
        protected override string PairSuffix() { return " </s>"; }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { "[INST]", "[/INST]" });
        }
    }

    public class LLama2ChatTemplate : LLama2Template
    {
        public override string GetName() { return "llama chat"; }
        public override string GetDescription() { return "llama (modified for chat)"; }
        public override string[] GetNameMatches() { return new string[] {"llama"}; }

        protected override string PlayerPrefix(string playerName) { return "### " + playerName + ":"; }
        protected override string AIPrefix(string AIName) { return "### " + AIName + ":"; }
        protected override string PrefixMessageSeparator() { return " "; }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { "[INST]", "[/INST]", "###" });
        }
    }

    public class MistralInstructTemplate : ChatTemplate
    {
        public override string GetName() { return "mistral instruct"; }
        public override string GetDescription() { return "mistral instruct"; }

        protected override string PromptPrefix() { return "<s>"; }
        protected override string SystemPrefix() { return ""; }
        protected override string SystemSuffix() { return "\n\n"; }
        protected override string RequestPrefix() { return "[INST] "; }
        protected override string RequestSuffix() { return " [/INST]"; }
        protected override string PairSuffix() { return "</s>"; }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { "[INST]", "[/INST]" });
        }
    }

    public class MistralChatTemplate : MistralInstructTemplate
    {
        public override string GetName() { return "mistral chat"; }
        public override string GetDescription() { return "mistral (modified for chat)"; }
        public override string[] GetNameMatches() { return new string[] {"mistral"}; }
        public override string[] GetChatTemplateMatches() { return new string[] {"{{ bos_token }}{% for message in messages %}{% if (message['role'] == 'user') != (loop.index0 % 2 == 0) %}{{ raise_exception('Conversation roles must alternate user/assistant/user/assistant/...') }}{% endif %}{% if message['role'] == 'user' %}{{ '[INST] ' + message['content'] + ' [/INST]' }}{% elif message['role'] == 'assistant' %}{{ message['content'] + eos_token}}{% else %}{{ raise_exception('Only user and assistant roles are supported!') }}{% endif %}{% endfor %}"}; }

        protected override string PlayerPrefix(string playerName) { return "### " + playerName + ":"; }
        protected override string AIPrefix(string AIName) { return "### " + AIName + ":"; }
        protected override string PrefixMessageSeparator() { return " "; }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { "[INST]", "[/INST]", "###" });
        }
    }

    public class AlpacaTemplate : ChatTemplate
    {
        public override string GetName() { return "alpaca"; }
        public override string GetDescription() { return "alpaca (best alternative)"; }
        public override string[] GetNameMatches() { return new string[] {"alpaca"}; }

        protected override string SystemSuffix() { return "\n\n"; }
        protected override string RequestSuffix() { return "\n"; }
        protected override string PlayerPrefix(string playerName) { return "### " + playerName + ":"; }
        protected override string AIPrefix(string AIName) { return "### " + AIName + ":"; }
        protected override string PrefixMessageSeparator() { return " "; }
        protected override string PairSuffix() { return "\n"; }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { "###" });
        }
    }

    public class Phi2Template : ChatTemplate
    {
        public override string GetName() { return "phi"; }
        public override string GetDescription() { return "phi"; }
        public override string[] GetNameMatches() { return new string[] {"phi"}; }

        protected override string SystemSuffix() { return "\n\n"; }
        protected override string RequestSuffix() { return "\n"; }
        protected override string PlayerPrefix(string playerName) { return playerName + ":"; }
        protected override string AIPrefix(string AIName) { return AIName + ":"; }
        protected override string PrefixMessageSeparator() { return " "; }
        protected override string PairSuffix() { return "\n"; }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { playerName + ":", AIName + ":" });
        }
    }

    public class ZephyrTemplate : ChatTemplate
    {
        public override string GetName() { return "zephyr"; }
        public override string GetDescription() { return "zephyr"; }
        public override string[] GetNameMatches() { return new string[] {"zephyr"}; }
        public override string[] GetChatTemplateMatches() { return new string[] {"{% for message in messages %}\n{% if message['role'] == 'user' %}\n{{ '<|user|>\n' + message['content'] + eos_token }}\n{% elif message['role'] == 'system' %}\n{{ '<|system|>\n' + message['content'] + eos_token }}\n{% elif message['role'] == 'assistant' %}\n{{ '<|assistant|>\n'  + message['content'] + eos_token }}\n{% endif %}\n{% if loop.last and add_generation_prompt %}\n{{ '<|assistant|>' }}\n{% endif %}\n{% endfor %}"}; }

        protected override string SystemPrefix() { return "<|system|>\n"; }
        protected override string SystemSuffix() { return "</s>\n"; }
        protected override string PlayerPrefix(string playerName) { return $"<|user|>\n"; }
        protected override string AIPrefix(string AIName) { return $"<|assistant|>\n"; }
        protected override string RequestSuffix() { return "</s>\n"; }
        protected override string PairSuffix() { return "</s>\n"; }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { $"<|user|>", $"<|assistant|>" });
        }
    }
}
