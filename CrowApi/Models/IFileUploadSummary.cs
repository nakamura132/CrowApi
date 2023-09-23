namespace CrowApi.Models
{
    public interface IFileUploadSummary
    {
        int TotalFilesUploaded { get; set; }
        string? TotalSizeUploaded { get; set; }
        IList<string>? UploadedFiles { get; set; }
        IList<string>? NotUploadedFiles { get; set; }
    }
}
