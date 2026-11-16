namespace EcoTilt.Gameplay
{
    /// <summary>Objeto que puede volver al punto de inicio.</summary>
    public interface IRespawnable
    {
        bool IsRespawning { get; }
        void RequestRespawn();
    }
}
