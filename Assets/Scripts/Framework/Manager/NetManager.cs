using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetManager : MonoBehaviour
{
    private NetClient Client;
    /// <summary>
    /// 消息队列集合
    /// </summary>
    private Queue<KeyValuePair<int, string>> Messages = new Queue<KeyValuePair<int, string>>();
    private XLua.LuaFunction ReceiveMessage;

    public void Init()
    {
        Client = new NetClient();
        ReceiveMessage = Manager.Lua.LuaEnv.Global.Get<XLua.LuaFunction>("ReceiveMessage");
    }

    void Update()
    {
        if (Messages.Count > 0)
        {
            KeyValuePair<int, string> msg = Messages.Dequeue();
            ReceiveMessage?.Call(msg.Key, msg.Value);
        }
    }

    /// <summary>
    /// 接收数据
    /// </summary>
    /// <param name="msgid">ID</param>
    /// <param name="message">消息体</param>
    public void Receive(int msgid, string message)
    {
        Messages.Enqueue(new KeyValuePair<int, string>(msgid,message));
    }

    /// <summary>
    /// 发送消息
    /// </summary>
    /// <param name="messageid">消息ID</param>
    /// <param name="message">消息体</param>
    public void SendMessage(int messageid, string message)
    {
        Client.SendMessage(messageid,message);
    }

    /// <summary>
    /// 连接服务器
    /// </summary>
    /// <param name="post">地址</param>
    /// <param name="port">端口</param>
    public void connectService(string post, int port)
    {
        Client.OnConnectServer(post,port);
    }

    /// <summary>
    /// 网络连接
    /// </summary>
    public void NetConnected()
    {

    }

    /// <summary>
    /// 服务器断开连接
    /// </summary>
    public void DisConnected()
    {

    }

}
