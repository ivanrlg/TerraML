namespace FuzzySat.Web.Components.Shared.ProjectSetup;

public enum InputMode { DirectPath, ImportSentinel2, PreStacked }

public record BandEntry(string Name, int Index, string Description);

public record ClassEntry(string Name, int Code, string Color);

public record SensorPreset(string Name, int Bands, string Resolution);
