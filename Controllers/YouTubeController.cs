using System;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using YouTubeDownloaderAPI.Models;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using YouTubeDownloaderAPI.Hubs;

namespace YouTubeDownloaderAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class YouTubeController : ControllerBase
    {
        private readonly IHubContext<ProgressHub> _hubContext;
        private readonly string _downloadFolder = Path.Combine(Directory.GetCurrentDirectory(), "downloads");

        public YouTubeController(IHubContext<ProgressHub> hubContext)
        {
            _hubContext = hubContext;

            if (!Directory.Exists(_downloadFolder))
            {
                Directory.CreateDirectory(_downloadFolder);
            }
        }

        [HttpPost("download_video")]
        public async Task<IActionResult> DownloadVideo([FromBody] DownloadRequest request)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveProgress", 50.0);
            if (string.IsNullOrWhiteSpace(request.Url) || string.IsNullOrWhiteSpace(request.FormatId))
                return BadRequest("URL and Format ID are required");

            try
            {
                
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"-f {request.FormatId} -o - {request.Url}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                        return StatusCode(500, "Failed to start yt-dlp process.");

                    Response.Headers.Add("Content-Disposition", "attachment; filename=video.mp4");
                    Response.ContentType = "application/octet-stream";

                    var progressStream = process.StandardError;
                    string progressLine;

                    using (var videoStream = process.StandardOutput.BaseStream)
                    {
                        var buffer = new byte[8192]; 
                        int bytesRead;

                        while ((bytesRead = await videoStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await Response.Body.WriteAsync(buffer, 0, bytesRead);
                        }
                    }
                    while ((progressLine = await progressStream.ReadLineAsync()) != null)
                    {
                        if (progressLine.Contains("download"))
                        {

                            var progressMatch = Regex.Match(progressLine, @"(\d+(\.\d+)?)%");
                            if (progressMatch.Success)
                            {
                                var progress = progressMatch.Groups[1].Value;
                                await _hubContext.Clients.All.SendAsync("ReceiveProgress", progress);
                            }
                        }
                    }
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        string error = await process.StandardError.ReadToEndAsync();
                        return StatusCode(500, $"yt-dlp failed: {error}");
                    }

                    return new EmptyResult(); 
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error downloading the video: {ex.Message}");
                return StatusCode(500, $"Error downloading the video: {ex.Message}");
            }
        }

        [HttpPost("get_video_info")]
        public async Task<IActionResult> GetVideoInfo([FromBody] DownloadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Url))
                return BadRequest("URL is required");

            try
            {                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "yt-dlp",
                    Arguments = $"-j {request.Url}", 
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

            
                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                        return StatusCode(500, "Failed to start yt-dlp process.");

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        return StatusCode(500, $"yt-dlp failed: {error}");
                    }

                    var videoDetails = JObject.Parse(output);
                    var title = videoDetails["title"]?.ToString();
                    var description = videoDetails["description"]?.ToString();

                    return Ok(new
                    {
                        message = "Video metadata fetched successfully",
                        title = title,
                        description = description
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching the video metadata: {ex.Message}");
                return StatusCode(500, $"Error fetching the video metadata: {ex.Message}");
            }
        }
    }
}
