using EcoTilt.Board;
using UnityEditor;
using UnityEngine;

namespace EcoTilt.EditorTools
{
    /// <summary>
    /// En el editor: guarda las mallas del tablero como assets (para que la escena las conserve)
    /// e instancia los contenedores como prefabs enlazados.
    /// </summary>
    internal sealed class EditorBoardSink : IBoardBuildSink
    {
        private readonly string _meshFolder;

        public EditorBoardSink(string meshFolder)
        {
            _meshFolder = meshFolder;
            EditorAssetUtility.EnsureFolder(meshFolder);
        }

        public void OnMeshCreated(Mesh mesh)
        {
            string path = $"{_meshFolder}/{mesh.name}.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
        }

        public GameObject Instantiate(GameObject prefab, Transform parent)
        {
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        }
    }
}
