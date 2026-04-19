using CapShop.AuthService.Data;
using CapShop.AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapShop.AuthService.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        // dependency injection of the AuthDbContext to interact with the database for user and role management operations such as checking if an email exists
        private readonly AuthDbContext _db;

        public AuthRepository(AuthDbContext db)
        {
            _db = db;
        }
        // Check if a user with the given email already exists in the database by querying the Users DbSet and return true if a matching email is found, otherwise return false. 
        public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
        {
            return _db.Users.AnyAsync(x => x.Email == email, ct);
        }
        // Retrieve a Role entity from the database by its name by querying the Roles DbSet and return the matching Role object if found, otherwise return null. This method is used to assign roles to users during registration or role management operations and to check user permissions based on their assigned roles when generating JWT tokens or authorizing access to protected resources in the application.

        public Task<Role?> GetRoleByNameAsync(string roleName, CancellationToken ct = default)
        {
            return _db.Roles.FirstOrDefaultAsync(x => x.Name == roleName, ct);
        }

        // Add a new User entity to the database by adding it to the Users DbSet. This method is used during user registration to create a new user record in the database with the provided user information and assigned roles. The actual saving of changes to the database is done separately by calling SaveChangesAsync to allow for better control over transaction management and error handling when performing multiple related data access operations in a single unit of work.

        public Task AddUserAsync(User user, CancellationToken ct = default)
        {
            _db.Users.Add(user);
            return Task.CompletedTask;
        }
        // Update an existing User entity in the database by marking it as modified in the Users DbSet. This method is used to update user information such as enabling or disabling 2FA methods, changing user roles, or updating other user properties. Similar to AddUserAsync, the actual saving of changes to the database is done separately by calling SaveChangesAsync to allow for better control over transaction management and error handling when performing multiple related data access operations in a single unit of work.

        public Task UpdateUserAsync(User user, CancellationToken ct = default)
        {
            _db.Users.Update(user);
            return Task.CompletedTask;
        }
        // Retrieve an active User entity from the database by its email along with the associated roles by querying the Users DbSet and including the related UserRoles and Roles navigation properties. This method is used during user authentication to verify the user's credentials and retrieve their assigned roles for generating JWT tokens and authorizing access to protected resources in the application. It returns the matching User object if found and active, otherwise returns null.

        public Task<User?> GetActiveUserByEmailWithRolesAsync(string email, CancellationToken ct = default)
        {
            return _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.Email == email && x.IsActive, ct);
        }
        // Retrieve a User entity from the database by its email along with the associated roles by querying the Users DbSet and including the related UserRoles and Roles navigation properties. This method is used in scenarios where we need to retrieve user information regardless of their active status, such as during administrative operations or when checking for existing users during registration. It returns the matching User object if found, otherwise returns null.

        public Task<User?> GetUserByEmailAsync(string email, CancellationToken ct = default)
        {
            return _db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(x => x.Email == email, ct);
        }
        // Save changes to the database asynchronously with support for cancellation tokens to allow for better performance and responsiveness of the application when handling authentication-related data access operations. This method is called after performing one or more data modifications such as adding or updating users to persist those changes to the database in a single unit of work, ensuring data integrity and consistency.

        public Task SaveChangesAsync(CancellationToken ct = default)
        {
            return _db.SaveChangesAsync(ct);
        }
    }
}