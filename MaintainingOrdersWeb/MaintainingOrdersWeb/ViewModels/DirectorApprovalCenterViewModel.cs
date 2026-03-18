namespace MaintainingOrdersWeb.ViewModels;

public class DirectorApprovalCenterViewModel
{
    public int OrdersAwaitingReview { get; set; }

    public int LowMarginProducts { get; set; }

    public int CriticalStockItems { get; set; }

    public int ShipmentsRequiringAttention { get; set; }
}
