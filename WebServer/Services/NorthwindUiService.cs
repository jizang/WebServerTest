using Microsoft.EntityFrameworkCore;
using WebServer.Models.NorthwindDB;

namespace WebServer.Services;

public class NorthwindUiService
{
    private readonly NorthwindDBContext _context;

    public NorthwindUiService(NorthwindDBContext context)
    {
        _context = context;
    }

    // 取得客戶顯示名稱 (格式：公司名 (ID))
    public async Task<string> GetCustomerLabelAsync(string? customerId)
    {
        if (string.IsNullOrEmpty(customerId)) return string.Empty;

        var customer = await _context.Customers
            .AsNoTracking()
            .Where(c => c.CustomerID == customerId)
            .Select(c => new { c.CompanyName, c.CustomerID })
            .FirstOrDefaultAsync();

        return customer != null ? $"{customer.CompanyName} ({customer.CustomerID})" : string.Empty;
    }

    // 取得員工顯示名稱 (格式：FirstName LastName)
    public async Task<string> GetEmployeeLabelAsync(int? employeeId)
    {
        if (employeeId == null) return string.Empty;

        var employee = await _context.Employees
            .AsNoTracking()
            .Where(e => e.EmployeeID == employeeId)
            .Select(e => new { e.FirstName, e.LastName })
            .FirstOrDefaultAsync();

        return employee != null ? $"{employee.FirstName} {employee.LastName}" : string.Empty;
    }
}
