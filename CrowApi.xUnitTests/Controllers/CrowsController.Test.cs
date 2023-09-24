using CrowApi.Controllers;
using CrowApi.Models;
using CrowApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Text;

namespace CrowApi.xUnitTests.Controllers
{
    public class CrowsControllerTest
    {
        private readonly CrowsController _controller;
        private Mock<IFileService> _fileServiceMock;
        public CrowsControllerTest()
        {
            // fileService のモックをセット
            _fileServiceMock = new Mock<IFileService>();
            // logger のモックをセット
            // ILogger には実装すべきインターフェイス項目がないため、Setup は省略
            var loggerMock = new Mock<ILogger<CrowsController>>();
            // config のモックをセット
            var configMock = new Mock<IConfiguration>();

            _controller = new CrowsController( loggerMock.Object, configMock.Object, _fileServiceMock.Object );
        }

        [Fact]
        public async Task UploadFileAsyncTestAsync()
        {
            // Arrage (準備)
            // fileService のセットアップ
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("this is CrowsController unit test."));
            string filename = "無題1.txt";
            _fileServiceMock.Setup( m => m.SaveFileAsync( stream, filename ) ).ReturnsAsync( 34 );

            // httpContext のセットアップ
            string boundary = "--------------------------620627111980243306924807";
            long contentLength = 432;
            // HeaderDictionary を作成し、HTTPヘッダをセット
            var headers = new Dictionary<string, StringValues>
            {
                { "Accept", "*/*" },
                { "Connection", "keep-alive" },
                { "Host", "localhost:5021" },
                { "User-Agent", "PostmanRuntime/7.32.3" },
                { "Accept-Encoding", "gzip, deflate, br" },
                { "Content-Type", $"multipart/form-data; boundary={boundary}" },
                { "Content-Length", $"{contentLength}" },
            };
            var headerDictionary = new HeaderDictionary( headers );
            // multipart/form-data 形式のリクエストボディを作成
            // ファイルコンテンツを作成
            string fileContent = """
                abc
                こんにちは
                """;
            // リクエストボディを作成
            // 複数のセクションを boundary で区切った形
            // セクションヘッダとセクションボディは空白行を空ける
            // 最後のセクションの boundary には最後であることを示す "--" が末尾に付与される
            string formData = $"""
                --{boundary}
                Content-Disposition: form-data; name=""; filename="{filename}"; filename*="UTF-8''%E7%84%A1%E9%A1%8C1.txt
                
                {fileContent}
                --{boundary}
                Content-Disposition: form-data; name=""; filename="b.txt"

                {fileContent}
                --{boundary}--
                """;
            var formDataBytes = Encoding.UTF8.GetBytes(formData);
            // HttpRequestFeature を作成し、HTTPリクエストの各構成要素をセット
            var requestFeature = new HttpRequestFeature()
            {
                Headers = headerDictionary,
                Method = "POST",
                Path = "/api/Crows",
                Protocol = "http",
                Body = new MemoryStream(formDataBytes)
            };
            // Feature コレクションを作成し、HttpRequestFeature をセット
            var features = new FeatureCollection();
            features.Set<IHttpRequestFeature>( requestFeature );
            // Initialize the DefaultHttpContext with the feature collection.
            var httpContext = new DefaultHttpContext( features );
            //var requestFeature = httpContext.Features.Get<IHttpRequestFeature>();
            //var httpRequestMock = new Mock<HttpRequest>();
            //var headers = new Mock<IHeaderDictionary>();

            //string boundary = "--------------------------620627111980243306924807";
            //httpRequestMock.Setup( h => h.ContentType ).Returns( $"multipart/form-data; boundary={boundary}" );
            //httpRequestMock.Setup( h => h.ContentLength ).Returns( 432 );
            //httpRequestMock.Setup( h => h.Method ).Returns( "POST" );
            //httpRequestMock.Setup( h => h.Path ).Returns( "http://localhost:5021/api/Crows" );

            // リクエストをコントローラーアクションに渡す
            _controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = httpContext
            };

            // Act (実行)
            var result = await _controller.UploadFilesAsync();

            // Assert (確認)
            // 処理成功時：201 Created を返す
            Assert.NotNull( result );
            Assert.IsType<CreatedResult>( result );
            // インターフェイスを継承・実装しているかを判定する。
            // 判定成功時に戻り値として「検証した型にキャストしたオブジェクト」を返す
            var fileUploadSummary = Assert.IsAssignableFrom<IFileUploadSummary>( ( (CreatedResult)result ).Value );
            Assert.NotNull( fileUploadSummary );
            Assert.Equal( 2, fileUploadSummary.TotalFilesUploaded );
            Assert.Equal( $"{contentLength}" ,fileUploadSummary.TotalSizeUploaded );
            Assert.NotNull( fileUploadSummary.UploadedFiles );
            Assert.Equal( 2, fileUploadSummary.UploadedFiles.Count );
            Assert.NotNull( fileUploadSummary.NotUploadedFiles );
            Assert.Equal( 0, fileUploadSummary.NotUploadedFiles.Count );
        }
    }
}
