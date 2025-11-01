using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using RCleaner.Logging;

namespace RCleaner
{
    public class Cleaner : ICleaner
    {
        private readonly ILogger _logger;

        public Cleaner(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI = 0x00000002;
        private const uint SHERB_NOSOUND = 0x00000004;

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

        public void CleanUserTemp()
        {
            var temp = Path.GetTempPath();
            _logger.Info($"Очистка пользовательской временной папки: {temp}");
            var res = DeleteFilesAndDirs(temp);
            PrintResult(res);
        }

        public void CleanWindowsTemp()
        {

            if (!ElevationHelper.IsElevated())
            {
                _logger.Warn("Операция требует прав администратора.");
                _logger.Info("Перезапустить программу с правами администратора? (UAC)...");
                var started = ElevationHelper.RelaunchAsAdmin("CleanWindowsTemp");
                if (started)
                {
                    Environment.Exit(0);
                }
                else
                {
                    _logger.Info("Перезапуск с повышенными правами не выполнен (отмена или ошибка).");
                    return;
                }
            }

            var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var path = Path.Combine(win, "Temp");
            _logger.Info($"Очистка Windows\\Temp: {path}");
            var res = DeleteFilesAndDirs(path);
            PrintResult(res);
        }

        public void EmptyRecycleBin()
        {
            if (!ElevationHelper.IsElevated())
            {
                _logger.Warn("Операция требует прав администратора.");
                _logger.Info("Перезапустить программу с правами администратора? (UAC)...");
                var started = ElevationHelper.RelaunchAsAdmin("EmptyRecycleBin");
                if (started)
                {
                    Environment.Exit(0);
                }
                else
                {
                    _logger.Info("Перезапуск с повышенными правами не выполнен (отмена или ошибка).");
                    return;
                }
            }

            _logger.Info("Очистка корзины...");
            var hr = SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
            if (hr == 0)
            {
                _logger.Success("Корзина успешно очищена.");
            }
            if (hr == -2147418113 )
            {
                _logger.Info("Корзина уже пуста.");
            }
            else
            {
                _logger.Warn($"SHEmptyRecycleBin вернул код: {hr}");
            }
        }

        public void ScanAndReport()
        {
            _logger.Info("Сканирование основных временных папок...");
            var temp = Path.GetTempPath();
            var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            var winTemp = Path.Combine(win, "Temp");
            var items = new[] { temp, winTemp };
            long totalFiles = 0; long totalBytes = 0; int totalDirs = 0;
            foreach (var p in items)
            {
                _logger.Info(string.Empty);
                _logger.Info($"Папка: {p}");
                try
                {
                    var stats = ScanFolder(p);
                    _logger.Info($"  Файлов: {stats.files}, Директорий: {stats.dirs}, Потенциально освобождаемое: {FormatBytes(stats.bytes)}");
                    totalFiles += stats.files; totalDirs += stats.dirs; totalBytes += stats.bytes;
                }
                catch (UnauthorizedAccessException)
                {
                    if (!ElevationHelper.IsElevated())
                    {
                        _logger.Warn($"Доступ к папке {p} запрещён (требуются права администратора).");
                        _logger.Info("Перезапустить программу с правами администратора и повторно просканировать? (UAC)...");
                        var started = ElevationHelper.RelaunchAsAdmin("ScanAndReport");
                        if (started)
                        {
                            Environment.Exit(0);
                        }
                        else
                        {
                            _logger.Info("Перезапуск с повышенными правами не выполнен (отмена или ошибка).");
                        }
                    }
                    else
                    {
                        _logger.Warn($"Пропущено (ошибка доступа) к {p} несмотря на повышенные права.");
                    }
                }
            }
            _logger.Info(string.Empty);
            _logger.Info($"Итого: файлов {totalFiles}, директорий {totalDirs}, потенц. освобождаемое {FormatBytes(totalBytes)}");
        }

        public void FlushDns()
        {
            _logger.Info("Выполняется ipconfig /flushdns ...");
            try
            {
                var psi = new ProcessStartInfo("ipconfig", "/flushdns") { RedirectStandardOutput = true, UseShellExecute = false };
                var p = Process.Start(psi);
                if (p != null)
                {
                    var outp = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    _logger.Info(outp.Trim());
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Не удалось выполнить flushdns: {ex.Message}");
            }
        }

        public void ClearNetworkCache()
        {
            // Некоторые операции требуют админских прав
            if (!ElevationHelper.IsElevated())
            {
                _logger.Warn("Операция требует прав администратора (для очистки ARP/Winsock).");
                _logger.Info("Перезапустить программу с правами администратора? (UAC)...");
                var started = ElevationHelper.RelaunchAsAdmin("ClearNetworkCache");
                if (started)
                {
                    Environment.Exit(0);
                }
                else
                {
                    _logger.Info("Перезапуск с повышенными правами не выполнен (отмена или ошибка).");
                    return;
                }
            }

            try
            {
                _logger.Info("Очищаю ARP-кеш: arp -d *");
                var psi1 = new ProcessStartInfo("arp", "-d *") { RedirectStandardOutput = true, UseShellExecute = false };
                var p1 = Process.Start(psi1);
                if (p1 != null) { _logger.Info(p1.StandardOutput.ReadToEnd().Trim()); p1.WaitForExit(); }

                _logger.Info("Сбрасываю Winsock: netsh winsock reset");
                var psi2 = new ProcessStartInfo("netsh", "winsock reset") { RedirectStandardOutput = true, UseShellExecute = false };
                var p2 = Process.Start(psi2);
                if (p2 != null) { _logger.Info(p2.StandardOutput.ReadToEnd().Trim()); p2.WaitForExit(); }

                _logger.Success("Сетевой кэш очищен (возможно потребуется перезагрузка для полного эффекта).");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Ошибка при очистке сетевого кэша: {ex.Message}");
            }
        }
        private static (int files, int dirs, long bytes) ScanFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path)) return (0, 0, 0);
                var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories);
                int fcount = 0; long bytes = 0;
                foreach (var f in files)
                {
                    try
                    {
                        var fi = new FileInfo(f);
                        fcount++;
                        bytes += fi.Length;
                    }
                    catch { }
                }
                int dcount = Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories).Count();
                return (fcount, dcount, bytes);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  Пропущено (ошибка доступа или другая): {ex.Message}");
                Console.ResetColor();
                return (0, 0, 0);
            }
        }

        private DeletionResult DeleteFilesAndDirs(string path)
        {
            var res = new DeletionResult();
            if (!Directory.Exists(path))
            {
                Console.WriteLine("Папка не найдена.");
                return res;
            }
            try
            {
                foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        long len = fi.Length;
                        fi.IsReadOnly = false;
                        File.Delete(file);
                        res.FilesDeleted++;
                        res.BytesFreed += len;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        res.FilesSkipped++;
                    }
                    catch (IOException)
                    {
                        res.FilesSkipped++;
                    }
                    catch { res.FilesSkipped++; }
                }
                var dirs = Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length);
                foreach (var dir in dirs)
                {
                    try
                    {
                        if (Directory.EnumerateFileSystemEntries(dir).Any()) continue;
                        Directory.Delete(dir);
                        res.DirsDeleted++;
                    }
                    catch { res.DirsSkipped++; }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Ошибка при очистке: {ex.Message}");
                Console.ResetColor();
            }
            return res;
        }

        private void PrintResult(DeletionResult res)
        {
            _logger.Info(string.Empty);
            _logger.Success($"Удалено файлов: {res.FilesDeleted}, удалено директорий: {res.DirsDeleted}");
            _logger.Success($"Освобождено места: {FormatBytes(res.BytesFreed)}");
            if (res.FilesSkipped + res.DirsSkipped > 0)
            {
                _logger.Warn($"Пропущено: файлов {res.FilesSkipped}, директорий {res.DirsSkipped} (нет доступа или заняты)");
            }
        }

        private static string FormatBytes(long bytes)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB" };
            if (bytes == 0) return "0 B";
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 2);
            return $"{num} {suf[place]}";
        }
    }
}
