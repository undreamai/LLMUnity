/// @file
/// @brief File implementing a resumable Web client
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LLMUnity
{
    public class ResumingWebClient

    {
        private const int timeoutMs = 30 * 1000;
        private SynchronizationContext _context;
        private const int DefaultDownloadBufferLength = 65536;
        private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(timeoutMs) };
        private List<HttpRequestMessage> requests = new List<HttpRequestMessage>();

        public ResumingWebClient()
        {
            _context = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public long GetURLFileSize(string address)
        {
            return GetURLFileSize(new Uri(address)).GetAwaiter().GetResult();
        }

        public async Task<long> GetURLFileSize(Uri address)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Head, address))
                {
                    var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    response.EnsureSuccessStatusCode();
                    return response.Content.Headers.ContentLength ?? 0;
                }
            }
            catch (Exception e)
            {
                LLMUnitySetup.LogError($"Error getting file size: {e.Message}");
                return -1; // Return -1 if an error occurs
            }
        }

        public Task DownloadFileTaskAsyncResume(Uri address, string fileName, bool resume = false, Callback<float> progressCallback = null)
        {
            var tcs = new TaskCompletionSource<object>(address);
            FileStream fs = null;
            long bytesToSkip = 0;

            try
            {
                FileMode filemode = FileMode.Create;
                if (resume)
                {
                    var fileInfo = new FileInfo(fileName);
                    if (fileInfo.Exists) bytesToSkip = fileInfo.Length;
                }

                var request = new HttpRequestMessage(HttpMethod.Get, address);
                requests.Add(request);

                if (bytesToSkip > 0)
                {
                    long remoteFileSize = GetURLFileSize(address).GetAwaiter().GetResult();
                    if (bytesToSkip >= remoteFileSize)
                    {
                        LLMUnitySetup.Log($"File is already fully downloaded: {fileName}");
                        tcs.TrySetResult(true);
                        return tcs.Task;
                    }

                    filemode = FileMode.Append;
                    LLMUnitySetup.Log($"File exists at {fileName}, skipping {bytesToSkip} bytes");
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(bytesToSkip, null);
                }

                fs = new FileStream(fileName, filemode, FileAccess.Write);
                DownloadBitsAsync(request, fs, bytesToSkip, progressCallback, tcs);
            }
            catch (Exception e)
            {
                fs?.Close();
                tcs.TrySetException(e);
            }

            return tcs.Task;
        }

        public void CancelDownloadAsync()
        {
            LLMUnitySetup.Log("Cancellation requested, aborting download.");
            foreach (var request in requests) AbortRequest(request);
            requests.Clear();
        }

        public void AbortRequest(HttpRequestMessage request)
        {
            try
            {
                request?.Dispose();
            }
            catch (Exception e)
            {
                LLMUnitySetup.LogError($"Error aborting request: {e.Message}");
            }
        }

        private async void DownloadBitsAsync(HttpRequestMessage request, Stream writeStream, long bytesToSkip = 0, Callback<float> progressCallback = null, TaskCompletionSource<object> tcs = null)
        {
            try
            {
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    long contentLength = response.Content.Headers.ContentLength ?? -1;
                    byte[] copyBuffer = new byte[contentLength == -1 || contentLength > DefaultDownloadBufferLength ? DefaultDownloadBufferLength : contentLength];

                    long TotalBytesToReceive = Math.Max(contentLength, 0) + bytesToSkip;
                    long BytesReceived = bytesToSkip;

                    using (writeStream)
                    using (Stream readStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    {
                        while (true)
                        {
                            int bytesRead = await readStream.ReadAsync(copyBuffer, 0, copyBuffer.Length).ConfigureAwait(false);
                            if (bytesRead == 0)
                            {
                                break;
                            }

                            BytesReceived += bytesRead;
                            if (BytesReceived != TotalBytesToReceive)
                            {
                                PostProgressChanged(progressCallback, BytesReceived, TotalBytesToReceive);
                            }

                            await writeStream.WriteAsync(copyBuffer, 0, bytesRead).ConfigureAwait(false);
                        }

                        if (TotalBytesToReceive < 0)
                        {
                            TotalBytesToReceive = BytesReceived;
                        }
                        PostProgressChanged(progressCallback, BytesReceived, TotalBytesToReceive);
                    }
                }
                tcs.TrySetResult(true);
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
                LLMUnitySetup.LogError(e.Message);
                AbortRequest(request);
            }
            finally
            {
                writeStream?.Close();
                requests.Remove(request);
            }
        }

        private void PostProgressChanged(Callback<float> progressCallback, long BytesReceived, long TotalBytesToReceive)
        {
            if (progressCallback != null && BytesReceived > 0)
            {
                float progressPercentage = TotalBytesToReceive < 0 ? 0 : TotalBytesToReceive == 0 ? 1 : (float)BytesReceived / TotalBytesToReceive;
                _context.Post(_ => progressCallback?.Invoke(progressPercentage), null);
            }
        }
    }
}
