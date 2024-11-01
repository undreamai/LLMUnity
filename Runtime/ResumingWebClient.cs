/// @file
/// @brief File implementing a resumable Web client
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LLMUnity
{
    /// @ingroup utils
    /// <summary>
    /// Class implementing a resumable Web client
    /// </summary>
    public class ResumingWebClient : WebClient
    {
        private const int timeoutMs = 30 * 1000;
        private SynchronizationContext _context;
        private const int DefaultDownloadBufferLength = 65536;
        List<WebRequest> requests = new List<WebRequest>();

        public ResumingWebClient()
        {
            _context = SynchronizationContext.Current ?? new SynchronizationContext();
        }

        public long GetURLFileSize(string address)
        {
            return GetURLFileSize(new Uri(address));
        }

        public long GetURLFileSize(Uri address)
        {
            WebRequest request = GetWebRequest(address);
            request.Method = "HEAD";
            WebResponse response = request.GetResponse();
            return response.ContentLength;
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

                WebRequest request = GetWebRequest(address);
                if (request is HttpWebRequest webRequest && bytesToSkip > 0)
                {
                    long remoteFileSize = GetURLFileSize(address);
                    if (bytesToSkip >= remoteFileSize)
                    {
                        LLMUnitySetup.Log($"File is already fully downloaded: {fileName}");
                        tcs.TrySetResult(true);
                        return tcs.Task;
                    }

                    filemode = FileMode.Append;
                    LLMUnitySetup.Log($"File exists at {fileName}, skipping {bytesToSkip} bytes");
                    webRequest.AddRange(bytesToSkip);
                    webRequest.ReadWriteTimeout = timeoutMs;
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
            foreach (WebRequest request in requests) AbortRequest(request);
            requests.Clear();
        }

        public void AbortRequest(WebRequest request)
        {
            try
            {
                request?.Abort();
            }
            catch (Exception e)
            {
                LLMUnitySetup.LogError($"Error aborting request: {e.Message}");
            }
        }

        private async void DownloadBitsAsync(WebRequest request, Stream writeStream, long bytesToSkip = 0, Callback<float> progressCallback = null, TaskCompletionSource<object> tcs = null)
        {
            try
            {
                requests.Add(request);
                WebResponse response = await request.GetResponseAsync().ConfigureAwait(false);

                long contentLength = response.ContentLength;
                byte[] copyBuffer = new byte[contentLength == -1 || contentLength > DefaultDownloadBufferLength ? DefaultDownloadBufferLength : contentLength];

                long TotalBytesToReceive = Math.Max(contentLength, 0) + bytesToSkip;
                long BytesReceived = bytesToSkip;

                using (writeStream)
                using (Stream readStream = response.GetResponseStream())
                {
                    if (readStream != null)
                    {
                        while (true)
                        {
                            int bytesRead = await readStream.ReadAsync(new Memory<byte>(copyBuffer)).ConfigureAwait(false);
                            if (bytesRead == 0)
                            {
                                break;
                            }

                            BytesReceived += bytesRead;
                            if (BytesReceived != TotalBytesToReceive)
                            {
                                PostProgressChanged(progressCallback, BytesReceived, TotalBytesToReceive);
                            }

                            await writeStream.WriteAsync(new ReadOnlyMemory<byte>(copyBuffer, 0, bytesRead)).ConfigureAwait(false);
                        }
                    }

                    if (TotalBytesToReceive < 0)
                    {
                        TotalBytesToReceive = BytesReceived;
                    }
                    PostProgressChanged(progressCallback, BytesReceived, TotalBytesToReceive);
                }
                tcs.TrySetResult(true);
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
                LLMUnitySetup.LogError(e.Message);
                AbortRequest(request);
                tcs.TrySetResult(false);
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
