using MaintainingOrdersWeb.Models;
using MaintainingOrdersWeb.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace MaintainingOrdersWeb.Services;

public class DeleteDependencyService
{
    private readonly MyDbContext _context;

    public DeleteDependencyService(MyDbContext context)
    {
        _context = context;
    }

    public async Task<DeleteDependencyInfo> CheckAsync(string entityName, int id)
    {
        return entityName switch
        {
            "Client" => await CheckClientAsync(id),
            "Product" => await CheckProductAsync(id),
            "Order" => await CheckOrderAsync(id),
            "Supplier" => await CheckSupplierAsync(id),
            "User" => await CheckUserAsync(id),
            "Role" => await CheckRoleAsync(id),
            "OrderStatus" => await CheckOrderStatusAsync(id),
            "ShipmentStatus" => await CheckShipmentStatusAsync(id),
            "DeliveryMethod" => await CheckDeliveryMethodAsync(id),
            "Shipment" => await CheckShipmentAsync(id),
            _ => new DeleteDependencyInfo()
        };
    }

    private async Task<DeleteDependencyInfo> CheckClientAsync(int id)
        => await _context.Orders.AnyAsync(o => o.ClientId == id)
            ? Block("Клиента нельзя удалить, потому что он используется в заказах.")
            : Allow();

    private async Task<DeleteDependencyInfo> CheckProductAsync(int id)
        => await _context.OrderItems.AnyAsync(i => i.ProductId == id) || await _context.SupplyItems.AnyAsync(i => i.ProductId == id)
            ? Block("Товар нельзя удалить, потому что он уже используется в заказах или поставках.")
            : Allow();

    private async Task<DeleteDependencyInfo> CheckOrderAsync(int id)
        => await _context.OrderItems.AnyAsync(i => i.OrderId == id)
            ? Block("Заказ нельзя удалить, потому что у него есть связанные позиции состава заказа.")
            : Allow();

    private async Task<DeleteDependencyInfo> CheckSupplierAsync(int id)
        => await _context.Products.AnyAsync(p => p.SuppliersId == id) || await _context.Shipments.AnyAsync(s => s.SuppliersId == id)
            ? Block("Поставщика нельзя удалить, потому что с ним связаны товары или поставки.")
            : Allow();

    private async Task<DeleteDependencyInfo> CheckUserAsync(int id)
        => await _context.Orders.AnyAsync(o => o.UserId == id) || await _context.Shipments.AnyAsync(s => s.UserId == id)
            ? Block("Пользователя нельзя удалить, потому что за ним закреплены заказы или поставки.")
            : Allow();

    private async Task<DeleteDependencyInfo> CheckRoleAsync(int id)
        => await _context.Users.AnyAsync(u => u.RoleId == id)
            ? Block("Роль нельзя удалить, потому что она назначена пользователям.")
            : Allow();

    private async Task<DeleteDependencyInfo> CheckOrderStatusAsync(int id)
        => await _context.Orders.AnyAsync(o => o.StatusId == id)
            ? Block("Статус заказа нельзя удалить, потому что он используется в заказах.")
            : Allow();

    private async Task<DeleteDependencyInfo> CheckShipmentStatusAsync(int id)
        => await _context.Shipments.AnyAsync(s => s.StatusshId == id)
            ? Block("Статус поставки нельзя удалить, потому что он используется в поставках.")
            : Allow();

    private async Task<DeleteDependencyInfo> CheckDeliveryMethodAsync(int id)
        => await _context.Orders.AnyAsync(o => o.MethodId == id)
            ? Block("Способ доставки нельзя удалить, потому что он используется в заказах.")
            : Allow();

    private async Task<DeleteDependencyInfo> CheckShipmentAsync(int id)
        => await _context.SupplyItems.AnyAsync(i => i.ShipmentId == id)
            ? Block("Поставку нельзя удалить, потому что у неё есть связанные позиции состава поставки.")
            : Allow();

    private static DeleteDependencyInfo Allow() => new();

    private static DeleteDependencyInfo Block(string message) => new()
    {
        CanDelete = false,
        WarningMessage = message
    };
}
