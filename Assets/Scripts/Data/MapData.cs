using System;
using System.Collections.Generic;

namespace TowerDefense.Data
{
    [Serializable]
    public class MapData
    {
        public int Width; 
        public int  Height;
        public List<TileData> Tiles = new();
    }
}