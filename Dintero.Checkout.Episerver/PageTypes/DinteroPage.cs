using EPiServer.Core;
using EPiServer.DataAnnotations;

namespace Dintero.Checkout.Episerver.PageTypes
{
    [ContentType(GUID = "6DAA3F90-7A61-4773-9211-63DD5C65762B",
        DisplayName = "Dintero Page",
        Description = "",
        GroupName = "Payment",
        Order = 100)]
    public class DinteroPage : PageData
    {
    }
}