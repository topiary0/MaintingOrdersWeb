namespace MaintainingOrdersWeb.ViewModels;

public class DeleteDependencyInfo
{
    public bool CanDelete { get; set; } = true;

    public string WarningMessage { get; set; } = string.Empty;
}
