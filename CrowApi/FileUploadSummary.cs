namespace CrowApi
{
    public class FileUploadSummary
    {
        public int TotalFilesUploaded { get; set; }
        public string? TotalSizeUploaded { get; set; }
        public IList<string>? FilePaths { get; set; }
        public IList<string>? NotUploadedFiles { get; set; }
    }
}
