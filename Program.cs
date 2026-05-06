using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;

namespace TriangleAnalyzer
{
    class Program
    {
        private static readonly string LogFilePath = "triangle_log.txt";

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            // Пример ручного ввода
            while (true)
            {
                Console.WriteLine("\nВведите три стороны треугольника (или 'exit' для выхода):");
                Console.Write("Сторона A: ");
                string inputA = Console.ReadLine();
                if (inputA?.ToLower() == "exit") break;

                Console.Write("Сторона B: ");
                string inputB = Console.ReadLine();
                if (inputB?.ToLower() == "exit") break;

                Console.Write("Сторона C: ");
                string inputC = Console.ReadLine();
                if (inputC?.ToLower() == "exit") break;

                ProcessRequest(inputA, inputB, inputC);
            }
        }

        static void ProcessRequest(string inputA, string inputB, string inputC)
        {
            DateTime timestamp = DateTime.Now;
            string[] rawParams = { inputA, inputB, inputC };
            string resultType = "";
            List<(int x, int y)> coordinates = new List<(int, int)>();

            bool isValidNumbers = true;
            float a = 0, b = 0, c = 0;

            // Парсинг с проверкой на вещественные положительные числа
            if (!float.TryParse(inputA, NumberStyles.Float, CultureInfo.InvariantCulture, out a) || a <= 0)
                isValidNumbers = false;
            if (!float.TryParse(inputB, NumberStyles.Float, CultureInfo.InvariantCulture, out b) || b <= 0)
                isValidNumbers = false;
            if (!float.TryParse(inputC, NumberStyles.Float, CultureInfo.InvariantCulture, out c) || c <= 0)
                isValidNumbers = false;

            if (!isValidNumbers)
            {
                // Нечисловые или неположительные данные
                resultType = "";
                coordinates = new List<(int, int)> { (-2, -2), (-2, -2), (-2, -2) };
                LogFailure(timestamp, rawParams, "Некорректный ввод: все стороны должны быть вещественными положительными числами.", null);
                OutputResult(resultType, coordinates);
                return;
            }

            // Проверка существования треугольника
            if (a + b <= c || a + c <= b || b + c <= a)
            {
                resultType = "не треугольник";
                coordinates = new List<(int, int)> { (-1, -1), (-1, -1), (-1, -1) };
                LogFailure(timestamp, rawParams, $"Стороны {a}, {b}, {c} не образуют треугольник.", null);
                OutputResult(resultType, coordinates);
                return;
            }

            // Определение типа треугольника
            float eps = 1e-5f; // для сравнения float
            if (Math.Abs(a - b) < eps && Math.Abs(b - c) < eps)
                resultType = "равносторонний";
            else if (Math.Abs(a - b) < eps || Math.Abs(a - c) < eps || Math.Abs(b - c) < eps)
                resultType = "равнобедренный";
            else
                resultType = "разносторонний";

            // Вычисление координат для размещения в поле 100x100
            coordinates = CalculateCoordinates(a, b, c);

            // Успешное логирование
            LogSuccess(timestamp, rawParams, resultType, coordinates);

            // Вывод результата пользователю
            OutputResult(resultType, coordinates);
        }

        static List<(int x, int y)> CalculateCoordinates(float a, float b, float c)
        {
            // Размещаем треугольник удобно для отображения:
            // Вершина A в (10, 90) — нижний левый угол с отступом
            // Вершина B в (90, 90) — нижний правый угол
            // Вершина C вычисляется по пересечению окружностей
            // Масштабируем так, чтобы максимальная сторона умещалась в 80 px (от 10 до 90)

            float maxSide = Math.Max(a, Math.Max(b, c));
            float scale = 80.0f / maxSide; // чтобы самая длинная сторона стала 80 px

            // Приводим длины к масштабу
            double scaledA = a * scale;
            double scaledB = b * scale;
            double scaledC = c * scale;

            // Координаты A и B фиксируем на оси X
            int Ax = 10;
            int Ay = 90;
            int Bx = 10 + (int)Math.Round(scaledC);
            int By = 90;

            // Находим координаты C по пересечению двух окружностей:
            // (Cx - Ax)^2 + (Cy - Ay)^2 = scaledB^2
            // (Cx - Bx)^2 + (Cy - By)^2 = scaledA^2
            // Решение для Cy < 90 (чтобы треугольник был выше основания)

            double dx = Bx - Ax;
            double dy = By - Ay; // = 0
            double l = Math.Sqrt(dx * dx + dy * dy);
            if (l < 0.001) return new List<(int, int)> { (Ax, Ay), (Bx, By), (Ax, Ay + 1) }; // fallback

            double r1 = scaledB;
            double r2 = scaledA;

            // Вычисление точки пересечения
            double d = l;
            double aLocal = (r1 * r1 - r2 * r2 + d * d) / (2 * d);
            double h = Math.Sqrt(Math.Abs(r1 * r1 - aLocal * aLocal));

            double xm = Ax + (dx * aLocal) / d;
            double ym = Ay + (dy * aLocal) / d;

            // Две возможные точки: выше или ниже основания. Выбираем ту, что выше (y < Ay)
            double Cx1 = xm + h * (dy) / d;
            double Cy1 = ym - h * (dx) / d;

            double Cx2 = xm - h * (dy) / d;
            double Cy2 = ym + h * (dx) / d;

            int Cx, Cy;
            if (Cy1 < Ay)
            {
                Cx = (int)Math.Round(Cx1);
                Cy = (int)Math.Round(Cy1);
            }
            else
            {
                Cx = (int)Math.Round(Cx2);
                Cy = (int)Math.Round(Cy2);
            }

            // Клипируем координаты в поле 0..100 (аналог Math.Clamp для .NET Framework)
            Cx = Clamp(Cx, 0, 100);
            Cy = Clamp(Cy, 0, 100);
            int AxFinal = Clamp(Ax, 0, 100);
            int AyFinal = Clamp(Ay, 0, 100);
            int BxFinal = Clamp(Bx, 0, 100);
            int ByFinal = Clamp(By, 0, 100);

            return new List<(int, int)> { (AxFinal, AyFinal), (BxFinal, ByFinal), (Cx, Cy) };
        }

        // Метод-замена для Math.Clamp (нет в .NET Framework)
        static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        static void OutputResult(string type, List<(int x, int y)> coords)
        {
            Console.WriteLine($"\nТип треугольника: {type}");
            Console.WriteLine($"Координаты: A({coords[0].x},{coords[0].y}), B({coords[1].x},{coords[1].y}), C({coords[2].x},{coords[2].y})");
        }

        static void LogSuccess(DateTime timestamp, string[] parameters, string triangleType, List<(int x, int y)> coords)
        {
            string logEntry = $"[УСПЕХ] {timestamp:yyyy-MM-dd HH:mm:ss.fff} | Параметры: {parameters[0]}, {parameters[1]}, {parameters[2]} | Тип: {triangleType} | Координаты: A({coords[0].x},{coords[0].y}) B({coords[1].x},{coords[1].y}) C({coords[2].x},{coords[2].y})";

            // В консоль
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"LOG: {logEntry}");
            Console.ResetColor();

            // В файл
            File.AppendAllText(LogFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
        }

        static void LogFailure(DateTime timestamp, string[] parameters, string errorMessage, Exception ex = null)
        {
            string errorDetails = errorMessage;
            if (ex != null)
                errorDetails += $" | Исключение: {ex.Message}\n{ex.StackTrace}";

            string logEntry = $"[НЕУСПЕХ] {timestamp:yyyy-MM-dd HH:mm:ss.fff} | Параметры: {parameters[0]}, {parameters[1]}, {parameters[2]} | Ошибка: {errorDetails}";

            // В консоль
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"LOG: {logEntry}");
            Console.ResetColor();

            // В файл
            File.AppendAllText(LogFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
        }
    }
}