﻿namespace InMemBus.Tests.TestInfrastructure;

public class TestDataAsserter
{
    private readonly List<Guid> ids = [];
    private readonly List<string> values = [];

    public void Add(Guid id) => ids.Add(id);
    public void Add(string value) => values.Add(value);
    public bool Assert(Guid id) => ids.Contains(id);
    public bool Assert(string value) => values.Contains(value);
    public bool AssertMultiple(Guid id, int howMany) => ids.Count(x => x == id) == howMany;
    public void Remove(Guid id) => ids.Remove(id);

    public bool Poll(Func<TestDataAsserter, bool> predicate, int maxSeconds = 10)
    {
        var maxAttempts = maxSeconds * 10;
        const int interval = 100;
        var attempts = 0;

        while (attempts < maxAttempts)
        {
            var ok = predicate.Invoke(this);

            if (ok)
            {
                return true;
            }

            attempts++;

            Thread.Sleep(interval);
        }

        return false;
    }
}