using System.Globalization;

namespace Rhmn.Core;

public enum ClockMode { Automatic, RenderAtTime, AnimationClock }
public sealed record FrameRate(int Numerator, int Denominator)
{
    public double Value => (double)Numerator / Denominator;
    public string Argument => $"{Numerator}/{Denominator}";
    public override string ToString() => Value.ToString("0.###", CultureInfo.InvariantCulture) + " fps";
    public static readonly FrameRate[] Supported = [new(24000,1001), new(24,1), new(25,1), new(30000,1001), new(30,1), new(50,1), new(60000,1001), new(60,1)];
}
public sealed record Resolution(int Width, int Height, string Label)
{
    public override string ToString() => $"{Label} · {Width} × {Height}";
    public static readonly Resolution[] Presets = [new(3840,2160,"4K UHD"),new(1920,1080,"Full HD"),new(4096,2160,"DCI 4K"),new(2160,3840,"Vertikal 4K"),new(1080,1920,"Vertikal HD"),new(2160,2160,"Persegi 4K"),new(1080,1080,"Persegi HD")];
}
public sealed record RenderSettings
{
    public int Width { get; init; } = 3840;
    public int Height { get; init; } = 2160;
    public FrameRate Fps { get; init; } = new(30,1);
    public decimal DurationSeconds { get; init; } = 12;
    public int BitrateMbps { get; init; } = 60;
    public string EncoderPreset { get; init; } = "medium";
    public ClockMode Mode { get; init; } = ClockMode.Automatic;
    public bool AllowNetwork { get; init; }
    public bool CheckLoop { get; init; } = true;
    public int Seed { get; init; } = 12345;
    public int FrameCount => checked((int)Math.Ceiling(DurationSeconds * Fps.Numerator / Fps.Denominator));
    public double ActualDuration => FrameCount / Fps.Value;
    public long EstimatedBytes => (long)(ActualDuration * BitrateMbps * 1_000_000 / 8);
    public void Validate()
    {
        if (Width is < 64 or > 4096 || Height is < 64 or > 4096 || Width % 2 != 0 || Height % 2 != 0) throw new ArgumentException("Ukuran harus genap, antara 64–4096 piksel.");
        if (!FrameRate.Supported.Contains(Fps)) throw new ArgumentException("FPS tidak didukung. Gunakan salah satu preset.");
        if (DurationSeconds is < 1 or > 600) throw new ArgumentException("Durasi harus antara 1–600 detik.");
        if (BitrateMbps is < 1 or > 200) throw new ArgumentException("Bitrate harus antara 1–200 Mbps.");
        if (EncoderPreset is not ("fast" or "medium" or "slow")) throw new ArgumentException("Preset enkoder tidak valid.");
        if (!Enum.IsDefined(Mode)) throw new ArgumentException("Mode animasi tidak valid.");
    }
}
public sealed class RenderJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntryFile { get; set; } = "";
    public string OutputFile { get; set; } = "";
    public RenderSettings Settings { get; set; } = new();
    public string Status { get; set; } = "Menunggu";
    public string Detail { get; set; } = "";
    public string Name => Path.GetFileName(EntryFile);
}
public sealed record RenderProgress(double Fraction, string Message);
