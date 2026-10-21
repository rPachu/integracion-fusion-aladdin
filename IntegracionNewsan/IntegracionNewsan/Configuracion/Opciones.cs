namespace IntegracionNewsan.Configuracion;

/// <summary>
/// Configuración del programa. Se lee de appsettings.json y, encima, de appsettings.Local.json
/// (este último guarda las contraseñas y NO se sube al repositorio).
/// </summary>
public sealed class Opciones
{
    public CapatazOpciones Capataz { get; set; } = new();
    public FusionOpciones Fusion { get; set; } = new();
    public DeclaracionOpciones Declaracion { get; set; } = new();
    public EjecucionOpciones Ejecucion { get; set; } = new();
}

/// <summary>Conexión a la API de CAPATAZ.</summary>
public sealed class CapatazOpciones
{
    /// <summary>Ej.: http://localhost:18001 (en el servidor del cliente, su IP o nombre).</summary>
    public string UrlApi { get; set; } = "http://localhost:18001";
    public string Usuario { get; set; } = "";
    public string Password { get; set; } = "";
    /// <summary>Id de la empresa en CAPATAZ (header idEmp).</summary>
    public int IdEmpresa { get; set; }
    /// <summary>Id de la consulta personalizada "Integracion Fusion" (cambia en cada instalación).</summary>
    public int IdConsulta { get; set; }
    public int TimeoutSegundos { get; set; } = 60;
}

/// <summary>Conexión a Oracle Fusion (Newsan). Autenticación básica con el usuario técnico.</summary>
public sealed class FusionOpciones
{
    /// <summary>Ej.: https://hdqc-dev1.fa.ocs.oraclecloud.com (TEST)</summary>
    public string UrlBase { get; set; } = "";
    public string Usuario { get; set; } = "";
    /// <summary>Va en appsettings.Local.json (NO se sube al repositorio).</summary>
    public string Password { get; set; } = "";
    public string VersionApi { get; set; } = "11.13.18.05";
    public int TimeoutSegundos { get; set; } = 60;
    /// <summary>
    /// true = no se envía más que ReadyQuantity (regla de Newsan). false = se envía igual,
    /// solo para probar con Newsan el rechazo de Fusion. En producción, siempre true.
    /// </summary>
    public bool ControlarDisponible { get; set; } = true;
}

/// <summary>Valores fijos del JSON de declaración de producto terminado.</summary>
public sealed class DeclaracionOpciones
{
    public string SourceSystemCode { get; set; } = "FUSION_MOBILE";
    public string SourceSystemType { get; set; } = "EXTERNAL";
    /// <summary>Organización de Fusion (en el ejemplo de Newsan: PCANS).</summary>
    public string OrganizationCode { get; set; } = "";
    /// <summary>Secuencia de la operación en la OT. Con el envío real se toma de la consulta a la OT.</summary>
    public int WoOperationSequenceNumber { get; set; } = 10;
    /// <summary>Zona horaria a agregar a TransactionDate (ej. "-03:00"). Vacío = sin zona, como el ejemplo de Newsan.</summary>
    public string ZonaHoraria { get; set; } = "";
    public string TransactionNote { get; set; } = "Declaracion CAPATAZ Aladdin";
    public string UnidadMedida { get; set; } = "EA";
    public string FromDispatchState { get; set; } = "READY";
    public string ToDispatchState { get; set; } = "COMPLETE";
    /// <summary>Prefijo del LPN (ExternalSystemPackingUnit). Newsan pide que empiece con "LPN".</summary>
    public string PrefijoLpn { get; set; } = "LPN";
    /// <summary>Formato del número de serie (expresión regular). Newsan: 073-NNNNNNNNN/AAAA.</summary>
    public string FormatoSerie { get; set; } = @"^073-\d{9}/\d{4}$";
    /// <summary>Diferencia máxima (en años) entre el año modelo del VIN y el año de la declaración.</summary>
    public int ToleranciaAnoModelo { get; set; } = 1;
    public string FlexContext { get; set; } = "MOTOS";
    public string BloqueadoDnrpa { get; set; } = "No";
}

/// <summary>Cómo corre el programa.</summary>
public sealed class EjecucionOpciones
{
    /// <summary>"Api" (lee de CAPATAZ) o "ArchivoPrueba" (lee de un JSON local, para probar sin la API).</summary>
    public string Origen { get; set; } = "Api";
    public string ArchivoPrueba { get; set; } = "datos-prueba/consulta-ejemplo.json";
    /// <summary>"Simulacion" (guarda el JSON en la carpeta de salida) o "Fusion" (envío real, pendiente).</summary>
    public string ModoEnvio { get; set; } = "Simulacion";
    /// <summary>Si es true, en simulación las motos quedan marcadas como enviadas (igual que en el envío real).</summary>
    public bool RegistrarEnviadosEnSimulacion { get; set; } = true;
    /// <summary>Solo procesa motos de esta OT de Fusion (vacío = todas). Para pruebas controladas.</summary>
    public string FiltroOtFusion { get; set; } = "";
    /// <summary>Máximo de motos a enviar en una corrida (0 = sin límite). Para pruebas controladas.</summary>
    public int MaximoEnvios { get; set; } = 0;
    public string CarpetaSalida { get; set; } = "salida";
    public string CarpetaLogs { get; set; } = "logs";
    public string CarpetaEstado { get; set; } = "estado";
}
