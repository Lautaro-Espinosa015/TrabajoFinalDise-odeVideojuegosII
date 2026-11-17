namespace EcoTilt.Gameplay
{
    /// <summary>Objeto que puede volver al punto de inicio.</summary>
    public interface IRespawnable
    {
        bool IsRespawning { get; }
        /// <summary>Pide volver al inicio. Devuelve false si no corresponde (ya está reapareciendo o está desactivado).</summary>
        bool RequestRespawn();
    }
}
