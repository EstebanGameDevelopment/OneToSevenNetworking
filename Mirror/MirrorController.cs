#if ENABLE_MIRROR
using Mirror;
using Mirror.Discovery;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YourVRExperience.Utils;

namespace YourVRExperience.Networking
{
    [RequireComponent(typeof(NetworkDiscovery))]
    public class MirrorController : NetworkManager
    {
        private static MirrorController _instance;

        public static MirrorController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType<MirrorController>();
                    if (!_instance)
                    {
                        _instance = Instantiate(Resources.Load("Prefabs/Network/Mirror/MirrorController") as GameObject).GetComponent<MirrorController>();
                        _instance.gameObject.name = "MirrorController";
                        DontDestroyOnLoad(_instance.gameObject);
                    }
                }
                return _instance;
            }
        }

        private NetworkDiscovery m_networkDiscovery;
        private bool m_discovering = false;
        private bool m_isServer = false;
        private bool m_isConnected = false;
        private MirrorConnection m_mirrorConnection;

        private int m_instanceCounter = 0;

        public int UniqueNetworkID
        {
            get
            {
                if (m_mirrorConnection != null)
                {
                    return (int)m_mirrorConnection.netId;
                }
                else
                {
                    return -1;
                }
            }
        }

        public MirrorConnection Connection
        {
            get { return m_mirrorConnection; }
        }

        public bool IsConnected
        {
            get { return m_isConnected; }
        }

        public bool IsServer
        {
            get { return m_isServer; }
        }

        public void Initialize()
        {
        }

        public void Connect()
        {
            if (m_networkDiscovery == null)
            {
                m_networkDiscovery = GetComponent<NetworkDiscovery>();
                m_networkDiscovery.OnServerFound.AddListener(OnDiscoveredServer);
            }

            m_discovering = true;
            m_isConnected = false;
            m_networkDiscovery.StartDiscovery();

            SystemEventController.Instance.Event += OnSystemEvent;
            Invoke("CancelDiscovery", 3);

            Debug.LogError("%%%%%%%%%% MirrorDiscoveryController::START SEARCHING FOR A SERVER...");
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            SystemEventController.Instance.Event -= OnSystemEvent;

            if (this.mode == NetworkManagerMode.Host)
            {
                NetworkServer.Shutdown();
            }
            else
            {
                NetworkClient.Shutdown();
            }

            m_networkDiscovery.OnServerFound.RemoveListener(OnDiscoveredServer);
            m_networkDiscovery = null;
        }

        private void OnSystemEvent(string _nameEvent, object[] _parameters)
        {
            if (_nameEvent == NetworkController.EVENT_MIRROR_LOCAL_CONNECTION)
            {
                m_mirrorConnection = (MirrorConnection)_parameters[0];
                SystemEventController.Instance.DispatchSystemEvent(NetworkController.EVENT_NETWORK_CONNECTION_WITH_ROOM);
            }
            if (_nameEvent == NetworkController.EVENT_MIRROR_NEW_CLIENT_CONNECTION)
            {

            }
        }

        public void OnDiscoveredServer(ServerResponse info)
        {
            if (m_discovering)
            {
                m_discovering = false;
                m_isServer = false;
                m_isConnected = true;
                StartClient(info.uri);
                Debug.LogError("%%%%%%%%%% MirrorDiscoveryController::STARTED AS A CLIENT (MIRROR) CONNECTED TO SERVER[" + info.EndPoint.Address.ToString() + "].");
            }
        }

        private void CancelDiscovery()
        {
            if (m_discovering)
            {
                m_discovering = false;
                m_networkDiscovery.StopDiscovery();

                m_isServer = true;
                m_isConnected = true;
                StartHost();
                m_networkDiscovery.AdvertiseServer();
                Debug.LogError("%%%%%%%%%% MirrorDiscoveryController::STARTED AS A SERVER (MIRROR).");
            }
        }

        public int CmdNetworkObject(string _uniqueNetworkName, string _prefab, Vector3 _position, int _owner, params object[] _parameters)
        {
            string initialData = "";
            string initialTypes = "";
            MirrorController.Serialize(_parameters, ref initialData, ref initialTypes);
            m_mirrorConnection.CmdNetworkObject(_uniqueNetworkName, _prefab, m_instanceCounter, _position, _owner, initialData, initialTypes);
            m_instanceCounter++;
            return m_instanceCounter - 1;
        }

        public void TakeNetworkAuthority(NetworkIdentity _target)
        {
            m_mirrorConnection.CmdAssignNetworkAuthority(_target, m_mirrorConnection.GetComponent<NetworkIdentity>());
        }

        public void DispatchNetworkEvent(string _nameEvent, int _originNetworkID, int _targetNetworkID, params object[] _list)
        {
            string types = "";
            string output = "";
            Serialize(_list, ref output, ref types);
            m_mirrorConnection.CmdMessageFromClientsToServer(_nameEvent, _originNetworkID, _targetNetworkID, output, types);
        }

        public static void Serialize(object[] _list, ref string _output, ref string _types)
        {
            for (int i = 0; i < _list.Length; i++)
            {
                if (_list[i] is int)
                {
                    _output += ((int)_list[i]).ToString();
                    _types += "int";
                }
                else if (_list[i] is float)
                {
                    _output += ((float)_list[i]).ToString();
                    _types += "float";
                }
                else if (_list[i] is string)
                {
                    _output += ((string)_list[i]);
                    _types += "string";
                }
                else if (_list[i] is bool)
                {
                    _output += ((bool)_list[i]).ToString();
                    _types += "bool";
                }
                else if (_list[i] is Vector3)
                {
                    _output += Utilities.SerializeVector3((Vector3)_list[i]);
                    _types += "Vector3";
                }
                if (i + 1 < _list.Length)
                {
                    _output += ",";
                    _types += ",";
                }
            }
        }

        public static void Deserialize(List<object> _parameters, string _data, string _types)
        {
            string[] data = _data.Split(',');
            string[] types = _types.Split(',');

            for (int i = 0; i < data.Length; i++)
            {
                string type = types[i];
                if (type.Equals("int"))
                {
                    _parameters.Add(int.Parse(data[i]));
                }
                else if (type.Equals("float"))
                {
                    _parameters.Add(float.Parse(data[i]));
                }
                else if (type.Equals("string"))
                {
                    _parameters.Add(data[i]);
                }
                else if (type.Equals("bool"))
                {
                    _parameters.Add(bool.Parse(data[i]));
                }
                else if (type.Equals("Vector3"))
                {
                    _parameters.Add(Utilities.DeserializeVector(data[i]));
                }
            }
        }
    }
}
#endif