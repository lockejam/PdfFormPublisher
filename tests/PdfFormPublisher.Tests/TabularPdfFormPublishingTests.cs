namespace PdfFormPublisher.Tests;

public sealed class TabularPdfFormPublishingTests
{
    [Fact]
    public void Publish_sets_tabular_rows_and_page_metadata()
    {
        var firstPageTemplate = TestPdfTemplates.CreateTabularTemplate("tabular-first", rowCount: 2, includeInitialFields: true);
        var continuationTemplate = TestPdfTemplates.CreateTabularTemplate("tabular-continuation", rowCount: 2, includeInitialFields: false);
        var settings = new TabularPdfFormSettings
        {
            FirstPageFilePath = firstPageTemplate,
            ContinuationPageFilePath = continuationTemplate,
            FirstPageRowCount = 2,
            ContinuationPageRowCount = 2
        };

        var form = new TestInventoryPdfForm(settings)
        {
            Title = "Tool List",
            Items =
            [
                new TestInventoryLine { Description = "Wrench", Cost = 1.25m },
                new TestInventoryLine { Description = "Spacer", Cost = 0.00m, SkipLineNumber = true },
                new TestInventoryLine { Description = "Hammer", Cost = 2.75m }
            ]
        };

        var pdfBytes = form.Publish();

        var fields = TestPdfTemplates.ReadFieldValues(pdfBytes);
        Assert.Equal("Tool List", fields["Title_sheet(1)"]);
        Assert.Equal("1", fields["PageNumber_sheet(1)"]);
        Assert.Equal("2", fields["NumberOfPages_sheet(1)"]);
        Assert.Equal("1.25", fields["SheetTotal_sheet(1)"]);
        Assert.Equal("1", fields["LineNumber.0_sheet(1)"]);
        Assert.Equal("Wrench", fields["Description.0_sheet(1)"]);
        Assert.Equal("1.25", fields["Cost.0_sheet(1)"]);
        Assert.Equal(string.Empty, fields["LineNumber.1_sheet(1)"]);
        Assert.Equal("Spacer", fields["Description.1_sheet(1)"]);
        Assert.Equal("2", fields["PageNumber_sheet(2)"]);
        Assert.Equal("2", fields["NumberOfPages_sheet(2)"]);
        Assert.Equal("2.75", fields["SheetTotal_sheet(2)"]);
        Assert.Equal("1", fields["LineNumber.0_sheet(2)"]);
        Assert.Equal("Hammer", fields["Description.0_sheet(2)"]);
        Assert.Equal("2.75", fields["Cost.0_sheet(2)"]);
        Assert.DoesNotContain("Title_sheet(2)", fields.Keys);
    }

    [Fact]
    public void Publish_accepts_template_streams_and_writes_to_output_stream()
    {
        var firstPageTemplateBytes = File.ReadAllBytes(TestPdfTemplates.CreateTabularTemplate("tabular-stream-first", rowCount: 2, includeInitialFields: true));
        var continuationTemplateBytes = File.ReadAllBytes(TestPdfTemplates.CreateTabularTemplate("tabular-stream-continuation", rowCount: 2, includeInitialFields: false));
        var settings = new TabularPdfFormSettings
        {
            FirstPageRowCount = 2,
            ContinuationPageRowCount = 2
        };

        var form = new TestInventoryPdfForm(settings)
        {
            Title = "Stream Tool List",
            Items =
            [
                new TestInventoryLine { Description = "Clamp", Cost = 3.50m },
                new TestInventoryLine { Description = "Bracket", Cost = 4.25m },
                new TestInventoryLine { Description = "Gauge", Cost = 5.75m }
            ]
        };

        using (var firstPageTemplateStream = new MemoryStream(firstPageTemplateBytes))
        {
            using (var continuationTemplateStream = new MemoryStream(continuationTemplateBytes))
            {
                using (var outputStream = new MemoryStream())
                {
                    form.Publish(firstPageTemplateStream, continuationTemplateStream, outputStream);

                    var fields = TestPdfTemplates.ReadFieldValues(outputStream.ToArray());
                    Assert.Equal("Stream Tool List", fields["Title_sheet(1)"]);
                    Assert.Equal("1", fields["PageNumber_sheet(1)"]);
                    Assert.Equal("2", fields["NumberOfPages_sheet(1)"]);
                    Assert.Equal("7.75", fields["SheetTotal_sheet(1)"]);
                    Assert.Equal("Clamp", fields["Description.0_sheet(1)"]);
                    Assert.Equal("Bracket", fields["Description.1_sheet(1)"]);
                    Assert.Equal("2", fields["PageNumber_sheet(2)"]);
                    Assert.Equal("5.75", fields["SheetTotal_sheet(2)"]);
                    Assert.Equal("Gauge", fields["Description.0_sheet(2)"]);
                    Assert.DoesNotContain("Title_sheet(2)", fields.Keys);
                    Assert.True(firstPageTemplateStream.CanRead);
                    Assert.True(continuationTemplateStream.CanRead);
                    Assert.True(outputStream.CanWrite);
                }
            }
        }
    }
}
