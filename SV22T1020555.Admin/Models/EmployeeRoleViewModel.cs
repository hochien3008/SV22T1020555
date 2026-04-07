using SV22T1020555.Models.HR;

namespace SV22T1020555.Admin.Models
{
    public class EmployeeRoleViewModel
    {
        public Employee Employee { get; set; } = new Employee();
        public List<string> SelectedRoles { get; set; } = new List<string>();
    }
}

