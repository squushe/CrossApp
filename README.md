# CrossApp
 Наскрізний проєкт з крос-платформного програмування.
 Предметна область: Замовлення. Сутності: Customer, Product, Order, OrderLine.
 Призначення: оформлення замовлень і підрахунок сум.

## Структура solution
CrossApp/
├── CrossApp.slnx
├── README.md
├── .gitignore
└── src/
    ├── Core/
    │   ├── Core.csproj
    │   └── EnvironmentInfo.cs
    └── Cli/
        ├── Cli.csproj
        └── Program.cs

## Build

Для збірки проєкту використовується команда:

dotnet build

## Run

Для запуску консольного застосунку:

dotnet run --project src/Cli

Програма виводить інформацію про операційну систему, Runtime, архітектуру процесу, RID та каталог застосунку.

## Publish
### Self-contained

Публікація застосунку разом із необхідним .NET Runtime:

dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true -p:PublishDir=bin/publish-self-contained/

### Framework-dependent

Публікація застосунку без .NET Runtime:

dotnet publish src/Cli -c Release -r osx-arm64 --self-contained false -p:PublishDir=bin/publish-framework/

### Single-file

Публікація self-contained застосунку в одному виконуваному файлі:

dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:PublishDir=bin/publish-single-file/

У результаті каталог publish містить один основний виконуваний файл. Після публікації необхідно перевірити кількість файлів, розмір каталогу та можливість запуску застосунку.

### Trimmed

Публікація self-contained застосунку з trimming:

dotnet publish src/Cli -c Release -r osx-arm64 --self-contained true -p:PublishTrimmed=true -p:PublishDir=bin/publish-trimmed/

Trimming видаляє код, який аналізатор вважає невикористовуваним, що дозволяє зменшити розмір застосунку.

## Середовище
.NET SDK 10.0
macOS 26.6
Архітектура: ARM64
RID: osx-arm64
## Результати publish

| RID | Режим | Розмір publish | Потрібен runtime |
|---|---|---:|---|
| osx-arm64 | self-contained | 83 MB | ні |
| osx-arm64 | framework-dependent | 172 KB | так (.NET 10) |
| osx-arm64 | single-file | 76 MB | ні |
| osx-arm64 | trimmed | 20 MB | ні |


Self-contained версія має більший розмір, оскільки містить необхідний .NET Runtime і тому не потребує його попереднього встановлення.

Framework-dependent версія має значно менший розмір, але для її запуску необхідний встановлений .NET Runtime відповідної версії.

Single-file дозволяє об'єднати застосунок в один виконуваний файл, що спрощує його поширення.

Trimming дозволяє зменшити розмір застосунку шляхом видалення невикористовуваного коду.

## Multi-targeting

Бібліотека Core підтримує цільові платформи net8.0 та net10.0.

У Core.csproj використовується:

<TargetFrameworks>net8.0;net10.0</TargetFrameworks>

Для визначення версії збірки використовується умовна компіляція:

if NET10_0_OR_GREATER
const string BuildNote = "збірка під net10.0";
else
const string BuildNote = "збірка під net8.0";
endif