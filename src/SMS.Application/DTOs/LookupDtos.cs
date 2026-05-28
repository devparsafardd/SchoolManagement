namespace SMS.Application.DTOs;

public class LookupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

public class CityLookupDto : LookupDto
{
    public int ProvinceId { get; set; }
    public string? ProvinceName { get; set; }
}

public class GradeLookupDto : LookupDto
{
    public int EducationLevelId { get; set; }
    public bool IsDescriptive { get; set; }
}
