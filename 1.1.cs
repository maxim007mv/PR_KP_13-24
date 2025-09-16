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

        private static int Main(string[] args)
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

            if (args.Length > 0)
            {
                var expr = string.Join(" ", args);
                return RunOnce(expr);
            }

            PrintBanner();
            while (true)
            {
                Console.Write("> ");
                var line = Console.ReadLine();
                if (line is null)
                    break;
                line = line.Trim();
                if (line.Length == 0 || string.Equals(line, "exit", StringComparison.OrdinalIgnoreCase) || string.Equals(line, "quit", StringComparison.OrdinalIgnoreCase))
                    break;

                RunOnce(line);
            }

            return 0;
        }

        private static int RunOnce(string input)
        {
            var result = Evaluate(input);
            if (result.Success)
            {
                Console.WriteLine(FormatDecimal(result.Value));
                return 0;
            }
            else
            {
                Console.WriteLine($"Ошибка: {result.Error}");
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

        private static EvalResult Evaluate(string input)
        {
            if (input is null)
                return EvalResult.Fail("Пустой ввод.");

            if (input.Length > MaxInputLength)
                return EvalResult.Fail($"Слишком длинное выражение (>{MaxInputLength} символов).");

            var match = Expression.Match(input);
            if (!match.Success)
            {
                return EvalResult.Fail("Ожидается выражение: <число> <операция> <число> с точкой как разделителем дробной части.");
            }

            var leftText = match.Groups[1].Value;
            var op = match.Groups[2].Value[0];
            var rightText = match.Groups[3].Value;

            if (!TryParseDecimal(leftText, out var left, out var errLeft))
                return EvalResult.Fail($"Некорректный левый операнд: {errLeft}");
            if (!TryParseDecimal(rightText, out var right, out var errRight))
                return EvalResult.Fail($"Некорректный правый операнд: {errRight}");

            if (Math.Abs(left) > MaxOperandAbs || Math.Abs(right) > MaxOperandAbs)
                return EvalResult.Fail($"Операнды должны быть в диапазоне [-{MaxOperandAbs}, {MaxOperandAbs}].");

            try
            {
                decimal value = op switch
                {
                    '+' => checked(left + right),
                    '-' => checked(left - right),
                    '*' => checked(left * right),
                    '/' => right == 0m
                        ? throw new DivideByZeroException()
                        : left / right,
                    _ => throw new InvalidOperationException("Недопустимая операция.")
                };

                if (Math.Abs(value) > MaxResultAbs)
                    return EvalResult.Fail($"Результат выходит за пределы допустимого диапазона [-{MaxResultAbs}, {MaxResultAbs}].");

              
                value = decimal.Round(value, 4, MidpointRounding.ToEven);
                return EvalResult.Ok(value);
            }
            catch (DivideByZeroException)
            {
                return EvalResult.Fail("Деление на ноль запрещено.");
            }
            catch (OverflowException)
            {
                return EvalResult.Fail("Переполнение при вычислении. Уточните значения.");
            }
            catch (Exception ex)
            {
                return EvalResult.Fail($"Не удалось вычислить выражение: {ex.Message}");
            }
        }

        private static bool TryParseDecimal(string text, out decimal value, out string error)
        {
            
            var dot = text.IndexOf('.');
            if (dot >= 0)
            {
                var frac = text.Length - dot - 1;
                if (frac == 0)
                {
                    value = 0m;
                    error = "после точки нет цифр";
                    return false;
                }
                if (frac > 4)
                {
                    value = 0m;
                    error = "не более 4 знаков после точки";
                    return false;
                }
            }

            
            if (text.Contains(','))
            {
                value = 0m;
                error = "используйте точку '.' как разделитель дробной части";
                return false;
            }

            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            {
                error = string.Empty;
                return true;
            }
            else
            {
                error = "не удалось распознать число";
                return false;
            }
        }

        private static string FormatDecimal(decimal d)
        {
           
            var s = d.ToString("0.####", CultureInfo.InvariantCulture);
            return s;
        }

        private readonly record struct EvalResult(bool Success, decimal Value, string Error)
        {
            public static EvalResult Ok(decimal value) => new(true, value, string.Empty);
            public static EvalResult Fail(string error) => new(false, 0m, error);
        }
    }
}
