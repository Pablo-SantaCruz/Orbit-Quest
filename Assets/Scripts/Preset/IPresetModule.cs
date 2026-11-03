public interface IPresetModule
{
    string Id { get; }
    string Capture(PresetSessionContext context);
    void Apply(string payload, PresetSessionContext context);
}
