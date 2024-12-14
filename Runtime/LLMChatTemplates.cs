/// @file
/// @brief File implementing the chat templates.
using System.Collections.Generic;
using System.IO;

namespace LLMUnity
{
    /// @ingroup template
    /// <summary>
    /// Class implementing the skeleton of a chat template
    /// </summary>
    public abstract class ChatTemplate
    {
        /// <summary> the default template used when it can't be determined ("chatml") </summary>
        public static string DefaultTemplate;
        /// <summary> a dictionary from chat template name to chat template type.
        /// It can be used to get the chat template names supported with:
        /// \code
        /// ChatTemplate.templates.Keys
        /// \endcode
        /// </summary>
        public static Dictionary<string, ChatTemplate> templates;
        /// \cond HIDE
        public static ChatTemplate[] templateClasses;
        public static Dictionary<string, string> templatesDescription;
        public static Dictionary<string, string> modelTemplates;
        public static Dictionary<string, string> chatTemplates;
        /// \endcond

        static ChatTemplate()
        {
            DefaultTemplate = "chatml";

            templateClasses = new ChatTemplate[]
            {
                new ChatMLTemplate(),
                new AlpacaTemplate(),
                new GemmaTemplate(),
                new MistralChatTemplate(),
                new MistralInstructTemplate(),
                new LLama3ChatTemplate(),
                new LLama2ChatTemplate(),
                new LLama2Template(),
                new Phi3_5Template(),
                new Phi3Template(),
                new Phi2Template(),
                new VicunaTemplate(),
                new ZephyrTemplate(),
            };

            templates = new Dictionary<string, ChatTemplate>();
            templatesDescription = new Dictionary<string, string>();
            modelTemplates = new Dictionary<string, string>();
            chatTemplates = new Dictionary<string, string>();
            foreach (ChatTemplate template in templateClasses)
            {
                if (templates.ContainsKey(template.GetName())) LLMUnitySetup.LogError($"{template.GetName()} already in templates");
                templates[template.GetName()] = template;
                if (templatesDescription.ContainsKey(template.GetDescription())) LLMUnitySetup.LogError($"{template.GetDescription()} already in templatesDescription");
                templatesDescription[template.GetDescription()] = template.GetName();
                foreach (string match in template.GetNameMatches())
                {
                    if (modelTemplates.ContainsKey(match)) LLMUnitySetup.LogError($"{match} already in modelTemplates");
                    modelTemplates[match] = template.GetName();
                }
                foreach (string match in template.GetChatTemplateMatches())
                {
                    if (chatTemplates.ContainsKey(match)) LLMUnitySetup.LogError($"{match} already in chatTemplates");
                    chatTemplates[match] = template.GetName();
                }
            }
        }

        /// <summary>
        /// Determines the chat template name from a search name.
        /// It searches if any of the chat template names is a substring of the provided name.
        /// </summary>
        /// <param name="name">search name</param>
        /// <returns>chat template name</returns>
        public static string FromName(string name)
        {
            if (name == null) return null;
            string nameLower = name.ToLower();
            foreach (var pair in modelTemplates)
            {
                if (nameLower.Contains(pair.Key)) return pair.Value;
            }
            return null;
        }

        /// <summary>
        /// Determines the chat template name from a Jinja template.
        /// </summary>
        /// <param name="template">Jinja template</param>
        /// <returns>chat template name</returns>
        public static string FromTemplate(string template)
        {
            if (template == null) return null;
            string templateTrim = template.Trim();
            if (chatTemplates.TryGetValue(templateTrim, out string value))
                return value;
            return null;
        }

        /// <summary>
        /// Determines the chat template name from a GGUF file.
        /// It reads the GGUF file and then determines the chat template name based on:
        /// - the jinja template defined in the file (if it exists and matched)
        /// - the model name defined in the file (if it exists and matched)
        /// - the filename defined in the file (if matched)
        /// - otherwises uses the DefaultTemplate
        /// </summary>
        /// <param name="path">GGUF file path</param>
        /// <returns>template name</returns>
        public static string FromGGUF(string path)
        {
            return FromGGUF(new GGUFReader(path), path);
        }

        public static string FromGGUF(GGUFReader reader, string path)
        {
            string name;
            name = FromTemplate(reader.GetStringField("tokenizer.chat_template"));
            if (name != null) return name;

            name = FromName(reader.GetStringField("general.name"));
            if (name != null) return name;

            name = FromName(Path.GetFileNameWithoutExtension(path));
            if (name != null) return name;

            LLMUnitySetup.Log("No chat template could be matched, fallback to ChatML");
            return DefaultTemplate;
        }

        /// <summary>
        /// Creates the chat template based on the provided chat template name
        /// </summary>
        /// <param name="template">chat template name</param>
        /// <returns>chat template</returns>
        public static ChatTemplate GetTemplate(string template)
        {
            return templates[template];
        }

        /// <summary> Returns the chat template name </summary>
        public virtual string GetName() { return ""; }
        /// <summary> Returns the chat template description </summary>
        public virtual string GetDescription() { return ""; }
        /// <summary> Returns an array of names that can be used to match the chat template </summary>
        public virtual string[] GetNameMatches() { return new string[] {}; }
        /// <summary> Returns an array of jinja templates that can be used to match the chat template </summary>
        public virtual string[] GetChatTemplateMatches() { return new string[] {}; }
        /// <summary> Returns an array of the stopwords used by the template </summary>
        public virtual string[] GetStop(string playerName, string AIName) { return new string[] {}; }

        protected virtual string PromptPrefix() { return ""; }
        protected virtual string SystemPrefix() { return ""; }
        protected virtual string SystemSuffix() { return ""; }
        protected virtual string PlayerPrefix(string playerName) { return ""; }
        protected virtual string AIPrefix(string AIName) { return ""; }
        protected virtual string PrefixMessageSeparator() { return ""; }
        protected virtual string RequestPrefix() { return ""; }
        protected virtual string RequestSuffix() { return ""; }
        protected virtual string PairSuffix() { return ""; }

        /// <summary> Constructs the prompt using the template based on a list of ChatMessages </summary>
        /// <param name="messages"> list of ChatMessages e.g. the LLMCharacter chat </param>
        /// <param name="AIName"> the AI name </param>
        /// <param name="endWithPrefix"> whether to end the prompt with the AI prefix </param>
        /// <returns>prompt</returns>
        public virtual string ComputePrompt(List<ChatMessage> messages, string playerName, string AIName, bool endWithPrefix = true)
        {
            string chatPrompt = PromptPrefix();
            int start = 0;
            if (messages[0].role == "system")
            {
                chatPrompt += RequestPrefix() + SystemPrefix() + messages[0].content + SystemSuffix();
                start = 1;
            }
            for (int i = start; i < messages.Count; i += 2)
            {
                if (i > start || start == 0) chatPrompt += RequestPrefix();
                chatPrompt += PlayerPrefix(messages[i].role) + PrefixMessageSeparator() + messages[i].content + RequestSuffix();
                if (i < messages.Count - 1)
                {
                    chatPrompt += AIPrefix(messages[i + 1].role) + PrefixMessageSeparator() + messages[i + 1].content + PairSuffix();
                }
            }
            if (endWithPrefix) chatPrompt += AIPrefix(AIName);
            return chatPrompt;
        }

        protected string[] AddStopNewlines(string[] stop)
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

    /// @ingroup template
    /// <summary>
    /// Class implementing the ChatML template
    /// </summary>
    public class ChatMLTemplate : ChatTemplate
    {
        public override string GetName() { return "chatml"; }
        public override string GetDescription() { return "chatml (most widely used)"; }
        public override string[] GetNameMatches() { return new string[] {"chatml", "hermes", "qwen"}; }
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

    /// @ingroup template
    /// <summary>
    /// Class implementing the LLama2 template
    /// </summary>
    public class LLama2Template : ChatTemplate
    {
        public override string GetName() { return "llama"; }
        public override string GetDescription() { return "llama 2"; }

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

    /// @ingroup template
    /// <summary>
    /// Class implementing a modified version of the LLama2 template for chat
    /// </summary>
    public class LLama2ChatTemplate : LLama2Template
    {
        public override string GetName() { return "llama chat"; }
        public override string GetDescription() { return "llama 2 (chat)"; }
        public override string[] GetNameMatches() { return new string[] {"llama-2", "llama v2"}; }

        protected override string PlayerPrefix(string playerName) { return "### " + playerName + ":"; }
        protected override string AIPrefix(string AIName) { return "### " + AIName + ":"; }
        protected override string PrefixMessageSeparator() { return " "; }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { "[INST]", "[/INST]", "###" });
        }
    }

    /// @ingroup template
    /// <summary>
    /// Class implementing the LLama3 template for chat
    /// </summary>
    public class LLama3ChatTemplate : ChatTemplate
    {
        public override string GetName() { return "llama3 chat"; }
        public override string GetDescription() { return "llama 3 (chat)"; }
        public override string[] GetNameMatches() { return new string[] {"llama-3", "llama v3"}; }
        public override string[] GetChatTemplateMatches() { return new string[] {"{% set loop_messages = messages %}{% for message in loop_messages %}{% set content = '<|start_header_id|>' + message['role'] + '<|end_header_id|>\n\n'+ message['content'] | trim + '<|eot_id|>' %}{% if loop.index0 == 0 %}{% set content = bos_token + content %}{% endif %}{{ content }}{% endfor %}{{ '<|start_header_id|>assistant<|end_header_id|>\n\n' }}"};}

        protected override string SystemPrefix() { return "<|start_header_id|>system<|end_header_id|>\n\n"; }
        protected override string SystemSuffix() { return "<|eot_id|>"; }

        protected override string RequestSuffix() { return "<|eot_id|>"; }
        protected override string PairSuffix() { return "<|eot_id|>"; }

        protected override string PlayerPrefix(string playerName) { return $"<|start_header_id|>{playerName}<|end_header_id|>\n\n"; }
        protected override string AIPrefix(string AIName) { return $"<|start_header_id|>{AIName}<|end_header_id|>\n\n"; }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { "<|eot_id|>" });
        }
    }

    /// @ingroup template
    /// <summary>
    /// Class implementing the Mistral Instruct template
    /// </summary>
    public class MistralInstructTemplate : ChatTemplate
    {
        public override string GetName() { return "mistral instruct"; }
        public override string GetDescription() { return "mistral instruct"; }

        protected override string SystemPrefix() { return ""; }
        protected override string SystemSuffix() { return "\n\n"; }
        protected override string RequestPrefix() { return "[INST] "; }
        protected override string RequestSuffix() { return " [/INST]"; }
        protected override string PairSuffix() { return "</s>"; }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { "</s>", "[INST]", "[/INST]" });
        }
    }

    /// @ingroup template
    /// <summary>
    /// Class implementing a modified version of the Mistral Instruct template for chat
    /// </summary>
    public class MistralChatTemplate : MistralInstructTemplate
    {
        public override string GetName() { return "mistral chat"; }
        public override string GetDescription() { return "mistral (chat)"; }
        public override string[] GetNameMatches() { return new string[] {"mistral"}; }
        public override string[] GetChatTemplateMatches() { return new string[] {"{{ bos_token }}{% for message in messages %}{% if (message['role'] == 'user') != (loop.index0 % 2 == 0) %}{{ raise_exception('Conversation roles must alternate user/assistant/user/assistant/...') }}{% endif %}{% if message['role'] == 'user' %}{{ '[INST] ' + message['content'] + ' [/INST]' }}{% elif message['role'] == 'assistant' %}{{ message['content'] + eos_token}}{% else %}{{ raise_exception('Only user and assistant roles are supported!') }}{% endif %}{% endfor %}"}; }

        protected override string PlayerPrefix(string playerName) { return "### " + playerName + ":"; }
        protected override string AIPrefix(string AIName) { return "### " + AIName + ":"; }
        protected override string PrefixMessageSeparator() { return " "; }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { "</s>", "[INST]", "[/INST]", "###" });
        }
    }

    /// @ingroup template
    /// <summary>
    /// Class implementing the Gemma template
    /// </summary>
    public class GemmaTemplate : ChatTemplate
    {
        public override string GetName() { return "gemma"; }
        public override string GetDescription() { return "gemma"; }
        public override string[] GetNameMatches() { return new string[] {"gemma"}; }

        protected override string RequestSuffix() { return "<end_of_turn>\n"; }
        protected override string PairSuffix() { return "<end_of_turn>\n"; }

        protected override string PlayerPrefix(string playerName) { return "<start_of_turn>" + playerName + "\n"; }
        protected override string AIPrefix(string AIName) { return "<start_of_turn>" + AIName + "\n"; }

        public override string ComputePrompt(List<ChatMessage> messages, string playerName, string AIName, bool endWithPrefix = true)
        {
            List<ChatMessage> messagesSystemPrompt = messages;
            if (messages[0].role == "system")
            {
                string firstUserMessage = messages[0].content;
                int start = 1;
                if (messages.Count > 1)
                {
                    if (firstUserMessage != "") firstUserMessage += "\n\n";
                    firstUserMessage += messages[1].content;
                    start = 2;
                }
                messagesSystemPrompt = new List<ChatMessage>(){new ChatMessage { role = playerName, content = firstUserMessage }};
                messagesSystemPrompt.AddRange(messages.GetRange(start, messages.Count - start));
            }
            return base.ComputePrompt(messagesSystemPrompt, playerName, AIName, endWithPrefix);
        }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { "<start_of_turn>", "<end_of_turn>" });
        }
    }

    /// @ingroup template
    /// <summary>
    /// Class implementing the Alpaca template
    /// </summary>
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

    /// @ingroup template
    /// <summary>
    /// Class implementing the Vicuna template
    /// </summary>
    public class VicunaTemplate : ChatTemplate
    {
        public override string GetName() { return "vicuna"; }
        public override string GetDescription() { return "vicuna"; }
        public override string[] GetNameMatches() { return new string[] {"vicuna"}; }
        public override string[] GetChatTemplateMatches() { return new string[] {"{% if not add_generation_prompt is defined %}{% set add_generation_prompt = false %}{% endif %}{% for message in messages %}{% if message['role'] == 'system' %}{{message['content'] + ' '}}{% elif message['role'] == 'user' %}{{ 'USER: ' + message['content'] + ' '}}{% elif message['role'] == 'assistant' %}{{ 'ASSISTANT: ' + message['content'] + ' '}}{% endif %}{% endfor %}{% if add_generation_prompt %}{{ 'ASSISTANT: '}}{% endif %}"}; }

        protected override string SystemSuffix() { return "\n"; }
        protected override string PlayerPrefix(string playerName) { return "\n" + playerName + ":"; }
        protected override string AIPrefix(string AIName) { return "\n" + AIName + ":"; }
        protected override string PrefixMessageSeparator() { return " "; }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { playerName + ":", AIName + ":" });
        }
    }

    /// @ingroup template
    /// <summary>
    /// Class implementing the Phi-2 template
    /// </summary>
    public class Phi2Template : ChatTemplate
    {
        public override string GetName() { return "phi"; }
        public override string GetDescription() { return "phi-2"; }
        public override string[] GetNameMatches() { return new string[] {"phi-2"}; }

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

    /// @ingroup template
    /// <summary>
    /// Class implementing the Phi-3 template
    /// </summary>
    public class Phi3Template : ChatTemplate
    {
        public override string GetName() { return "phi-3"; }
        public override string GetDescription() { return "phi-3"; }
        public override string[] GetNameMatches() { return new string[] {"phi-3"}; }
        public override string[] GetChatTemplateMatches() { return new string[] {"{{ bos_token }}{% for message in messages %}{% if (message['role'] == 'user') %}{{'<|user|>' + '\n' + message['content'] + '<|end|>' + '\n' + '<|assistant|>' + '\n'}}{% elif (message['role'] == 'assistant') %}{{message['content'] + '<|end|>' + '\n'}}{% endif %}{% endfor %}"}; }

        protected override string PlayerPrefix(string playerName) { return $"<|user|>\n"; }
        protected override string AIPrefix(string AIName) { return $"<|assistant|>\n"; }
        protected override string RequestSuffix() { return "<|end|>\n"; }
        protected override string PairSuffix() { return "<|end|>\n"; }


        public override string ComputePrompt(List<ChatMessage> messages, string playerName, string AIName, bool endWithPrefix = true)
        {
            List<ChatMessage> messagesSystemPrompt = messages;
            if (messages[0].role == "system")
            {
                string firstUserMessage = messages[0].content;
                int start = 1;
                if (messages.Count > 1)
                {
                    if (firstUserMessage != "") firstUserMessage += "\n\n";
                    firstUserMessage += messages[1].content;
                    start = 2;
                }
                messagesSystemPrompt = new List<ChatMessage>(){new ChatMessage { role = "user", content = firstUserMessage }};
                messagesSystemPrompt.AddRange(messages.GetRange(start, messages.Count - start));
            }
            return base.ComputePrompt(messagesSystemPrompt, playerName, AIName, endWithPrefix);
        }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { "<|end|>", "<|user|>", "<|assistant|>" });
        }
    }

    /// @ingroup template
    /// <summary>
    /// Class implementing the Phi-3.5 template
    /// </summary>
    public class Phi3_5Template : ChatTemplate
    {
        public override string GetName() { return "phi-3.5"; }
        public override string GetDescription() { return "phi-3.5"; }
        public override string[] GetNameMatches() { return new string[] {"phi-3.5"}; }
        public override string[] GetChatTemplateMatches() { return new string[] {"{% for message in messages %}{% if message['role'] == 'system' and message['content'] %}{{'<|system|>\n' + message['content'] + '<|end|>\n'}}{% elif message['role'] == 'user' %}{{'<|user|>\n' + message['content'] + '<|end|>\n'}}{% elif message['role'] == 'assistant' %}{{'<|assistant|>\n' + message['content'] + '<|end|>\n'}}{% endif %}{% endfor %}{% if add_generation_prompt %}{{ '<|assistant|>\n' }}{% else %}{{ eos_token }}{% endif %}"};}

        protected override string PlayerPrefix(string playerName) { return $"<|user|>\n"; }
        protected override string AIPrefix(string AIName) { return $"<|assistant|>\n"; }
        protected override string RequestSuffix() { return "<|end|>\n"; }
        protected override string PairSuffix() { return "<|end|>\n"; }
        protected override string SystemPrefix() { return "<|system|>\n"; }
        protected override string SystemSuffix() { return "<|end|>\n"; }

        public override string[] GetStop(string playerName, string AIName)
        {
            return AddStopNewlines(new string[] { "<|end|>", "<|user|>", "<|assistant|>" });
        }
    }

    /// @ingroup template
    /// <summary>
    /// Class implementing the Zephyr template
    /// </summary>
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
