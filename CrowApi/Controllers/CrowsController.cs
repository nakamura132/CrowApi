using CrowApi.Models;
using CrowApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Threading.Tasks;

namespace CrowApi.Controllers
{
    /// <summary>
    /// Crows エンドポイントへの全ての動作を定義します
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CrowsController : ControllerBase
    {
        private readonly ILogger<CrowsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IFileService _fileService;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">DIされるロギングサービス</param>
        /// <param name="configuration">DIされる構成情報サービス</param>
        /// <param name="fileService">DIされるファイルサービス</param>
        public CrowsController(
            ILogger<CrowsController> logger,
            IConfiguration configuration,
            IFileService fileService)
        {
            _logger = logger;
            _configuration = configuration;
            _fileService = fileService;
        }

        /// <summary>
        /// ファイルをアップロードします
        /// </summary>
        /// <returns></returns>
        /// <remarks>HTTPヘッダーの Content-Type が multipart/form-data の場合のみ有効です</remarks>
        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> UploadFilesAsync()
        {
            var request = HttpContext.Request;

            // validation of Content-Type
            // 1. first, it must be a form-data request
            // 2. a boundary should be found in the Content-Type
            if ( (false == request.HasFormContentType) ||
                (false == MediaTypeHeaderValue.TryParse(request.ContentType, out var mediaTypeHeader)) ||
                string.IsNullOrEmpty(mediaTypeHeader.Boundary.Value) )
            {
                return new UnsupportedMediaTypeResult();
            }

            var reader = new MultipartReader(mediaTypeHeader.Boundary.Value, request.Body);

            int fileCount = 0;
            long totalSizeInBytes = 0;
            var uploadedFiles = new List<string>();
            var notUploadedFiles = new List<string>();

            do
            {
                var section = await reader.ReadNextSectionAsync();
                if ( section is null )
                {
                    break;
                }
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition,
                    out var contentDisposition);

#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
                if ( hasContentDispositionHeader
                     && contentDisposition.DispositionType.Equals("form-data")
                     && !string.IsNullOrEmpty(contentDisposition.FileName.Value) )
                {
                    totalSizeInBytes += await _fileService.SaveFileAsync( section.Body, contentDisposition.FileName.Value );
                    uploadedFiles.Add( contentDisposition.FileName.Value );
                    fileCount++;
                }
                else
                {
                    // 処理対象ではないセクションだった場合、ログ出力だけして次のセクションに進む
                    _logger.LogInformation($"invalid content disposition header : {section.Headers.ToString()}");
                }
#pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。
            } while ( true );

            var fileUploadSummary = new FileUploadSummary
            {
                TotalFilesUploaded = fileCount,
                TotalSizeUploaded = totalSizeInBytes.ToString(),
                UploadedFiles = uploadedFiles,
                NotUploadedFiles = notUploadedFiles
            };
            return Created(nameof(CrowsController), fileUploadSummary);
        }
    }
}
