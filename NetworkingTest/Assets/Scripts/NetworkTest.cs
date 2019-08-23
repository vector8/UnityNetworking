using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;

public class NetworkTest : MonoBehaviour
{
    public bool isServer = false;
    public int serverPort = 3000;
    public int clientPort = 3001;

    public IPAddress serverAddress = IPAddress.Loopback;

    // Start is called before the first frame update
    void Start()
    {
        if (isServer)
        {
            UdpNetworking.initialize(serverPort);
        }
        else
        {
            UdpNetworking.initialize(clientPort);
        }

    }

    // Update is called once per frame
    void Update()
    {
        while (UdpNetworking.messageQueue.Count > 0)
        {
            Message msg = UdpNetworking.messageQueue.Dequeue();

            print(msg.stringContent);

            if (isServer)
            {
                // respond to message
                IPEndPoint endPoint = new IPEndPoint(msg.sourceEndPoint.Address, clientPort);
                Message msgToSend = new Message(endPoint);
                msgToSend.messageType = Message.MessageType.Ack;
                msgToSend.stringContent = "Hello client!";
                UdpNetworking.send(msgToSend);
            }
        }

        if (!isServer && Input.GetKeyDown(KeyCode.Space))
        {
            Message message = new Message(new IPEndPoint(serverAddress, serverPort));
            message.messageType = Message.MessageType.Connect;
            message.stringContent = "Hello server!";
            UdpNetworking.send(message);
        }
    }

    private void OnApplicationQuit()
    {
        UdpNetworking.close();
    }
}
