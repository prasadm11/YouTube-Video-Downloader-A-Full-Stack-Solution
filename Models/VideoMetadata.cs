public class VideoMetadata
{
    public string Title { get; set; }
    public string Description { get; set; }

    public VideoMetadata(string title, string description)
    {
        Title = title;
        Description = description;
    }
}
