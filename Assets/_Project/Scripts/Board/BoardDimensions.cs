using System;
using UnityEngine;

namespace EcoTilt.Board
{
    /// <summary>Medidas del tablero (en metros). Las del laberinto (pasillo y pared) están en el BoardLayout.</summary>
    [Serializable]
    public sealed class BoardDimensions
    {
        [Header("Física")]
        public float FloorThickness = 0.3f;
        public float WallHeight = 0.7f;

        [Header("Visual")]
        public float SlabDepth = 0.9f;
        public float TileGap = 0.06f;
        public float TileHeight = 0.2f;
        public float PlinthHeight = 0.12f;
        public float RailTop = 0.66f;
        public float PostSpacing = 0.2f;
        public float PostSize = 0.05f;

        [Header("Salida y salto")]
        public float ApronLength = 2.2f;
        public float ApronWidth = 6.4f;
        public float RampLength = 1.6f;
        public float RampRise = 0.4f;
        public float RampThickness = 0.25f;
        public float GapLength = 1.1f;

        [Header("Contenedores")]
        public float BinWidth = 1.8f;
        public float BinDepth = 1.8f;
        public float BinHeight = 1.0f;
        public float BinWall = 0.08f;
        public float BinSpacing = 2.0f;
        public float BinRimY = -0.6f;

        [Header("Inicio")]
        public float SpawnHeight = 1.2f;
    }
}
