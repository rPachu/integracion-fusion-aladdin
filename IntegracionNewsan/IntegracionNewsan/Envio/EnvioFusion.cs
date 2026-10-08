using IntegracionNewsan.Configuracion;
using IntegracionNewsan.Modelos;

namespace IntegracionNewsan.Envio;

/// <summary>
/// Envío real a Oracle Fusion. PENDIENTE: se completa cuando Newsan defina el usuario técnico
/// y el método de autenticación.
///
/// Lo que tiene que hacer (según Newsan, mail de Sofía del 08/10/2026):
///
/// 1. ObtenerDisponibleAsync:
///    GET {UrlBase}/fscmRestApi/resources/11.13.18.05/workOrders
///        ?q=OrganizationCode='{organizacion}' and WorkOrderNumber='{otFusion}'
///        &amp;expand=WorkOrderOperation&amp;onlyData=true
///    Devolver ReadyQuantity de la operación (ReceivedQuantity / ReadyQuantity / CompletedQuantity).
///    Leer también OperationSequenceNumber y OrganizationCode para usarlos en la declaración.
///
/// 2. EnviarAsync:
///    POST {UrlBase}/fscmRestApi/resources/11.13.18.05/operationTransactions
///    con el JSON de DeclaracionPT. Ok si responde 2xx; si no, devolver el detalle del error.
/// </summary>
public sealed class EnvioFusion(FusionOpciones opciones) : IEnvioDeclaracion
{
    private string Pendiente =>
        "El envío a Fusion todavía no está implementado (falta definir credenciales con Newsan). " +
        $"Usar Ejecucion.ModoEnvio = \"Simulacion\". (UrlBase configurada: '{opciones.UrlBase}')";

    public Task<int?> ObtenerDisponibleAsync(string otFusion, CancellationToken ct) =>
        throw new NotImplementedException(Pendiente);

    public Task<ResultadoEnvio> EnviarAsync(MotoDeclarada moto, DeclaracionPT declaracion, CancellationToken ct) =>
        throw new NotImplementedException(Pendiente);
}
