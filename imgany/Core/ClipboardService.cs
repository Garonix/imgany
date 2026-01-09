using System;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace imgany.Core
{
    public class ClipboardService
    {
        private readonly ConfigManager _config;

        public ClipboardService(ConfigManager config)
        {
            _config = config;
        }

        public bool HasImage()
        {
            return Clipboard.ContainsImage();
        }

        public bool HasImageLink(out string url)
        {
            url = null;
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText()?.Trim();
                if (string.IsNullOrEmpty(text)) return false;

                // Improved Regex:
                // Relaxed: Allow spaces around, simple http protocol.
                // Matches: http(s)://... .ext ...
                string pattern = @"^https?://.+(?:\.png|\.jpg|\.jpeg|\.gif|\.webp|\.bmp)(?:\?.*)?$";
                
                if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase))
                {
                    url = text;
                    return true;
                }
            }
            return false;
        }

        // Result: (MemoryStream of PNG, Suggested FileName)
        public async Task<(MemoryStream, string)> GetClipboardImageStreamAsync(string prefix)
        {
             // Run on UI thread to access Clipboard, then offload encoding
             // Actually Clipboard must be accessed on STA thread (UI thread OK).
             if (Clipboard.ContainsImage())
             {
                 using (var image = Clipboard.GetImage())
                 {
                     if (image == null) return (null, null);
                     
                     // Deep clone to memory stream
                     var ms = new MemoryStream();
                     image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                     ms.Position = 0; // Rewind
                     
                     string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                     string filename = $"{prefix}_{timestamp}.png";
                     
                     return (ms, filename);
                 }
             }
             return (null, null);
        }

        public async Task<string> SaveImageFromClipboardAsync(string targetFolder)
        {
            try
            {
                var (stream, filename) = await GetClipboardImageStreamAsync(_config.FilePrefix);
                if (stream != null)
                {
                    using (stream)
                    {
                        filename = EnsureUnique(targetFolder, filename);
                        string fullPath = Path.Combine(targetFolder, filename);
                        
                        using (var fs = File.Create(fullPath))
                        {
                            await stream.CopyToAsync(fs);
                        }
                        return fullPath;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Save Image Error: {ex.Message}");
            }
            return null;
        }

        private string EnsureUnique(string folder, string filename)
        {
             string fullPath = Path.Combine(folder, filename);
             if (!File.Exists(fullPath)) return filename;
             
             string nameNoExt = Path.GetFileNameWithoutExtension(filename);
             string ext = Path.GetExtension(filename);
             int index = 1;
             while(true)
             {
                 string newName = $"{nameNoExt}_{index}{ext}";
                 if (!File.Exists(Path.Combine(folder, newName))) return newName;
                 index++;
             }
        }

        public async Task<string> DownloadAndSaveImageAsync(string url, string targetFolder)
        {
            try
            {
                using (var handler = new HttpClientHandler { AllowAutoRedirect = true })
                using (var client = new HttpClient(handler))
                {
                    // Optimization: Network Robustness
                    // 1. User-Agent
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    // 2. Timeout
                    client.Timeout = TimeSpan.FromSeconds(30);

                    // 3. Referer (Anti-Hotlinking fix)
                    try 
                    {
                         var uri = new Uri(url);
                         client.DefaultRequestHeaders.Referrer = new Uri($"{uri.Scheme}://{uri.Host}");
                    }
                    catch {}

                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        var data = await response.Content.ReadAsByteArrayAsync();
                        if (data != null && data.Length > 0)
                        {
                            // Try to guess extension from Content-Type header first, then URL
                            string ext = "jpg"; 
                            var contentType = response.Content.Headers.ContentType?.MediaType;
                            if (!string.IsNullOrEmpty(contentType))
                            {
                                if (contentType.Contains("png")) ext = "png";
                                else if (contentType.Contains("gif")) ext = "gif";
                                else if (contentType.Contains("webp")) ext = "webp";
                                else if (contentType.Contains("jpeg")) ext = "jpg";
                            }
                            else 
                            {
                                if (url.Contains(".png")) ext = "png";
                                else if (url.Contains(".gif")) ext = "gif";
                                else if (url.Contains(".webp")) ext = "webp";
                            }

                            string filename = GenerateNextFilename(targetFolder, _config.FilePrefix, ext);
                            string fullPath = Path.Combine(targetFolder, filename);
                            
                            await File.WriteAllBytesAsync(fullPath, data);
                            return fullPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Download Error: {ex.Message}");
            }
            return null;
        }

        private string GenerateNextFilename(string folder, string prefix, string ext)
        {
            // Optimization: Timestamp-based Naming
            // Structure: Prefix_yyyyMMdd_HHmmss.ext
            // Performance: O(1) mostly. If collision (same second), append _index.
            
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string baseName = $"{prefix}_{timestamp}";
            string name = $"{baseName}.{ext}";
            string path = Path.Combine(folder, name);

            if (!File.Exists(path))
            {
                return name;
            }

            // Collision handling (fast loop)
            int index = 1;
            while (true)
            {
                name = $"{baseName}_{index}.{ext}";
                path = Path.Combine(folder, name);
                if (!File.Exists(path))
                {
                    return name;
                }
                index++;
            }
        }
    }
}
