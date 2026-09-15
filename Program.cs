using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class Program
{
    private const int _maxCells = 4096;

    public static void Main()
    {
        while (true)
        {
            Console.WriteLine("=== Генератор палитры ===");
            Console.WriteLine("Для выхода введите 'exit' или 'q'.");
            Console.WriteLine($"Максимум клеток: {_maxCells} (количество сторон * количество разделов).");
            Console.WriteLine();

            int? teamCount = ReadPositiveInt("Введите количество сторон: ");
            if (teamCount is null) break;

            int? subteamCount = ReadPositiveInt("Введите количество разделов: ");
            if (subteamCount is null) break;

            if (teamCount.Value * subteamCount.Value > _maxCells)
            {
                Console.WriteLine(
                    $"Слишком много клеток: {teamCount.Value} * {subteamCount.Value} = " +
                    $"{teamCount.Value * subteamCount.Value}, а максимум {_maxCells}. Попробуйте снова.\n");
                continue;
            }

            try
            {
                GenerateImage(teamCount.Value, subteamCount.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при генерации: {ex.Message}\n");
            }

            Console.WriteLine();
        }

        Console.WriteLine("Выход. Нажмите любую клавишу...");
        Console.ReadKey();
    }

    private static int? ReadPositiveInt(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? s = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(s) &&
                (s.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                 s.Trim().Equals("q", StringComparison.OrdinalIgnoreCase)))
                return null;

            if (!string.IsNullOrWhiteSpace(s) &&
                int.TryParse(s.Trim(), out int value) &&
                value > 0)
                return value;

            Console.WriteLine("Некорректное значение. Ожидалось положительное целое число. Попробуйте снова.\n");
        }
    }

    private static void GenerateImage(int teamCount, int subteamCount)
    {
        const int cellSize = 100;

        int height = teamCount * cellSize;
        int width = subteamCount * cellSize;

        using var image = new Image<Rgba32>(width, height);

        Console.WriteLine($"{nameof(teamCount)}: {teamCount};");
        Console.WriteLine($"{nameof(subteamCount)}: {subteamCount};");

        for (int team = 0; team < teamCount; team++)
        {
            for (int sub = 0; sub < subteamCount; sub++)
            {
                var index = new IndexOfPlayer(team, sub);
                Rgba32 color = PlayerPalette.GetColor(index, teamCount, subteamCount);

                int x0 = team * cellSize;
                int y0 = sub * cellSize;

                for (int x = 0; x < cellSize; x++)
                {
                    for (int y = 0; y < cellSize; y++)
                        image[y0 + y, x0 + x] = color;
                }

                Console.WriteLine($"[{team}, {sub}] => ({color.R}, {color.G}, {color.B})");
            }
        }

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string fileName = $"output_{teamCount}x{subteamCount}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        string outputPath = Path.Combine(desktopPath, fileName);

        image.SaveAsPng(outputPath);

        Console.WriteLine($"PNG успешно создано: {outputPath}");
    }

    private static Rgba32 HslToRgba32(double h, double s, double l)
    {
        h = ((h % 360) + 360) % 360;
        s = Math.Clamp(s, 0.0, 1.0);
        l = Math.Clamp(l, 0.0, 1.0);

        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double hp = h / 60.0;
        double m = l - c / 2;

        double r1 = Math.Clamp(Math.Abs(hp - 3) - 1, 0, 1);
        double g1 = Math.Clamp(2 - Math.Abs(hp - 2), 0, 1);
        double b1 = Math.Clamp(2 - Math.Abs(hp - 4), 0, 1);

        byte r = (byte)Math.Round((r1 * c + m) * 255);
        byte g = (byte)Math.Round((g1 * c + m) * 255);
        byte b = (byte)Math.Round((b1 * c + m) * 255);

        return new Rgba32(r, g, b);
    }

    public readonly struct IndexOfPlayer : IEquatable<IndexOfPlayer>
    {
        public IndexOfPlayer(int indexOfSide, int indexOfPlayer)
        {
            IndexOfSide = Math.Max(indexOfSide, 0);
            IndexOfPlayerOnSide = Math.Max(indexOfPlayer, 0);
        }

        public int IndexOfSide { get; }
        public int IndexOfPlayerOnSide { get; }

        public override string ToString() => $"{IndexOfSide}-{IndexOfPlayerOnSide}";

        public bool Equals(IndexOfPlayer other) => IndexOfSide == other.IndexOfSide && IndexOfPlayerOnSide == other.IndexOfPlayerOnSide;

        public override bool Equals(object? obj) => obj is IndexOfPlayer other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(IndexOfSide, IndexOfPlayerOnSide);

        public static bool operator ==(IndexOfPlayer left, IndexOfPlayer right) => left.Equals(right);
        public static bool operator !=(IndexOfPlayer left, IndexOfPlayer right) => !(left == right);
    }
    public static class PlayerPalette
    {
        public static Rgba32 GetColor(IndexOfPlayer index, int teamCount, int subteamCount)
        {
            var (hue, saturation, lightness) = GetHSL(index, teamCount, subteamCount);
            return HslToRgba32(hue, saturation, lightness);
        }

        public static (double hue, double saturation, double lightness) GetHSL(IndexOfPlayer index, int teamCount, int subteamCount)
        {
            int half = teamCount / 2;

            double hueOffset = 60.0;
            double step = 360.0 / Math.Max(teamCount, 1);

            double v = subteamCount <= 1
                ? 0.5
                : index.IndexOfPlayerOnSide / (double)(subteamCount - 1);

            double hue;
            double u;
            double lightness;

            if (index.IndexOfSide < half)
            {
                u = half <= 1 ? 0.5 : index.IndexOfSide / (double)(half - 1);

                hue = hueOffset - index.IndexOfSide * step;
                lightness = 1.0 - (u + v) * 0.25;
            }
            else
            {
                int localIndex = index.IndexOfSide - half;
                int localCount = teamCount - half;

                int reversedLocalIndex = localCount - 1 - localIndex;

                u = localCount <= 1 ? 0.5 : reversedLocalIndex / (double)(localCount - 1);

                double vRev = 1.0 - v;

                hue = 240.0 - localIndex * step;
                lightness = 0.5 - (u + vRev) * 0.25;
            }

            return (hue, 1, lightness);
        }
    }
}