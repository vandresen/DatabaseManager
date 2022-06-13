using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class DisplayMessage : IDisplayMessage
    {
        private readonly IJSRuntime js;

        public DisplayMessage(IJSRuntime js)
        {
            this.js = js;
        }

        public async ValueTask DisplayErrorMessage(string message)
        {
            await DoDisplayMessage("Error", message, "error");
        }

        public async ValueTask DisplaySuccessMessage(string message)
        {
            await DoDisplayMessage("Success", message, "success");
        }

        private async ValueTask DoDisplayMessage(string title, string message, string messageType)
        {
            await js.InvokeVoidAsync("Swal.fire", title, message, messageType);
        }
    }
}
