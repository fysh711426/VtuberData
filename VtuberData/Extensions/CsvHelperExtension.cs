using CsvHelper;

namespace VtuberData.Extensions
{
    internal static class CsvHelperExtension
    {
        internal static Task<IEnumerable<T>> GetAllRecordsAsync<T>(this CsvReader reader)
        {
            var tcs = new TaskCompletionSource<IEnumerable<T>>();
            Task.Run(() =>
            {
                try
                {
                    tcs.SetResult(reader.GetRecords<T>());
                }
                catch(Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }
    }
}
