using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using RCleaner.Logging;

namespace RCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Robust Cleaner";
            var logger = new ConsoleLogger();
            var cleaner = new Cleaner(logger);

            if (args != null && args.Length >= 2 && args[0] == "--elevatedAction")
            {
                var actionName = args[1].Trim('"');
                if (actionName != "CleanWindowsTemp" && actionName != "EmptyRecycleBin" && actionName != "ClearNetworkCache" && actionName != "ScanAndReport")
                {
                    return;
                }

                switch (actionName)
                {
                    case "CleanWindowsTemp":
                        cleaner.CleanWindowsTemp();
                        break;
                    case "EmptyRecycleBin":
                        cleaner.EmptyRecycleBin();
                        break;
                    case "ClearNetworkCache":
                        cleaner.ClearNetworkCache();
                        break;
                    case "ScanAndReport":
                        cleaner.ScanAndReport();
                        break;
                }

                logger.Info(string.Empty);
                logger.Info("Нажмите любую клавишу для выхода...");
                Console.ReadKey(true);
                return;
            }

            var menu = new List<MenuItem>
            {
                new MenuItem("🧹 Очистить пользовательские временные файлы (%TEMP%)", () => cleaner.CleanUserTemp()),
                new MenuItem("🧺 Очистить C:\\Windows\\Temp", () => cleaner.CleanWindowsTemp()),
                new MenuItem("♻️ Очистить корзину", () => cleaner.EmptyRecycleBin()),
                new MenuItem("🔎 Просканировать и показать статистику", () => cleaner.ScanAndReport()),
                new MenuItem("🌐 Очистить DNS (ipconfig /flushdns)", () => cleaner.FlushDns()),
                new MenuItem("🛜 Очистить сетевой кэш (ARP/Winsock)", () => cleaner.ClearNetworkCache()),
                new MenuItem("❌ Выход", () => { /* noop */ })
            };

            var tui = new Tui(menu);
            tui.Run();
        }
    }
}
