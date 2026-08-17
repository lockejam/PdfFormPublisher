using PdfFormPublisher.Attributes;
using PdfFormPublisher.Interfaces;
using System.Reflection;

namespace PdfFormPublisher;

internal static class Helper
{
    /// <summary>
    /// Extension that breaks down a models properties and attributes to be readable by the PDF form.
    /// </summary>
    /// <param name="form"></param>
    /// <returns></returns>
    public static IEnumerable<FormField> GetFormFields(this IPdfFormPublisher form)
    {
        ArgumentNullException.ThrowIfNull(form);

        return form.GetType()
                   .GetProperties()
                   .Select(prop => new
                   {
                       prop,
                       formAttribute = prop.GetCustomAttributes<FormFieldAttribute>(false).SingleOrDefault(),
                       dataAttribute = prop.GetCustomAttributes<DataLineAttribute>(false).SingleOrDefault()

                   })
                   .Where(x => x.formAttribute == null
                            || x.formAttribute.IncludeField)
                   .Select(x => new FormField
                   {
                       Name = x.formAttribute?.FieldName ?? x.prop.Name,
                       Value = x.prop.GetValue(form),
                       DataFormat = x.formAttribute?.DataFormat,
                       IsInitial = x.dataAttribute?.IsInitial ?? false,
                       IsLineNumber = x.dataAttribute?.IsLineNumber ?? false,
                       IsPageNumber = x.dataAttribute?.IsPageNumber ?? false,
                       IsNumberOfPages = x.dataAttribute?.IsNumberOfPages ?? false,
                       SumOf = x.dataAttribute?.SumOf
                   });
    }

    /// <summary>
    /// Break down list of items into a readable DataLine class.
    /// </summary>
    /// <param name="datalines"></param>
    /// <returns></returns>
    public static IEnumerable<DataLine> GetFormFields(this IEnumerable<IDataLine> datalines)
    {
        ArgumentNullException.ThrowIfNull(datalines);

        return datalines.Select((line, index) =>
        {
            if (line is null)
            {
                throw new InvalidOperationException($"Items contains a null entry at index {index}.");
            }

            return new DataLine
            {
                SkipLineNumber = line.SkipLineNumber,
                FormFields = line.GetFormFields().ToList()
            };
        });
    }
}
