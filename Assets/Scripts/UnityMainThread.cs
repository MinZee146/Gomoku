using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThread : Singleton<UnityMainThread>
{
    private Queue<Action> _queue = new Queue<Action>();

    private void Update()
    {
        lock (_queue)
        {
            while (_queue.Count > 0)    
            {
                _queue.Dequeue().Invoke();
            }
        }
    }
    
    public void Enqueue(Action action)
    {
        lock (_queue)
        {
            _queue.Enqueue(action);
        }
    }
}
