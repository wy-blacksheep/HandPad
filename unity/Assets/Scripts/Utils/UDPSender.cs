using UnityEngine;
using System.Collections;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDPSender : MonoBehaviour
{
    //knuckle data


    //touch data
    public string IP = "127.0.0.1";
    public int PORT = 5004;
    
    IPEndPoint remoteEndPoint;
    UdpClient client;
    
    public void Start()
    {
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(IP), PORT);
        client = new UdpClient();
        
    }
    public void sendString(string message)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length, remoteEndPoint);
        }
        catch (Exception err)
        {
            print(err.ToString());
        }
    }
}