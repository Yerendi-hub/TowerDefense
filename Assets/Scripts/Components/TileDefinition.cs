using UnityEngine;
using TowerDefense.Enums;

namespace TowerDefense.Components
{
    [RequireComponent(typeof(MeshFilter))]
    public class TileDefinition : MonoBehaviour
    {
        [SerializeField] private string _definitionId;
        [SerializeField] private ConnectionType _north;
        [SerializeField] private ConnectionType _east;
        [SerializeField] private ConnectionType _south;
        [SerializeField] private ConnectionType _west;
        [SerializeField] private Sprite _sprite;

        public string DefinitionId => _definitionId;
        public ConnectionType North => _north;
        public ConnectionType East => _east;
        public ConnectionType South => _south;
        public ConnectionType West => _west;
        public Sprite Sprite => _sprite;
    }
}