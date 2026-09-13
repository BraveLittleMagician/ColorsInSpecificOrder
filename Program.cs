using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class Program
{
    public static void Main()
    {
        const int teamCount = 6;
        const int subteamCount = 6;
        const int cellSize = 100;

        int width = teamCount * cellSize;
        int height = subteamCount * cellSize;

        using var image = new Image<Rgba32>(width, height);

        Console.WriteLine($"{nameof(teamCount)}: {teamCount};\n{nameof(subteamCount)}: {subteamCount};");

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
                    {
                        image[y0 + y, x0 + x] = color;
                        if (x == 0 && y == 0)
                            Console.WriteLine($"[{x0 / cellSize}, {y0 / cellSize}] => ({color.R}, {color.G}, {color.B})");
                    }
                }
            }
        }

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        string outputPath = Path.Combine(desktopPath, "output.png");

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
            double u = teamCount <= 1 ? 0.5 : index.IndexOfSide / (double)(teamCount - 1);
            double v = subteamCount <= 1 ? 0.5 : index.IndexOfPlayerOnSide / (double)(subteamCount - 1);

            double d = (u + v) * 0.5;
            double hueOffset = 60.0;

            double step = 360.0 / Math.Max(teamCount, 1);
            int half = teamCount / 2;

            double delta = index.IndexOfSide < half ? -index.IndexOfSide * step : (index.IndexOfSide - half + 1) * step;

            double teamHue = hueOffset + delta;
            double lightness = 1 - d;

            return (teamHue, 1, lightness);
        }
    }
}