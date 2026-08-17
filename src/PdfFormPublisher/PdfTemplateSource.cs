using iText.Kernel.Pdf;

namespace PdfFormPublisher;

internal sealed class PdfTemplateSource
{
    private readonly string? _filePath;
    private readonly byte[]? _templateBytes;

    private PdfTemplateSource(string description, string? filePath, byte[]? templateBytes)
    {
        Description = description;
        _filePath = filePath;
        _templateBytes = templateBytes;
    }

    public string Description { get; }

    public static PdfTemplateSource FromFilePath(string? filePath, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new InvalidOperationException($"{propertyName} must be set before publishing.");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"The PDF template '{filePath}' was not found.", filePath);
        }

        return new PdfTemplateSource($"'{filePath}'", filePath, templateBytes: null);
    }

    public static PdfTemplateSource FromStream(Stream templateStream, string parameterName, string description)
    {
        ArgumentNullException.ThrowIfNull(templateStream, parameterName);

        if (!templateStream.CanRead)
        {
            throw new ArgumentException("Template stream must be readable.", parameterName);
        }

        using (var buffer = new MemoryStream())
        {
            templateStream.CopyTo(buffer);
            return new PdfTemplateSource(description, filePath: null, buffer.ToArray());
        }
    }

    public PdfReader CreateReader()
    {
        if (_filePath is not null)
        {
            return new PdfReader(_filePath);
        }

        return new PdfReader(new MemoryStream(_templateBytes!, writable: false));
    }
}
