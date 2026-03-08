namespace MaintainingOrdersWeb.Infrastructure;

public static class AppRoles
{
    public const string Director = "Директор";
    public const string SalesManager = "Менеджер";
    public const string Logistician = "Логист";
    public const string Warehouse = "Сотрудник";
    public const string Accountant = "Бухгалтер";

    public const string AllBusinessRoles =
        Director + "," + SalesManager + "," + Logistician + "," + Warehouse + "," + Accountant;

    public const string ManagementRoles = Director + "," + SalesManager + "," + Logistician;
    public const string SalesAndFinanceRoles = Director + "," + SalesManager + "," + Logistician + "," + Accountant;
}
