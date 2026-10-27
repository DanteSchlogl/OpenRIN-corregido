namespace Services.DomainModel
{
    /// <summary>Niveles de severidad de bitácora (REQ-ARQ-003), de menor a mayor criticidad.</summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    }
}
