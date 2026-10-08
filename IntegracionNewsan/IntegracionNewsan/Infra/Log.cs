namespace IntegracionNewsan.Infra;

/// <summary>Log simple: escribe en consola y en un archivo por día (carpeta de logs).</summary>
public sealed class Log
{
    private readonly string _carpeta;
    private readonly object _bloqueo = new();

    public Log(string carpeta)
    {
        _carpeta = carpeta;
        Directory.CreateDirectory(carpeta);
    }

    public void Info(string mensaje) => Escribir("INFO ", mensaje);

    public void Advertencia(string mensaje) => Escribir("AVISO", mensaje);

    public void Error(string mensaje, Exception? ex = null) =>
        Escribir("ERROR", ex is null ? mensaje : $"{mensaje} {ex.GetType().Name}: {ex.Message}");

    private void Escribir(string nivel, string mensaje)
    {
        var linea = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{nivel}] {mensaje}";
        lock (_bloqueo)
        {
            Console.WriteLine(linea);
            File.AppendAllText(Path.Combine(_carpeta, $"{DateTime.Now:yyyy-MM-dd}.log"), linea + Environment.NewLine);
        }
    }
}
