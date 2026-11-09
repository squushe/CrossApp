namespace Core.Storage;

using Core.Abstractions;
using Core.Configuration;

public static class StoreFactory
{
    public static IOrderStore Create(string[] args, string? configuredPath = null)
    {
        bool useFile = args.Contains("--file");
        string dataPath = configuredPath ?? new StorageOptions().ResolveFullPath(AppContext.BaseDirectory);

        IOrderStore store = useFile
            ? new FileOrderStore(dataPath)
            : new InMemoryOrderStore(SampleData.Orders());

        if (args.Contains("--cache"))
        {
            store = new CachingOrderStore(store);
        }

        return store;
    }
}
