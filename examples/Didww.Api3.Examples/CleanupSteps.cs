using System.Runtime.ExceptionServices;

namespace Didww.Api3.Examples;

// Runs the cleanup steps of an example one after another, also when a step
// fails, so that one failed delete does not leave the other resources behind.
// The failures are thrown at the end.
public static class CleanupSteps
{
    public static async Task RunAllAsync(IEnumerable<Func<Task>> steps)
    {
        var failures = new List<System.Exception>();
        foreach (var step in steps)
        {
            try
            {
                await step();
            }
            catch (System.Exception e)
            {
                failures.Add(e);
            }
        }

        if (failures.Count == 1)
            ExceptionDispatchInfo.Throw(failures[0]);
        if (failures.Count > 1)
            throw new AggregateException("Several cleanup steps failed", failures);
    }

    public static Task RunAllAsync(params Func<Task>[] steps) => RunAllAsync((IEnumerable<Func<Task>>)steps);
}
