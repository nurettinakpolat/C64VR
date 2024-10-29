using System;
using System.Collections.Generic;
using UnityEngine;

public class DoOnMainThread : MonoBehaviour
{

    public readonly Queue<Action> ExecuteOnMainThread = new Queue<Action>();

    public virtual void Update()
    {
        while (ExecuteOnMainThread.Count > 0)
        {
            ExecuteOnMainThread.Dequeue().Invoke();
        }
    }
}