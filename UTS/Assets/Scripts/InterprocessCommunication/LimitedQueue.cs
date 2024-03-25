

using System.Collections.Generic;

public class LimitedQueue<T> : Queue<T> {
// Credits: Espo https://stackoverflow.com/a/1305/7069108
    public int Limit { get; set; }
    public event QueueOverflow<LimitedQueue<T>> onQueueOverflow;

    public LimitedQueue(int limit) : base(limit)
    {
        Limit = limit;
    }

    public new void Enqueue(T item) {
        while (Count >= Limit) {
            Dequeue();
            onQueueOverflow?.Invoke(this);
        }
        base.Enqueue(item);
    }
}
