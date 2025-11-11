using System;

namespace UndreamAI.LlamaLib
{
    public class LLMClient : LLMLocal
    {
        public LLMClient(LLMProvider provider)
        {
            if (provider.disposed)
                throw new ObjectDisposedException(nameof(provider));

            llamaLib = provider.llamaLib;
            llm = CreateClient(provider);
        }

        public LLMClient(string url, int port, string apiKey = "")
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));

            try
            {
                llamaLib = new LlamaLib(false);
                llm = CreateRemoteClient(url, port, apiKey);
            }
            catch
            {
                llamaLib?.Dispose();
                throw;
            }
        }

        private IntPtr CreateClient(LLMProvider provider)
        {
            var llm = llamaLib.LLMClient_Construct(provider.llm);
            if (llm == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create LLMClient");
            return llm;
        }

        private IntPtr CreateRemoteClient(string url, int port, string apiKey = "")
        {
            var llm = llamaLib.LLMClient_Construct_Remote(url ?? string.Empty, port, apiKey ?? string.Empty);
            if (llm == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to create remote LLMClient for {url}:{port}");
            return llm;
        }
    }
}
