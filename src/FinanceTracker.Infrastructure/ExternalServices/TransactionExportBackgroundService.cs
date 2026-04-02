using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;
using FinanceTracker.Application.Common;
using FinanceTracker.Application.Interfaces;
using FinanceTracker.Contracts;
using FinanceTracker.Domain.Entities;
using FinanceTracker.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FinanceTracker.Infrastructure.ExternalServices;

public class TransactionExportBackgroundService : BackgroundService, ITransactionExportService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Channel<ExportJobWorkItem> _queue = Channel.CreateUnbounded<ExportJobWorkItem>();
    private readonly ConcurrentDictionary<Guid, ExportJobState> _jobs = new();

    static TransactionExportBackgroundService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public TransactionExportBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public bool IsMoreThanOneMonth(TransactionExportRequestDto request)
    {
        return TryResolvePeriod(request, out _, out _, out var monthCount, out _) && monthCount > 1;
    }

    public async Task<Result<FileExportDto>> ExportDirectAsync(string userId, TransactionExportRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<FileExportDto>.Failure("User not authenticated.");

        if (!TryValidateFormat(request.Format, out var formatError))
            return Result<FileExportDto>.Failure(formatError!);

        if (!TryResolvePeriod(request, out var fromDate, out var toDate, out var monthCount, out var periodError))
            return Result<FileExportDto>.Failure(periodError!);

        if (monthCount > 1)
            return Result<FileExportDto>.Failure("Exports larger than one month must be processed as a background job.");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var transactions = await LoadTransactionsAsync(db, userId, fromDate, toDate, ct);

        var file = GenerateFile(transactions, request.Format, fromDate, toDate);
        return Result<FileExportDto>.Success(file);
    }

    public async Task<Result<ExportJobCreatedDto>> QueueExportAsync(string userId, TransactionExportRequestDto request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Result<ExportJobCreatedDto>.Failure("User not authenticated.");

        if (!TryValidateFormat(request.Format, out var formatError))
            return Result<ExportJobCreatedDto>.Failure(formatError!);

        if (!TryResolvePeriod(request, out var fromDate, out var toDate, out var monthCount, out var periodError))
            return Result<ExportJobCreatedDto>.Failure(periodError!);

        if (monthCount <= 1)
            return Result<ExportJobCreatedDto>.Failure("Use direct export for one month or less.");

        var jobId = Guid.NewGuid();
        var state = new ExportJobState
        {
            JobId = jobId,
            UserId = userId,
            Request = request,
            Status = ExportJobStatus.Queued,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        _jobs[jobId] = state;
        await _queue.Writer.WriteAsync(new ExportJobWorkItem(jobId, userId), ct);

        return Result<ExportJobCreatedDto>.Success(
            new ExportJobCreatedDto(jobId, "Queued", "Export job queued."));
    }

    public Task<Result<ExportJobStatusDto>> GetJobStatusAsync(string userId, Guid jobId, CancellationToken ct)
    {
        if (!_jobs.TryGetValue(jobId, out var state) || state.UserId != userId)
            return Task.FromResult(Result<ExportJobStatusDto>.Failure("Export job not found."));

        var dto = new ExportJobStatusDto(
            state.JobId,
            state.Status.ToString(),
            state.Progress,
            state.FileName,
            state.Error,
            state.Status == ExportJobStatus.Completed && state.Content is not null);

        return Task.FromResult(Result<ExportJobStatusDto>.Success(dto));
    }

    public Task<Result<FileExportDto>> DownloadJobAsync(string userId, Guid jobId, CancellationToken ct)
    {
        if (!_jobs.TryGetValue(jobId, out var state) || state.UserId != userId)
            return Task.FromResult(Result<FileExportDto>.Failure("Export job not found."));

        if (state.Status != ExportJobStatus.Completed || state.Content is null || state.ContentType is null || state.FileName is null)
            return Task.FromResult(Result<FileExportDto>.Failure("Export file is not ready yet."));

        return Task.FromResult(Result<FileExportDto>.Success(
            new FileExportDto(state.Content, state.ContentType, state.FileName)));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var workItem in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            if (!_jobs.TryGetValue(workItem.JobId, out var state))
                continue;

            try
            {
                state.Status = ExportJobStatus.Running;
                state.Progress = 10;

                if (!TryResolvePeriod(state.Request, out var fromDate, out var toDate, out _, out var periodError))
                    throw new InvalidOperationException(periodError ?? "Invalid export period.");

                await using var scope = _scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                state.Progress = 35;
                var transactions = await LoadTransactionsAsync(db, state.UserId, fromDate, toDate, stoppingToken);

                state.Progress = 80;
                var file = GenerateFile(transactions, state.Request.Format, fromDate, toDate);

                state.Content = file.Content;
                state.ContentType = file.ContentType;
                state.FileName = file.FileName;
                state.Status = ExportJobStatus.Completed;
                state.Progress = 100;
                state.CompletedAt = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                state.Status = ExportJobStatus.Failed;
                state.Progress = 100;
                state.Error = ex.Message;
                state.CompletedAt = DateTime.UtcNow;
            }
        }
    }

    private static async Task<List<Transaction>> LoadTransactionsAsync(
        AppDbContext db,
        string userId,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken ct)
    {
        var toExclusive = toDate.Date.AddDays(1);

        return await db.Transactions
            .Where(t => t.UserId == userId && t.Date >= fromDate && t.Date < toExclusive)
            .OrderBy(t => t.Date)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    private static FileExportDto GenerateFile(List<Transaction> transactions, string format, DateTime fromDate, DateTime toDate)
    {
        var normalizedFormat = format.Trim().ToLowerInvariant();
        var periodPart = fromDate.Month == toDate.Month && fromDate.Year == toDate.Year
            ? $"{fromDate:yyyy-MM}"
            : $"{fromDate:yyyy-MM}_to_{toDate:yyyy-MM}";

        return normalizedFormat switch
        {
            "csv" => new FileExportDto(
                GenerateCsv(transactions),
                "text/csv",
                $"transactions_{periodPart}.csv"),
            "pdf" => new FileExportDto(
                GeneratePdf(transactions, fromDate, toDate),
                "application/pdf",
                $"transactions_{periodPart}.pdf"),
            _ => throw new InvalidOperationException("Unsupported export format.")
        };
    }

    private static byte[] GenerateCsv(List<Transaction> transactions)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date,Type,Category,Description,Amount,Currency,BaseAmount,Notes");

        foreach (var t in transactions)
        {
            sb.AppendLine(string.Join(",",
                Csv(t.Date.ToString("yyyy-MM-dd")),
                Csv(t.Type.ToString()),
                Csv(t.Category.ToString()),
                Csv(t.Description),
                Csv(t.Amount.ToString("0.00")),
                Csv(t.Currency),
                Csv(t.AmountInBaseCurrency.ToString("0.00")),
                Csv(t.Notes ?? string.Empty)));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] GeneratePdf(List<Transaction> transactions, DateTime fromDate, DateTime toDate)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header()
                    .Column(column =>
                    {
                        column.Item().Text("Transaction Export").Bold().FontSize(16);
                        column.Item().Text($"Period: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}").FontSize(10).FontColor(Colors.Grey.Darken2);
                        column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC").FontSize(9).FontColor(Colors.Grey.Darken1);
                    });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(70);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(95);
                        columns.RelativeColumn(2);
                        columns.ConstantColumn(75);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(75);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Date").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Type").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Category").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Description").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Amount").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Currency").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Base").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Notes").SemiBold();
                    });

                    foreach (var t in transactions)
                    {
                        BodyCell(table, t.Date.ToString("yyyy-MM-dd"));
                        BodyCell(table, t.Type.ToString());
                        BodyCell(table, t.Category.ToString());
                        BodyCell(table, t.Description);
                        BodyCell(table, t.Amount.ToString("N2"));
                        BodyCell(table, t.Currency);
                        BodyCell(table, t.AmountInBaseCurrency.ToString("N2"));
                        BodyCell(table, t.Notes ?? "-");
                    }
                });
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container
            .Background(Colors.Grey.Lighten2)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Padding(4);
    }

    private static void BodyCell(TableDescriptor table, string text)
    {
        table.Cell()
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3)
            .PaddingHorizontal(4)
            .Text(text);
    }

    private static string Csv(string value)
    {
        var escaped = value.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static bool TryValidateFormat(string format, out string? error)
    {
        var normalized = format.Trim().ToLowerInvariant();
        if (normalized is "csv" or "pdf")
        {
            error = null;
            return true;
        }

        error = "Only csv and pdf export formats are supported.";
        return false;
    }

    private static bool TryResolvePeriod(
        TransactionExportRequestDto request,
        out DateTime fromDate,
        out DateTime toDate,
        out int monthCount,
        out string? error)
    {
        fromDate = default;
        toDate = default;
        monthCount = 0;

        var hasSingleMonth = request.Month.HasValue || request.Year.HasValue;
        var hasRange = request.FromMonth.HasValue || request.FromYear.HasValue || request.ToMonth.HasValue || request.ToYear.HasValue;

        if (hasSingleMonth && hasRange)
        {
            error = "Provide either month/year or from/to period, not both.";
            return false;
        }

        if (request.Month.HasValue || request.Year.HasValue)
        {
            if (!request.Month.HasValue || !request.Year.HasValue)
            {
                error = "Both month and year must be provided.";
                return false;
            }

            if (!IsValidMonth(request.Month.Value) || !IsValidYear(request.Year.Value))
            {
                error = "Invalid month/year values.";
                return false;
            }

            fromDate = new DateTime(request.Year.Value, request.Month.Value, 1);
            toDate = fromDate.AddMonths(1).AddDays(-1);
            monthCount = 1;
            error = null;
            return true;
        }

        if (!request.FromMonth.HasValue || !request.FromYear.HasValue || !request.ToMonth.HasValue || !request.ToYear.HasValue)
        {
            error = "For multi-month exports, fromMonth/fromYear/toMonth/toYear are required.";
            return false;
        }

        if (!IsValidMonth(request.FromMonth.Value) || !IsValidMonth(request.ToMonth.Value)
            || !IsValidYear(request.FromYear.Value) || !IsValidYear(request.ToYear.Value))
        {
            error = "Invalid period values.";
            return false;
        }

        fromDate = new DateTime(request.FromYear.Value, request.FromMonth.Value, 1);
        toDate = new DateTime(request.ToYear.Value, request.ToMonth.Value, 1).AddMonths(1).AddDays(-1);

        if (toDate < fromDate)
        {
            error = "The end period must be after the start period.";
            return false;
        }

        monthCount = ((request.ToYear.Value - request.FromYear.Value) * 12)
            + (request.ToMonth.Value - request.FromMonth.Value) + 1;

        error = null;
        return true;
    }

    private static bool IsValidMonth(int month) => month is >= 1 and <= 12;

    private static bool IsValidYear(int year) => year is >= 2000 and <= 2100;

    private enum ExportJobStatus
    {
        Queued,
        Running,
        Completed,
        Failed
    }

    private sealed class ExportJobState
    {
        public Guid JobId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public TransactionExportRequestDto Request { get; set; } =
            new("csv", null, null, null, null, null, null);
        public ExportJobStatus Status { get; set; }
        public int Progress { get; set; }
        public string? Error { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public byte[]? Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    private sealed record ExportJobWorkItem(Guid JobId, string UserId);
}
