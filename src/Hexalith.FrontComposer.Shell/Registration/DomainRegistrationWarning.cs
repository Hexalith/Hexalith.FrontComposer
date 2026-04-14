namespace Hexalith.FrontComposer.Shell.Registration;

/// <summary>
/// Describes a generated registration type that partially matched the discovery pattern.
/// </summary>
internal sealed class DomainRegistrationWarning {
    public DomainRegistrationWarning(string registrationType, bool hasManifest, bool hasRegisterMethod) {
        RegistrationType = registrationType;
        HasManifest = hasManifest;
        HasRegisterMethod = hasRegisterMethod;
    }

    public string RegistrationType { get; }

    public bool HasManifest { get; }

    public bool HasRegisterMethod { get; }
}
