using UnityEngine;

namespace EcoTilt.Board
{
    /// <summary>
    /// Qué hacer con lo que genera la construcción del tablero. En el juego las mallas viven en memoria;
    /// en el editor se guardan como assets y los contenedores se instancian como prefabs enlazados.
    /// </summary>
    public interface IBoardBuildSink
    {
        void OnMeshCreated(Mesh mesh);
        GameObject Instantiate(GameObject prefab, Transform parent);
    }
}
