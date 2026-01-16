using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Basic launch command processor (Multiplay prefers passing IP and port along)
/// </summary>
public class ApplicationData
{
    /// <summary>
    /// Commands Dictionary
    /// Supports flags and single variable args (eg. '-argument', '-variableArg variable')
    /// 
    /// This class is a helper class for reading and processing command line arguments
    /// which are used to set application data such as IP address and port numbers for the server.
    /// 
    /// The UGS Multiplay server hosting automatically passes the IP and port as command line arguments
    /// </summary>
    Dictionary<string, Action<string>> m_CommandDictionary = new Dictionary<string, Action<string>>();

    const string IPCmd = "ip";
    const string PortCmd = "port";
    const string QueryPortCmd = "queryPort";

    public static string IP()
    {
        return PlayerPrefs.GetString(IPCmd);
    }

    public static int Port()
    {
        return PlayerPrefs.GetInt(PortCmd);
    }

    public static int QPort()
    {
        return PlayerPrefs.GetInt(QueryPortCmd);
    }

    //Ensure this gets instantiated Early on
    public ApplicationData()
    {
        SetIP("127.0.0.1");
        SetPort("7777");
        SetQueryPort("7787");
        
        // Specify supported commands and their handlers
        m_CommandDictionary["-" + IPCmd] = SetIP;
        m_CommandDictionary["-" + PortCmd] = SetPort;
        m_CommandDictionary["-" + QueryPortCmd] = SetQueryPort;

        // Process command line arguments.
        ProcessCommandLinearguments(Environment.GetCommandLineArgs());
    }

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

    bool EvaluatedArgs(string arg, string nextArg)
    {
        /// This method Evaluates Command Line args and executes them.
        /// First we check if the arg is a proper m_CommandDictionary command.
        /// And then we check if the next arg is a not a proper m_CommandDictionary command.
        /// If both of these checks are true, we execute the command 
        /// Example for what this function can evaluete: "-ip 127.0.0.1"
        if (!IsCommand(arg))
            return false;
        if (IsCommand(nextArg))
        {
            return false;
        }
        m_CommandDictionary[arg].Invoke(nextArg);
        return true;
    }

    void SetIP(string ipArgument)
    {
        PlayerPrefs.SetString(IPCmd, ipArgument);
    }

    void SetPort(string portArgument)
    {
        if (int.TryParse(portArgument, out int parsedPort))
        {
            PlayerPrefs.SetInt(PortCmd, parsedPort);
        }
        else
        {
            Debug.LogError($"{portArgument} does not contain a parseable port!");
        }
    }

    void SetQueryPort(string qPortArgument)
    {
        if (int.TryParse(qPortArgument, out int parsedQPort))
        {
            PlayerPrefs.SetInt(QueryPortCmd, parsedQPort);
        }
        else
        {
            Debug.LogError($"{qPortArgument} does not contain a parseable query port!");
        }
    }

    bool IsCommand(string arg)
    {
        /// This method Checks if a Command Line arg is a valid command.
        /// Checking if its empty or null
        /// Checking if its in the m_CommandDictionary
        /// Checking if it starts with a '-'
        return !string.IsNullOrEmpty(arg) && m_CommandDictionary.ContainsKey(arg) && arg.StartsWith("-");
    }
}