# Genera la planilla de equivalencias (estados y ultimos movimientos) desde ProcuIntermedia.
# Lee con SqlDataReader via .NET para respetar los acentos (sqlcmd los rompe en consola).
$ErrorActionPreference = "Stop"
$salida = "C:\Users\DEFAUL~2\AppData\Local\Temp\claude\246c5c90601f88e1\75c62d96-8898-449e-a60f-97cc1d9d375b\scratchpad\planilla"
$cn = New-Object System.Data.SqlClient.SqlConnection "Server=localhost;Database=ProcuIntermedia;Integrated Security=true;TrustServerCertificate=true"
$cn.Open()

# Devuelve una lista de pares (valor, cantidad) leidos del reader.
function Consultar($sql) {
    $cmd = $cn.CreateCommand()
    $cmd.CommandText = $sql
    $r = $cmd.ExecuteReader()
    $filas = New-Object System.Collections.ArrayList
    while ($r.Read()) {
        $null = $filas.Add([pscustomobject]@{ valor = $r.GetString(0); cantidad = $r.GetInt32(1) })
    }
    $r.Close()
    return $filas
}

$sqlEstados = "SELECT ISNULL(NULLIF(LTRIM(RTRIM(estado)),''),'(vacio o nulo)') AS valor, COUNT(*) AS cantidad " +
              "FROM stg.Expediente GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(estado)),''),'(vacio o nulo)') ORDER BY COUNT(*) DESC"
$sqlMovs    = "SELECT ISNULL(NULLIF(LTRIM(RTRIM(ult_mov_proc)),''),'(vacio o nulo)') AS valor, COUNT(*) AS cantidad " +
              "FROM stg.Expediente GROUP BY ISNULL(NULLIF(LTRIM(RTRIM(ult_mov_proc)),''),'(vacio o nulo)') ORDER BY COUNT(*) DESC"

$estados = Consultar $sqlEstados
$movs = Consultar $sqlMovs
$cn.Close()

$totalExp = 0
foreach ($f in $estados) { $totalExp += $f.cantidad }

function Escribir($filas, $archivo, $encabezadoDestino, $total) {
    $lineas = New-Object System.Collections.Generic.List[string]
    $lineas.Add("valor_en_ProcuIntermedia;cantidad_expedientes;porcentaje;$encabezadoDestino;observaciones_del_estudio")
    foreach ($f in $filas) {
        $v = $f.valor -replace ';', ','
        $pct = [math]::Round(100.0 * $f.cantidad / $total, 2)
        $lineas.Add("$v;$($f.cantidad);$pct;;")
    }
    $ruta = Join-Path $salida $archivo
    # UTF-8 con BOM para que Excel en es-AR abra los acentos bien
    [System.IO.File]::WriteAllLines($ruta, $lineas, (New-Object System.Text.UTF8Encoding $true))
    Write-Output "$archivo : $($filas.Count) valores"
}

Escribir $estados "1-estados.csv" "INSTANCIA_destino" $totalExp
Escribir $movs "2-ultimos-movimientos.csv" "ETAPA_destino" $totalExp
Write-Output "total expedientes: $totalExp"
