namespace Negocio.DomainModel.Enums
{
    /// <summary>
    /// Decisión clínica del médico tras agotar el protocolo de contacto (REQ-FUNC-010).
    /// </summary>
    public enum DecisionClinica
    {
        /// <summary>Sin decisión registrada todavía.</summary>
        Ninguna,

        /// <summary>El médico contactará al paciente personalmente.</summary>
        ContactarPersonalmente,

        /// <summary>Se programa visita domiciliaria.</summary>
        VisitaDomiciliaria,

        /// <summary>Se cambia la periodicidad de reporte (actualiza la HC).</summary>
        CambiarPeriodicidad,

        /// <summary>El paciente se declara no contactable.</summary>
        DeclararNoContactable
    }
}
