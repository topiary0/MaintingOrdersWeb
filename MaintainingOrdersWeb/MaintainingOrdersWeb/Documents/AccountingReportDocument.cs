using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MaintainingOrdersWeb.Documents;

public class AccountingReportDocument : IDocument
{
    private readonly int _quarterOrders;
    private readonly decimal _quarterRevenue;
    private readonly int _clientsCount;
    private readonly DateTime _generatedAt;

    public AccountingReportDocument(int quarterOrders, decimal quarterRevenue, int clientsCount, DateTime generatedAt)
    {
        _quarterOrders = quarterOrders;
        _quarterRevenue = quarterRevenue;
        _clientsCount = clientsCount;
        _generatedAt = generatedAt;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(30);
            page.Size(PageSizes.A4);

            page.Header()
                .Text("Бухгалтерская отчетность")
                .SemiBold()
                .FontSize(20)
                .FontColor(Colors.Blue.Darken2);

            page.Content().Column(column =>
            {
                column.Spacing(10);
                column.Item().Text($"Сформировано: {_generatedAt:dd.MM.yyyy HH:mm}");
                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                column.Item().Text($"Заказов за квартал: {_quarterOrders}").FontSize(14);
                column.Item().Text($"Выручка за квартал: {_quarterRevenue:N2} ₽").FontSize(14);
                column.Item().Text($"Количество клиентов в базе: {_clientsCount}").FontSize(14);
            });

            page.Footer()
                .AlignCenter()
                .Text(x =>
                {
                    x.Span("Стр. ");
                    x.CurrentPageNumber();
                    x.Span(" из ");
                    x.TotalPages();
                });
        });
    }
}
