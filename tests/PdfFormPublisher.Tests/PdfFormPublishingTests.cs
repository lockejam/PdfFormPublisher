namespace PdfFormPublisher.Tests;

public sealed class PdfFormPublishingTests
{
    [Fact]
    public void Publish_maps_model_values_to_pdf_fields()
    {
        var templatePath = TestPdfTemplates.CreateSimpleTemplate();
        var form = new SimplePdfFormModel(templatePath)
        {
            Title = "Supply Request",
            Alias = "FP-001",
            Date = new DateTime(2026, 5, 3),
            Choice = ["Blue"]
        };

        var pdfBytes = form.Publish();

        var fields = TestPdfTemplates.ReadFieldValues(pdfBytes);
        Assert.Equal("Supply Request", fields["Title"]);
        Assert.Equal("FP-001", fields["RENAMED"]);
        Assert.Equal("2026-05-03", fields["Date"]);
        Assert.Equal("Blue", fields["Choice"]);
    }

    [Fact]
    public void Publish_accepts_template_stream_without_file_path()
    {
        var templateBytes = File.ReadAllBytes(TestPdfTemplates.CreateSimpleTemplate());
        using (var templateStream = new MemoryStream(templateBytes))
        {
            var form = new SimplePdfFormModel
            {
                Title = "Stream Request",
                Alias = "FP-STREAM",
                Date = new DateTime(2026, 5, 24),
                Choice = ["Green"]
            };

            var pdfBytes = form.Publish(templateStream);

            var fields = TestPdfTemplates.ReadFieldValues(pdfBytes);
            Assert.Equal("Stream Request", fields["Title"]);
            Assert.Equal("FP-STREAM", fields["RENAMED"]);
            Assert.Equal("2026-05-24", fields["Date"]);
            Assert.Equal("Green", fields["Choice"]);
            Assert.True(templateStream.CanRead);
        }
    }

    [Fact]
    public void Publish_writes_template_stream_output_to_stream()
    {
        var templateBytes = File.ReadAllBytes(TestPdfTemplates.CreateSimpleTemplate());
        using (var templateStream = new MemoryStream(templateBytes))
        {
            using (var outputStream = new MemoryStream())
            {
                var form = new SimplePdfFormModel
                {
                    Title = "Output Stream Request",
                    Alias = "FP-OUTPUT",
                    Date = new DateTime(2026, 5, 25),
                    Choice = ["Red"]
                };

                form.Publish(templateStream, outputStream);

                var fields = TestPdfTemplates.ReadFieldValues(outputStream.ToArray());
                Assert.Equal("Output Stream Request", fields["Title"]);
                Assert.Equal("FP-OUTPUT", fields["RENAMED"]);
                Assert.Equal("2026-05-25", fields["Date"]);
                Assert.Equal("Red", fields["Choice"]);
                Assert.True(templateStream.CanRead);
                Assert.True(outputStream.CanWrite);
            }
        }
    }

    [Fact]
    public void Publish_maps_bool_values_to_checkbox_states()
    {
        var templatePath = TestPdfTemplates.CreateCheckboxTemplate();
        var form = new CheckboxPdfFormModel(templatePath)
        {
            DefaultChecked = true,
            CustomChecked = true,
            DefaultUnchecked = false
        };

        var pdfBytes = form.Publish();

        var fields = TestPdfTemplates.ReadFieldValues(pdfBytes);
        Assert.Equal("Yes", fields["DefaultChecked"]);
        Assert.Equal("On", fields["CustomChecked"]);
        Assert.Equal("Off", fields["DefaultUnchecked"]);
    }
}
