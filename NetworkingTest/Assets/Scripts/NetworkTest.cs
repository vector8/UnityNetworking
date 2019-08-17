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

            print(msg.content);

            if (isServer)
            {
                // respond to message
                IPEndPoint endPoint = new IPEndPoint(msg.source.Address, clientPort);
                UdpNetworking.send("Hello client!", endPoint);
            }
        }

        if (!isServer && Input.GetKeyDown(KeyCode.Space))
        {
            UdpNetworking.send("Hello server!", new IPEndPoint(serverAddress, serverPort));
        }
    }

    private void OnApplicationQuit()
    {
        UdpNetworking.close();
    }
}
