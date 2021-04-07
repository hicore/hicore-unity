# Hicore Unity

This client is built for Unity3D Engine. You can download Hicore.unitypackage from the [release page](https://github.com/hicore/hicore-unity/releases) and import it to your project and start.
For more information check [Unity Doc](https://hicore.dev/unity/)
## Setup

After you add Unity package to your game you are able to create a new C# file and follow these steps.

### Step 1: Connect To Mothership Server

Create a socket and client object to connect the mothership server. Insert the hostname and server port you want to connect.

```csharp
using System.Collections.Generic;
using Hicore;
using Hicore.Authentications;
using Hicore.Storage;

public class Game : MonoBehaviour
{
    private HicoreSocket socket = new HicoreSocket("http://127.0.0.1", 7192)
    {
        Parameters = new Dictionary<string, string>
        {
            {"socketKey", "defaultKey"}
        }
    };

    private Client client;
}
```
Connect to server

```csharp
    private void Start()
    {
        socket.ConnectAsync();
        client = new Client(socket);
    }
```

### Step 2: Connect To Child Server

For connecting to child server add this line to Start function

```csharp
  client.connectChild("http://127.0.0.1", 7100);
```

## Socket Messages

If you want to see a socket messages such as server status, you can follow this structure

```csharp
    socket.OnConnected += () => { Debug.Log("Connected"); };
    socket.OnClosed += () => { Debug.Log("Disconnected"); };
    socket.OnPing += args => { Debug.Log("Ping " + args.GetPingInMilliseconds()); };

```

## Create User

With a client object, you can create a new user or login the existing user. Authenticate offers more options to create or login, For more information check [Authentication](https://hicore.dev/authentication/) part. For example, we've created a new user by deviceId

```csharp
    var deviceId = SystemInfo.deviceUniqueIdentifier;
    client.AuthenticateDeviceId(deviceId, (user, result) =>{
        Debug.Log($"NEW USER BY DEVICEID-> ({result.Type})  ({result.Message}) ({result.Code}) ");
    });
```

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.
