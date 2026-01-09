using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace imgany.Core
{
    public class UploadService
    {
        private ConfigManager _config;
        private static readonly HttpClient _sharedClient;

        static UploadService()
        {
            _sharedClient = new HttpClient();
            _sharedClient.Timeout = TimeSpan.FromSeconds(60);
            _sharedClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public UploadService(ConfigManager config)
        {
            _config = config;
        }

        public async Task<string> UploadImageAsync(Stream imageStream, string originalFileName)
        {
            return await UploadInternalAsync(imageStream, originalFileName);
        }

        public async Task<string> UploadImageAsync(string filePath)
        {
             using (var fs = File.OpenRead(filePath))
             {
                 return await UploadInternalAsync(fs, Path.GetFileName(filePath));
             }
        }

        private async Task<string> UploadInternalAsync(Stream stream, string fileName)
        {
            FileLog($"=== API Call: UploadImageAsync (Stream) ===");
            FileLog($"File: {fileName}");
            FileLog($"Config UploadToHost: {_config.UploadToHost}");
            FileLog($"Config HostUrl: {_config.UploadHostUrl}");

            if (!_config.UploadToHost || string.IsNullOrEmpty(_config.UploadHostUrl))
            {
                FileLog("Skipping upload: Upload disabled or Host URL empty.");
                return null;
            }

            if (_config.UploadHostType.StartsWith("Lsky Pro"))
            {
                return await UploadToLskyProAsync(stream, fileName);
            }
            
            FileLog($"Unsupported Host Type: {_config.UploadHostType}");
            return null;
        }

        private void FileLog(string message)
        {
            try
            {
                // Portable Mode: Log to Exe directory
                string path = Path.Combine(AppContext.BaseDirectory, "debug.log");
                File.AppendAllText(path, $"{DateTime.Now}: {message}{Environment.NewLine}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FileLog Failed: {ex.Message}");
            }
        }

        private async Task<string> UploadToLskyProAsync(Stream stream, string fileName)
        {
            try
            {
                FileLog("--- Starting Upload Process ---");
                string baseUrl = _config.UploadHostUrl.TrimEnd('/');
                if (!baseUrl.EndsWith("/api/v1")) baseUrl += "/api/v1";
                string uploadUrl = $"{baseUrl}/upload";
                FileLog($"Upload URL: {uploadUrl}");
                
                if (_config.UploadAsGuest)
                {
                    FileLog("Mode: Guest");
                    return await ExecuteUpload(uploadUrl, null, stream, fileName);
                }

                FileLog("Mode: Authenticated");
                string token = _config.UploadToken;
                
                if (string.IsNullOrEmpty(token))
                {
                    FileLog("No cached token, attempting login...");
                    token = await GetTokenAsync(baseUrl, _config.UploadEmail, _config.UploadPassword);
                    if (string.IsNullOrEmpty(token)) 
                    {
                        FileLog("Login failed, aborting upload.");
                        return null; 
                    }
                    _config.UploadToken = token; 
                }

                string result = await ExecuteUpload(uploadUrl, token, stream, fileName);
                
                if (result == "401")
                {
                    FileLog("Got 401 Unauthorized. Refreshing token...");
                    token = await GetTokenAsync(baseUrl, _config.UploadEmail, _config.UploadPassword);
                    if (!string.IsNullOrEmpty(token))
                    {
                        _config.UploadToken = token;
                        return await ExecuteUpload(uploadUrl, token, stream, fileName);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                FileLog($"Upload Exception: {ex.Message}\n{ex.StackTrace}");
                Debug.WriteLine($"Upload Exception: {ex.Message}");
            }
            return null;
        }

        private async Task<string> GetTokenAsync(string baseUrl, string email, string password)
        {
            try
            {
                string tokenUrl = $"{baseUrl}/tokens";
                FileLog($"Login URL: {tokenUrl}");
                // Not using shared client for Auth to keep headers clean? 
                // Actually SharedClient is fine, headers are set per request primarily or default.
                // We set Default headers in static constructor.
                // But Authorization header is specific.
                
                // For login, we don't need Auth header.
                // Content-Type is set by JsonContent.
                
                var data = new { email = email, password = password };
                var jsonContent = new StringContent(JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, "application/json");

                using (var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl))
                {
                    request.Content = jsonContent;
                    request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    
                    var response = await _sharedClient.SendAsync(request);
                    var jsonResponse = await response.Content.ReadAsStringAsync();

                    FileLog($"Login Response Code: {response.StatusCode}");
                    FileLog($"Login Response Body: {jsonResponse}");

                    if (response.IsSuccessStatusCode)
                    {
                        var node = JsonNode.Parse(jsonResponse);
                        if (node["status"]?.GetValue<bool>() == true)
                        {
                            string t = node["data"]?["token"]?.ToString();
                            FileLog(string.IsNullOrEmpty(t) ? "Token parsed is empty" : "Token acquired");
                            return t;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                 FileLog($"Login Exception: {ex.Message}");
            }
            return null;
        }

        private async Task<string> ExecuteUpload(string url, string token, Stream stream, string fileName)
        {
             using (var content = new MultipartFormDataContent())
             {
                 // Important: We must not close the stream if it's passed in from outside?
                 // StreamContent does not close the stream by default unless we dipose it.
                 // We are disposing content.
                 
                 // If stream is MemoryStream, we can rewind it if needed, but usually we just read.
                 if (stream.Position > 0 && stream.CanSeek) stream.Position = 0;

                 var fileContent = new StreamContent(stream);
                 string ext = Path.GetExtension(fileName).TrimStart('.');
                 if (string.IsNullOrEmpty(ext)) ext = "png"; // Fallback
                 
                 fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/" + ext);
                 content.Add(fileContent, "file", fileName);

                 using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                 {
                     if (!string.IsNullOrEmpty(token))
                     {
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                     }
                     request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                     request.Content = content;
                     
                     FileLog($"Sending POST to {url}");
                     var response = await _sharedClient.SendAsync(request);
                     var json = await response.Content.ReadAsStringAsync();
                     
                     FileLog($"Upload Response Code: {response.StatusCode}");
                     FileLog($"Upload Response Body: {json}");

                     if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                     {
                         return "401";
                     }

                     if (response.IsSuccessStatusCode)
                     {
                         var jsonObj = JsonNode.Parse(json);
                         if (jsonObj["status"]?.GetValue<bool>() == true)
                         {
                             string remoteUrl = jsonObj["data"]?["links"]?["url"]?.ToString();
                             FileLog($"Upload Success: {remoteUrl}");
                             return remoteUrl;
                         }
                         else
                         {
                             FileLog($"Lsky Logic Error: {json}");
                         }
                     }
                 }
             }
             return null;
        }
    }
}
