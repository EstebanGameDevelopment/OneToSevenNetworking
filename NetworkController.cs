using UnityEngine;

namespace YourVRExperience.Networking
{
    public class NetworkController : ScriptableObject
    {
        public const string EVENT_NETWORK_CONNECTION_WITH_ROOM = "EVENT_NETWORK_CONNECTION_WITH_ROOM";

        public const string EVENT_MIRROR_NETWORK_AVATAR_INITED = "EVENT_MIRROR_NETWORK_AVATAR_INITED";
        public const string EVENT_MIRROR_LOCAL_CONNECTION = "EVENT_MIRROR_LOCAL_CONNECTION";
        public const string EVENT_MIRROR_NEW_CLIENT_CONNECTION = "EVENT_MIRROR_NEW_CLIENT_CONNECTION";

        public const bool DEBUG = true;

        // ----------------------------------------------
        // EVENT SYSTEM
        // ----------------------------------------------	
        public delegate void MultiplayerNetworkEvent(string _nameEvent, int _originNetworkID, int _targetNetworkID, params object[] _parameters);

        public event MultiplayerNetworkEvent NetworkEvent;

        public void DispatchEvent(string _nameEvent, int _originNetworkID, int _targetNetworkID, params object[] _parameters)
        {
            if (NetworkEvent != null) NetworkEvent(_nameEvent, _originNetworkID, _targetNetworkID, _parameters);
        }

        // ----------------------------------------------
        // SINGLETON
        // ----------------------------------------------	
        private static NetworkController _instance;

        public static NetworkController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = ScriptableObject.CreateInstance<NetworkController>();
                }
                return _instance;
            }
        }

        private int m_networkInstanceCounter = 0;
        private bool m_isMultiplayer = false;

        public int UniqueNetworkID
        {
            get
            {
#if ENABLE_PHOTON
                return PhotonController.Instance.UniqueNetworkID;
#elif ENABLE_MIRROR
                return MirrorController.Instance.UniqueNetworkID;
#else
            return -1;
#endif
            }
        }
        public bool IsServer
        {
            get
            {
#if ENABLE_PHOTON
                return PhotonController.Instance.IsServer;
#elif ENABLE_MIRROR
                return MirrorController.Instance.IsServer;
#else
            return false;
#endif
            }
        }
        public bool IsConnected
        {
            get
            {
                if (!m_isMultiplayer)
                {
                    return false;
                }
                else
                {
#if ENABLE_PHOTON
                return PhotonController.Instance.IsConnected;
#elif ENABLE_MIRROR
                    return MirrorController.Instance.IsConnected;
#else
            return false;
#endif
                }
            }
        }
        public bool IsMultiplayer
        {
            set { m_isMultiplayer = value; }
        }

        public void Initialize()
        {
#if ENABLE_MIRROR
            MirrorController.Instance.Initialize();
#endif
        }


        public void Connect()
        {
#if ENABLE_PHOTON
            PhotonController.Instance.Connect();
#elif ENABLE_MIRROR
            MirrorController.Instance.Connect();
#endif
        }


        public void DispatchNetworkEvent(string _nameEvent, int _originNetworkID, int _targetNetworkID, params object[] _list)
        {
#if ENABLE_PHOTON
            PhotonController.Instance.DispatchNetworkEvent(_nameEvent, _originNetworkID, _targetNetworkID, _list);
#elif ENABLE_MIRROR
            MirrorController.Instance.DispatchNetworkEvent(_nameEvent, _originNetworkID, _targetNetworkID, _list);
#endif
        }

        public string CreateNetworkPrefab(string _namePrefab, GameObject _prefab, string _pathToPrefab, Vector3 _position, Quaternion _rotation, byte _data, params object[] _parameters)
        {
            string uniqueNetworkName = _namePrefab + "_" + m_networkInstanceCounter++;
#if ENABLE_PHOTON
            PhotonController.Instance.CreateNetworkPrefab(uniqueNetworkName, _prefab, _pathToPrefab, _position, _rotation, _data, _parameters);
#elif ENABLE_MIRROR
            MirrorController.Instance.CmdNetworkObject(uniqueNetworkName, _pathToPrefab, _position, UniqueNetworkID, _parameters);
#endif
            return uniqueNetworkName;
        }
    }
}