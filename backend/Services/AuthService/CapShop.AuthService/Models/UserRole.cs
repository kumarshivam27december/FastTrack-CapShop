namespace CapShop.AuthService.Models
{
    public class UserRole
    {
        // this class represents the many-to-many relationship between User and Role entities in the database and contains foreign keys for both UserId and RoleId as well as navigation properties to access the related User and Role objects when querying the database using Entity Framework Core
        
        public int UserId { get; set; }

        public User User { get; set; } = null!;

        public int RoleId { get; set; }

        public Role Role { get; set; } = null!;

    }
}