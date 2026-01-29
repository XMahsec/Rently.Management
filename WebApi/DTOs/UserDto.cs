namespace Rently.Management.WebApi.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Status { get; set; } = "";
}

public class CreateUserDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Role { get; set; }
    public string? ApprovalStatus { get; set; }
}

public class UpdateUserDto
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? ApprovalStatus { get; set; }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = ""; // "Active", "Non-Active", "Blocked"
}

public class PagedResultDto<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
