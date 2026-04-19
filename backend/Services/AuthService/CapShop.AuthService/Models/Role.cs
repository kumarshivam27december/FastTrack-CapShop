namespace CapShop.AuthService.Models
{
    public class Role
    {
        // this class represents the Role entity in the database and contains properties for the role's Id and Name
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
        // Navigation property for the many-to-many relationship with User through UserRole which allows us to easily access the users assigned to a role when querying the database using Entity Framework Core
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
