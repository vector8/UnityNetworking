using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

public class Message
{
    public enum MessageType
    {
        Connect,
        Disconnect,
        Ack,
        Update,
        Chat,
        Error
    }

    public uint messageID;
    private IPEndPoint _sourceEndPoint, _destinationEndPoint;
    public IPEndPoint sourceEndPoint
    {
        get
        {
            return _sourceEndPoint;
        }
        set
        {
            _sourceEndPoint = value;
        }
    }

    public IPEndPoint destinationEndPoint
    {
        get
        {
            return _destinationEndPoint;
        }
        set
        {
            _destinationEndPoint = value;
            messageID = UdpNetworking.getNextMessageID(_destinationEndPoint);
        }
    }
    public MessageType messageType;
    public string stringContent = "";

    public Message(byte[] bytesToParse, IPEndPoint source)
    {
        byte[] idBytes = new byte[sizeof(uint)];
        Array.Copy(bytesToParse, 0, idBytes, 0, sizeof(uint));
        messageID = (uint)byteArrayToObject(idBytes);

        byte[] typeBytes = new byte[sizeof(MessageType)];
        Array.Copy(bytesToParse, sizeof(uint), typeBytes, 0, sizeof(MessageType));
        messageType = (MessageType)byteArrayToObject(typeBytes);

        stringContent = Encoding.UTF8.GetString(bytesToParse, sizeof(uint) + sizeof(MessageType), bytesToParse.Length - (sizeof(uint) + sizeof(MessageType)));

        sourceEndPoint = source;
    }

    // If you use this constructor, you MUST manually set the destinationEndPoint property in order to send it!
    public Message()
    {

    }

    public Message(IPEndPoint destination)
    {
        destinationEndPoint = destination;
    }

    public byte[] toBytes()
    {
        int totalSize = sizeof(uint) + sizeof(MessageType) + stringContent.Length * sizeof(char);

        byte[] bytes = new byte[totalSize];

        objectToByteArray(messageID).CopyTo(bytes, 0);

        objectToByteArray(messageType).CopyTo(bytes, sizeof(uint));

        Encoding.ASCII.GetBytes(stringContent).CopyTo(bytes, sizeof(uint) + sizeof(MessageType));

        return bytes;
    }

    public static byte[] objectToByteArray(object obj)
    {
        BinaryFormatter bf = new BinaryFormatter();
        MemoryStream ms = new MemoryStream();
        bf.Serialize(ms, obj);
        return ms.ToArray();
    }

    public static object byteArrayToObject(byte[] arrBytes)
    {
        MemoryStream ms = new MemoryStream(arrBytes);
        BinaryFormatter bf = new BinaryFormatter();
        return bf.Deserialize(ms);
    }
}

public static class UdpNetworking
{
    public static Queue<Message> messageQueue = new Queue<Message>();

    private static UdpClient sender, listener;
    private static int _listenPort;

    public static void initialize(int listenPort)
    {
        Debug.Log(sizeof(Message.MessageType));

        _listenPort = listenPort;
        sender = new UdpClient();
        listener = new UdpClient(_listenPort);

        try
        {
            listener.BeginReceive(new AsyncCallback(receive), null);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }

    private static void receive(IAsyncResult asyncResult)
    {
        IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Any, _listenPort);
        byte[] received = listener.EndReceive(asyncResult, ref remoteIpEndPoint);

        Message msg = new Message(received, remoteIpEndPoint);
        messageQueue.Enqueue(msg);

        listener.BeginReceive(new AsyncCallback(receive), null);
    }

    public static void send(Message messageToSend)
    {
        if (messageToSend != null)
        {
            byte[] sendBuffer = messageToSend.toBytes();

            try
            {
                sender.BeginSend(sendBuffer, sendBuffer.Length, messageToSend.destinationEndPoint, sendComplete, null);
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

        Debug.Log($"Send complete. Number of bytes sent: {u.EndSend(asyncResult)}");
    }

    public static void close()
    {
        sender.Close();
        listener.Close();
    }

    private static Dictionary<IPEndPoint, uint> messageIDs = new Dictionary<IPEndPoint, uint>();

    public static uint getNextMessageID(IPEndPoint remote)
    {
        if (!messageIDs.ContainsKey(remote))
        {
            messageIDs[remote] = 0;
            return 0;
        }

        messageIDs[remote]++;
        return messageIDs[remote];
    }
}
