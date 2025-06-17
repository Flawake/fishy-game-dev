using System;
using System.Collections.Generic;

public class PriorityQueue<T>
{
    private readonly List<(T item, float priority)> heap = new();
    private readonly IComparer<float> comparer;

    public int Count => heap.Count;

    public PriorityQueue(IComparer<float> customComparer = null)
    {
        comparer = customComparer ?? Comparer<float>.Default;
    }

    public void Enqueue(T item, float priority)
    {
        heap.Add((item, priority));
        SiftUp(heap.Count - 1);
    }

    public T Dequeue()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("The queue is empty");

        T rootItem = heap[0].item;
        var last = heap[heap.Count - 1];
        heap.RemoveAt(heap.Count - 1);

        if (heap.Count > 0)
        {
            heap[0] = last;
            SiftDown(0);
        }

        return rootItem;
    }

    public T Peek()
    {
        if (heap.Count == 0)
            throw new InvalidOperationException("The queue is empty");
        return heap[0].item;
    }

    private void SiftUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (comparer.Compare(heap[index].priority, heap[parent].priority) >= 0)
                break;

            (heap[index], heap[parent]) = (heap[parent], heap[index]);
            index = parent;
        }
    }

    private void SiftDown(int index)
    {
        int last = heap.Count - 1;
        while (true)
        {
            int left = 2 * index + 1;
            int right = 2 * index + 2;
            int smallest = index;

            if (left <= last && comparer.Compare(heap[left].priority, heap[smallest].priority) < 0)
                smallest = left;
            if (right <= last && comparer.Compare(heap[right].priority, heap[smallest].priority) < 0)
                smallest = right;

            if (smallest == index)
                break;

            (heap[index], heap[smallest]) = (heap[smallest], heap[index]);
            index = smallest;
        }
    }
}
