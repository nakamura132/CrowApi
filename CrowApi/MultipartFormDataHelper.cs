using Microsoft.Net.Http.Headers;

namespace CrowApi
{
    public class MultipartFormDataHelper
    {
        /// <summary>
        /// get the boundary information
        /// </summary>
        /// <param name="mediaType"></param>
        /// <returns></returns>
        /// <exception cref="InvalidDataException"></exception>
        public static string GetBoundary(MediaTypeHeaderValue mediaType)
        {
            var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;
            if( string.IsNullOrWhiteSpace(boundary))
            {
                throw new InvalidDataException("Missing content-type boundary.");
            }
            return boundary;
        }

        /// <summary>
        /// validate if it was multipart form data
        /// </summary>
        /// <param name="contentType"></param>
        /// <returns></returns>
        public static bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                && contentType.IndexOf("multipart/", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool HasFormDataContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            // for example, Content-Disposition: form-data; name="subdirectory";
            return contentDisposition != null
                && contentDisposition.DispositionType.Equals("form-data", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrEmpty(contentDisposition.FileName.Value)
                && string.IsNullOrEmpty(contentDisposition.FileNameStar.Value);
        }

        public static bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            // for example, Content-Disposition: form-data; name="files"; filename="OnScreenControl_7.58.zip"
            return contentDisposition != null
                && contentDisposition.DispositionType.Equals("form-data", StringComparison.OrdinalIgnoreCase)
                && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                || (!string.IsNullOrEmpty(contentDisposition.FileNameStar.Value)));
        }
    }
}
