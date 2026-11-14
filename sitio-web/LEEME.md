# procu360.com.ar · sitio promocional

Sitio estático de una sola página. No necesita base de datos ni nada que compilar: se suben los archivos tal cual.

## Qué subir

Todo el contenido de esta carpeta a la raíz del dominio (`wwwroot` del sitio en IIS, o `public_html` en un hosting con cPanel):

```
index.html          la página
404.html            página de "no encontrada"
assets/css/         estilos
assets/js/          comportamiento (botón para recorrer, contadores, formulario)
assets/img/         favicon
robots.txt, sitemap.xml
web.config          configuración para IIS (Windows Server)
.htaccess           configuración para Apache (hosting compartido)
```

Se usa `web.config` o `.htaccess` según el servidor; el otro se ignora y puede quedar. Este `LEEME.md` no hace falta subirlo, y si se sube, la configuración impide que se sirva.

## Antes de publicar

1. **Formspree.** Crear el formulario en https://formspree.io con el email que va a recibir las consultas y reemplazar `TU_ID_FORMSPREE` en `index.html` por su identificador (lo que sigue a `/f/` en el endpoint que te da Formspree). En la configuración del formulario, restringir el dominio a `procu360.com.ar`.
2. **WhatsApp.** Los enlaces usan `5493513473045`. Probar que abra el chat correcto; cm-tech.com.ar usa `543513473045`.
3. **Ingresar.** Los botones "Ingresar" y "Ya soy cliente" apuntan a `https://app.procu360.com.ar/cuenta/ingresar`. Si la aplicación va a vivir en otra dirección, cambiarla en `index.html` (aparece tres veces).
4. **Seguridad en servidores.** La sección "En los servidores" describe copias aisladas, discos cifrados y restauraciones de prueba: tiene que coincidir con lo que realmente esté implementado.

## IIS (Windows Server)

1. Crear el sitio con los enlaces `procu360.com.ar` y `www.procu360.com.ar`, en 80 y 443.
2. Instalar un certificado para los dos nombres (por ejemplo con win-acme, que usa Let's Encrypt y lo renueva solo).
3. Instalar el módulo **URL Rewrite**; sin él, `web.config` da error 500.
4. El app pool puede ser "Sin código administrado": el sitio es estático.

## Probar después de subir

- `http://procu360.com.ar` y `https://www.procu360.com.ar` redirigen a `https://procu360.com.ar/`.
- `https://procu360.com.ar/no-existe` muestra la página 404.
- El formulario envía y el aviso de "¡Gracias!" aparece; el email llega a la casilla configurada en Formspree.
- https://securityheaders.com da A o mejor.

## Al actualizar estilos o scripts

Los navegadores guardan estos archivos un día. Si se cambia `estilos.css` o `sitio.js`, subir el número de `?v=1` en las dos líneas de `index.html` que los cargan para que se vean los cambios de inmediato.
