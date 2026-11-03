# Лабораторна робота 8

Тема: async/await, скасування, доменні винятки, unit-тести Core.
Предметна область: замовлення. Виконано основне та всі чотири додаткові завдання.

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

Проєкт `tests/Core.Tests` використовує xUnit, має посилання тільки на Core. `TestData` містить власні дані; `SampleData` і каталог `data/` у тестах не використовуються. Кожен тест створює нове сховище. Два тести файлової інтеграції створюють унікальні тимчасові файли й видаляють їх у `finally`.

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

Основне завдання: 13 тестових методів, 16 сценаріїв з урахуванням `InlineData`. Разом із додатковими завданнями: 21 метод, 25 сценаріїв.

## 4. Результати перевірки

Команди запускаються з кореня репозиторію:

```text
dotnet test
Passed!  - Failed: 0, Passed: 25, Skipped: 0, Total: 25

dotnet run --project src/Cli -f net10.0 -- import-lines
Імпортовано рядків: 3

dotnet run --project src/Cli -f net10.0 -- import-lines --cancel-after-ms 0
Джерело скасування: таймаут.
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

1. Запустити `dotnet test` і показати 25 успішних сценаріїв.
2. Відкрити `src/Core/Import/JsonOrderLineImporter.cs`: показати `await`, `useAsync: true`, перевірку токена й номер запису в помилці.
3. Показати передавання токена з `src/Cli/Program.cs` через `src/Core/Services/OrderService.cs` до імпортера.
4. Запустити звичайний імпорт, скасування через `--cancel-after-ms 0`, імпорт `data/order-lines-invalid.json`.
5. Відкрити `AddLine_UpdatesTotalAndRaisesChangedExactlyOnce`: подія перехоплюється списком у пам'яті, консоль і диск не потрібні.


## 6. Додаткові завдання

### 6.1. Потоковий імпорт

`IStreamingOrderLineImporter.ReadAsync` повертає `IAsyncEnumerable<ImportedOrderLine>`. Реалізація читає масив через `JsonSerializer.DeserializeAsyncEnumerable`, перетворює кожен DTO і повертає `yield return`. На параметрі токена стоїть `[EnumeratorCancellation]`: завдяки цьому токен, переданий через `.WithCancellation(ct)`, також враховується ітератором.

`OrderService.ImportStreamingAsync` застосовує кожен отриманий рядок відразу через `await foreach`. Масив вхідних DTO не матеріалізується. Самі додані рядки замовлень залишаються в пам'яті сховища. При пізнішій помилці або скасуванні попередні успішні зміни залишаються; транзакційного відкочування немає. Звичайний `ImportAsync` зберігає попередню перевірку всього файлу.

```sh
dotnet run --project src/Cli -f net10.0 -- import-lines --stream --progress
```

### 6.2. Зв'язані токени

```csharp
using var userCts = new CancellationTokenSource();
using var timeoutCts = new CancellationTokenSource();
// Ctrl+C викликає userCts.Cancel(), таймер - timeoutCts.CancelAfter(...).
using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
    userCts.Token, timeoutCts.Token);
```

Обом режимам імпорту передається `linkedCts.Token`. Після `OperationCanceledException` Cli перевіряє початкові джерела і показує `Джерело скасування: таймаут.` або `Джерело скасування: Ctrl+C.`. Якщо обидва джерела вже скасовані, повідомлення про Ctrl+C має пріоритет.

Тест `ImportAsync_WithLinkedTokens_IsCancelledByTimeoutAndNotUser` чекає через `TaskCompletionSource`, поки імпортер почне очікувати, потім викликає `timeoutCts.CancelAfter(20 мс)`. Перевіряються скасований таймаут, скасований зв'язаний токен, нескасований користувацький токен і токен у винятку. Захисний `WaitAsync(5 с)` не дає тесту зависнути та не маскує неспрацювання таймера: тоді тест впаде з `TimeoutException`.

### 6.3. IProgress<int>

Обидва методи сервісу приймають `IProgress<int>? progress`. Звичайний імпорт повідомляє відсоток опрацьованих записів, а потоковий - приблизну частку прочитаних байтів. Через випереджувальну буферизацію JSON на малих файлах можливий стрибок 0 → 99. Порожній успішний імпорт дає 0 → 100. Після помилки або скасування 100 не повідомляється.

`ConsoleImportProgress` реалізує `IProgress<int>` синхронно, щоб повідомлення не губилися після завершення консольної програми. Повторні значення відсіюються; останній успішний відсоток - 100.

```text
dotnet run --project src/Cli -f net10.0 -- import-lines --stream --progress
Прогрес імпорту: 0%
Прогрес імпорту: 99%
Прогрес імпорту: 100%
Імпортовано рядків: 3
```

### 6.4. Покриття

```sh
dotnet test --collect:"XPlat Code Coverage"
```

Всі 25 сценаріїв пройшли. Покриття рядків Core: **28.93%** (193/667); покриття розгалужень: **27.55%**. Детальний збережений підсумок: [coverage-summary.md](coverage-summary.md).

Непокритими залишилися попередні імпортери товарів, файлове сховище, кеш і фабрика, демонстраційні дані, інформація про середовище, бенчмарк і частина звітів та доменних переходів. Це прийнятно для демонстрації конкретних вимог лабораторної: перевірено інваріанти, події, детермінований звіт, звичайний і потоковий імпорт, прогрес, скасування й таймаут. Для повної перевірки Core потрібне розширення тестів та окремі інтеграційні перевірки інфраструктури.

### Нові тести

| Назва | Перевірка |
|---|---|
| ReadAsync_WithEnumeratorCancellation_ThrowsBeforeOpeningFile | Токен лише в WithCancellation діє через EnumeratorCancellation |
| ImportStreamingAsync_AppliesEachRowBeforeRequestingNextAndSkipsDuplicates | Рядок застосовується до отримання наступного; дублікати пропускаються; правильні подія й токен |
| ImportStreamingAsync_WhenAlreadyCancelled_DoesNotOpenFile | Заздалегідь скасований потік не відкриває файл |
| ImportStreamingAsync_WhenCancelledAfterFirstRow_DoesNotReportCompletion | Немає другого додавання та 100% після скасування |
| ImportAsync_ReportsProcessedRowsIncludingSkippedDuplicates | Прогрес 0, 50, 100 з урахуванням пропущеного дубліката |
| ImportStreamingAsync_WithInvalidSecondRecord_KeepsFirstAndDisposesStream | Номер поганого запису, збереження першого рядка, звільнення файлу, немає 100% |
| ImportAsync_WithEmptySource_ReportsCompletion (Theory, 2 набори) | Порожній звичайний та потоковий імпорт дають 0 записів, прогрес 0 → 100 |
| ImportAsync_WithLinkedTokens_IsCancelledByTimeoutAndNotUser | Скасування саме таймером без сигналу від користувача |

Додатково вручну перевірено Ctrl+C і додатний таймаут на великому тимчасовому файлі в обох режимах імпорту. У кожному випадку CLI правильно визначив джерело скасування і завершився з кодом 2.
