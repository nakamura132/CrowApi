using CrowApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using static System.Collections.Specialized.BitVector32;

namespace CrowApi.Services
{
    /// <summary>
    /// ローカルファイルストレージへの操作を担当するサービス
    /// </summary>
    public class FileService : IFileService
    {
        ILogger<FileService> _logger;
        IConfiguration _configuration;
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">ロガーの依存性注入</param>
        /// <param name="configuration">コンフィグの依存性注入</param>
        public FileService( ILogger<FileService> logger, IConfiguration configuration )
        {
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// ストリームを指定したファイルに書き出し保存する
        /// </summary>
        /// <param name="stream">ストリーム</param>
        /// <param name="fileName">書き出し対象ファイル名</param>
        /// <returns>書き込んだバイト数</returns>
        /// <exception cref="Exception"></exception>
        public async Task<long> SaveFileAsync( Stream stream, string fileName )
        {
            // ファイル保存用ディレクトリ情報を取得
            var saveDirectoryName = _configuration.GetValue<string>("CustomConfig:UploadedFilesContainerRoot:Name");
            if ( string.IsNullOrEmpty( saveDirectoryName ) )
            {
                // ファイル保存用ディレクトリ情報を取得できない
                _logger.LogError( "the configuration of directory for saving files is not found." );
                throw new Exception( "内部サーバーエラー: the configuration of directory for saving files is not found." );
            }
            // ファイル保存用ディレクトリの存在確認
            if ( false == Path.Exists( saveDirectoryName ) )
            {
                var createDirectoryIfNotExists = _configuration.GetValue<bool>("CustomConfig:UploadedFilesContainerRoot:CreateIfNotExists", false);
                if ( createDirectoryIfNotExists )
                {
                    try
                    {
                        _logger.LogInformation( $"create a directory for saving files. : {saveDirectoryName}" );
                        Directory.CreateDirectory( saveDirectoryName );
                    }
                    catch ( Exception ex )
                    {
                        _logger.LogError( $"failed to create a directory for saving files. : {ex}" );
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
                    _logger.LogError( "prohibited creating a directory for saving files." );
                    throw new Exception( "内部サーバーエラー: prohibited creating a directory for saving files." );
                }
            }
            var saveToPath = Path.Combine(saveDirectoryName, fileName);
            // await using を使用するとリソース破棄を非同期に行う
            await using var outputStream = System.IO.File.Create( saveToPath );
            // 非同期ファイル保存
            await stream.CopyToAsync( outputStream );
            _logger.LogInformation( $"completed uploading file {fileName} to {saveToPath}" );

            return stream.Length;
        }
    }
}
