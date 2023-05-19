using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sort the dictionary descending rather than ascending
/// </summary>
/// <src>https://stackoverflow.com/a/931939/5232304</src>
class DescendingComparer<T> : IComparer<T> where T : IComparable<T>
{
    public int Compare(T x, T y)
    {
        return y.CompareTo(x);
    }
}

/// <summary>
/// A priority queue implementation
/// </summary>
/// <src>https://stackoverflow.com/a/4994931/5232304</src>
public class PriorityQueue<T> where T : IComparable<T>
{
    int total_size;
    SortedDictionary<T, Queue> storage;

    public PriorityQueue()
    {
        storage = new SortedDictionary<T, Queue>(new DescendingComparer<T>());
        this.total_size = 0;
    }

    public bool IsEmpty()
    {
        return (total_size == 0);
    }

    public Route Dequeue()
    {
        if (IsEmpty())
        {
            throw new Exception("Please check that priorityQueue is not empty before dequeing");
        }
        else
            foreach (Queue q in storage.Values)
            {
                // we use a sorted dictionary
                if (q.Count > 0)
                {
                    total_size--;
                    return (Route)q.Dequeue();
                }
            }

        Debug.Assert(false, "not supposed to reach here. problem with changing total_size");

        return null; // not supposed to reach here.
    }

    public Route Peek()
    {
        if (IsEmpty())
            throw new Exception("Please check that priorityQueue is not empty before peeking");
        else
            foreach (Queue q in storage.Values)
            {
                if (q.Count > 0)
                    return (Route)q.Peek();
            }

        Debug.Assert(false, "not supposed to reach here. problem with changing total_size");

        return null; // not supposed to reach here.
    }

    public Route Dequeue(T prio)
    {
        total_size--;
        return (Route)storage[prio].Dequeue();
    }

    public void Enqueue(Route item, T prio)
    {
        if (!storage.ContainsKey(prio))
        {
            storage.Add(prio, new Queue());
        }
        storage[prio].Enqueue(item);
        total_size++;

    }

    public int Count()
    {
        int i = 0;
        foreach (Queue queue in storage.Values)
            i += queue.Count;
        return i;
    }
}