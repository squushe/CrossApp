using System.Text.Json;
using Core.Dto;

namespace Core.Import;

public static class ProductJsonImporter
{
    public static ImportResult<DomainRecord> Load(string path)
    {
        var errors = new List<string>();
        
        try
        {
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            
            var products = JsonSerializer.Deserialize<List<ProductDto>>(json, options) ?? new List<ProductDto>();
            
            var items = products.Cast<DomainRecord>().ToList();
            
            return new ImportResult<DomainRecord>(items, errors);
        }
        catch (Exception ex)
        {
            errors.Add($"Помилка читання JSON: {ex.Message}");
            return new ImportResult<DomainRecord>(new List<DomainRecord>(), errors);
        }
    }
}
