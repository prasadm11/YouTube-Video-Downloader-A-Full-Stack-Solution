using Microsoft.AspNetCore.SignalR;

namespace YouTubeDownloaderAPI.Hubs
{
    public class ProgressHub : Hub
    {
        public async Task ReportProgress(double progress)
        {
            await Clients.All.SendAsync("ReceiveProgress", progress);
        }
    }
}
