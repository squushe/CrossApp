using Core.Dto;
using Core.Import;

string path = args.Length > 0 ? args[0] : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

string extension = Path.GetExtension(path).ToLowerInvariant();

ImportResult<DomainRecord> result = extension switch
{
    ".csv" => ProductCsvImporter.Load(path),
    ".json" => ProductJsonImporter.Load(path),
    _ => new ImportResult<DomainRecord>(new List<DomainRecord>(), new List<string> { $"Непідтримуваний формат файлу: {extension}" })
};

int total = result.Items.Count + result.Errors.Count;
int accepted = result.Items.Count;
int skipped = result.Errors.Count;
double percentError = total == 0 ? 0 : (skipped * 100.0 / total);

Console.WriteLine($"Статистика імпорту: усього {total} / прийнято {accepted} / пропущено {skipped} / помилок {percentError:F1}%");

Console.WriteLine("\nПерші завантажені записи:");
foreach (DomainRecord item in result.Items.Take(5))
{
    string print = item switch
    {
        ProductDto p => $"[Товар]   {p.Id,-6} {p.Name,-30} {p.Price,8:F2}",
        CustomerDto c => $"[Клієнт]  {c.Id,-6} {c.Name,-30} {c.Email}",
        _ => item.ToString()!
    };
    Console.WriteLine($" {print}");
}

if (result.Errors.Count > 0)
{
    Console.WriteLine($"\nПропущено рядків: {result.Errors.Count}");
    foreach (string e in result.Errors)
        Console.WriteLine($" ! {e}");
}

return 0;