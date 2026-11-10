using Xunit;

// Las pruebas de identidad crean y limpian usuarios de prueba en la misma base:
// corren de a una para que la limpieza de una clase no borre datos de otra.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
