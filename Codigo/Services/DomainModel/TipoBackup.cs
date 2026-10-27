namespace Services.DomainModel
{
    /// <summary>Tipo de respaldo soportado por el módulo de backups (REQ-ARQ-005).</summary>
    public enum TipoBackup
    {
        /// <summary>Respaldo completo (FULL): copia total de la base de datos.</summary>
        Completo,

        /// <summary>Respaldo diferencial (DIFFERENTIAL): cambios desde el último respaldo completo.</summary>
        Diferencial
    }
}
