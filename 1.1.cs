using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CalculatorApp
{
    internal static class Program
    {
        private const int MaxInputLength = 100;
        private const decimal MaxOperandAbs = 1_000_000m;
        private const decimal MaxResultAbs = 1_000_000_000m;
        private static readonly Regex Expression = new(
            pattern: @"^\s*([+-]?\d+(?:\.\d+)?)\s*([+\-*/])\s*([+-]?\d+(?:\.\d+)?)\s*$",
            options: RegexOptions.Compiled);

        private static int Main(string[] s)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            if (s.Length > 0)
            {
                var l = string.Join(" ", s);
                return RunOnce(l);
            }

            PrintBanner();
            while (true)
            {
                Console.Write("> ");
                var l = Console.ReadLine();
                if (l is null)
                    break;
                l = l.Trim();
                if (l.Length == 0
                    || string.Equals(l, "exit", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(l, "quit", StringComparison.OrdinalIgnoreCase))
                    break;

                RunOnce(l);
            }

            return 0;
        }

        private static int RunOnce(string s)
        {
            var l = Evaluate(s);
            if (l.Success)
            {
                Console.WriteLine(FormatDecimal(l.Value));
                return 0;
            }
            else
            {
                Console.WriteLine($"Ошибка: {l.Error}");
                return 1;
            }
        }

        private static void PrintBanner()
        {
            Console.WriteLine("Калькулятор (+, -, *, /) с ограничениями.");
            Console.WriteLine("Введите выражение вида: <число> <операция> <число> (например: 12.5 * 3)");
            Console.WriteLine("Десятичный разделитель — точка '.' (пример: 3.14)");
            Console.WriteLine("Ограничения: два операнда, |операнд| ≤ 1 000 000, |результат| ≤ 1 000 000 000, до 4 знаков после точки.");
            Console.WriteLine("Команды: 'exit' или пустая строка — выход.\n");
        }

        private static EvalResult Evaluate(string s)
        {
            if (s is null)
                return EvalResult.Fail("Пустой ввод.");

            if (s.Length > MaxInputLength)
                return EvalResult.Fail($"Слишком длинное выражение (>{MaxInputLength} символов).");

            var l = Expression.Match(s);
            if (!l.Success)
            {
                return EvalResult.Fail("Ожидается выражение: <число> <операция> <число> с точкой как разделителем дробной части.");
            }

            var s1 = l.Groups[1].Value; // левый операнд (строка)
            var k = l.Groups[2].Value[0]; // операция
            var s2 = l.Groups[3].Value; // правый операнд (строка)

            if (!TryParseDecimal(s1, out var l1, out var s3))
                return EvalResult.Fail($"Некорректный левый операнд: {s3}");
            if (!TryParseDecimal(s2, out var l2, out var s4))
                return EvalResult.Fail($"Некорректный правый операнд: {s4}");

            if (Math.Abs(l1) > MaxOperandAbs || Math.Abs(l2) > MaxOperandAbs)
                return EvalResult.Fail($"Операнды должны быть в диапазоне [-{MaxOperandAbs}, {MaxOperandAbs}].");

            try
            {
                decimal l3 = k switch
                {
                    '+' => checked(l1 + l2),
                    '-' => checked(l1 - l2),
                    '*' => checked(l1 * l2),
                    '/' => l2 == 0m
                        ? throw new DivideByZeroException()
                        : l1 / l2,
                    _ => throw new InvalidOperationException("Недопустимая операция.")
                };

                if (Math.Abs(l3) > MaxResultAbs)
                    return EvalResult.Fail($"Результат выходит за пределы допустимого диапазона [-{MaxResultAbs}, {MaxResultAbs}].");

                l3 = decimal.Round(l3, 4, MidpointRounding.ToEven);
                return EvalResult.Ok(l3);
            }
            catch (DivideByZeroException)
            {
                return EvalResult.Fail("Деление на ноль запрещено.");
            }
            catch (OverflowException)
            {
                return EvalResult.Fail("Переполнение при вычислении. Уточните значения.");
            }
            catch (Exception k1)
            {
                return EvalResult.Fail($"Не удалось вычислить выражение: {k1.Message}");
            }
        }

        private static bool TryParseDecimal(string s, out decimal l, out string k)
        {
            var k1 = s.IndexOf('.');
            if (k1 >= 0)
            {
                var l1 = s.Length - k1 - 1;
                if (l1 == 0)
                {
                    l = 0m;
                    k = "после точки нет цифр";
                    return false;
                }
                if (l1 > 4)
                {
                    l = 0m;
                    k = "не более 4 знаков после точки";
                    return false;
                }
            }

            if (s.Contains(','))
            {
                l = 0m;
                k = "используйте точку '.' как разделитель дробной части";
                return false;
            }

            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out l))
            {
                k = string.Empty;
                return true;
            }
            else
            {
                k = "не удалось распознать число";
                return false;
            }
        }

        private static string FormatDecimal(decimal l)
        {
            var s = l.ToString("0.####", CultureInfo.InvariantCulture);
            return s;
        }

        private readonly record struct EvalResult(bool Success, decimal Value, string Error)
        {
            public static EvalResult Ok(decimal l) => new(true, l, string.Empty);
            public static EvalResult Fail(string s) => new(false, 0m, s);
        }
    }
}