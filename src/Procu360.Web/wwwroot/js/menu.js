// Preferencias de visualización del menú (por usuario, en este navegador) y atajo de teclado "[".
// Son conveniencias de visualización: si el navegador bloquea el almacenamiento, el menú igual funciona.

export function leerPreferencia(clave) {
    try {
        return window.localStorage.getItem(clave);
    } catch {
        return null;
    }
}

export function guardarPreferencia(clave, valor) {
    try {
        window.localStorage.setItem(clave, valor);
    } catch {
        // Sin almacenamiento: la preferencia dura solo mientras la página esté abierta.
    }
}

export function anchoVentana() {
    return window.innerWidth;
}

// El acordeón del logo se muestra una vez por sesión del navegador (sessionStorage).
// Sin almacenamiento, se anima en cada carga completa: no rompe nada.
export function logoYaAnimado() {
    try {
        return window.sessionStorage.getItem("procu360.logo") === "1";
    } catch {
        return false;
    }
}

export function marcarLogoAnimado() {
    try {
        window.sessionStorage.setItem("procu360.logo", "1");
    } catch {
        // Sin almacenamiento: la próxima carga vuelve a animar el logo.
    }
}

// Registra el atajo "[" una sola vez por página. Se ignora mientras se escribe en un campo.
export function registrarAtajo(referencia) {
    if (window.__procu360AtajoRegistrado) {
        window.__procu360AtajoRegistrado = referencia;
        return;
    }
    window.__procu360AtajoRegistrado = referencia;
    document.addEventListener("keydown", (evento) => {
        if (evento.key !== "[" || evento.ctrlKey || evento.metaKey || evento.altKey) return;
        if (esCampoDeEdicion(evento.target)) return;
        window.__procu360AtajoRegistrado?.invokeMethodAsync("AlternarDesdeAtajoAsync");
    });
}

function esCampoDeEdicion(elemento) {
    return !!elemento && (elemento.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(elemento.tagName));
}
