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
        [HttpPost("uploadlarge")]
        [DisableFormValueModelBinding]
        public async Task<IActionResult> UploadFilesAsync() {
            if (false == MultipartFormDataHelper.IsMultipartContentType(Request.ContentType)){
                ModelState.AddModelError("File",
                    $"The request couldn't be processed (Error 1).");
                // Log Error
                return BadRequest(ModelState);
            }

            // find the boundary
            var boundary = MultipartFormDataHelper.GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType));
            // use boundary to iterator through the multipart section
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);

            do
            {
                var section = await reader.ReadNextSectionAsync();
                if( section is null)
                {
                    break;
                }
                ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                if (false == MultipartFormDataHelper.HasFileContentDisposition(contentDisposition))
                {
                    ModelState.AddModelError("File", $"The request couldn't be processed (Error 2).");
                    // Log Error
                    return BadRequest(ModelState);
                }

            } while (true);

            return Created(nameof(CrowsController), null);
        }

        private async Task<long> SaveFileAsync(MultipartSection section, string subDirectory){
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
