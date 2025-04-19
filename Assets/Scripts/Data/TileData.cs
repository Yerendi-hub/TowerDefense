using System;

namespace TowerDefense.Data
{
    [Serializable]
    public class TileData
    {
        public int X;
        public int Y;
        public string DefinitionId;
        public int Rotation;
    }
}