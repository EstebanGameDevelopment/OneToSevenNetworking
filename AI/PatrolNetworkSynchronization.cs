using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YourVRExperience.Utils;

namespace YourVRExperience.Networking
{
    [RequireComponent(typeof(PatrolWaypoints))]
    public class PatrolNetworkSynchronization : MonoBehaviour
    {
        public const string EVENT_NETWORK_PATROLWAYPOINTS_REQUEST_SYNCRONIZATION = "EVENT_NETWORK_PATROLWAYPOINTS_REQUEST_SYNCRONIZATION";
        public const string EVENT_NETWORK_PATROLWAYPOINTS_RESPONSE_SYNCRONIZATION = "EVENT_NETWORK_PATROLWAYPOINTS_RESPONSE_SYNCRONIZATION";

        private PatrolWaypoints m_patrolWaypoints;

        void Awake()
        {
            m_patrolWaypoints = this.GetComponent<PatrolWaypoints>();
            SystemEventController.Instance.Event += OnSystemEvent;
        }

        void OnDestroy()
        {
            if (SystemEventController.Instance != null) SystemEventController.Instance.Event -= OnSystemEvent;
            if (NetworkController.Instance != null) NetworkController.Instance.NetworkEvent -= OnNetworkEvent;            
        }

        private void OnNetworkEvent(string _nameEvent, int _originNetworkID, int _targetNetworkID, object[] _parameters)
        {
            if (_nameEvent == EVENT_NETWORK_PATROLWAYPOINTS_REQUEST_SYNCRONIZATION)
            {
                if (NetworkController.Instance.IsServer)
                {
                    string namePlatform = (string)_parameters[0];
                    if (this.gameObject.name == namePlatform)
                    {
                        NetworkController.Instance.DispatchNetworkEvent(EVENT_NETWORK_PATROLWAYPOINTS_RESPONSE_SYNCRONIZATION, -1, -1, this.gameObject.name, m_patrolWaypoints.State, transform.position, m_patrolWaypoints.CurrentWaypoint, m_patrolWaypoints.TimeDone);
                    }
                }
            }
            if (_nameEvent == EVENT_NETWORK_PATROLWAYPOINTS_RESPONSE_SYNCRONIZATION)
            {
                string namePlatform = (string)_parameters[0];
                if (this.gameObject.name == namePlatform)
                {
                    m_patrolWaypoints.State = (int)_parameters[1];
                    transform.position = (Vector3)_parameters[2];
                    m_patrolWaypoints.CurrentWaypoint = (int)_parameters[3];
                    m_patrolWaypoints.TimeDone = (float)_parameters[4];
                }
            }
        }

        private void OnSystemEvent(string _nameEvent, object[] _parameters)
        {
            if (_nameEvent == PatrolWaypoints.EVENT_PATROLWAYPOINTS_HAS_STARTED)
            {
                if (m_patrolWaypoints == (PatrolWaypoints)_parameters[0])
                {
                    bool autoStart = (bool)_parameters[1];
                    if (autoStart)
                    {
                        if (NetworkController.Instance.IsConnected)
                        {
                            NetworkController.Instance.NetworkEvent += OnNetworkEvent;
                            if (!NetworkController.Instance.IsServer)
                            {
                                NetworkController.Instance.DispatchNetworkEvent(EVENT_NETWORK_PATROLWAYPOINTS_REQUEST_SYNCRONIZATION, -1, -1, this.gameObject.name);
                            }
                        }
                    }
                }
            }
            if (_nameEvent == PatrolWaypoints.EVENT_PATROLWAYPOINTS_WAYPOINT_TO_CERO)
            {
                if (this.gameObject == (GameObject)_parameters[0])
                {
                    if (NetworkController.Instance.IsConnected)
                    {
                        if (NetworkController.Instance.IsServer)
                        {
                            NetworkController.Instance.DispatchNetworkEvent(EVENT_NETWORK_PATROLWAYPOINTS_RESPONSE_SYNCRONIZATION, -1, -1, this.gameObject.name, m_patrolWaypoints.State, transform.position, m_patrolWaypoints.CurrentWaypoint, m_patrolWaypoints.TimeDone);
                        }
                    }
                }                
            }
        }
    }
}