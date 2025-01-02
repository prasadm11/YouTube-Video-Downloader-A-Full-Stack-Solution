document.addEventListener("DOMContentLoaded", function () {

  const connection = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5194/progressHub") 
      .build();


  connection.on("ReceiveProgress", function (progress) {
      console.log("Download progress: " + progress + "%");

      document.getElementById("progressContainer").style.display = "block";

      document.getElementById("progressBar").style.width = progress + "%";
      document.getElementById("progressText").innerText = progress + "%";
  });

  connection.start().catch(function (err) {
      return console.error("Error starting SignalR connection: " + err.toString());
  });

  async function downloadVideo() {
      const url = document.getElementById("videoUrl").value;
      const resolution = document.getElementById("resolution").value;


      const urlRegex = /^(https?\:\/\/)?(www\.)?(youtube|youtu|vimeo)\.(com|co|tv|net)\/.+$/;
      if (!url || !urlRegex.test(url)) {
          alert("Please provide a valid YouTube URL.");
          return;
      }

      const formatIdMap = {
          "480p": "18",
          "360p": "17",
          "720p": "22",
          "1080p": "37"
      };

      const formatId = formatIdMap[resolution.toLowerCase()];

      if (!formatId) {
          alert("Selected resolution is invalid.");
          return;
      }

      document.getElementById("downloadButton").disabled = true;

      try {
          const requestData = {
              url: url,
              FormatId: formatId 
          };
          console.log("Sending request data:", requestData);

          const response = await fetch("http://localhost:5194/api/YouTube/download_video", {
              method: "POST",
              headers: {
                  "Content-Type": "application/json",
                  "Accept": "application/json"
              },
              body: JSON.stringify(requestData),
              credentials: "include" 
          });

          if (!response.ok) {
              const errorText = await response.text();
              console.error("Error downloading video: ", response.statusText);
              console.error("Error details: ", errorText);
              alert("Failed to download video. Please try again.");
              return;
          }

          const blob = await response.blob();
          const downloadLink = document.createElement("a");
          downloadLink.href = URL.createObjectURL(blob);
          downloadLink.download = "video.mp4"; 
          downloadLink.click();
          
          URL.revokeObjectURL(downloadLink.href);

      } catch (error) {
          console.error("Error downloading video: ", error);
          alert("Failed to download video. Please try again.");
      }

      document.getElementById("downloadButton").disabled = false;
  }

  document.getElementById("downloadButton").addEventListener("click", downloadVideo);
});
