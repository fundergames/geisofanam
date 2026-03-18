using System;

public class DelayedOperationEvent<T> : IGameEvent
{
    public Action<T> Operation { get; set; }
    public T Data { get; set; }
    public float DelayInSeconds { get; set; }
    public float StartTime { get; set; }
}