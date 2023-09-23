namespace CrowApi.Models
{
    public class FileUploadSummary : IFileUploadSummary
    {
        public int TotalFilesUploaded { get; set; }
        public string? TotalSizeUploaded { get; set; }
        public IList<string>? UploadedFiles { get; set; }
        public IList<string>? NotUploadedFiles { get; set; }
    }
}
