using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SignalManager : MonoBehaviour
{
    public static SignalManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public UnityEvent<string> OnSignalReceived = new UnityEvent<string>();
    public UnityEvent<string> OnEndSignalReceived = new UnityEvent<string>();
    public UnityEvent<List<string>> OnSignalListReceived;
    public UnityEvent<List<string>> OnActiveSignalListReceived;

    public void SendSignal(string signal)
    {
        OnSignalReceived.Invoke(signal);
    }

    public void SendEndSignal(string signal)
    {
        OnEndSignalReceived.Invoke(signal);
    }
    public void SendSignalList(List<string> signals)
    {
        OnSignalListReceived.Invoke(signals);
    }
    public void ActiveSignalListReceived(List<string> signals)
    {
        OnActiveSignalListReceived.Invoke(signals);
    }
}

