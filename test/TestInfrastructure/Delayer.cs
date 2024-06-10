namespace InMemBus.Tests.TestInfrastructure;

public static class Delayer
{
    public static async Task DelayAsync(int? milliseconds = null)
    {
        var ms = milliseconds ?? Faker.RandomNumber.Next(500, 1500);

        await Task.Delay(ms);
    }
}