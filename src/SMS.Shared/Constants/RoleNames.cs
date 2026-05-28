namespace SMS.Shared.Constants;

public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string SchoolAdmin = "SchoolAdmin";
    public const string Principal = "Principal";
    public const string VicePrincipal = "VicePrincipal";
    public const string Teacher = "Teacher";
    public const string Counselor = "Counselor";
    public const string Accountant = "Accountant";
    public const string Student = "Student";
    public const string Parent = "Parent";

    public const string AdminGroup = SuperAdmin + "," + SchoolAdmin + "," + Principal;
    public const string ManagerGroup = AdminGroup + "," + VicePrincipal;
    public const string EducatorGroup = ManagerGroup + "," + Teacher + "," + Counselor;
}

public static class PositionNames
{
    public const string Teacher = "Teacher";
    public const string Principal = "Principal";
    public const string VicePrincipal = "VicePrincipal";
    public const string Counselor = "Counselor";
    public const string Admin = "Admin";
}
