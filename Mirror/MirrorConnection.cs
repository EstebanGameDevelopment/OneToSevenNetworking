#if ENABLE_MIRROR
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YourVRExperience.Utils;

namespace YourVRExperience.Networking
{
	public class MirrorConnection : NetworkBehaviour
	{
		private void Start()
		{
			int netID = (int)this.netId;
			syncInterval = 0.033f;

			if (isLocalPlayer)
			{
				SystemEventController.Instance.DispatchSystemEvent(NetworkController.EVENT_MIRROR_LOCAL_CONNECTION, this);

			}
			else
			{
				SystemEventController.Instance.DispatchSystemEvent(NetworkController.EVENT_MIRROR_NEW_CLIENT_CONNECTION, netID);
			}
		}

		[Command]
		public void CmdMessageFromClientsToServer(string _nameEvent, int _origin, int _target, string _data, string _types)
		{
			RpcMessageFromServerToClients(_nameEvent, _origin, _target, _data, _types);
		}

		[ClientRpc]
		private void RpcMessageFromServerToClients(string _nameEvent, int _origin, int _target, string _data, string _types)
		{
			List<object> parameters = new List<object>();
			MirrorController.Deserialize(parameters, _data, _types);
			NetworkController.Instance.DispatchEvent(_nameEvent, _origin, _target, parameters.ToArray());
		}

		[Command]
		public void CmdNetworkObject(string _uniqueNetworkName, string _prefab, int _instanceCounter, Vector3 _position, int _owner, string _data, string _types)
		{
			GameObject networkGO = Instantiate(Resources.Load(_prefab) as GameObject);
			networkGO.transform.position = _position;
			networkGO.name = _uniqueNetworkName;
			networkGO.GetComponent<NetworkGameID>().InitialData = _data;
			networkGO.GetComponent<NetworkGameID>().InitialTypes = _types;
			networkGO.GetComponent<NetworkGameID>().Owner = _owner;
			networkGO.GetComponent<NetworkGameID>().InstanceID = _instanceCounter;
			NetworkServer.Spawn(networkGO);
		}

		[Command]
		public void CmdAssignNetworkAuthority(NetworkIdentity _target, NetworkIdentity _clientId)
		{
			if (_target.hasAuthority && _target.connectionToClient != _clientId.connectionToClient)
			{
				_target.RemoveClientAuthority();
			}

			if (!_target.hasAuthority)
			{
				_target.AssignClientAuthority(_clientId.connectionToClient);
			}
		}

		[Command]
		public void CmdDestroy(NetworkIdentity _target)
		{
			GameObject.Destroy(_target.gameObject);
		}

	}
}
#endif