using System;
using System.Collections.Generic;
using Services.DomainModel;

namespace Services.DAL.Interfaces
{
    /// <summary>Contrato de persistencia de usuarios y permisos (REQ-ARQ-006).</summary>
    public interface IUsuarioRepository
    {
        /// <summary>Busca un usuario por su nombre de usuario. Devuelve null si no existe.</summary>
        Usuario? ObtenerPorNombreUsuario(string nombreUsuario);

        /// <summary>Busca un usuario por Id. Devuelve null si no existe.</summary>
        Usuario? ObtenerPorId(int id);

        /// <summary>Indica si ya existe un usuario con ese nombre.</summary>
        bool ExisteNombreUsuario(string nombreUsuario);

        /// <summary>Inserta un usuario nuevo y devuelve el Id generado.</summary>
        int Registrar(Usuario usuario);

        /// <summary>Actualiza el estado de acceso (intentos fallidos y bloqueo temporal).</summary>
        void ActualizarAcceso(int idUsuario, int intentosFallidos, DateTime? bloqueadoHasta);

        /// <summary>Habilita o deshabilita las credenciales del usuario indicado.</summary>
        void ActualizarEstado(string nombreUsuario, bool activo);

        /// <summary>Indica si el perfil indicado posee el permiso indicado.</summary>
        bool PerfilTienePermiso(string perfil, string permiso);

        /// <summary>Actualiza el hash de contraseña del usuario indicado (recuperación de credenciales).</summary>
        void ActualizarPassword(int idUsuario, string hashPassword);

        /// <summary>Todos los usuarios del sistema, ordenados por nombre de usuario.</summary>
        List<Usuario> ObtenerTodos();
    }
}
