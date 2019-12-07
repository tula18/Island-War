﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    // Public  

    [Header("Login UI Panel")]
    public InputField playerNameInput;
    public GameObject loginPanel;

    [Header("Connection Status")]
    public Text connectionStatusText;

    [Header("Game Options UI Panel")]
    public GameObject gameOptionsPanel;

    [Header("Create Room UI Panel")]
    public GameObject createRoomPanel;
    public InputField roomNameInput;
    public InputField maxPlayerAmountInput;

    [Header("Inside Room UI Panel")]
    public GameObject insideRoomPanel;

    [Header("Room List UI Panel")]
    public GameObject roomListPanel;

    [Header("Join Random Room UI Panel")]
    public GameObject joinRandomRoomPanel;
    public GameObject roomListPrefab;
    public GameObject roomListParentGameObject;

    // Private 

    private Dictionary<string, RoomInfo> cachedRoomList;

    // ----------------------------------------------------------------------------- //

    #region Unity Methods

    // Start is called before the first frame update
    void Start()
    {
        ActivatePanel(loginPanel.name);
        cachedRoomList = new Dictionary<string, RoomInfo>();
    }

    // Update is called once per frame
    void Update()
    {
        // Display connection status
        connectionStatusText.text = "Connection status : " + PhotonNetwork.NetworkClientState;
    }

    #endregion

    #region Private Methods

    // Join room button located inside RoomListPrefab
    void OnJoinedRoomButtonClicked(string roomName)
    {
        // At this stage we do not need to stay in lobby
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
        }
        PhotonNetwork.JoinRoom(roomName);
    }

    #endregion

    #region Public Methods

    // Activate only relevant panel and deactivate the rest 
    public void ActivatePanel(string panelToActivate)
    {
        loginPanel.SetActive(panelToActivate.Equals(loginPanel.name));
        gameOptionsPanel.SetActive(panelToActivate.Equals(gameOptionsPanel.name));
        createRoomPanel.SetActive(panelToActivate.Equals(createRoomPanel.name));
        roomListPanel.SetActive(panelToActivate.Equals(roomListPanel.name));
        insideRoomPanel.SetActive(panelToActivate.Equals(insideRoomPanel.name));
        joinRandomRoomPanel.SetActive(panelToActivate.Equals(joinRandomRoomPanel.name));
    }

    #endregion

    #region UI Callbacks

    // Create Room button located in the CreateRoomPanel
    public void OnRoomCreateButtonClicked()
    {
        string roomName = roomNameInput.text;
        // If room name field is empty generate a random room name E.g. Room 324
        if (string.IsNullOrEmpty(roomName))
        {
            roomName = "Room " + Random.Range(1, 1000);
        }
        // Set room configurations
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = (byte)int.Parse(maxPlayerAmountInput.text);
        // Create room on a server
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    // Cancel button located in the CreateRoomPanel
    public void OnCancelButtonClicked()
    {
        ActivatePanel(gameOptionsPanel.name);
    }

    // Login button located in the LoginPanel
    public void OnLoginButtonClicked()
    {
        string playerName = playerNameInput.text;
        if (!string.IsNullOrEmpty(playerName))
        {
            // Assign player name on server
            PhotonNetwork.LocalPlayer.NickName = playerName;
            // Connect to Photon server
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            Debug.Log("Player name is invalid!");
        }
    }

    // Room List button located in GameOptionsPanel
    public void OnShowRoomListButtonClicked()
    {
        // Must be in lobby to see the existing rooms list
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        ActivatePanel(roomListPanel.name);
    }

    #endregion

    #region Photon Callbacks

    // On connection to Internet
    public override void OnConnected()
    {
        Debug.Log("Connected to Internet");
    }

    // On connection to Photon server
    public override void OnConnectedToMaster()
    {
        Debug.Log(PhotonNetwork.LocalPlayer.NickName + " is connected to Photon server");
        ActivatePanel(gameOptionsPanel.name);
    }

    // Called for any update of the room-listing while in a lobby on the Master Server
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo room in roomList)
        {
            Debug.Log(room.Name);
            // Remove the lists that are closed, invisible or removed from the list
            // RemovedFromList is true when the room is hidden, closed or full
            if (!room.IsOpen || !room.IsVisible || room.RemovedFromList)
            {
                if (cachedRoomList.ContainsKey(room.Name))
                {
                    cachedRoomList.Remove(room.Name);
                }
            }
            else
            {
                // Update cached room list
                if (cachedRoomList.ContainsKey(room.Name))
                {
                    cachedRoomList[room.Name] = room;
                }
                // Add the new room to the cached room list
                else
                {
                    cachedRoomList.Add(room.Name, room);
                }
            }
        }
        // Instantiate game object roomListPrefab
        foreach (RoomInfo room in cachedRoomList.Values)
        {
            GameObject roomListGameObject = Instantiate(roomListPrefab);
            // Put newly instantiated roomListObjects under Parent object for better arrangement
            roomListGameObject.transform.SetParent(roomListParentGameObject.transform);
            // Avoid scaling issues
            roomListGameObject.transform.localScale = Vector3.one;
            // Set name and player amount of a room 
            roomListGameObject.transform.Find("RoomNameText").GetComponent<Text>().text = room.Name;
            roomListGameObject.transform.Find("RoomPlayersText").GetComponent<Text>().text = room.PlayerCount + 
            " / " + room.MaxPlayers;
            // Join relevant room 
            roomListGameObject.transform.Find("JoinRoomButton").GetComponent<Button>().onClick.AddListener(() 
            => OnJoinedRoomButtonClicked(room.Name));
        }
    }

    // Called when this client created a room and entered it (OnJoinedRoom is being called as well)
    public override void OnCreatedRoom()
    {
        Debug.Log(PhotonNetwork.CurrentRoom.Name + " is created");
    }

    // Called when any client joined an existing room 
    public override void OnJoinedRoom()
    {
        Debug.Log(PhotonNetwork.LocalPlayer.NickName + " has joined to " + PhotonNetwork.CurrentRoom.Name);
        ActivatePanel(insideRoomPanel.name);
    }

    #endregion
}
