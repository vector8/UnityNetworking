using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;

public class LobbyServer : MonoBehaviour
{
    public int listenPort = 3000;

    // Clients currently connected to server, with latest message ID received
    public Dictionary<IPEndPoint, uint> connectedClients = new Dictionary<IPEndPoint, uint>();

    // Start is called before the first frame update
    void Start()
    {
        UdpNetworking.initialize(listenPort);
    }

    // Update is called once per frame
    void Update()
    {
        while (UdpNetworking.messageQueue.Count > 0)
        {
            Message msg = UdpNetworking.messageQueue.Dequeue();

            switch (msg.messageType)
            {
                case Message.MessageType.Connect:
                    if (!connectedClients.ContainsKey(msg.sourceEndPoint))
                    {
                        connectedClients[msg.sourceEndPoint] = msg.messageID;

                        Message connBroadcast = new Message();
                        connBroadcast.messageType = Message.MessageType.Update;
                        connBroadcast.stringContent = $"{msg.sourceEndPoint.Address.ToString()} has connected.";
                        Debug.Log(connBroadcast.stringContent);

                        foreach (IPEndPoint client in connectedClients.Keys)
                        {
                            if (client != msg.sourceEndPoint)
                            {
                                IPEndPoint dest = client;
                                dest.Port = 3001;
                                connBroadcast.destinationEndPoint = dest;

                                UdpNetworking.send(connBroadcast);
                            }
                        }
                    }
                    break;
                case Message.MessageType.Disconnect:
                    if (connectedClients.Remove(msg.sourceEndPoint))
                    {
                        Message dcBroadcast = new Message();
                        dcBroadcast.messageType = Message.MessageType.Update;
                        dcBroadcast.stringContent = $"{msg.sourceEndPoint.Address.ToString()} has disconnected.";
                        Debug.Log(dcBroadcast.stringContent);

                        foreach (IPEndPoint client in connectedClients.Keys)
                        {
                            IPEndPoint dest = client;
                            dest.Port = 3001;
                            dcBroadcast.destinationEndPoint = dest;

                            UdpNetworking.send(dcBroadcast);
                        }
                    }
                    break;
                case Message.MessageType.Ack:
                    break;
                case Message.MessageType.Update:
                    break;
                case Message.MessageType.Chat:
                    if (connectedClients.ContainsKey(msg.sourceEndPoint))
                    {
                        Debug.Log($"[MSG][{DateTime.Now.ToString()}][{msg.sourceEndPoint.ToString()}]: {msg.stringContent}");

                        Message broadcast = new Message();
                        broadcast.messageType = Message.MessageType.Chat;
                        broadcast.stringContent = $"{msg.sourceEndPoint.Address.ToString()} has connected.";

                        foreach (IPEndPoint client in connectedClients.Keys)
                        {
                            if (client != msg.sourceEndPoint)
                            {
                                IPEndPoint dest = client;
                                dest.Port = 3001;
                                broadcast.destinationEndPoint = dest;

                                UdpNetworking.send(broadcast);
                            }
                        }
                    }
                    else
                    {
                        IPEndPoint dest = msg.sourceEndPoint;
                        dest.Port = 3001;
                        Message errorMessage = new Message(dest);
                        errorMessage.messageType = Message.MessageType.Error;
                        errorMessage.stringContent = "Error: Unauthorized client.";

                        UdpNetworking.send(errorMessage);
                    }
                    break;
                case Message.MessageType.Error:
                    break;
                default:
                    break;
            }
        }
    }

    private void OnApplicationQuit()
    {
        UdpNetworking.close();
    }
}
