#if ENABLE_PHOTON
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YourVRExperience.Utils;

namespace YourVRExperience.Networking
{
    public class PhotonController :
#if ENABLE_PHOTON
    MonoBehaviourPunCallbacks
#else
    MonoBehaviour
#endif
    {
#if ENABLE_PHOTON
        public const bool DEBUG = true;

        public const string MY_ROOM_NAME = "MyRoomName";
        public const byte PHOTON_EVENT_CODE = 99;

        // ----------------------------------------------
        // SINGLETON
        // ----------------------------------------------	
        private static PhotonController _instance;

        public static PhotonController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(PhotonController)) as PhotonController;
                    if (!_instance)
                    {
                        GameObject container = new GameObject();
                        DontDestroyOnLoad(container);
                        container.name = "PhotonController";
                        _instance = container.AddComponent(typeof(PhotonController)) as PhotonController;
                    }
                }
                return _instance;
            }
        }

        private int m_uniqueNetworkID = -1;
        private bool m_isConnected = false;
        private List<string> m_roomsLobby = new List<string>();
        private int m_totalNumberOfPlayers = -1;
        private RaiseEventOptions m_raiseEventOptions = new RaiseEventOptions
        {
            Receivers = ReceiverGroup.All,
            CachingOption = EventCaching.DoNotCache
        };
        private SendOptions m_sendOptions = new SendOptions { Reliability = true };

        public int UniqueNetworkID
        {
            get { return m_uniqueNetworkID; }
        }
        public bool IsServer
        {
            get { return PhotonNetwork.IsMasterClient; }
        }
        public bool IsConnected
        {
            get { return m_uniqueNetworkID != -1; }
        }

        public void Connect()
        {
            PhotonNetwork.LocalPlayer.NickName = Utilities.RandomCodeGeneration(UnityEngine.Random.Range(100, 999).ToString());
            PhotonNetwork.ConnectUsingSettings();

            PhotonNetwork.NetworkingClient.EventReceived += OnPhotonEvent;
        }

        void OnDestroy()
        {
            PhotonNetwork.NetworkingClient.EventReceived -= OnPhotonEvent;
        }

        private void OnPhotonEvent(EventData _eventData)
        {
            if (_eventData.Code == PHOTON_EVENT_CODE)
            {
                object[] data = (object[])_eventData.CustomData;
                string eventMessage = (string)data[0];
                int originNetworkID = (int)data[1];
                int targetNetworkID = (int)data[2];
                object[] paramsEvent = null;
                if (data.Length > 3)
                {
                    paramsEvent = new object[data.Length - 3];
                    for (int i = 3; i < data.Length; i++)
                    {
                        paramsEvent[i - 3] = (object)data[i];
                    }
                }
                NetworkController.Instance.DispatchEvent(eventMessage, originNetworkID, targetNetworkID, paramsEvent);
            }
        }

        public void DispatchNetworkEvent(string _nameEvent, int _originNetworkID, int _targetNetworkID, params object[] _list)
        {
            object[] data = new object[3 + _list.Length];
            data[0] = _nameEvent;
            data[1] = _originNetworkID;
            data[2] = _targetNetworkID;
            if (_list.Length > 0)
            {
                for (int i = 0; i < _list.Length; i++)
                {
                    data[3 + i] = _list[i];
                }
            }
            PhotonNetwork.RaiseEvent(PHOTON_EVENT_CODE, data, m_raiseEventOptions, m_sendOptions);
        }

        public override void OnConnectedToMaster()
        {
            if (DEBUG) Utilities.DebugLogColor("PhotonController::OnLeftLobby", Color.red);
            m_isConnected = true;
            GetListRooms();
        }

        public void GetListRooms()
        {
            if (!PhotonNetwork.InLobby)
            {
                PhotonNetwork.JoinLobby();
                if (DEBUG) Utilities.DebugLogColor("PhotonController::GetListRooms:REQUEST TO JOIN THE LOBBY", Color.red);
            }
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            if (DEBUG) Utilities.DebugLogColor("PhotonController::OnRoomListUpdate:roomList.Count[" + roomList.Count + "]", Color.red);
            m_roomsLobby.Clear();
            for (int i = 0; i < roomList.Count; i++)
            {
                RoomInfo info = roomList[i];
                if (!info.IsOpen || !info.IsVisible || info.RemovedFromList) continue;
                m_roomsLobby.Add(info.Name);
            }
            if (DEBUG) Utilities.DebugLogColor("PhotonController::OnRoomListUpdate::REPORTING LIST OF ROOMS[" + m_roomsLobby.Count + "]", Color.red);
            if (m_roomsLobby.Count > 0)
            {
                JoinRoom(MY_ROOM_NAME);
            }
            else
            {
                CreateRoom(MY_ROOM_NAME, 2);
            }
        }

        public void CreateRoom(string _nameRoom, int _totalNumberOfPlayers)
        {
            if (m_totalNumberOfPlayers == -1)
            {
                m_totalNumberOfPlayers = _totalNumberOfPlayers;
                RoomOptions options = new RoomOptions { MaxPlayers = (byte)m_totalNumberOfPlayers, PlayerTtl = 10000 };
                PhotonNetwork.CreateRoom(_nameRoom, options, null);
                if (DEBUG) Utilities.DebugLogColor("PhotonController::CreateRoom::CREATING THE ROOM...", Color.red);
            }
        }

        public void JoinRoom(string _room)
        {
            if (m_totalNumberOfPlayers == -1)
            {
                m_totalNumberOfPlayers = -999999;
                if (PhotonNetwork.InLobby)
                {
                    PhotonNetwork.LeaveLobby();
                }
                PhotonNetwork.JoinRoom(_room);
                if (DEBUG) Utilities.DebugLogColor("PhotonController::JoinRoom::JOINING THE ROOM....", Color.red);
            }
        }

        public override void OnLeftLobby()
        {
            if (DEBUG) Utilities.DebugLogColor("PhotonController::OnLeftLobby", Color.red);
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            if (DEBUG) Utilities.DebugLogColor("PhotonController::OnCreateRoomFailed", Color.red);
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            if (DEBUG) Utilities.DebugLogColor("PhotonController::OnJoinRoomFailed", Color.red);
        }

        public override void OnJoinedRoom()
        {
            if (m_uniqueNetworkID == -1)
            {
                m_uniqueNetworkID = PhotonNetwork.LocalPlayer.ActorNumber;
                SystemEventController.Instance.DispatchSystemEvent(NetworkController.EVENT_NETWORK_CONNECTION_WITH_ROOM);
                if (DEBUG) Utilities.DebugLogColor("PhotonController::OnJoinedRoom::UniqueNetworkID[" + UniqueNetworkID + "]::MasterClient[" + PhotonNetwork.IsMasterClient + "]", Color.red);
            }
        }

        public override void OnLeftRoom()
        {
            if (DEBUG) Utilities.DebugLogColor("PhotonController::OnLeftRoom", Color.red);
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            int otherNetworkID = newPlayer.ActorNumber;
            if (DEBUG) Utilities.DebugLogColor("PhotonController::OnPlayerEnteredRoom::otherNetworkID[" + otherNetworkID + "]", Color.red);
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            if (DEBUG) Utilities.DebugLogColor("PhotonController::OnPlayerLeftRoom", Color.red);
        }

        public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
        {
            if (DEBUG) Utilities.DebugLogColor("PhotonController::OnMasterClientSwitched", Color.red);
        }

        public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (DEBUG) Utilities.DebugLogColor("PhotonController::OnPlayerPropertiesUpdate", Color.red);
        }

        public GameObject CreateNetworkPrefab(string _uniqueNetworkName, GameObject _prefab, string _pathToPrefab, Vector3 _position, Quaternion _rotation, byte _data, params object[] _parameters)
        {
            GameObject networkInstance;
            if ((_parameters != null) && (_parameters.Length > 0))
            {
                networkInstance = PhotonNetwork.Instantiate(_pathToPrefab, _position, _rotation, _data, _parameters);
            }
            else
            {
                networkInstance = PhotonNetwork.Instantiate(_pathToPrefab, _position, _rotation, _data);
            }
            networkInstance.name = _uniqueNetworkName;
            return networkInstance;
        }
#endif
    }
}