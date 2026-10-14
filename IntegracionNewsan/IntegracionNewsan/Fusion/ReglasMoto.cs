using System.Globalization;
using System.Text.RegularExpressions;

namespace IntegracionNewsan.Fusion;

/// <summary>
/// Reglas de negocio de los datos de la moto, definidas con Newsan (oct-2026):
/// - Número de serie: 073-NNNNNNNNN/AAAA (9 dígitos + año calendario). Lo genera Aladdin (función NSAN de CAPATAZ).
/// - Año modelo: se toma del carácter 10 del VIN (norma ISO 3779). Ej.: V = 2027.
/// - LPN: uno por moto, empieza con "LPN". Se arma a partir del número de serie (único).
/// </summary>
public static class ReglasMoto
{
    // Códigos de año de la posición 10 del VIN (ISO 3779). Ciclo de 30 años: A = 1980, 2010, 2040...
    // No se usan I, O, Q, U, Z ni 0.
    private const string CodigosAno = "ABCDEFGHJKLMNPRSTVWXY123456789";
    private const int AnoBase = 1980;

    /// <summary>¿La serie respeta el formato configurado (expresión regular)?</summary>
    public static bool SerieValida(string serie, string formato) =>
        Regex.IsMatch(serie, formato, RegexOptions.CultureInvariant);

    /// <summary>
    /// Año modelo a partir del VIN. Como el código se repite cada 30 años, se elige el año
    /// más cercano al año de referencia (el de la declaración).
    /// Devuelve null y el motivo si no se puede determinar.
    /// </summary>
    public static int? AnoModeloDesdeVin(string vin, int anoReferencia, out string motivo)
    {
        motivo = "";
        var v = vin.Trim().ToUpperInvariant();

        if (v.Length != 17)
        {
            motivo = $"el VIN tiene {v.Length} caracteres (deben ser 17)";
            return null;
        }

        var indice = CodigosAno.IndexOf(v[9]);
        if (indice < 0)
        {
            motivo = $"el carácter 10 del VIN ('{v[9]}') no es un código de año válido";
            return null;
        }

        var ano = AnoBase + indice;
        while (ano + 15 < anoReferencia) ano += 30; // acerca el año al de referencia
        return ano;
    }

    /// <summary>Año de la declaración (si la fecha no se puede leer, el año actual).</summary>
    public static int AnoDeclaracion(string fechaDeclaracion) =>
        DateTime.TryParse(fechaDeclaracion, CultureInfo.InvariantCulture, DateTimeStyles.None, out var f)
            ? f.Year
            : DateTime.Today.Year;

    /// <summary>LPN de la moto: prefijo + número de serie, con "/" reemplazado por "-". Ej.: LPN073-000000006-2026.</summary>
    public static string Lpn(string serie, string prefijo) => prefijo + serie.Trim().Replace('/', '-');
}
