namespace PdfFormPublisher.Tests;

public sealed class PublishingValidationTests
{
    [Fact]
    public void PdfForm_constructor_rejects_blank_template_path()
    {
        Assert.Throws<ArgumentException>(() => new SimplePdfFormModel(" "));
    }

    [Fact]
    public void PdfForm_publish_reports_missing_template_file()
    {
        var form = new SimplePdfFormModel(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.pdf"));

        Assert.Throws<FileNotFoundException>(() => form.Publish());
    }

    [Fact]
    public void TabularPdfForm_publish_requires_items()
    {
        var settings = new TabularPdfFormSettings
        {
            FirstPageFilePath = TestPdfTemplates.CreateTabularTemplate("missing-items", rowCount: 1, includeInitialFields: true),
            ContinuationPageFilePath = TestPdfTemplates.CreateTabularTemplate("missing-items-cont", rowCount: 1, includeInitialFields: false),
            FirstPageRowCount = 1,
            ContinuationPageRowCount = 1
        };

        var form = new TestInventoryPdfForm(settings);

        var exception = Assert.Throws<InvalidOperationException>(() => form.Publish());
        Assert.Equal("Items must be set before publishing.", exception.Message);
    }

    [Fact]
    public void TabularPdfForm_publish_requires_continuation_rows_when_items_overflow_first_page()
    {
        var settings = new TabularPdfFormSettings
        {
            FirstPageFilePath = TestPdfTemplates.CreateTabularTemplate("bad-continuation-count", rowCount: 1, includeInitialFields: true),
            FirstPageRowCount = 1,
            ContinuationPageRowCount = 0
        };

        var form = new TestInventoryPdfForm(settings)
        {
            Items =
            [
                new TestInventoryLine { Description = "One", Cost = 1.00m },
                new TestInventoryLine { Description = "Two", Cost = 2.00m }
            ]
        };

        var exception = Assert.Throws<InvalidOperationException>(() => form.Publish());
        Assert.Equal("Settings.ContinuationPageRowCount must be greater than zero when items exceed the first-page row count.", exception.Message);
    }

    [Fact]
    public void TabularPdfForm_stream_publish_requires_continuation_template_stream_when_items_overflow_first_page()
    {
        var templateBytes = File.ReadAllBytes(TestPdfTemplates.CreateTabularTemplate("missing-continuation-stream", rowCount: 1, includeInitialFields: true));
        var settings = new TabularPdfFormSettings
        {
            FirstPageRowCount = 1,
            ContinuationPageRowCount = 1
        };

        var form = new TestInventoryPdfForm(settings)
        {
            Items =
            [
                new TestInventoryLine { Description = "One", Cost = 1.00m },
                new TestInventoryLine { Description = "Two", Cost = 2.00m }
            ]
        };

        using (var templateStream = new MemoryStream(templateBytes))
        {
            var exception = Assert.Throws<InvalidOperationException>(() => form.Publish(templateStream));
            Assert.Equal("continuationPageTemplateStream must be provided when items exceed the first-page row count.", exception.Message);
        }
    }
}
