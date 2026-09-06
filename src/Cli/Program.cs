using System.Runtime.InteropServices;
using System.Text.Json;

if (args.Contains("--json"))
{
    var envInfo = new
    {
        OSDescription = RuntimeInformation.OSDescription,
        EnvironmentOS = Environment.OSVersion.ToString(),
        ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
        NetVersion = Environment.Version.ToString(),
        Runtime = RuntimeInformation.FrameworkDescription,
        AppDirectory = AppContext.BaseDirectory,
        CurrentDirectory = Environment.CurrentDirectory
    };
    
    Console.WriteLine(JsonSerializer.Serialize(envInfo));
}
else
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine("Студент: Рожко Максим, група ФЕІ-32");
    Console.WriteLine(new string('-', 52));

    Console.WriteLine($"ОС (OSDescription) : {RuntimeInformation.OSDescription}");
    Console.WriteLine($"ОС (Environment) : {Environment.OSVersion}");
    Console.WriteLine($"Архітектура процесу : {RuntimeInformation.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR) : {Environment.Version}");
    Console.WriteLine($"Runtime : {RuntimeInformation.FrameworkDescription}");
    Console.WriteLine($"Каталог застосунку : {AppContext.BaseDirectory}");
    Console.WriteLine($"Поточний каталог : {Environment.CurrentDirectory}");

    Console.WriteLine(new string('-', 52));
    Console.WriteLine("Предметна область: Замовлення (клієнт, товар, замовлення, рядок замовлення)");
}