namespace Baldic.Loader.Manifest
{
    public enum DiagnosticSeverity
    {
        Warning,
        Error
    }

    /// <summary>
    /// A single diagnostic produced during manifest parsing or validation.
    /// </summary>
    public sealed class ParseDiagnostic
    {
        public DiagnosticSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }

        public ParseDiagnostic(DiagnosticSeverity severity, string code, string message)
        {
            Severity = severity;
            Code = code;
            Message = message;
        }

        public override string ToString() => $"[{Severity}] {Code}: {Message}";

        public static ParseDiagnostic Error(string code, string message) =>
            new ParseDiagnostic(DiagnosticSeverity.Error, code, message);

        public static ParseDiagnostic Warning(string code, string message) =>
            new ParseDiagnostic(DiagnosticSeverity.Warning, code, message);
    }
}
