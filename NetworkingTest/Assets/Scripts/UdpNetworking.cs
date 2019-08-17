using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

using UnityEngine;

using System.Collections.Generic;

public struct Message
{
    public string content;
    public IPEndPoint source;
}

public static class UdpNetworking
{

    public static Queue<Message> messageQueue = new Queue<Message>();

    private static UdpClient sender, listener;
    private static int _listenPort;

    public static void initialize(int listenPort)
    {
        _listenPort = listenPort;
        sender = new UdpClient();
        listener = new UdpClient(_listenPort);

        try
        {
            listener.BeginReceive(new AsyncCallback(receive), null);
        }
        catch(Exception e)
        {
            Debug.LogError(e);
        }
    }

    private static void receive(IAsyncResult asyncResult)
    {
        IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, _listenPort);
        byte[] received = listener.EndReceive(asyncResult, ref remoteIpEndPoint);

        Message msg = new Message();
        msg.content = Encoding.UTF8.GetString(received);
        msg.source = remoteIpEndPoint;
        messageQueue.Enqueue(msg);

        listener.BeginReceive(new AsyncCallback(receive), null);
    }

    public static void send(string textToSend, IPEndPoint targetEndpoint)
    {
        if (textToSend.Length > 0)
        {
            byte[] sendBuffer = Encoding.ASCII.GetBytes(textToSend);

            try
            {
                sender.BeginSend(sendBuffer, sendBuffer.Length, targetEndpoint, sendComplete, null);
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception {e.Message}");
            }
        }
    }

    public static void sendComplete(IAsyncResult asyncResult)
    {
        UdpClient u = (UdpClient)asyncResult.AsyncState;

        Console.WriteLine($"Send complete. Number of bytes sent: {u.EndSend(asyncResult)}");
    }

    public static void close()
    {
        sender.Close();
        listener.Close();
    }
}
