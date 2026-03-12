using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MaintainingOrdersWeb.Documents;

public class SalesReportDocument : IDocument
{
    public class SalesRow
    {
        public string Product { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }

    private readonly DateOnly _from;
    private readonly DateOnly _to;
    private readonly IReadOnlyCollection<SalesRow> _rows;

    public SalesReportDocument(DateOnly from, DateOnly to, IReadOnlyCollection<SalesRow> rows)
    {
        _from = from;
        _to = to;
        _rows = rows;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(24);
            page.Size(PageSizes.A4);

            page.Header().Column(col =>
            {
                col.Item().Text("Отчет по продажам").FontSize(18).SemiBold().FontColor(Colors.Blue.Darken2);
                col.Item().Text($"Период: {_from:dd.MM.yyyy} - {_to:dd.MM.yyyy}").FontSize(11);
                col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
            });

            page.Content().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(6).Text("Товар").SemiBold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("Кол-во").SemiBold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(6).AlignRight().Text("Сумма, ₽").SemiBold();
                });

                foreach (var row in _rows)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).Text(row.Product);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignRight().Text(row.Quantity.ToString());
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6).AlignRight().Text(row.Amount.ToString("N2"));
                }
            });

            page.Footer().AlignRight().Text(x =>
            {
                x.Span("Стр. ");
                x.CurrentPageNumber();
                x.Span("/");
                x.TotalPages();
            });
        });
    }
}
