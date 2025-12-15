using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Vocabulary:
// 1. IP (Internet Protocol Address) 
// = a numerical label assigned to each device connected to a computer network that uses the internet protocol for communication
// = Tells other devices where to send data.
// 2. Port
// = a numerical identifier (16-bit number, ranging from 0 to 65535) associated with an IP address
// = helps distinguish between different network services running on the same computer. 
// = Think of it like a door number within a building (the computer) – it directs traffic to the correct service. 
// 3. Query Port
// = separate port used specifically for server info queries (Ex: Server status, player counts)
// = Helps keep game traffic and server info requests separate
// = Common in multiplayer games for tools like server browsers

// What does this class do?
// This class parses command-line arguments when you launch your Unity app.
// For example, you could launch your app like this: MyGame.exe -ip 192.168.0.5 -port 8888 -queryPort 9999
// This class basically reads and stores those values -> PlayerPrefs (Unity's system for saving simple data)
// Other code can then get the IP, port, query port by calling this: ApplicationData.IP(), APplicationData.Port(), etc.
// This is helpful for dedicated servers or hosting tools like Unity Multiplay, where you often pass IP and port info via command-line arguments


/// <summary>
/// Basic launch command processor (Multiplay prefers passing IP and port along)
/// </summary>
public class ApplicationData // Not Monobehaviour -> don't attach to gameObjects
{
    /// <summary>
    /// Commands Dictionary
    /// Supports flags and single variable args (eg. '-argument', '-variableArg variable')
    /// </summary>
    
    // maps command-line argument names like -ip to the functions that handle them
    Dictionary<string, Action<string>> m_CommandDictionary = new Dictionary<string, Action<string>>();

    // const strings for the name of commands -> gelps to avoid hardcoding "ip" or "port" all over the class
    const string k_IPCmd = "ip";
    const string k_PortCmd = "port";
    const string k_QueryPortCmd = "queryPort";

    // These three methods are for getting the strings/ints of IP, Port, QPort
    // For example, you can call ApplicationData.IP() to get the string of the IP
    public static string IP()
    {
        return PlayerPrefs.GetString(k_IPCmd);
    }

    public static int Port()
    {
        return PlayerPrefs.GetInt(k_PortCmd);
    }

    public static int QPort()
    {
        return PlayerPrefs.GetInt(k_QueryPortCmd);
    }

    //Ensure this gets instantiated Early on
    // This is a constructor that sets default values as shown
    // Also fills the dicts with keys such as -ip, -port, -queryPort, and the values as the SetIp, setPort and SetQueryPort
    public ApplicationData()
    {
        SetIP("127.0.0.1");
        SetPort("7777");
        SetQueryPort("7787");
        m_CommandDictionary["-" + k_IPCmd] = SetIP;
        m_CommandDictionary["-" + k_PortCmd] = SetPort;
        m_CommandDictionary["-" + k_QueryPortCmd] = SetQueryPort;
        ProcessCommandLinearguments(Environment.GetCommandLineArgs()); // to process the arguments passed to the app
    }

    // This basically loops thru all arguments -> Ex: -ip 192.168.0.5 
    // For each arg, check if it is a known command -> Yes: calls matching method
    void ProcessCommandLinearguments(string[] args)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Launch Args: ");
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var nextArg = "";
            if (i + 1 < args.Length) // if we are evaluating the last item in the array, it must be a flag
                nextArg = args[i + 1];

            if (EvaluatedArgs(arg, nextArg))
            {
                sb.Append(arg);
                sb.Append(" : ");
                sb.AppendLine(nextArg);
                i++;
            }
        }

        Debug.Log(sb);
    }



    /// <summary>
    /// Commands and values come in the args array in pairs, so we
    /// </summary>

    // Checks if arg is a known command : Ex: -ip
    // makes sure nextrg is not another command (flags would be handled separately)
    bool EvaluatedArgs(string arg, string nextArg)
    {
        if (!IsCommand(arg))
            return false;
        if (IsCommand(nextArg)) // If you have need for flags, make a separate dict for those.
        {
            return false;
        }

        m_CommandDictionary[arg].Invoke(nextArg); // calls the appropriate method: Ex: -ip 192.168.0.5  -->  calls SetIP("192.168.0.5")
        return true;
    }

    // Stores the IP string into playerprefs
    void SetIP(string ipArgument) 
    {
        PlayerPrefs.SetString(k_IPCmd, ipArgument);
    }

    // tries to parse port as an integer
    // Logs an error if parsing fails
    void SetPort(string portArgument)
    {
        if (int.TryParse(portArgument, out int parsedPort))
        {
            PlayerPrefs.SetInt(k_PortCmd, parsedPort);
        }
        else
        {
            Debug.LogError($"{portArgument} does not contain a parseable port!");
        }
    }

    // Same as SetPort but for the query port.
    void SetQueryPort(string qPortArgument)
    {
        if (int.TryParse(qPortArgument, out int parsedQPort))
        {
            PlayerPrefs.SetInt(k_QueryPortCmd, parsedQPort);
        }
        else
        {
            Debug.LogError($"{qPortArgument} does not contain a parseable query port!");
        }
    }

    // Helper to Check if an Argument is a Command
    // Checks: Not empty, exists in the dict, starts with -
    bool IsCommand(string arg)
    {
        return !string.IsNullOrEmpty(arg) && m_CommandDictionary.ContainsKey(arg) && arg.StartsWith("-");
    }
}
