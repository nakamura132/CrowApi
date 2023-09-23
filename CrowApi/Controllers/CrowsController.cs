using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Threading.Tasks;

namespace CrowApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CrowsController : ControllerBase
    {
        private readonly ILogger<CrowsController> _logger;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">DIされたロギングサービス</param>
        /// <param name="configuration">DIされた構成情報サービス</param>
        public CrowsController(
            ILogger<CrowsController> logger,
            IConfiguration configuration )
        {
            _logger = logger;
            _configuration = configuration;
        }

        [HttpPost]
        //[Route(nameof(UploadFilesAsync))]
        [DisableFormValueModelBinding]
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
            var filePaths = new List<string>();
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
                    // Get the temporary folder, and combine a random file name with it
                    //var fileName = Path.GetRandomFileName();
                    var fileName = contentDisposition.FileName.Value;
                    // ファイル保存用ディレクトリ情報を取得
                    var saveDirectoryName = _configuration.GetValue<string>("CustomConfig:UploadedFilesContainerRoot:Name");
                    if( string.IsNullOrEmpty(saveDirectoryName))
                    {
                        // ファイル保存用ディレクトリ情報を取得できない
                        _logger.LogInformation("the configuration of directory for saving files is not found.");
                        throw new Exception("内部サーバーエラー: the configuration of directory for saving files is not found.");
                    }
                    // ファイル保存用ディレクトリの存在確認
                    if( false == Path.Exists(saveDirectoryName) )
                    {
                        var createDirectoryIfNotExists = _configuration.GetValue<bool>("CustomConfig:UploadedFilesContainerRoot:CreateIfNotExists", false);
                        if(  createDirectoryIfNotExists )
                        {
                            try
                            {
                                _logger.LogInformation($"create a directory for saving files. : {saveDirectoryName}");
                                Directory.CreateDirectory(saveDirectoryName);
                            }
                            catch ( Exception ex)
                            {
                                _logger.LogError($"couldn't create a directory for saving files. : {ex}");
                                // ファイル保存用ディレクトリを作成できない場合
                                // このコントローラーは例外をスローし、例外処理ハンドラーミドルウェアがキャッチして例外処理を行う
                                // 例外処理ハンドラーは最終的にサーバーエラー (503) を返す
                                //
                                // *** 例外再スローの注意点   正しい : throw;   良くない : throw ex;   ⇒スタックトレースがリセットされてしまう ***
                                throw;
                            }
                        }
                        else
                        {
                            // ファイル保存用ディレクトリの作成が禁止されている
                            _logger.LogError("prohibited to create a directory for saving files.");
                            throw new Exception("内部サーバーエラー: prohibited to create a directory for saving files.");
                        }
                    }
                    var saveToPath = Path.Combine(saveDirectoryName, fileName);

                    // await using を使用するとリソース破棄を非同期に行う
                    await using var targetStream = System.IO.File.Create(saveToPath);
                    // 非同期ファイル保存
                    await section.Body.CopyToAsync(targetStream);
                    _logger.LogInformation($"completed uploading file {fileName} to {saveToPath}");

                    totalSizeInBytes = section.Body.Length;
                    filePaths.Add(saveToPath);
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
                FilePaths = filePaths,
                NotUploadedFiles = notUploadedFiles
            };
            return Created(nameof(CrowsController), fileUploadSummary);
        }

        private async Task<long> SaveFileAsync( MultipartSection section, string subDirectory )
        {
            // if subDirectory is null then assign string.Empty to subDirectory
            subDirectory ??= string.Empty;
            var target = Path.Combine("{root}", subDirectory);
            Directory.CreateDirectory(target);

            var fileSection = section.AsFileSection();

            var filePath = Path.Combine(target, fileSection.FileName);
            await using var localFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await fileSection.FileStream.CopyToAsync(localFileStream);

            return fileSection.FileStream.Length;
        }
    }
}
