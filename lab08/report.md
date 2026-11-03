# Лабораторна робота 8

Тема: async/await, скасування, доменні винятки, unit-тести Core.
Предметна область: замовлення. Виконано основне завдання.

## 1. Асинхронні операції

Додано контракт `IOrderLineImporter` та реалізацію `JsonOrderLineImporter`. Вони імпортують JSON-масив рядків до наявних замовлень. Читання файлу і десеріалізація асинхронні, оскільки очікують введення-виведення. `OrderService.ImportAsync` очікує імпортер, а Cli використовує top-level `await`. Імпортер передається сервісу параметром через ручне впровадження залежностей.

Фрагмент `LoadAsync`:

```csharp
ct.ThrowIfCancellationRequested();
ArgumentException.ThrowIfNullOrWhiteSpace(path);
await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
    FileShare.Read, 4096, useAsync: true);
var dtos = await JsonSerializer.DeserializeAsync<List<OrderLineImportDto?>>(
    stream, Options, cancellationToken: ct)
    ?? throw new DomainException($"Файл {Path.GetFileName(path)} не містить списку.");
```

У циклах перетворення та додавання рядків викликається `ct.ThrowIfCancellationRequested()`. Ланцюжок токена: Cli → `OrderService.ImportAsync` → `JsonOrderLineImporter.LoadAsync` → `JsonSerializer.DeserializeAsync`.

Доменні методи, сховища та звіти LINQ залишилися синхронними: обчислення в пам'яті не потребують штучного `Task.Run`, а переробка всього контракту сховища не потрібна для асинхронного імпорту.

Ключ ідемпотентності імпорту: замовлення + товар без урахування регістру. Повторні товари пропускаються. Для кожного доданого рядка сервіс піднімає одну подію `LineAdded`; для пропущеного або відхиленого рядка подія не піднімається. Скасування під час застосування не відкочує попередні успішні додавання.

## 2. Скасування

Cli створює `CancellationTokenSource`, обробник Ctrl+C сигналізує скасування, а токен передається до сервісу. За параметром `--cancel-after-ms N` задається таймаут. Значення 0 скасовує токен до початку імпорту, що дозволяє відтворити сценарій на малому файлі.

Фрагмент обробки Ctrl+C:

```csharp
ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; cts.Cancel(); };
Console.CancelKeyPress += handler;
try
{
    var added = await service.ImportAsync(new JsonOrderLineImporter(), path, cts.Token);
    Console.WriteLine($"Імпортовано рядків: {added}");
}
```

У `finally` обробник відписується. На верхньому рівні `OperationCanceledException` перехоплюється перед іншими винятками й виводиться `Імпорт скасовано.` без стектрейсу. Код завершення - 2.

## 3. Тести

Проєкт `tests/Core.Tests` використовує xUnit, має посилання тільки на Core. `TestData` містить власні дані; `SampleData` і каталог `data/` у тестах не використовуються. Кожен тест створює нове сховище. Єдиний тест файлової інтеграції створює унікальний тимчасовий файл і видаляє його в `finally`.

| Назва тесту | Що перевіряє | Очікуваний результат |
|---|---|---|
| AddLine_WithInvalidQuantity_ThrowsAndKeepsState | Кількість 0 та -5 (`Theory`) | ArgumentOutOfRangeException, незмінні рядки/сума, немає подій |
| AddLine_ToConfirmedOrder_ThrowsDomainExceptionAndKeepsState | Додавання до підтвердженого замовлення | DomainException, стан не змінено |
| AddLine_UpdatesTotalAndRaisesChangedExactlyOnce | Сума після додавання та дані події | 12,50 × 2 + 7 × 3 = 46; рівно одна LineAdded |
| Confirm_EmptyOrder_ThrowsDomainExceptionWithoutEvent | Підтвердження порожнього замовлення | DomainException, Draft, немає подій |
| ExpensiveOrders_CountsOnlyAboveThreshold | Пороги 0, 50, 200 (`Theory`) | Відповідно 3, 2, 0 замовлень |
| TopOrders_WithEqualTotals_SortsByIdRegardlessOfInputOrder | Детермінований топ при однаковій сумі | ORD-A, ORD-B для прямого та зворотного джерела |
| Totals_WithEmptySource_ReturnsZeros | Агрегати порожнього набору | Усі значення нульові |
| ImportAsync_AddsRowsRaisesEventsAndSkipsDuplicates | Імпорт, повтори, подія, передавання токена | Додано 1, повторно 0, одна подія, правильна сума та токен |
| ImportAsync_WhenAlreadyCancelled_ThrowsBeforeOpeningFile | Заздалегідь скасований токен у сервісі | OperationCanceledException, порожнє сховище |
| LoadAsync_WhenAlreadyCancelled_ThrowsBeforeOpeningFile | Скасування безпосередньо в імпортері | OperationCanceledException замість відкриття any.json |
| ImportAsync_WhenCancelledAfterFirstRow_StopsBeforeSecondRow | Перевірка токена в циклі застосування | Перший рядок додано, другий відсутній, одна подія |
| ImportAsync_ToConfirmedOrder_RejectsRowWithoutEvent | Бізнес-помилка під час імпорту | DomainException з номером запису, немає нових рядків і подій |
| LoadAsync_WithInvalidRecord_ReportsNumberAndPreservesInnerException | Поганий другий запис у тимчасовому файлі | DomainException «Запис 2», InnerException, незмінне замовлення |

Разом: 13 тестових методів, 16 сценаріїв з урахуванням `InlineData`.

## 4. Результати перевірки

Команди запускаються з кореня репозиторію:

```text
dotnet test
Passed!  - Failed: 0, Passed: 16, Skipped: 0, Total: 16

dotnet run --project src/Cli -f net10.0 -- import-lines
Імпортовано рядків: 3

dotnet run --project src/Cli -f net10.0 -- import-lines --cancel-after-ms 0
Імпорт скасовано.

dotnet run --project src/Cli -f net10.0 -- import-lines data/order-lines-invalid.json
Дані відхилено: Запис 2: Кількість має бути більшою за нуль (Parameter 'quantity')
Actual value was -1.
```

Також перевірено додатний таймаут 10 мс і сигнал Ctrl+C на тимчасовому файлі з 600 000 записів: обидва дали `Імпорт скасовано.` та код завершення 2. Тимчасовий файл видалено. Перевірено окремі повідомлення для відсутнього файлу, зіпсованого JSON і некоректного параметра таймауту. Попередній `report-sum` і далі показує 18 замовлень із сумою 8397,71 грн.

У вихідному коді Core, Cli і тестів немає `async void`, `.Result` і `.Wait()`. Тести пройшли двічі поспіль; збірка підтримує net8.0 і net10.0.

## 5. Види винятків

`DomainException` сигналізує про порушення бізнес-правила в поточному стані: наприклад, `Order.AddLine` не дозволяє змінювати підтверджене замовлення, а `Order.Confirm` не дозволяє підтверджувати порожнє. `ArgumentException` і його нащадки означають неправильний аргумент методу: порожній ID, від'ємну ціну, нульову або від'ємну кількість. На межі імпорту помилка аргументу із зовнішнього запису обгортається в `DomainException` із номером запису; первинна причина зберігається в `InnerException`. Синтаксично некоректний JSON залишається `JsonException`, файлові помилки - технічними винятками, а скасування - `OperationCanceledException`.

## Демонстрація на захисті

1. Запустити `dotnet test` і показати 16 успішних сценаріїв.
2. Відкрити `src/Core/Import/JsonOrderLineImporter.cs`: показати `await`, `useAsync: true`, перевірку токена й номер запису в помилці.
3. Показати передавання токена з `src/Cli/Program.cs` через `src/Core/Services/OrderService.cs` до імпортера.
4. Запустити звичайний імпорт, скасування через `--cancel-after-ms 0`, імпорт `data/order-lines-invalid.json`.
5. Відкрити `AddLine_UpdatesTotalAndRaisesChangedExactlyOnce`: подія перехоплюється списком у пам'яті, консоль і диск не потрібні.
