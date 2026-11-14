// Procu360 · procu360.com.ar
// Comportamiento del sitio: botón para recorrer la página, contadores de "Nosotros"
// y envío del formulario de contacto a Formspree. Sin dependencias.
(function () {
  'use strict';

  var raiz = document.documentElement;
  raiz.classList.remove('no-js');
  raiz.classList.add('js');

  var reducirMovimiento = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  // ---------------------------------------------------------------- recorrer la página

  var boton = document.querySelector('.recorrer');
  if (boton) {
    var flecha = boton.querySelector('.flecha');

    var estaAlFinal = function () {
      return window.scrollY + window.innerHeight >= raiz.scrollHeight - 240;
    };

    var actualizar = function () {
      var alFinal = estaAlFinal();
      var rotulo = alFinal ? 'Volver al inicio' : 'Ir al final de la página';
      if (flecha) flecha.classList.toggle('arriba', alFinal);
      boton.setAttribute('aria-label', rotulo);
      boton.title = rotulo;
    };

    window.addEventListener('scroll', actualizar, { passive: true });
    window.addEventListener('resize', actualizar, { passive: true });
    actualizar();

    boton.addEventListener('click', function () {
      window.scrollTo({
        top: estaAlFinal() ? 0 : raiz.scrollHeight,
        behavior: reducirMovimiento ? 'auto' : 'smooth'
      });
    });
  }

  // ---------------------------------------------------------------- contadores de "Nosotros"

  var tarjetas = document.querySelectorAll('.stat');
  var contadores = document.querySelectorAll('[data-contar]');

  var contar = function (elemento) {
    var fin = parseInt(elemento.getAttribute('data-contar'), 10) || 0;
    if (reducirMovimiento) {
      elemento.textContent = String(fin);
      return;
    }
    var duracion = 1800;
    var inicio = null;
    var paso = function (ahora) {
      if (inicio === null) inicio = ahora;
      var avance = Math.min(1, (ahora - inicio) / duracion);
      var suavizado = 1 - Math.pow(1 - avance, 3);
      elemento.textContent = String(Math.round(fin * suavizado));
      if (avance < 1) window.requestAnimationFrame(paso);
    };
    window.requestAnimationFrame(paso);
  };

  if ('IntersectionObserver' in window && !reducirMovimiento) {
    // Arrancan en cero y cuentan recién cuando la tarjeta aparece en pantalla.
    Array.prototype.forEach.call(contadores, function (c) { c.textContent = '0'; });
    var observador = new IntersectionObserver(function (entradas) {
      entradas.forEach(function (entrada) {
        if (!entrada.isIntersecting) return;
        entrada.target.classList.add('visible');
        Array.prototype.forEach.call(entrada.target.querySelectorAll('[data-contar]'), contar);
        observador.unobserve(entrada.target);
      });
    }, { threshold: 0.35 });
    Array.prototype.forEach.call(tarjetas, function (t) { observador.observe(t); });
  } else {
    Array.prototype.forEach.call(tarjetas, function (t) { t.classList.add('visible'); });
  }

  // ---------------------------------------------------------------- formulario de contacto

  var formulario = document.getElementById('form-contacto');
  if (formulario) {
    var exito = document.getElementById('form-exito');
    var error = document.getElementById('form-error');
    var enviar = formulario.querySelector('button[type="submit"]');
    var textoEnviar = enviar ? enviar.querySelector('.texto-enviar') : null;

    var mostrarError = function (mensaje) {
      if (!error) return;
      error.textContent = mensaje;
      error.hidden = false;
    };

    formulario.addEventListener('submit', function (evento) {
      evento.preventDefault();
      if (error) error.hidden = true;
      if (enviar) enviar.disabled = true;
      if (textoEnviar) textoEnviar.textContent = 'Enviando…';

      fetch(formulario.action, {
        method: 'POST',
        body: new FormData(formulario),
        headers: { Accept: 'application/json' }
      })
        .then(function (respuesta) {
          if (respuesta.ok) {
            formulario.reset();
            formulario.hidden = true;
            if (exito) {
              exito.hidden = false;
              exito.focus();
            }
            return null;
          }
          return respuesta.json().then(function (datos) {
            var detalle = datos && datos.errors
              ? datos.errors.map(function (e) { return e.message; }).join(' ')
              : '';
            mostrarError(detalle || 'No pudimos enviar la consulta. Probá de nuevo o escribinos por WhatsApp.');
          }, function () {
            mostrarError('No pudimos enviar la consulta. Probá de nuevo o escribinos por WhatsApp.');
          });
        })
        .catch(function () {
          mostrarError('No hay conexión en este momento. Probá de nuevo en unos minutos.');
        })
        .then(function () {
          if (enviar) enviar.disabled = false;
          if (textoEnviar) textoEnviar.textContent = 'Enviar consulta';
        });
    });
  }

  // ---------------------------------------------------------------- año en el pie

  var anio = document.getElementById('anio');
  if (anio) anio.textContent = String(new Date().getFullYear());
})();
