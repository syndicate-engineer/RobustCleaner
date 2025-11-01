
# RCleaner

Небольшая консольная утилита на C# для быстрой очистки временных файлов и сетевых кэшей в Windows.

- Очистка пользовательского `%TEMP%` и `C:\Windows\Temp` (опционально с повышением прав).
- Очистка корзины (через `SHEmptyRecycleBin`).
- Сетевые утилиты: `ipconfig /flushdns`, `arp -d *`, `netsh winsock reset`.

Сборка и запуск (PowerShell / Windows):

```powershell
dotnet build .\RCleaner.csproj -c Release
dotnet run --project .\RCleaner.csproj
```

