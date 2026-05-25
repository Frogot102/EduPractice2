using System;

namespace EduPractice2.Models
{
    public class Employee
    {
        public int IdEmployee { get; set; }
        public int IdUser { get; set; }          
        public string FullName { get; set; } = string.Empty; 
        public DateTime BirthDate { get; set; }
        public string? Address { get; set; }
        public string? Education { get; set; }
        public string? Qualification { get; set; }
        public string? OperationsList { get; set; }
        public int RoleId { get; set; }         
        public string? RoleName { get; set; }   


        public int Age
        {
            get
            {
                var today = DateTime.Today;
                var age = today.Year - BirthDate.Year;
                if (BirthDate.Date > today.AddYears(-age)) age--;
                return age;
            }
        }
    }
}