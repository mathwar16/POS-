namespace RestaurantBilling.Services
{
    public interface IReportService
    {
        Task GenerateAndSendReportAsync(string type, string category, DateTime start, DateTime end, string recipients);
    }
}
