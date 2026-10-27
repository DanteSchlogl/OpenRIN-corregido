// Lógica de navegación para el prototipo Frutiger Aero
function navigateTo(screenId) {
    // 1. Obtener todas las pantallas y ocultarlas (quitar la clase "active")
    const screens = document.querySelectorAll('.screen');
    screens.forEach(screen => {
        screen.classList.remove('active');
    });

    // 2. Buscar la pantalla destino y mostrarla (agregar la clase "active")
    const targetScreen = document.getElementById(screenId);
    if (targetScreen) {
        targetScreen.classList.add('active');
    } else {
        console.error('Pantalla no encontrada:', screenId);
    }
}

// 3. (Opcional pero recomendado) Asegurar que al recargar la página,
// siempre se muestre la pantalla de Login como punto de partida.
document.addEventListener('DOMContentLoaded', () => {
    navigateTo('login-screen');
});