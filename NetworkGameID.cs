#if ENABLE_PHOTON
using Photon.Pun;
#endif
#if ENABLE_MIRROR
using Mirror;
#endif
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using YourVRExperience.Utils;

namespace YourVRExperience.Networking
{
    public class NetworkGameID :
#if ENABLE_PHOTON
    MonoBehaviour
#elif ENABLE_MIRROR
    NetworkBehaviour
#else
    MonoBehaviour
#endif
    {
#if ENABLE_PHOTON
    private bool m_hasBeenInited = true;
#else
        private bool m_hasBeenInited = false;
#endif

        public bool HasBeenInited
        {
            get { return m_hasBeenInited; }
        }

#if ENABLE_PHOTON
        private PhotonView m_photonView;
        public PhotonView PhotonView
        {
            get
            {
                if (m_photonView == null)
                {
                    if (this != null)
                    {
                        m_photonView = GetComponent<PhotonView>();
                    }
                }
                return m_photonView;
            }
        }
        private PhotonTransformView m_photonTransformView;
        public PhotonTransformView PhotonTransformView
        {
            get
            {
                if (m_photonTransformView == null)
                {
                    if (this != null)
                    {
                        m_photonTransformView = GetComponent<PhotonTransformView>();
                    }
                }
                return m_photonTransformView;
            }
        }
#elif ENABLE_MIRROR
        [SyncVar]
        public string InitialData;

        [SyncVar]
        public string InitialTypes;

        [SyncVar]
        public int Owner;

        [SyncVar]
        public int InstanceID;

        public object[] InstantiationData;

        private NetworkIdentity m_mirrorView;

        public NetworkIdentity MirrorView
        {
            get
            {
                if (m_mirrorView == null)
                {
                    if (this != null)
                    {
                        m_mirrorView = GetComponent<NetworkIdentity>();
                    }
                }
                return m_mirrorView;
            }
        }

        public void SetInitialData(params object[] _data)
        {
            InitialData = "";
            InitialTypes = "";
            MirrorController.Serialize(_data, ref InitialData, ref InitialTypes);
        }

        public override void OnStartClient()
        {
            List<object> parameters = new List<object>();
            MirrorController.Deserialize(parameters, InitialData, InitialTypes);
            InstantiationData = parameters.ToArray();
            m_hasBeenInited = true;
            SystemEventController.Instance.DispatchSystemEvent(NetworkController.EVENT_MIRROR_NETWORK_AVATAR_INITED, this.gameObject);
        }
#endif


        public void Initialize(string _nameEvent)
        {
#if ENABLE_PHOTON
            if (PhotonView != null)
            {
                if (PhotonView.InstantiationData != null)
                {
                    if (PhotonView.InstantiationData.Length > 0)
                    {
                        int animationInitial = (int)PhotonView.InstantiationData[0];
                        StartCoroutine(InitializeAnimationAvatar(_nameEvent, animationInitial));
                    }
                }
            }
#endif
        }

        IEnumerator InitializeAnimationAvatar(string _nameEvent, int _animationInitial)
        {
            yield return new WaitForSeconds(0.1f);
            NetworkController.Instance.DispatchEvent(_nameEvent, -1, -1, this.gameObject, _animationInitial);
        }

        public bool IsConnected()
        {
#if ENABLE_PHOTON
            return PhotonController.Instance.IsConnected;
#elif ENABLE_MIRROR
            return MirrorController.Instance.IsConnected;
#else
            return false;
#endif
        }

        public int GetViewID()
        {
#if ENABLE_PHOTON
            if (PhotonView != null) return PhotonView.ViewID;
#elif ENABLE_MIRROR
            if (MirrorView != null) return (int)MirrorView.netId;
#endif
            return -1;
        }

        public bool IsMine()
        {
#if ENABLE_PHOTON
            if (PhotonView != null) return PhotonView.IsMine;
#elif ENABLE_MIRROR
            if (MirrorView != null) return Owner == MirrorController.Instance.UniqueNetworkID;
#endif
            return false;
        }

        public bool AmOwner()
        {
#if ENABLE_PHOTON
            if (PhotonView != null) return PhotonView.AmOwner;
#elif ENABLE_MIRROR
            if (MirrorView != null) return Owner == MirrorController.Instance.UniqueNetworkID;
#endif
            return false;
        }

        public void Destroy()
        {
#if ENABLE_PHOTON
            if ((PhotonView != null) && PhotonView.IsMine) PhotonNetwork.Destroy(PhotonView);
#elif ENABLE_MIRROR
            if ((MirrorView != null) && IsMine()) MirrorController.Instance.Connection.CmdDestroy(MirrorView);
#endif
        }

        public void SetEnabled(bool _enabled)
        {
#if ENABLE_PHOTON
            if (PhotonView != null) PhotonView.enabled = _enabled;
            if (PhotonTransformView != null) PhotonTransformView.enabled = _enabled;
#elif ENABLE_MIRROR
            if (m_mirrorView != null) m_mirrorView.enabled = _enabled;
#endif
        }
    }
}