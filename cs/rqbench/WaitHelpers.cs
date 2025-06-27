using System;
using System.Threading.Tasks;

public static class WaitHelpers
{
    public static async Task CloseOrTimeoutAsync(Task task, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout));
        if (completed != task)
            throw new TimeoutException($"timeout after {timeout}");
        await task; // propagate exceptions
    }
}
