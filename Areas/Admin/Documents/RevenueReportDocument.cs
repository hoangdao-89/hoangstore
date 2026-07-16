using hoangstore.Areas.Admin.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace hoangstore.Areas.Admin.Documents
{
    public class RevenueReportDocument : IDocument
    {
        private readonly RevenueReportViewModel _model;

        public RevenueReportDocument(RevenueReportViewModel model)
        {
            _model = model;
        }

        public DocumentMetadata GetMetadata()
        {
            return DocumentMetadata.Default;
        }

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));
                page.Header().Element(ComposeHeader);
                page.Content().PaddingVertical(15).Element(ComposeContent);
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Trang ");
                    x.CurrentPageNumber();
                    x.Span("/");
                    x.TotalPages();
                });
            });
        }

        private void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("HOANGSTORE").Bold().FontSize(20);
                    column.Item().Text("BÁO CÁO DOANH THU").Bold().FontSize(15);
                    column.Item().Text($"Từ {_model.FromDate:dd/MM/yyyy} đến {_model.ToDate:dd/MM/yyyy}");
                });

                row.ConstantItem(150).AlignRight().Column(column =>
                {
                    column.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}");
                    column.Item().Text("Đơn vị: VNĐ");
                });
            });
        }

        private void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(15);
                column.Item().Element(ComposeSummary);
                column.Item().Text("DOANH THU THEO NGÀY").Bold().FontSize(12);
                column.Item().Element(ComposeDailyTable);
                column.Item().Text("SẢN PHẨM BÁN CHẠY").Bold().FontSize(12);
                column.Item().Element(ComposeTopProductTable);
            });
        }

        private void ComposeSummary(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                SummaryCell(table, "Đơn đã giao", _model.DeliveredOrderCount.ToString());
                SummaryCell(table, "Tổng doanh thu", $"{_model.TotalRevenue:N0} đ");
                SummaryCell(table, "Giá trị đơn trung bình", $"{_model.AverageOrderValue:N0} đ");
            });
        }

        private static void SummaryCell(TableDescriptor table, string title, string value)
        {
            table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
            {
                column.Item().Text(title).FontColor(Colors.Grey.Darken1);
                column.Item().Text(value).Bold().FontSize(13);
            });
        }

        private void ComposeDailyTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(35);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                HeaderCell(table, "STT");
                HeaderCell(table, "Ngày");
                HeaderCell(table, "Số đơn");
                HeaderCell(table, "Doanh thu");

                var index = 1;
                foreach (var item in _model.RevenueByDate)
                {
                    BodyCell(table, index++.ToString());
                    BodyCell(table, item.Date.ToString("dd/MM/yyyy"));
                    BodyCell(table, item.OrderCount.ToString());
                    BodyCell(table, $"{item.Revenue:N0} đ");
                }

                if (!_model.RevenueByDate.Any())
                {
                    table.Cell().ColumnSpan(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8).AlignCenter().Text("Không có dữ liệu.");
                }
            });
        }

        private void ComposeTopProductTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(35);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                HeaderCell(table, "STT");
                HeaderCell(table, "Sản phẩm");
                HeaderCell(table, "Đã bán");
                HeaderCell(table, "Doanh thu");

                var index = 1;
                foreach (var item in _model.TopSellingProducts)
                {
                    BodyCell(table, index++.ToString());
                    BodyCell(table, item.ProductName);
                    BodyCell(table, item.QuantitySold.ToString());
                    BodyCell(table, $"{item.Revenue:N0} đ");
                }

                if (!_model.TopSellingProducts.Any())
                {
                    table.Cell().ColumnSpan(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8).AlignCenter().Text("Không có dữ liệu.");
                }
            });
        }

        private static void HeaderCell(TableDescriptor table, string text)
        {
            table.Cell().Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(7).Text(text).Bold();
        }

        private static void BodyCell(TableDescriptor table, string text)
        {
            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(7).Text(text);
        }
    }
}
