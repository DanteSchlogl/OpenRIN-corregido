using System;
using System.Collections.Generic;
using Services.BLL;

namespace Services.Facade
{
    /// <summary>
    /// Fachada pública de consistencia (REQ-ARQ-007): validación de entidades por atributos
    /// ([Requerido], [Unico], [Rango]) y transacciones atómicas para las operaciones de escritura.
    /// </summary>
    public static class ConsistenciaService
    {
        /// <summary>
        /// Valida una entidad según sus atributos de consistencia. Devuelve true si es válida;
        /// en caso contrario llena <paramref name="mensajesDeError"/> y registra un Warning en bitácora.
        /// El delegado opcional <paramref name="verificadorUnicidad"/> recibe (entidad, nombreDePropiedad)
        /// y debe devolver true cuando el valor de esa propiedad es único.
        /// </summary>
        public static bool ValidarEntidad<T>(
            T entidad,
            out List<string> mensajesDeError,
            Func<T, string, bool>? verificadorUnicidad = null)
            => ConsistenciaLogic.ValidarEntidad(entidad, out mensajesDeError, verificadorUnicidad);

        /// <summary>Inicia una transacción sobre la conexión configurada (por defecto "ServicesDB").</summary>
        public static void IniciarTransaccion(string nombreConexion = "ServicesDB")
            => ConsistenciaLogic.IniciarTransaccion(nombreConexion);

        /// <summary>Confirma (commit) la transacción activa.</summary>
        public static void ConfirmarTransaccion() => ConsistenciaLogic.ConfirmarTransaccion();

        /// <summary>Revierte (rollback) la transacción activa.</summary>
        public static void RevertirTransaccion() => ConsistenciaLogic.RevertirTransaccion();

        /// <summary>Indica si hay una transacción activa.</summary>
        public static bool TransaccionActiva => ConsistenciaLogic.TransaccionActiva;

        /// <summary>
        /// Ejecuta una operación dentro de una transacción: confirma al terminar o revierte
        /// automáticamente ante cualquier error (REQ-ARQ-007).
        /// </summary>
        public static T EjecutarEnTransaccion<T>(Func<T> operacion)
            => ConsistenciaLogic.EjecutarEnTransaccion(operacion);
    }
}
