namespace Core.Storage;

using Core.Abstractions;

public static class StoreFactory
{
    public static IOrderStore Create(string[] args)
    {
        bool useFile = args.Contains("--file");
        string dataPath = Path.Combine(AppContext.BaseDirectory, "data", "catalog.json");

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
