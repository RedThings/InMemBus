namespace InMemBus.TestInfrastructure;

public static class Delayer
{
    public static async Task DelayAsync(int? milliseconds = null)
    {
        var ms = milliseconds ?? Faker.RandomNumber.Next(50, 100);

        await Task.Delay(ms);
    }
}