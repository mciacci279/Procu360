// Botón flotante "ir al final": aparece solo cuando la página tiene más contenido abajo.
// Es una conveniencia de navegación; si algo falla, la página sigue funcionando con el scroll normal.

let referencia = null;
let observando = false;

/// Margen en píxeles para considerar que ya se llegó al final.
const MARGEN = 24;

export function registrar(dotnet) {
    referencia = dotnet;
    if (!observando) {
        observando = true;
        window.addEventListener("scroll", avisar, { passive: true });
        window.addEventListener("resize", avisar, { passive: true });
        // El contenido de Blazor cambia sin recargar la página: conviene revisar cada tanto.
        setInterval(avisar, 1000);
    }
    avisar();
}

export function desregistrar() {
    referencia = null;
}

/// True si hay contenido por debajo de lo que se ve.
function hayMasAbajo() {
    const alto = Math.max(
        document.body.scrollHeight, document.documentElement.scrollHeight,
        document.body.offsetHeight, document.documentElement.offsetHeight);
    const visto = window.scrollY + window.innerHeight;
    return alto - visto > MARGEN;
}

function avisar() {
    if (!referencia) return;
    try {
        referencia.invokeMethodAsync("CambioVisibilidad", hayMasAbajo());
    } catch {
        // El circuito se cerró (navegación o recarga): no hay a quién avisar.
        referencia = null;
    }
}

export function irAlFinal() {
    const alto = Math.max(
        document.body.scrollHeight, document.documentElement.scrollHeight,
        document.body.offsetHeight, document.documentElement.offsetHeight);
    window.scrollTo({ top: alto, behavior: "smooth" });
}

export function irAlInicio() {
    window.scrollTo({ top: 0, behavior: "smooth" });
}
