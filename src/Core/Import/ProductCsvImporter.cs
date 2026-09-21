using System.Globalization;
using Core.Dto;

namespace Core.Import;

public static class ProductCsvImporter
{
    private const char Separator = ';';

    public static ImportResult<DomainRecord> Load(string path)
    {
        var items = new List<DomainRecord>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            if (number == 1 && line.StartsWith("type", StringComparison.OrdinalIgnoreCase))
                continue;

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;
                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<DomainRecord>(items, errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(Separator, StringSplitOptions.TrimEntries);

        return parts switch
        {
            { Length: < 4 } => new ParseFailed($"очікую щонайменше 4 колонки, отримав {parts.Length}"),
            
            ["P", _, "", _] => new ParseFailed("назва товару порожня"),
            ["P", _, _, var priceStr] when !decimal.TryParse(priceStr, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price) || price < 0
                => new ParseFailed($"ціна '{priceStr}' не є коректним додатним числом"),
            ["P", var id, var name, var priceStr]
                => new ParseOk(new ProductDto(id, name, decimal.Parse(priceStr, NumberStyles.Number, CultureInfo.InvariantCulture))),
                
            ["C", _, "", _, _] => new ParseFailed("ім'я клієнта порожнє"),
            ["C", var cid, var cname, var email, var phone]
                => new ParseOk(new CustomerDto(cid, cname, email, string.IsNullOrWhiteSpace(phone) ? null : phone)),
            ["C", var cid, var cname, var email]
                => new ParseOk(new CustomerDto(cid, cname, email, null)),
                
            [var prefix, ..] when prefix != "P" && prefix != "C" 
                => new ParseFailed($"невідомий префікс запису '{prefix}'"),
                
            _ => new ParseFailed($"занадто багато колонок: {parts.Length}")
        };
    }

    private abstract record ParseOutcome;
    private sealed record ParseOk(DomainRecord Value) : ParseOutcome;
    private sealed record ParseFailed(string Reason) : ParseOutcome;
}
