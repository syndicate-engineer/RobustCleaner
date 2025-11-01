using System;
using System.Collections.Generic;
using System.Text;

namespace RCleaner
{
    public class Tui
    {     
        private void Render()
        {
            Console.Clear();
            Console.WriteLine(BoxedHeader("💾 RCleaner — Robust Clean!"));
            Console.WriteLine();
            for (int i = 0; i < _items.Count; i++)
            {
                var prefix = i == _selected ? "❯" : " ";
                if (i == _selected)
                {
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                    Console.ForegroundColor = ConsoleColor.Black;
                }
                else
                {
                    Console.ResetColor();
                    Console.ForegroundColor = ConsoleColor.White;
                }
                Console.WriteLine($" {prefix}  {_items[i].Title}");

                Console.ResetColor();
                Console.WriteLine();
            }
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Стрелки ↑↓ — навигация, Enter — выполнить, Q или Esc — выход");
        }
        private string BoxedHeader(string text)
        {
            var sb = new StringBuilder();
            var border = new string('═', text.Length + 2);
            sb.AppendLine($"╔{border}╗");
            sb.AppendLine($"║ {text} ║");
            sb.AppendLine($"╚{border}╝");
            return sb.ToString();
        }
        private readonly List<MenuItem> _items;
        private int _selected = 0;

        public Tui(List<MenuItem> items)
        {
            _items = items;
        }

        public void Run()
        {
            Console.CursorVisible = false;
            while (true)
            {
                Render();
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.UpArrow)
                {
                    _selected = (_selected - 1 + _items.Count) % _items.Count;
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    _selected = (_selected + 1) % _items.Count;
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    var item = _items[_selected];
                    if (item.Title.Contains("Выход")) break;
                    RunAction(item);
                }
                else if (key.Key == ConsoleKey.Escape || key.Key == ConsoleKey.Q)
                {
                    break;
                }
            }
            Console.ResetColor();
            Console.Clear();
            Console.CursorVisible = true;
        }

        private void RunAction(MenuItem item)
        {
            Console.Clear();
            Console.WriteLine(BoxedHeader("RCleaner — Выполнение задачи"));
            Console.WriteLine();
            try
            {
                item.Action();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Ошибка: {ex.Message}");
                Console.ResetColor();
            }
            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу, чтобы вернуться в меню...");
            Console.ReadKey(true);
            Console.Clear();
        }

    }
}
