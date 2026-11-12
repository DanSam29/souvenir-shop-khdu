using System.Globalization;
using System.Text;
using CsvHelper;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Layout;
using iText.Layout.Element;
using KhduSouvenirShop.API.DTOs.Responses;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;

namespace KhduSouvenirShop.API.Services
{
    /// <summary>
    /// Готовий файл звіту: вміст, MIME-тип і ім'я файлу.
    /// </summary>
    public record ReportFile(byte[] Content, string ContentType, string FileName);

    /// <summary>
    /// Експорт аналітичних звітів у CSV / XLSX / PDF.
    /// </summary>
    public interface IReportExportService
    {
        Task<ReportFile> ExportAsync(string type, string format, DateTime? from = null, DateTime? to = null);
    }

    public class ReportExportService : IReportExportService
    {
        private readonly IAnalyticsService _analyticsService;

        public ReportExportService(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        public async Task<ReportFile> ExportAsync(string type, string format, DateTime? from = null, DateTime? to = null)
        {
            var data = await BuildReportAsync(type, from, to);
            var stamp = DateTime.Now.ToString("yyyyMMdd");

            return format switch
            {
                "csv" => new ReportFile(BuildCsv(data), "text/csv", $"{data.FileSlug}_report_{stamp}.csv"),
                "xlsx" => new ReportFile(BuildXlsx(data),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"{data.FileSlug}_report_{stamp}.xlsx"),
                _ => new ReportFile(BuildPdf(data), "application/pdf", $"{data.FileSlug}_report_{stamp}.pdf")
            };
        }

        // Формування звіту відповідного типу (бере дані з кешованого AnalyticsService)
        private async Task<ReportData> BuildReportAsync(string type, DateTime? from, DateTime? to)
        {
            switch (type)
            {
                case "sales":
                {
                    var series = await _analyticsService.GetTimeSeriesAsync(from, to);
                    var rows = series.SalesByDay
                        .Select(d => new List<object?> { d.Date, d.Count, d.Amount })
                        .ToList();
                    return new ReportData
                    {
                        FileSlug = "sales",
                        Title = "Звіт про продажі / Sales report",
                        Columns = new()
                        {
                            new ReportColumn("Дата / Date"),
                            new ReportColumn("Замовлень / Orders", "0"),
                            new ReportColumn("Виручка / Revenue, грн", "#,##0.00")
                        },
                        Rows = rows,
                        Totals = rows.Count > 0
                            ? new List<object?> { "Разом / Total", rows.Sum(r => (int)r[1]!), rows.Sum(r => (decimal)r[2]!) }
                            : null,
                        Chart = new ChartSpec("Продажі за період / Sales", 0, 2, Line: true)
                    };
                }
                case "products":
                {
                    var products = await _analyticsService.GetTopProductsAsync(10, from, to);
                    var rows = products
                        .Select(p => new List<object?> { p.ProductId, p.Name, p.Quantity, p.Revenue })
                        .ToList();
                    return new ReportData
                    {
                        FileSlug = "products",
                        Title = "Топ товарів / Top products",
                        Columns = new()
                        {
                            new ReportColumn("ID", "0"),
                            new ReportColumn("Товар / Product"),
                            new ReportColumn("Продано / Sold", "0"),
                            new ReportColumn("Виручка / Revenue, грн", "#,##0.00")
                        },
                        Rows = rows,
                        Totals = rows.Count > 0
                            ? new List<object?> { "Разом / Total", string.Empty, rows.Sum(r => (int)r[2]!), rows.Sum(r => (decimal)r[3]!) }
                            : null,
                        Chart = new ChartSpec("Топ товарів за виручкою / Products by revenue", 1, 3, Line: false)
                    };
                }
                case "promotions":
                {
                    var promotions = await _analyticsService.GetPromotionStatsAsync(from, to);
                    var rows = promotions
                        .Select(p => new List<object?> { p.PromotionId, p.Name, p.Uses, p.TotalDiscountAmount, p.Revenue, p.ROI })
                        .ToList();
                    return new ReportData
                    {
                        FileSlug = "promotions",
                        Title = "Ефективність акцій / Promotions",
                        Columns = new()
                        {
                            new ReportColumn("ID", "0"),
                            new ReportColumn("Акція / Promotion"),
                            new ReportColumn("Використань / Uses", "0"),
                            new ReportColumn("Знижка / Discount, грн", "#,##0.00"),
                            new ReportColumn("Виручка / Revenue, грн", "#,##0.00"),
                            new ReportColumn("ROI", "0.00")
                        },
                        Rows = rows,
                        Totals = rows.Count > 0
                            ? new List<object?>
                            {
                                "Разом / Total", string.Empty,
                                rows.Sum(r => (int)r[2]!),
                                rows.Sum(r => (decimal)r[3]!),
                                rows.Sum(r => (decimal)r[4]!),
                                string.Empty
                            }
                            : null,
                        Chart = new ChartSpec("Виручка за акціями / Revenue by promotion", 1, 4, Line: false)
                    };
                }
                default:
                {
                    var financial = await _analyticsService.GetFinancialAsync(from, to);
                    return new ReportData
                    {
                        FileSlug = "financial",
                        Title = "Фінансовий звіт / Financial report",
                        Columns = new()
                        {
                            new ReportColumn("Показник / Metric"),
                            new ReportColumn("Значення / Value", "#,##0.00")
                        },
                        Rows = new List<List<object?>>
                        {
                            new() { "Виручка / Revenue, грн", financial.TotalRevenue },
                            new() { "Собівартість / Purchase cost, грн", financial.TotalPurchaseCost },
                            new() { "Валовий прибуток / Gross profit, грн", financial.GrossProfit },
                            new() { "Маржа / Margin, %", financial.Margin * 100m }
                        }
                    };
                }
            }
        }

        // ======================= CSV (CsvHelper) =======================

        private static byte[] BuildCsv(ReportData data)
        {
            using var writer = new StringWriter();
            var options = CsvWriterOptions.Default with { FormatProvider = CultureInfo.InvariantCulture };
            using (var csv = CsvWriter.Create(writer, options, true))
            {
                foreach (var column in data.Columns)
                    csv.WriteField(column.Title);
                csv.EndRecord();

                foreach (var row in data.Rows)
                {
                    foreach (var cell in row)
                        csv.WriteField(CellText(cell));
                    csv.EndRecord();
                }

                if (data.Totals != null)
                {
                    foreach (var cell in data.Totals)
                        csv.WriteField(CellText(cell));
                    csv.EndRecord();
                }
            }

            // BOM, щоб Excel коректно відкривав кирилицю в UTF-8
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(writer.ToString())).ToArray();
        }

        // ======================= Excel XLSX (EPPlus) =======================

        private static byte[] BuildXlsx(ReportData data)
        {
            ExcelPackage.License.SetNonCommercialOrganization("KhduSouvenirShop");
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Report");

            sheet.Cells[1, 1].Value = data.Title;
            sheet.Cells[1, 1].Style.Font.Bold = true;
            sheet.Cells[1, 1].Style.Font.Size = 14;

            const int headerRow = 3;
            for (var c = 0; c < data.Columns.Count; c++)
            {
                var cell = sheet.Cells[headerRow, c + 1];
                cell.Value = data.Columns[c].Title;
                cell.Style.Font.Bold = true;
            }

            var row = headerRow + 1;
            foreach (var dataRow in data.Rows)
            {
                for (var c = 0; c < dataRow.Count; c++)
                    SetCell(sheet.Cells[row, c + 1], dataRow[c], data.Columns[c].NumberFormat);
                row++;
            }

            if (data.Totals != null)
            {
                for (var c = 0; c < data.Totals.Count; c++)
                {
                    var cell = sheet.Cells[row, c + 1];
                    SetCell(cell, data.Totals[c], data.Columns[c].NumberFormat);
                    cell.Style.Font.Bold = true;
                }
                row++;
            }

            sheet.Cells.AutoFitColumns();

            if (data.Chart != null && data.Rows.Count > 1)
                AddXlsxChart(sheet, data, headerRow, row);

            return package.GetAsByteArray();
        }

        private static void SetCell(ExcelRange cell, object? value, string? numberFormat)
        {
            switch (value)
            {
                case int i:
                    cell.Value = i;
                    break;
                case decimal d:
                    cell.Value = d;
                    break;
                case string s:
                    cell.Value = s;
                    break;
                case null:
                    break;
                default:
                    cell.Value = value.ToString();
                    break;
            }

            if (numberFormat != null && value is not null and not string)
                cell.Style.Numberformat.Format = numberFormat;
        }

        private static void AddXlsxChart(ExcelWorksheet sheet, ReportData data, int headerRow, int lastRow)
        {
            var spec = data.Chart!;
            var firstData = headerRow + 1;
            var lastData = headerRow + data.Rows.Count;

            var chart = spec.Line
                ? sheet.Drawings.AddChart("reportChart", eChartType.Line)
                : sheet.Drawings.AddChart("reportChart", eChartType.ColumnClustered);
            chart.Title.Text = spec.Title;
            chart.SetPosition(lastRow + 1, 0, 0, 0);
            chart.SetSize(720, 320);

            var values = sheet.Cells[firstData, spec.ValueColumn + 1, lastData, spec.ValueColumn + 1];
            var categories = sheet.Cells[firstData, spec.CategoryColumn + 1, lastData, spec.CategoryColumn + 1];
            chart.Series.Add(values, categories);
        }

        // ======================= PDF (iText7) =======================

        private static byte[] BuildPdf(ReportData data)
        {
            using var stream = new MemoryStream();
            using (var writer = new PdfWriter(stream))
            using (var pdf = new PdfDocument(writer))
            {
                var font = ResolveFont();
                using (var document = new Document(pdf))
                {
                    document.Add(new Paragraph(data.Title).SetFont(font).SetFontSize(16));
                    document.Add(new Paragraph($"Сформовано / Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").SetFont(font).SetFontSize(9));
                    document.Add(new Paragraph(" ").SetFont(font).SetFontSize(9));

                    var table = new Table(Enumerable.Repeat(1f, data.Columns.Count).ToArray()).UseAllAvailableWidth();
                    foreach (var column in data.Columns)
                        table.AddHeaderCell(new Cell().Add(new Paragraph(column.Title).SetFont(font).SetFontSize(9).SimulateBold()));
                    foreach (var row in data.Rows)
                        foreach (var cell in row)
                            table.AddCell(new Cell().Add(new Paragraph(CellText(cell)).SetFont(font).SetFontSize(8)));
                    if (data.Totals != null)
                        foreach (var cell in data.Totals)
                            table.AddCell(new Cell().Add(new Paragraph(CellText(cell)).SetFont(font).SetFontSize(8).SimulateBold()));
                    document.Add(table);
                }

                if (data.Chart != null && data.Rows.Count > 1)
                    DrawBarChart(pdf, font, data);
            }
            return stream.ToArray();
        }

        // Простий стовпчиковий графік, намальований на окремій сторінці PDF
        private static void DrawBarChart(PdfDocument pdf, PdfFont font, ReportData data)
        {
            var spec = data.Chart!;
            var page = pdf.AddNewPage();
            var canvas = new PdfCanvas(page);
            var size = page.GetPageSize();

            const float margin = 50f;
            var left = margin;
            var right = size.GetWidth() - margin;
            var axisBottom = margin + 30f;
            var axisTop = size.GetHeight() - margin - 40f;

            canvas.BeginText().SetFontAndSize(font, 12).MoveText(left, size.GetHeight() - margin).ShowText(spec.Title).EndText();
            canvas.SetLineWidth(0.5f).MoveTo(left, axisBottom).LineTo(right, axisBottom).Stroke();

            var values = data.Rows.Select(r => ToDecimal(r[spec.ValueColumn])).ToList();
            var max = values.Max();
            if (max > 0)
            {
                var slot = (right - left) / values.Count;
                var barWidth = MathF.Min(slot * 0.6f, 45f);
                canvas.SetFillColor(new DeviceRgb(22, 93, 255));

                for (var i = 0; i < values.Count; i++)
                {
                    var height = (float)(values[i] / max) * (axisTop - axisBottom);
                    var x = left + slot * i + (slot - barWidth) / 2f;
                    canvas.Rectangle(x, axisBottom, barWidth, height).Fill();

                    var label = Truncate(data.Rows[i][spec.CategoryColumn]?.ToString() ?? string.Empty, 10);
                    canvas.BeginText().SetFontAndSize(font, 7).MoveText(x, axisBottom - 12f).ShowText(label).EndText();
                }

                canvas.BeginText().SetFontAndSize(font, 8).MoveText(left, axisTop + 4f)
                    .ShowText($"max: {max.ToString("0.##", CultureInfo.InvariantCulture)}").EndText();
            }

            canvas.Release();
        }

        // Підбір шрифту з підтримкою кирилиці для PDF-звітів
        private static PdfFont ResolveFont()
        {
            var windowsFont = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
            var candidates = new[]
            {
                windowsFont,
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                "/usr/share/fonts/dejavu/DejaVuSans.ttf",
                "/System/Library/Fonts/Supplemental/Arial.ttf"
            };
            foreach (var path in candidates)
            {
                if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
                {
                    try
                    {
                        return PdfFontFactory.CreateFont(path, PdfEncodings.IDENTITY_H, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                    }
                    catch
                    {
                        // спробувати наступний шрифт
                    }
                }
            }
            return PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        }

        private static string CellText(object? value)
            => value switch
            {
                null => string.Empty,
                int i => i.ToString(CultureInfo.InvariantCulture),
                decimal d => d.ToString("0.##", CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty
            };

        private static decimal ToDecimal(object? value)
            => value switch
            {
                decimal d => d,
                int i => i,
                double db => (decimal)db,
                _ => decimal.TryParse(value?.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0m
            };

        private static string Truncate(string value, int max)
            => value.Length <= max ? value : value.Substring(0, max - 3) + "...";

        // Внутрішня модель звіту: спільна для трьох форматерів
        private sealed class ReportData
        {
            public string FileSlug { get; init; } = string.Empty;
            public string Title { get; init; } = string.Empty;
            public List<ReportColumn> Columns { get; init; } = new();
            public List<List<object?>> Rows { get; init; } = new();
            public List<object?>? Totals { get; init; }
            public ChartSpec? Chart { get; init; }
        }

        private sealed record ReportColumn(string Title, string? NumberFormat = null);

        private sealed record ChartSpec(string Title, int CategoryColumn, int ValueColumn, bool Line);
    }
}
