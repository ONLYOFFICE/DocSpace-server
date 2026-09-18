using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace DocSpace.Webhooks.SDK.Client;

// The generated models open with
//
//     using FileParameter        = DocSpace.Webhooks.SDK.Client.FileParameter;
//     using OpenAPIDateConverter = DocSpace.Webhooks.SDK.Client.OpenAPIDateConverter;
//
// on every file. Neither alias is referenced by any generated member -- the
// contract has no file parameters and no date-only fields -- but C# still
// refuses to compile a `using` alias whose target does not exist.
//
// These two stand-ins are the entire reason this SDK can stay models-only.
// Generating the supporting files instead would pull in 17 files of HTTP
// client machinery plus Newtonsoft, Polly and JsonSubTypes, none of which a
// library that only *receives* webhooks has any use for.
//
// If a future contract change makes the models actually call into these, the
// build breaks loudly here rather than misbehaving at run time.

/// <summary>Stand-in for the generator's multipart file parameter type.</summary>
public class FileParameter
{
    public string Filename { get; set; } = "file";
    public string ContentType { get; set; } = "application/octet-stream";
    public Stream? Content { get; set; }
}

/// <summary>Stand-in for the generator's ISO-8601 date converter.</summary>
public class OpenAPIDateConverter : IsoDateTimeConverter
{
    public OpenAPIDateConverter() => DateTimeFormat = "yyyy-MM-dd";
}
