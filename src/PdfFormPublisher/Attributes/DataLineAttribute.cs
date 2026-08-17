namespace PdfFormPublisher.Attributes;

/// <summary>
/// Adds row and page behavior to a form or row property.
/// </summary>
/// <remarks>
/// Use this attribute when a value should be calculated by PdfFormPublisher, summed across
/// rows, or written only on the first page of a tabular form.
/// </remarks>
[AttributeUsage(AttributeTargets.Property)]
public class DataLineAttribute : Attribute
{
    /// <summary>
    /// Whether the field exists only on the first page template.
    /// </summary>
    /// <remarks>
    /// Set this to <see langword="true"/> for first-page-only fields so continuation pages
    /// do not try to write to a PDF field that is not present.
    /// </remarks>
    public bool IsInitial { get; set; }

    /// <summary>
    /// The row field name whose decimal values should be totaled for this field.
    /// </summary>
    public string? SumOf { get; set; }

    /// <summary>
    /// Whether PdfFormPublisher should write the current row number to this field.
    /// </summary>
    public bool IsLineNumber { get; set; }

    /// <summary>
    /// Whether PdfFormPublisher should write the current page number to this field.
    /// </summary>
    public bool IsPageNumber { get; set; }

    /// <summary>
    /// Whether PdfFormPublisher should write the total number of generated pages to this field.
    /// </summary>
    public bool IsNumberOfPages { get; set; }
}
