using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ABC_Retail.pt2.Functions.Helpers
{
    public sealed record FilePart(string FieldName, string FileName, MemoryStream Data);
    public sealed record FormData(IReadOnlyDictionary<string, string> Text, IReadOnlyList<FilePart> Files);

    public static class MultipartHelper
    {
        public static async Task<FormData> ParseAsync(Stream body, string contentType)
        {
            var text = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            var files = new List<FilePart>();

            if (string.IsNullOrEmpty(contentType) || !contentType.Contains("multipart/form-data"))
                return new FormData(text, files);

            var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(contentType).Boundary).Value
                           ?? throw new InvalidOperationException("Multipart boundary missing");

            var reader = new MultipartReader(boundary, body);
            MultipartSection? section;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var cd = ContentDispositionHeaderValue.Parse(section.ContentDisposition);
                if (cd.IsFileDisposition())
                {
                    var fieldName = cd.Name.Value?.Trim('"') ?? "file";
                    var fileName = cd.FileName.Value?.Trim('"') ?? "upload.bin";
                    var ms = new MemoryStream();
                    await section.Body.CopyToAsync(ms);
                    ms.Position = 0;
                    files.Add(new FilePart(fieldName, fileName, ms));
                }
                else if (cd.IsFormDisposition())
                {
                    var name = cd.Name.Value?.Trim('"') ?? "";
                    using var sr = new StreamReader(section.Body, Encoding.UTF8);
                    var val = await sr.ReadToEndAsync();
                    text[name] = val;
                }
            }

            return new FormData(text, files);
        }
    }
}
