using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class UDPListener : MonoBehaviour
{
    Thread receiveThread;
    UdpClient client;

    //public TouchInteraction snapping;
    //public HandInteractionManager interactionManager;
    public TouchGestureController touchGestureController;

    public int port = 5007;

    string receivedData = "";

    public void Start()
    {
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                receivedData = Encoding.UTF8.GetString(data);
                // extract id and state _ spearated
                //int idx = Int32.Parse(receivedData.Split('_')[0]);
                //int state = Int32.Parse(receivedData.Split('_')[1]);

                //new touch device
                int idx = Int32.Parse(receivedData.Split(',')[0]);
                int touchValue = Int32.Parse(receivedData.Split(',')[1]);

                
                //UnityMainThreadDispatcher.Instance().Enqueue(() => interactionManager.UpdateTouchState(receivedData));
                //UnityMainThreadDispatcher.Instance().Enqueue(() => touchGestureController.UpdateTouchStateBySpecificFinger(idx, state));
                //UnityMainThreadDispatcher.Instance().Enqueue(() => touchGestureController.UpdateTouchStateAllFinger(receivedData));
                UnityMainThreadDispatcher.Instance().Enqueue(() => touchGestureController.UpdateIndirectTouchStates(idx, touchValue));
            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }

    // on destroy close socket
    void OnDestroy()
    {
        if (receiveThread != null)
            receiveThread.Abort();
        if(client != null)
            client.Close();
    }
}