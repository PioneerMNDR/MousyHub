using MudBlazor;

namespace MousyHub.Models.Services
{
    public class AlertServices
    {

        private ISnackbar Snackbar { get; set; }
        public AlertServices(ISnackbar snackbar)
        {
            Snackbar = snackbar;
        }

        public void WarningAlert(string message)
        {
            Snackbar.Add(message, severity: Severity.Warning);
        }

        public void SuccessAlert(string message)
        {
            Snackbar.Add(message, severity: Severity.Success);
        }
        public void InfoAlert(string message)
        {
            Snackbar.Add(message, severity: Severity.Info);
        }
        public void ErrorAlert(string message)
        {
            Snackbar.Add(message, severity: Severity.Error);
        }
        public async Task InfoAlertAsync(string message)
        {
            Snackbar.Add(message, severity: Severity.Info);
            await Task.Delay(100);
        }

    }
}
