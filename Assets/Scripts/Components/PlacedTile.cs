using TowerDefense.Enums;
using UnityEngine;

namespace TowerDefense.Components
{
    public class PlacedTile : MonoBehaviour
    {
        [SerializeField] private string _definitionId;
        [SerializeField] private ConnectionType _north;
        [SerializeField] private ConnectionType _east;
        [SerializeField] private ConnectionType _south;
        [SerializeField] private ConnectionType _west;

        public string DefinitionId
        {
            get => _definitionId;
            set => _definitionId = value;
        }

        public ConnectionType North
        {
            get => _north;
            set => _north = value;
        }

        public ConnectionType East
        {
            get => _east;
            set => _east = value;
        }

        public ConnectionType South
        {
            get => _south;
            set => _south = value;
        }

        public ConnectionType West
        {
            get => _west;
            set => _west = value;
        }
    }
}