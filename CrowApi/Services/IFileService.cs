using CrowApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace CrowApi.Services
{
    public interface IFileService
    {
        /// <summary>
        /// ストリームを指定したファイルに書き出し保存する
        /// </summary>
        /// <param name="stream">ストリーム</param>
        /// <param name="fileName">書き出し対象ファイル名</param>
        /// <returns>書き込んだバイト数</returns>
        Task<long> SaveFileAsync( Stream stream, string fileName );
    }
}
